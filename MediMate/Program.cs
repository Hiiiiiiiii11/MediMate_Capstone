
using CloudinaryDotNet;
using DotNetEnv;
using MediMate.Middleware;
using MediMateRepository.Data;
using MediMateRepository.Repositories;
using MediMateService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Share.Cloudinaries;
using Share.Common;
using Share.Jwt;
using System.Security.Principal;
using System.Text;
using System.Text.Json;

namespace MediMate
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            if (builder.Environment.IsDevelopment())
            {
                Env.TraversePath().Load(".env.Local");
            }
            builder.Configuration.AddEnvironmentVariables();

            // --- PHẦN NÀY GIỮ NGUYÊN ---
            var connectionString = builder.Configuration.GetConnectionString("MedimateDbConnection");

            builder.Services.AddDbContext<MediMateDbContext>(options =>
            {
                options.UseNpgsql(connectionString);

                // Fix lỗi ngày giờ UTC của PostgreSQL
                AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            });

            builder.Services.AddHttpContextAccessor();


            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IAuthenticationRepository, AuthenticationRepository>();
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IUploadPhotoService, UploadPhotoService>();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            builder.Services.AddScoped<IFamilyService, FamilyService>();
            builder.Services.AddScoped<IMemberService, MemberService>();
            builder.Services.AddScoped<IHealthService, HealthService>();
            builder.Services.AddScoped<IPrescriptionService, PrescriptionService>();

            // Add services to the container.
            builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Ghi đè hành vi trả về lỗi Validation mặc định
        options.InvalidModelStateResponseFactory = context =>
        {
            // 1. Lấy danh sách lỗi từ ModelState
            var errors = context.ModelState
                .Where(e => e.Value.Errors.Count > 0)
                .SelectMany(x => x.Value.Errors)
                .Select(x => x.ErrorMessage)
                .ToList();

            // 2. Ghép các lỗi thành 1 chuỗi (hoặc xử lý tùy ý)
            var errorMessage = string.Join("; ", errors);

            // 3. Tạo ApiResponse chuẩn
            var response = ApiResponse<object>.Fail(errorMessage, 400);

            // 4. Trả về BadRequest với ApiResponse
            return new BadRequestObjectResult(response);
        };
    });
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Medimate API",
                    Version = "v1",
                    Description = "API for Medimate+"
                });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter JWT."
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });
            builder.Services.Configure<JwtSettings>(
            builder.Configuration.GetSection("JWT")
            );
            var jwtSettings = builder.Configuration.GetSection("JWT").Get<JwtSettings>();
            var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);


            builder.Services.Configure<CloudinarySettings>(
            builder.Configuration.GetSection("CloudinarySettings"));
            builder.Services.AddSingleton(provider =>
            {
                var config = builder.Configuration.GetSection("CloudinarySettings").Get<CloudinarySettings>();
                var account = new Account(config.CloudName, config.ApiKey, config.ApiSecret);
                return new Cloudinary(account);
            });

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(key)
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnChallenge = async context =>
                        {
                            // 1. Ngăn chặn behavior mặc định (tránh việc nó tự ghi header WWW-Authenticate không mong muốn)
                            context.HandleResponse();

                            // 2. Thiết lập Status Code 401
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            context.Response.ContentType = "application/json";

                            // 3. Tạo object ApiResponse chuẩn
                            var response = ApiResponse<object>.Fail("Unauthorized - Token is missing or invalid.", 401);

                            // 4. Serialize sang JSON (dùng CamelCase cho chuẩn api)
                            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            });

                            // 5. Ghi ra Response
                            await context.Response.WriteAsync(json);
                        },
                        OnForbidden = async context =>
                        {
                            // 1. Thiết lập Status Code 403
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            context.Response.ContentType = "application/json";

                            // 2. Tạo object ApiResponse chuẩn
                            var response = ApiResponse<object>.Fail("Forbidden - You do not have permission to access this resource.", 403);

                            // 3. Serialize và ghi ra Response
                            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            });

                            await context.Response.WriteAsync(json);
                        }
                    };
                });
            var app = builder.Build();
            app.UseMiddleware<GlobalExceptionMiddleware>();

          
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            // Tự động chạy migration khi app khởi động
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MediMateDbContext>();
                db.Database.Migrate();
            }

            app.Run();
        }
    }
}

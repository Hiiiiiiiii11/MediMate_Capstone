
using CloudinaryDotNet;
using DotNetEnv;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Hangfire;
using Hangfire.PostgreSql;
using MediMate.Middleware;
using MediMateRepository.Data;
using MediMateRepository.Repositories;
using MediMateRepository.Repositories.Implementations;
using MediMateService.Services;
using MediMateService.Services.Implementations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Share.Cloudinaries;
using Share.Common;
using Share.Jwt;
using System.Text;
using System.Text.Json;


namespace MediMate
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            var builder = WebApplication.CreateBuilder(args);
            if (builder.Environment.IsDevelopment())
            {
                Env.TraversePath().Load(".env.Local");
            }
            builder.Configuration.AddEnvironmentVariables();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("MediMatePolicy", policy =>
                {
                    policy.WithOrigins(
                            "https://medimate.health.vn",
                            "https://demo.medimate.health.vn",
                            "http://localhost:3000",   // React / Next.js
                            "http://localhost:5173",   // Vite
                            "http://localhost:4200"    // Angular
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            // --- PHẦN NÀY GIỮ NGUYÊN ---
            var connectionString = builder.Configuration.GetConnectionString("MedimateDbConnection");

            builder.Services.AddDbContext<MediMateDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
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
            builder.Services.AddTransient<IReminderJobService, ReminderJobService>();
            builder.Services.AddScoped<IMedicationSchedulesService, MedicationSchedulesService>();
            builder.Services.AddScoped<IFirebaseNotificationService, FirebaseNotificationService>();


            builder.Services.AddAutoMapper(typeof(Program));

            builder.Services.AddScoped<IMockDoctorRepository, MockDoctorRepository>();
            builder.Services.AddScoped<IDoctorService, DoctorService>();
            builder.Services.AddScoped<IMockRatingRepository, MockRatingRepository>();
            builder.Services.AddScoped<IRatingService, RatingService>();
            builder.Services.AddScoped<IMockAppointmentRepository, MockAppointmentRepository>();
            builder.Services.AddScoped<IAppointmentService, AppointmentService>();
            builder.Services.AddScoped<IConsultationService, ConsultationService>();
            builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
            builder.Services.AddScoped<INotificationSettingService, NotificationSettingService>();

            builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(builder.Configuration.GetConnectionString("MedimateDbConnection"))
);


            builder.Services.AddHangfireServer();


            var projectId = Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID");
            var clientEmail = Environment.GetEnvironmentVariable("FIREBASE_CLIENT_EMAIL");

            // Fix lỗi format Private Key (đổi chuỗi "\n" dạng text thành ký tự xuống dòng thật)
            var privateKey = Environment.GetEnvironmentVariable("FIREBASE_PRIVATE_KEY")?.Replace("\\n", "\n");

            if (!string.IsNullOrEmpty(projectId) && !string.IsNullOrEmpty(clientEmail) && !string.IsNullOrEmpty(privateKey))
            {
                // Cấu trúc lại file JSON trực tiếp trên RAM
                string firebaseJsonConfig = $$"""
    {
      "type": "service_account",
      "project_id": "{{projectId}}",
      "private_key": "{{privateKey}}",
      "client_email": "{{clientEmail}}"
    }
    """;

                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromJson(firebaseJsonConfig)
                });

                Console.WriteLine("Firebase initialized successfully from .env");
            }
            else
            {
                Console.WriteLine("Warning: Firebase configuration is missing in .env file.");
            }

            builder.Services.AddHttpClient(); 
            builder.Services.AddScoped<IOcrService, OcrService>();

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
            app.UseHangfireDashboard("/hangfire");


            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();
            app.UseCors("MediMatePolicy");
            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MediMateDbContext>();
                db.Database.Migrate();
                var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                var adminMail = config["SEED_ADMIN_MAIL"];
                var adminPassword = config["SEED_ADMIN_PASSWORD"];
                var adminFullName = config["SEED_ADMIN_FULLNAME"] ?? "Administrator";

                if (!string.IsNullOrEmpty(adminMail) && !string.IsNullOrEmpty(adminPassword))
                {
                    var exists = db.Users.Any(u => u.Email == adminMail);
                    if (!exists)
                    {
                        db.Users.Add(new MediMateRepository.Model.User
                        {
                            UserId = Guid.NewGuid(),
                            PhoneNumber = "0000000000",   // placeholder, required field
                            Email = adminMail,
                            FullName = adminFullName,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                            Role = "Admin",
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        });
                        db.SaveChanges();
                    }
                }
            }

            app.Run();
        }
    }
}

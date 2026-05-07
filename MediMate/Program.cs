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
using MediMateService.Hubs;
using MediMateService.Services;
using MediMateService.Services.Implementations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Share.Cloudinaries;
using Share.Common;
using Share.Jwt;
using System.Reflection;
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
                            "http://localhost:3000",
                            "http://localhost:5173",
                            "http://localhost:4200",
                            "http://localhost:8081",
                            "http://10.0.2.2:8081",
                            "exp://localhost:8081",
                            "exp://127.0.0.1:8081"
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
            builder.Services.AddScoped<IChatDoctorService, ChatDoctorService>();


            builder.Services.AddAutoMapper(typeof(Program));

            builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
            builder.Services.AddScoped<IDoctorService, DoctorService>();
            builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
            builder.Services.AddScoped<IAppointmentService, AppointmentService>();
            builder.Services.AddScoped<IConsultationService, ConsultationService>();
            builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
            builder.Services.AddScoped<INotificationSettingService, NotificationSettingService>();
            builder.Services.AddScoped<IMembershipPackageService, MembershipPackageService>();
            builder.Services.AddScoped<IDoctorBankAccountService, DoctorBankAccountService>();
            builder.Services.AddScoped<IDoctorDocumentService, DoctorDocumentService>();
            builder.Services.AddScoped<IDoctorAvailabilityExceptionService, DoctorAvailabilityExceptionService>();
            builder.Services.AddScoped<ICloudinaryUploadService, CloudinaryUploadService>();
            builder.Services.AddScoped<IPayOSService, PayOSService>();
            builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
            builder.Services.AddScoped<ITransactionService, TransactionService>();
            builder.Services.AddScoped<IDoctorAvailabilityService, DoctorAvailabilityService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IAgoraService, AgoraService>();
            builder.Services.AddScoped<IMedicationLogService, MedicationLogService>();
            builder.Services.AddScoped<IPrescriptionByDoctorService, PrescriptionByDoctorService>();
            builder.Services.AddScoped<IDrugDataService, DrugDataService>();
            builder.Services.AddScoped<IDrugInteractionService, DrugInteractionService>();
            builder.Services.AddScoped<IDrugInteractionAIService, DrugInteractionAIService>();
            builder.Services.AddScoped<IMedicationStatusJobService, MedicationStatusJobService>();
            builder.Services.AddScoped<IVersionService, VersionService>();
            builder.Services.AddScoped<IClinicService, ClinicService>();
            builder.Services.AddScoped<IPayoutService, PayoutService>();
            builder.Services.AddScoped<IAgoraRecordingService, AgoraRecordingService>();
            builder.Services.AddScoped<IUserBankAccountService, UserBankAccountService>();
            builder.Services.AddMemoryCache();
            builder.Services.AddSignalR();

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
            builder.Services.AddScoped<IEmailService, EmailService>();

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

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
                }
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
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        RoleClaimType = "Role"
                    };


                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            // 1. Nếu có Cookie thì dùng Cookie (cho Web truyền thống)
                            if (context.Request.Cookies.TryGetValue("token", out var token))
                            {
                                context.Token = token;
                            }
                            
                            // 2. Đọc token từ query string "access_token" do SignalR Client gửi lên
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub"))
                            {
                                context.Token = accessToken;
                            }
                            
                            return Task.CompletedTask;
                        },

                        // 2. CHECK TOKEN BLACKLIST SAU KHI PARSE THÀNH CÔNG (THÊM MỚI ĐOẠN NÀY)
                        OnTokenValidated = context =>
                        {
                            var cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();

                            string rawToken = string.Empty;

                            // Lấy từ Header trước
                            var authHeader = context.HttpContext.Request.Headers["Authorization"].ToString();
                            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                            {
                                rawToken = authHeader.Substring("Bearer ".Length).Trim();
                            }
                            // Nếu không có ở Header, lấy từ trường hợp Cookie ở trên truyền xuống
                            else if (!string.IsNullOrEmpty(context.SecurityToken?.UnsafeToString()))
                            {
                                // Dành cho .NET 8 (JsonWebToken)
                                rawToken = context.SecurityToken.UnsafeToString();
                            }

                            // KIỂM TRA BLACKLIST
                            if (!string.IsNullOrEmpty(rawToken) && cache.TryGetValue($"blacklist_{rawToken}", out _))
                            {
                                // Token nằm trong danh sách đen -> Đánh fail luôn! Nó sẽ tự nhảy xuống hàm OnChallenge bên dưới
                                context.Fail("Token đã bị thu hồi (Logged out).");
                            }

                            return Task.CompletedTask;
                        },

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
            app.UseForwardedHeaders();


            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();
            app.UseCors("MediMatePolicy");
            app.UseAuthentication();
            app.UseMiddleware<SessionMiddleware>();
            app.UseAuthorization();

            app.MapHub<MediMateHub>("/hub/medimate");
            app.MapControllers();

            // ─── Hangfire Recurring Jobs ───
            RecurringJob.AddOrUpdate<IMedicationStatusJobService>(
                "check-expired-medication-status",
                job => job.CheckAndUpdateExpiredStatusAsync(),
                Cron.Daily(0, 0) // Chạy lúc 00:00 (nửa đêm) mỗi ngày
            );
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


using DotNetEnv;
using MediMateRepository.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

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

            // Add services to the container.
            builder.Services.AddControllers();
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

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}

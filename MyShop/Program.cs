using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MyShop.DataContext;
using MyShop.Services.Flowers;
using MyShop.Services.Users;
using MyShop.Filters;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Amazon.S3;
using MyShop.Services.ApplicationDbContext;
using Amazon.Runtime;
using MyShop.Services.Reports;

namespace MyShop
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Read AWS credentials from appsettings.json
            var accessKey = builder.Configuration["AWS:AccessKey"];
            var secretKey = builder.Configuration["AWS:SecretKey"];
            var region = builder.Configuration["AWS:Region"];

           // Check if any of the required AWS settings are missing
           if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(region))
           {
               throw new Exception("AWS credentials or region are missing in appsettings.json");
           }

            // Create AWS credentials and S3 client
            var awsCredentials = new BasicAWSCredentials(accessKey, secretKey);
            var s3Client = new AmazonS3Client(awsCredentials, Amazon.RegionEndpoint.GetBySystemName(region));

            // Register the S3 client in the DI container
            builder.Services.AddSingleton<IAmazonS3>(s3Client);
            builder.Services.AddSingleton<S3StorageService>();
          


            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
            });

            // Đăng ký ApplicationDbContext trong dependency injection container
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Đăng ký FlowershopContext
            builder.Services.AddDbContext<FlowershopContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Đăng ký các service
            builder.Services.AddScoped<ISearchService, SearchService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IFlowerService, FlowerService>();
            builder.Services.AddScoped<ICategoryService, CategoryService>();
            builder.Services.AddScoped<IReportService, ReportService>();

            // Cấu hình Google Authentication và Cookie Authentication
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.IsEssential = true;
            })
            .AddGoogle(options =>
            {
                options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
                options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
                options.CallbackPath = "/api/LoginGoogle/callback";
            });
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Cấu hình JWT Authentication
            var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]);
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                };
            });

            // Add Authorization
            builder.Services.AddAuthorization();

            // Cấu hình CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            // Cấu hình Swagger
            builder.Services.AddSwaggerGen(c =>
            {
                c.OperationFilter<RemoveUnusedFieldsOperationFilter>();
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter a valid token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "Bearer"
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
                        new string[]{ }
                    }
                });
            });

            var app = builder.Build();

            // Cấu hình môi trường phát triển
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Cấu hình middleware
            app.UseHttpsRedirection();
            app.UseStaticFiles(); // Cho phép phục vụ các file tĩnh như hình ảnh từ wwwroot

            app.UseCors("AllowAllOrigins");

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSession();


            app.MapControllers();

            app.Run();
        }
    }
}



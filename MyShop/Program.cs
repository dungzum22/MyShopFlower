using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MyShop.DataContext;
using MyShop.Services.Flowers;
using MyShop.Services.Users;
using MyShop.Filters;
using MyShop.Services;
using System.Text;
using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;

namespace MyShop
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Đọc thông tin AWS từ appsettings.json
            var accessKey = builder.Configuration["AWS:AccessKey"];
            var secretKey = builder.Configuration["AWS:SecretKey"];
            var region = builder.Configuration["AWS:Region"];

            if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(region))
            {
                throw new Exception("AWS credentials or region are missing in appsettings.json");
            }

            var awsCredentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);
            var s3Client = new AmazonS3Client(awsCredentials, Amazon.RegionEndpoint.GetBySystemName(region));

            // Đăng ký IAmazonS3
            builder.Services.AddSingleton<IAmazonS3>(s3Client);

            //Đăng ký S3StorageService
            builder.Services.AddSingleton<S3StorageService>();

            // Add services to the container.
            builder.Services.AddControllers()
            .AddJsonOptions(options =>
                 {
                     options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
                     options.JsonSerializerOptions.WriteIndented = true;
                 });
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            // Đăng ký các service
            builder.Services.AddScoped<ISearchService, SearchService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IFlowerService, FlowerService>();
            builder.Services.AddScoped<ICategoryService, CategoryService>();

            //Đăng ký DbContext để kết nối với cơ sở dữ liệu
            builder.Services.AddDbContext<FlowershopContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            //Đăng ký UserService vào Dependency Injection(DI)
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IFlowerService, FlowerService>();
            //Đăng ký IHttpClientFactory để sử dụng HttpClient
            builder.Services.AddHttpClient();

            var configuration = builder.Configuration;




            // Sử dụng MockGHNService cho IGHNService để thử nghiệm
            builder.Services.AddScoped<IGHNService, MockGHNService>();

            // Đăng ký VNPayService vào Dependency Injection (DI)
            //builder.Services.AddScoped<IVNPayService, VNPayService>();





            // Configure JWT Authentication
            var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]);
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

            builder.Services.AddSwaggerGen(c =>
            {
                // Các cấu hình Swagger khác

                // Thêm bộ lọc để loại bỏ các trường không mong muốn
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

            // Add CORS setup to get API
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins", policy =>
                {
                    policy.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
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
            // Cho phép phục vụ các file tĩnh (như hình ảnh) từ thư mục wwwroot
            app.UseStaticFiles();  // Thêm dòng này để cho phép phục vụ file tĩnh

            // Use CORS
            app.UseCors("AllowAllOrigins");

            // Kích hoạt Authentication và Authorization middleware
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}

//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.IdentityModel.Tokens;
//using Microsoft.OpenApi.Models;
//using MyShop.DataContext;
//using MyShop.Services.Flowers;
//using MyShop.Services.Users;
//using MyShop.Filters;
//using System.Text;
//using Microsoft.AspNetCore.Authentication.Cookies;
//using Microsoft.AspNetCore.Authentication.Google;
//using MyShop.Services.ApplicationDbContext;

//namespace MyShop
//{
//    public class Program
//    {
//        public static void Main(string[] args)
//        {
//            var builder = WebApplication.CreateBuilder(args);

//            // Add services to the container.
//            builder.Services.AddControllers();
//            builder.Services.AddEndpointsApiExplorer();
//            builder.Services.AddSwaggerGen();
//            builder.Services.AddScoped<SearchService>();
//            // Đăng ký ApplicationDbContext trong dependency injection container
//            builder.Services.AddDbContext<ApplicationDbContext>(options =>
//                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//            builder.Services.AddControllers();
//            builder.Services.AddAuthentication(options =>
//            {
//                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
//                options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
//            })
//            .AddGoogle(options =>
//            {
//                options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
//                options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
//            })
//            .AddCookie();
//            builder.Services.AddAuthorization();

//            // Cấu hình CORS cho phép bất kỳ nguồn gốc nào (hoặc cụ thể)
//            builder.Services.AddCors(options =>
//            {
//                options.AddPolicy("AllowAll", builder =>
//                {
//                    builder.AllowAnyOrigin()
//                           .AllowAnyMethod()
//                           .AllowAnyHeader();
//                });
//            });

//            builder.Services.AddControllers();
//            builder.Services.AddDbContext<ApplicationDbContext>(options =>
//                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//            var app = builder.Build();

//            // Sử dụng CORS trong ứng dụng
//            app.UseCors("AllowAll");

//            //Đăng ký DbContext để kết nối với cơ sở dữ liệu
//            builder.Services.AddDbContext<FlowershopContext>(options =>
//            {
//                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
//            });

//            // Đăng ký UserService vào Dependency Injection (DI)
//            builder.Services.AddScoped<IUserService, UserService>();

//            builder.Services.AddScoped<FlowerService>();
//            builder.Services.AddScoped<CategoryService>();

//            // Cấu hình JWT Authentication
//            var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]);
//            builder.Services.AddAuthentication(options =>
//            {
//                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//            })
//            .AddJwtBearer(options =>
//            {
//                options.RequireHttpsMetadata = false;
//                options.SaveToken = true;
//                options.TokenValidationParameters = new TokenValidationParameters
//                {
//                    ValidateIssuerSigningKey = true,
//                    IssuerSigningKey = new SymmetricSecurityKey(key),
//                    ValidateIssuer = false,
//                    ValidateAudience = false,
//                };
//            });



//            builder.Services.AddSwaggerGen(c =>
//            {
//                // Các cấu hình Swagger khác

//                // Thêm bộ lọc để loại bỏ các trường không mong muốn
//                c.OperationFilter<RemoveUnusedFieldsOperationFilter>();
//                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//                {
//                    In = ParameterLocation.Header,
//                    Description = "Please enter a valid token",
//                    Name = "Authorization",
//                    Type = SecuritySchemeType.Http,
//                    BearerFormat = "JWT",
//                    Scheme = "Bearer"
//                });
//                c.AddSecurityRequirement(new OpenApiSecurityRequirement
//                {
//                    {
//                        new OpenApiSecurityScheme
//                        {
//                            Reference = new OpenApiReference
//                            {
//                                Type = ReferenceType.SecurityScheme,
//                                Id = "Bearer"
//                            }
//                        },
//                        new string[]{ }
//                    }
//                });
//            });


//            //Add CORS setup to get API
//            builder.Services.AddCors(options =>
//            {
//                options.AddPolicy("AllowAllOrigins", policy =>
//                {
//                    policy.AllowAnyOrigin()
//                    .AllowAnyHeader()
//                    .AllowAnyMethod();
//                });
//            });



//            // Configure the HTTP request pipeline.
//            if (app.Environment.IsDevelopment())
//            {
//                app.UseSwagger();
//                app.UseSwaggerUI();
//            }

//            app.UseHttpsRedirection();
//            // Cho phép phục vụ các file tĩnh (như hình ảnh) từ thư mục wwwroot
//            app.UseStaticFiles();  // Thêm dòng này để cho phép phục vụ file tĩnh

//            //user CORS
//            app.UseCors("AllowAllOrigins");

//            // Kích hoạt Authentication và Authorization middleware
//            app.UseAuthentication();
//            app.UseAuthorization();

//            app.MapControllers();

//            app.Run();


//        }
//    }
//}

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
using MyShop.Services.ApplicationDbContext;

namespace MyShop
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

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

            // Cấu hình Google Authentication và Cookie Authentication
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
            })
            .AddGoogle(options =>
            {
                options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
                options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
            })
            .AddCookie();

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

            app.MapControllers();

            app.Run();
        }
    }
}


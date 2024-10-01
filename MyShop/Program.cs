using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyShop.DataContext;
using MyShop.Filters;
using MyShop.Services;
using System.Text;

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
            builder.Services.AddSwaggerGen();

            //Đăng ký DbContext để kết nối với cơ sở dữ liệu
            builder.Services.AddDbContext<FlowershopContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            // Đăng ký UserService vào Dependency Injection (DI)
            builder.Services.AddScoped<IUserService, UserService>();

            // Cấu hình JWT Authentication
            var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]);
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });

            builder.Services.AddSwaggerGen(c =>
            {
                // Các cấu hình Swagger khác

                // Thêm bộ lọc để loại bỏ các trường không mong muốn
                c.OperationFilter<RemoveUnusedFieldsOperationFilter>();
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

            // Kích hoạt Authentication và Authorization middleware
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();


        }
    }
}

using Microsoft.EntityFrameworkCore;
using PharmacyAPI.Data;
using PharmacyAPI.Repositories;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using PharmacyAPI.DTOs;
using PharmacyAPI.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Add services to the container

// 1.1 Configure database context
builder.Services.AddDbContext<PharmacyCaseContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"))
);

// 1.2 Register AutoMapper and mapping profile
builder.Services.AddAutoMapper(typeof(MappingProfile));

// 1.3 Register repositories
builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>();
builder.Services.AddScoped<IDoctorUserRepository, DoctorUserRepository>();
builder.Services.AddScoped<IDrugRepository, DrugRepository>();
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<ISalesReportRepository, SalesReportRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// 1.4 Register Validators
builder.Services.AddScoped<IValidator<SupplierDTO>, SupplierValidator>();
builder.Services.AddScoped<IValidator<SalesReportDTO>, SalesReportValidator>();
builder.Services.AddScoped<IValidator<OrderDTO>, OrderValidator>();
builder.Services.AddScoped<IValidator<AdminUserDTO>, AdminUserDTOValidator>();
builder.Services.AddScoped<IValidator<DoctorUserDTO>, DoctorUserDTOValidator>();
builder.Services.AddScoped<IValidator<DrugDTO>, DrugValidator>();

// 1.5 Register other services (e.g., IUserService)
builder.Services.AddScoped<IUserService, UserService>();

// 1.6 Add controllers
builder.Services.AddControllers();

// 1.7 Configure Swagger for API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Define security scheme for Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        In = ParameterLocation.Header,
        Description = "Please enter a valid JWT token in the text field"
    });

    // Add security requirement for Swagger to use Bearer token
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] {}
        }
    });
});

// 1.8 Configure authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("admin"));
});

// 1.9 Configure JWT Authentication
var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:SecretKey"]);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],   // Use configuration for flexibility
            ValidAudience = builder.Configuration["Jwt:Audience"], // Use configuration for flexibility
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

// 2. Build the app
var app = builder.Build();

// 3. Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 3.1 Use authentication and authorization middlewares
app.UseAuthentication();
app.UseAuthorization();

// 3.2 Map controllers
app.MapControllers();

// 4. Run the app
app.Run();

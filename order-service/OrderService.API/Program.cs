using Confluent.Kafka;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OrderService.API.Consumers;
using OrderService.API.DependencyInjection;
using OrderService.API.Helpers;
using OrderService.API.Hubs;
using OrderService.API.Middleware;
using OrderService.API.Models;
using OrderService.API.Serialization;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Data;
using Serilog;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.StackExchangeRedis;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

//Add services to the container.
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000);
    options.ListenAnyIP(50);

});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Order Service API", Version = "v1" });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
    // Add JWT Authentication
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
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
            new string[] {}
        }
    });

});
builder.Services.AddScoped<JwtTokenHelper>();
builder.Services.AddApplication();  // Extension method to register MediatR, FluentValidation
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<DriverLocationConsumer>();
builder.Services.AddSingleton<IProducer<string, OrderAssignmentFailedEvent>>(sp => 
{
    var config = new ProducerConfig {
        BootstrapServers = builder.Configuration["Kafka:BootstrapServers"]
    };
    return new ProducerBuilder<string, OrderAssignmentFailedEvent>(config)
        .SetValueSerializer(new JsonSerializer<OrderAssignmentFailedEvent>())
        .Build();
});
// Add SignalR

builder.Services.AddSignalR()
    .AddStackExchangeRedis(builder.Configuration["Redis:ConnectionString"] + ",abortConnect=false", options =>
    {
        options.Configuration.ChannelPrefix = "TrackingHub";
    });

// Configure WebSocket options
builder.Services.Configure<HubOptions>(options => {
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
});
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", policy => {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();

    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowCustomerApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add Identity
builder.Services.AddIdentity<Customer, IdentityRole>()
    .AddEntityFrameworkStores<OrderDbContext>()
    .AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]))
    };
});

builder.Host.UseSerilog((context, config) =>
{
    config.WriteTo.Console()
          .ReadFrom.Configuration(context.Configuration);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    //app.UseHttpsRedirection();

}

app.UseMiddleware<ExceptionHandlingMiddleware>();
//app.UseCors("AllowAll");
app.UseCors("AllowCustomerApp");

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<TrackingHub>("/tracking");

app.MapControllers(); 


app.Run();



using Confluent.Kafka;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OrderService.API.Consumers;
using OrderService.API.DependencyInjection;
using OrderService.API.Helpers;
using OrderService.API.Middleware;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Data;
using Serilog;
using System.Reflection;
using System.Text;
using OrderService.Infrastructure.Services;
using StackExchange.Redis;
using OrderService.Infrastructure.Producers;
using OrderService.Infrastructure.Serialization;
using OrderService.API.HealthCheck;
using OrderService.Infrastructure.Hubs;
using OrderService.Domain.Models;
using Microsoft.AspNetCore.Authentication;
using OrderService.Infrastructure.Auth;
using Prometheus;
using AutoMapper;
using OrderService.Application.Mappings;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

//Add services to the container.

if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(5000);
        options.ListenAnyIP(50); 
    });
}

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
builder.Services.AddScoped<ServiceAuthenticationHandler>();
builder.Services.AddSingleton<IOrderCreatedProducer, OrderCreatedProducer>();
//builder.Services.AddHostedService<OrderAssignmentConsumer>();
//builder.Services.AddHostedService<DriverLocationConsumer>();
builder.Services.AddHostedService<KafkaConsumerService>();
builder.Services.AddAutoMapper(typeof(OrderMappingProfile).Assembly);
builder.Services.AddSingleton<IProducer<string, DriverAssignedEvent>>(sp =>
{
    var config = new ProducerConfig
    {
        BootstrapServers = builder.Configuration["Kafka:BootstrapServers"],
        EnableIdempotence = true,
        MessageSendMaxRetries = 3,
        Acks = Acks.All
    };

    return new ProducerBuilder<string, DriverAssignedEvent>(config)
        .SetValueSerializer(new JsonSerializer<DriverAssignedEvent>())
        .Build();
});
builder.Services.AddSingleton<IProducer<string, OrderAssignmentFailedEvent>>(sp => 
{
    var config = new ProducerConfig {
        BootstrapServers = builder.Configuration["Kafka:BootstrapServers"]
    };
    return new ProducerBuilder<string, OrderAssignmentFailedEvent>(config)
        .SetValueSerializer(new JsonSerializer<OrderAssignmentFailedEvent>())
        .Build();
});

builder.Services.AddSingleton<IProducer<string, DriverLocationEvent>>(sp =>
{
    var config = new ProducerConfig
    {
        BootstrapServers = builder.Configuration["Kafka:BootstrapServers"],
        EnableIdempotence = true,
        MessageSendMaxRetries = 3,
        Acks = Acks.All
    };
    return new ProducerBuilder<string, DriverLocationEvent>(config)
        .SetValueSerializer(new JsonSerializer<DriverLocationEvent>())
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
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = ConfigurationOptions.Parse(builder.Configuration.GetConnectionString("Redis"));
    configuration.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(configuration);
});
builder.Services.AddSingleton<RedisCacheService>();
//builder.Services.AddCors(options => {
//    options.AddPolicy("AllowAll", policy => {
//        policy.AllowAnyOrigin()
//              .AllowAnyMethod()
//              .AllowAnyHeader();

//    });
//});


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowCustomerApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(origin => true) 
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
}).AddScheme<AuthenticationSchemeOptions, ServiceAuthenticationHandler>(
            "ServiceAuthentication", opts => { });
// Add Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ServicePolicy", policy =>
        policy.RequireAuthenticatedUser()
             .AddAuthenticationSchemes("ServiceAuthentication")
             .RequireRole("Service"));
});


builder.Host.UseSerilog((context, config) =>
{
    config.WriteTo.Console()
          .ReadFrom.Configuration(context.Configuration);
});
builder.Services.AddHealthChecks()
    .AddCheck<KafkaHealthCheck>("kafka");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection();

}

app.UseMiddleware<ExceptionHandlingMiddleware>();
//app.UseMiddleware<JwtValidationMiddleware>();
//app.UseCors("AllowAll");
app.UseCors("AllowCustomerApp");
// Add Prometheus metrics endpoint
app.UseMetricServer();
app.UseHttpMetrics();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<TrackingHub>("/tracking");

app.MapControllers(); 


app.Run();



using NotificationService.API.Consumers;
using NotificationService.API.DI;
using NotificationService.API.Middleware;
using NotificationService.Application.Services;
using NotificationService.Domain.Interfaces;
using NotificationService.Domain.Settings;
using NotificationService.Infrastructure.Serialization;
using NotificationService.Infrastructure.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

builder.Services.AddInfrastructure(builder.Configuration);  // Exctenion method for infrasture service registeration
// Add services to the container
if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(8050);
        options.ListenAnyIP(80);

    });
}
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

// Register services
builder.Services.AddScoped<INotificationHandler, NotificationHandler>();
builder.Services.AddHostedService<NotificationConsumerService>();

builder.Services.Configure<CustomerServiceSettings>(
    builder.Configuration.GetSection("Services:CustomerService"));
builder.Host.UseSerilog((context, config) =>
{
    config.WriteTo.Console()
          .ReadFrom.Configuration(context.Configuration);
});
builder.Services.AddSwaggerGen();

// Add health checks
builder.Services.AddHealthChecks();
var app = builder.Build();

app.MapHealthChecks("/health");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection();

}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseRouting();
app.MapControllers();



app.Run();

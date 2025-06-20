using AIAssistanceAPI;
using DBUtility;
using log4net.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Utility;

var builder = WebApplication.CreateBuilder(args);

// Get the port from environment or use default for local dev
var port = Environment.GetEnvironmentVariable("PORT") ?? "35524";

// Configure Kestrel to listen on the appropriate port
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
});

// Add services
XmlConfigurator.Configure(new FileInfo("log4net.config"));

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
    options.JsonSerializerOptions.WriteIndented = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "HISServiceAPI", Version = "v1" });
});

// Local settings
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Services.Configure<AppLocalSetting>(builder.Configuration.GetSection("AppLocalSettings"));
builder.Services.AddSingleton<IAppLocalSetting>(sp => sp.GetRequiredService<IOptions<AppLocalSetting>>().Value);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Middleware
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowLocalhost");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

Logger.WriteInfoLog("API Started");

app.Run();

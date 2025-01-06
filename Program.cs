using Serilog;
using Serilog.Sinks.Elasticsearch;
using EventGenerator.Services;
using Nest;

var builder = WebApplication.CreateBuilder(args);

var elasticUri = new Uri(builder.Configuration["ElasticConfiguration:Uri"]);
var username = builder.Configuration["ElasticConfiguration:Username"];
var password = builder.Configuration["ElasticConfiguration:Password"];

// Configure Elasticsearch client
var settings = new ConnectionSettings(elasticUri)
    .DefaultIndex("camera-events")
    .EnableApiVersioningHeader()
    .BasicAuthentication(username, password);

var elasticClient = new ElasticClient(settings);
builder.Services.AddSingleton<IElasticClient>(elasticClient);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(elasticUri)
    {
        AutoRegisterTemplate = true,
        IndexFormat = $"eventgenerator-logs-{DateTime.UtcNow:yyyy-MM}",
        ModifyConnectionSettings = conn => conn.BasicAuthentication(username, password)
    })
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSingleton<IEventGeneratorService, EventGeneratorService>();
builder.Services.AddHostedService<AutoEventGeneratorService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

try
{
    Log.Information("Starting web application");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

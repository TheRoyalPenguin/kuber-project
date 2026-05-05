using Serilog;
using Serilog.Formatting.Compact;

namespace MyApp.API;

public class Program
{
    public static void Main(string[] args)
    {
        // Настройка Serilog для вывода логов в консоль в формате JSON
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console(new CompactJsonFormatter())
            .CreateLogger();
        
        Log.Information("Starting web application");
        var builder = WebApplication.CreateBuilder(args);

        // Подключаем Serilog вместо стандартного логгера
        builder.Host.UseSerilog();
        
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Подключаем Redis. Берем строку подключения из переменных окружения (для Kubernetes)
        // Если переменной нет, пытаемся подключиться к локальному Redis (для тестов)
        var redisConnectionString = builder.Configuration.GetValue<string>("REDIS_CONN_STRING") ?? "localhost:6379";
        
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "MyApp_";
        });
        
        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthorization();
        app.MapControllers();

        app.Run();
        
        Log.CloseAndFlush();
    }
}
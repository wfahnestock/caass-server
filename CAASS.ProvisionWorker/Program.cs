using CAASS.ProvisionWorker.Messaging;

namespace CAASS.ProvisionWorker;

public class Program
{
    public static IConfiguration Configuration { get; private set; }
    
    public static void Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args);

        Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile(
                $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json",
                optional: true)
            .AddEnvironmentVariables()
            .Build();

        builder.ConfigureServices(services =>
        {
            services.AddHostedService<TenantCreatedConsumerService>();
            services.Configure<RabbitMqSettings>(Configuration.GetSection("RabbitMQ"));
        });
        

        var host = builder.Build();
        host.Run();
    }
}
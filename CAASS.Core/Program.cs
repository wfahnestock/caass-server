using CAASS.Core.Models.Context;
using Microsoft.AspNetCore;
using Microsoft.EntityFrameworkCore;

namespace CAASS.Core;

public class Program
{
    public static void Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        
        // Apply all migrations
        if (args.Contains("--migrate"))
        {
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<UserContext>();
            
                Console.WriteLine("Applying migrations...");
                context.Database.Migrate();
                Console.WriteLine("Migrations applied successfully");
            }
            return;
        }
        
        host.Run();
    }

    private static IWebHostBuilder CreateHostBuilder(string[] args) =>
        WebHost.CreateDefaultBuilder(args)
            .UseIISIntegration()
            .UseStartup<Startup>();
}
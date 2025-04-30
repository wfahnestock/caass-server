using Microsoft.EntityFrameworkCore;

namespace CAASS.Auth;

public static class DbInitializerExtensions
{
    public static IApplicationBuilder InitializeDatabase(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var contexts = scope.ServiceProvider.GetServices<DbContext>();
            
        foreach (var context in contexts)
        {
            context.Database.EnsureCreated();
        }
            
        return app;
    }
}
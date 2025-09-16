using Betsson.OnlineWallets.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Betsson.OnlineWallets.ApiTests;

public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    private readonly string _dbName;

    public CustomWebApplicationFactory(string dbName)
    {
        _dbName = dbName ?? Guid.NewGuid().ToString();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<OnlineWalletContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<OnlineWalletContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
            });
        });
    }

    internal void SetupWalletData(Action<OnlineWalletContext> initializer)
    {
        if (initializer == null)
        {
            throw new ArgumentNullException(nameof(initializer));
        }

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OnlineWalletContext>();
        initializer(db);
        db.SaveChanges();
    }
}

using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MarketService;

internal static class Program
{
    public static void Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MarketContext>();
            Console.WriteLine("Migrate db.");
            db.Database.Migrate();
        }

        host.Run();
    }

    // EF Core uses this method at design time to access the DbContext
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
    }
}

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        var connectionString = $"{Environment.GetEnvironmentVariable("MARKET_CONNECTION_STRING")}";
        Console.WriteLine($"Startup connectionString: {connectionString}");
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        var connectionString = $"{Environment.GetEnvironmentVariable("MARKET_CONNECTION_STRING")}";
        Console.WriteLine($"connectionString: {connectionString}");
// Add services to the container.
        services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.Configure<RpcConfigOptions>(
            Configuration.GetSection(RpcConfigOptions.RpcConfig));
        services.AddDbContextFactory<MarketContext>(options =>
            options
                .UseNpgsql(connectionString)
                .UseLowerCaseNamingConvention()
                .ConfigureWarnings(w => w.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
        );
        services.AddSingleton<RpcClient>();
        services.AddSingleton<Receiver>();
        services.AddHostedService<ShopWorker>();
        services.AddHostedService<ProductWorker>();
        services.AddMvc()
            .AddJsonOptions(
                options => { options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles; }
            );
        services.AddDbContextFactory<MarketContext>(options =>
            options
                .UseNpgsql($"{Environment.GetEnvironmentVariable("MARKET_CONNECTION_STRING")}")
                .UseLowerCaseNamingConvention()
        );
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
// Configure the HTTP request pipeline.h
        // if (env.IsDevelopment())
        // {
        app.UseSwagger();
        app.UseSwaggerUI();
        // }

        // app.UseHttpsRedirection();
        // app.UseAuthorization();
        app.UseRouting();
        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }
}

using CodeEvaluator.Data.Contexts;
using CodeRunner.Handlers;
using CodeRunner.Services;
using Docker.DotNet;

namespace CodeRunner;

public abstract class Program
{
    public static void Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
        // DbContexts
        builder.Services.AddDbContext<CodeDataDbContext>();
        
        // Singletons
        builder.Services.AddSingleton<DockerClient>(_ => new DockerClientConfiguration().CreateClient());
        
        // Scoped
        builder.Services.AddScoped<CodeQueueHandler>();
        
        // Services
        builder.Services.AddHostedService<CodeQueueProcessingService>();

        IHost host = builder.Build();
        host.Run();
    }
}
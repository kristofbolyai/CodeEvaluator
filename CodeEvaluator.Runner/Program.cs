using CodeEvaluator.Data.Contexts;
using CodeEvaluator.Runner.Handlers;
using CodeEvaluator.Runner.Services;
using Docker.DotNet;

namespace CodeEvaluator.Runner;

public abstract class Program
{
    public static void Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
        CreateApplicationBuilder(builder);

        IHost host = builder.Build();
        host.Run();
    }

    public static void CreateApplicationBuilder(IHostApplicationBuilder builder)
    {
        // DbContexts
        builder.Services.AddDbContextFactory<CodeDataDbContext>();

        // Singletons
        builder.Services.AddSingleton<CodeExecutionHandler>();
        builder.Services.AddSingleton<CodeQueueHandler>();
        builder.Services.AddSingleton<ContainerHandler>();
        builder.Services.AddSingleton<DockerClient>(_ => new DockerClientConfiguration().CreateClient());

        // Services
        builder.Services.AddHostedService<CodeQueueProcessingService>();
    }
}
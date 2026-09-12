using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Modules.Internal;

/// <summary>
/// Everything the pipeline offers, expressed as one module added to the root. Only where a module
/// goes differs between the root and a branch, so only that is left open.
/// </summary>
internal abstract class ModulePipelineBase : IModulePipeline
{
    protected abstract ModulePipeline Root { get; }

    public abstract IModulePipeline AddModule(ModuleBase module);

    public IModulePipeline AddModule<TModule>()
        where TModule : ModuleBase, new()
        => AddModule(new TModule());

    public IModulePipeline ConfigureBuilder(Action<WebApplicationBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        return AddModule(new LambdaModule(configure));
    }

    public IModulePipeline ConfigureServices(Action<IServiceCollection> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        return ConfigureBuilder(builder => configure(builder.Services));
    }

    public IModulePipeline ConfigureServices(Action<IServiceCollection, IConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        return ConfigureBuilder(builder => configure(builder.Services, builder.Configuration));
    }

    public IModulePipeline ConfigureServices(
        Action<IServiceCollection, IConfiguration, IWebHostEnvironment> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);

        return ConfigureBuilder(builder => configure(builder.Services, builder.Configuration, builder.Environment));
    }

    public IModulePipeline ConfigureApp(Action<WebApplication> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        return ConfigureApp(app =>
        {
            configure(app);

            return Task.CompletedTask;
        });
    }

    public IModulePipeline ConfigureApp(Func<WebApplication, Task> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        return AddModule(new LambdaModule(configure));
    }

    public IModulePipeline When(bool condition)
        => new ModuleBranch(Root, condition);

    public IModulePipeline When(Func<bool> condition)
    {
        ArgumentNullException.ThrowIfNull(condition);

        return When(condition());
    }

    public IModulePipeline When(Func<IConfiguration, bool> condition)
    {
        ArgumentNullException.ThrowIfNull(condition);

        return When(condition(Root.Builder.Configuration));
    }

    public IModulePipeline WhenEnvironment(string environmentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);

        return When(Root.Builder.Environment.IsEnvironment(environmentName));
    }

    public IModulePipeline WhenDevelopment()
        => WhenEnvironment(Environments.Development);

    public IModulePipeline WhenProduction()
        => WhenEnvironment(Environments.Production);

    public IModulePipeline Always()
        => Root;

    public Task<WebApplication> BuildAsync()
        => Root.BuildApplicationAsync();
}

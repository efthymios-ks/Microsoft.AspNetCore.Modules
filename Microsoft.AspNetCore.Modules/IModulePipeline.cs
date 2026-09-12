using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Modules;

/// <summary>
/// Collects modules and inline configuration, then builds the application. A <c>When</c> opens a
/// branch: everything written after it belongs to that branch, until the next <c>When</c> or an
/// <see cref="Always"/>.
/// </summary>
public interface IModulePipeline
{
    IModulePipeline AddModule(ModuleBase module);

    IModulePipeline AddModule<TModule>()
        where TModule : ModuleBase, new();

    /// <summary>Configuration that needs the builder itself, not only its services.</summary>
    IModulePipeline ConfigureBuilder(Action<WebApplicationBuilder> configure);

    IModulePipeline ConfigureServices(Action<IServiceCollection> configure);

    IModulePipeline ConfigureServices(Action<IServiceCollection, IConfiguration> configure);

    IModulePipeline ConfigureServices(Action<IServiceCollection, IConfiguration, IWebHostEnvironment> configure);

    /// <summary>Runs once the application is built, in the order it was written.</summary>
    IModulePipeline ConfigureApp(Action<WebApplication> configure);

    IModulePipeline ConfigureApp(Func<WebApplication, Task> configure);

    /// <summary>Opens a branch that is kept only when the condition holds.</summary>
    IModulePipeline When(bool condition);

    IModulePipeline When(Func<bool> condition);

    /// <summary>Reads configuration that is already loaded, so the condition is decided here.</summary>
    IModulePipeline When(Func<IConfiguration, bool> condition);

    /// <summary>Matches the environment name, ignoring case.</summary>
    IModulePipeline WhenEnvironment(string environmentName);

    IModulePipeline WhenDevelopment();

    IModulePipeline WhenProduction();

    /// <summary>Leaves the current branch, so what follows is kept whatever the condition was.</summary>
    IModulePipeline Always();

    /// <summary>
    /// Registers every module, builds the application, checks dependencies, then runs each module
    /// against the built application.
    /// </summary>
    Task<WebApplication> BuildAsync();
}

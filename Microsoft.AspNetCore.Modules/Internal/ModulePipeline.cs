using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Modules.Internal;

/// <summary>The pipeline that owns the builder and the modules. Branches all lead back here.</summary>
internal sealed class ModulePipeline : ModulePipelineBase
{
    private readonly List<ModuleBase> _modules = [];

    public ModulePipeline(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        Builder = builder;
    }

    public WebApplicationBuilder Builder { get; }

    protected override ModulePipeline Root
        => this;

    public override IModulePipeline AddModule(ModuleBase module)
    {
        ArgumentNullException.ThrowIfNull(module);

        _modules.Add(module);

        return this;
    }

    public async Task<WebApplication> BuildApplicationAsync()
    {
        foreach (var module in _modules)
        {
            module.ConfigureBuilder(Builder);
        }

        var app = Builder.Build();

        ValidateDependencies(app);

        foreach (var module in _modules)
        {
            await module.ConfigureAppAsync(app);
        }

        return app;
    }

    /// <summary>
    /// Every dependency is checked before any module runs, so a miss cannot leave the application
    /// half configured.
    /// </summary>
    private void ValidateDependencies(WebApplication app)
    {
        var moduleTypes = _modules
            .Select(module => module.GetType())
            .ToHashSet();

        // Asking whether a type can be resolved, rather than resolving it, keeps this from building
        // a service early — or from being refused a scoped one.
        var services = app.Services.GetRequiredService<IServiceProviderIsService>();

        foreach (var module in _modules)
        {
            foreach (var dependency in module.Dependencies)
            {
                if (moduleTypes.Contains(dependency) || services.IsService(dependency))
                {
                    continue;
                }

                var error = new InvalidOperationException(
                    $"Module '{module.GetType().Name}' depends on '{dependency.Name}', which is neither a registered module nor a registered service."
                );

                throw error;
            }
        }
    }
}

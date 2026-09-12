using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Modules.Internal;

/// <summary>
/// What inline configuration becomes, so a lambda and a module are the same thing to the pipeline.
/// </summary>
internal sealed class LambdaModule : ModuleBase
{
    private readonly Action<WebApplicationBuilder>? _configureBuilder;
    private readonly Func<WebApplication, Task>? _configureApp;

    public LambdaModule(Action<WebApplicationBuilder> configureBuilder)
        => _configureBuilder = configureBuilder;

    public LambdaModule(Func<WebApplication, Task> configureApp)
        => _configureApp = configureApp;

    public override void ConfigureBuilder(WebApplicationBuilder builder)
        => _configureBuilder?.Invoke(builder);

    public override Task ConfigureAppAsync(WebApplication app)
        => _configureApp?.Invoke(app) ?? Task.CompletedTask;
}

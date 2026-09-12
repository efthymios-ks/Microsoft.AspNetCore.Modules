using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Modules.Tests;

public sealed class ModulePipelineTests
{
    [Fact]
    public void ToPipeline_WhenBuilderIsNull_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((WebApplicationBuilder)null!).ToPipeline());
    }

    [Fact]
    public async Task AddModule_WhenModuleIsAdded_ShouldRunBothOfItsSteps()
    {
        // Arrange
        var steps = new List<string>();
        var module = new RecordingModule("only", steps);
        var builder = CreateBuilder();

        // Act
        await using var app = await builder
            .ToPipeline()
            .AddModule(module)
            .BuildAsync();

        // Assert
        Assert.Equal(["only:builder", "only:app"], steps);
    }

    [Fact]
    public async Task AddModule_WhenGenericOverloadIsUsed_ShouldAddOneOfThatType()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        await using var app = await builder
            .ToPipeline()
            .AddModule<MarkerModule>()
            .BuildAsync();

        // Assert
        Assert.NotNull(app.Services.GetService<MarkerService>());
    }

    [Fact]
    public void AddModule_WhenModuleIsNull_ShouldThrow()
    {
        // Arrange
        var pipeline = CreateBuilder().ToPipeline();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => pipeline.AddModule(null!));
    }

    [Fact]
    public async Task BuildAsync_WhenAModuleNeedsAnotherModulesService_ShouldRegisterEveryModuleFirst()
    {
        // Arrange
        var builder = CreateBuilder();
        MarkerService? resolved = null;

        // Act
        await using var app = await builder
            .ToPipeline()
            .ConfigureApp(application => resolved = application.Services.GetService<MarkerService>())
            .AddModule<MarkerModule>()
            .BuildAsync();

        // Assert
        Assert.NotNull(resolved);
    }

    [Fact]
    public async Task BuildAsync_WhenSeveralModulesRun_ShouldFollowTheOrderTheyWereAdded()
    {
        // Arrange
        var steps = new List<string>();
        var builder = CreateBuilder();

        // Act
        await using var app = await builder
            .ToPipeline()
            .AddModule(new RecordingModule("first", steps))
            .AddModule(new RecordingModule("second", steps))
            .BuildAsync();

        // Assert
        Assert.Equal(
            ["first:builder", "second:builder", "first:app", "second:app"],
            steps
        );
    }

    [Fact]
    public async Task ConfigureBuilder_WhenCalled_ShouldSeeTheBuilder()
    {
        // Arrange
        var builder = CreateBuilder();
        WebApplicationBuilder? seen = null;

        // Act
        await using var app = await builder
            .ToPipeline()
            .ConfigureBuilder(current => seen = current)
            .BuildAsync();

        // Assert
        Assert.Same(builder, seen);
    }

    [Fact]
    public async Task ConfigureServices_WhenCalled_ShouldRegisterTheService()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        await using var app = await builder
            .ToPipeline()
            .ConfigureServices(services => services.AddSingleton(new MarkerService()))
            .BuildAsync();

        // Assert
        Assert.NotNull(app.Services.GetService<MarkerService>());
    }

    [Fact]
    public async Task ConfigureServices_WhenConfigurationIsWanted_ShouldPassIt()
    {
        // Arrange
        var builder = CreateBuilder();
        builder.Configuration["Marker"] = "value";
        string? seen = null;

        // Act
        await using var app = await builder
            .ToPipeline()
            .ConfigureServices((_, configuration) => seen = configuration["Marker"])
            .BuildAsync();

        // Assert
        Assert.Equal("value", seen);
    }

    [Fact]
    public async Task ConfigureServices_WhenEnvironmentIsWanted_ShouldPassIt()
    {
        // Arrange
        var builder = CreateBuilder(Environments.Staging);
        string? seen = null;

        // Act
        await using var app = await builder
            .ToPipeline()
            .ConfigureServices((_, _, environment) => seen = environment.EnvironmentName)
            .BuildAsync();

        // Assert
        Assert.Equal(Environments.Staging, seen);
    }

    [Fact]
    public async Task ConfigureApp_WhenSynchronous_ShouldRunAgainstTheBuiltApplication()
    {
        // Arrange
        var builder = CreateBuilder();
        WebApplication? seen = null;

        // Act
        await using var app = await builder
            .ToPipeline()
            .ConfigureApp(application => seen = application)
            .BuildAsync();

        // Assert
        Assert.Same(app, seen);
    }

    [Fact]
    public async Task ConfigureApp_WhenAsynchronous_ShouldBeAwaitedBeforeBuildReturns()
    {
        // Arrange
        var builder = CreateBuilder();
        var finished = false;

        // Act
        await using var app = await builder
            .ToPipeline()
            .ConfigureApp(async _ =>
            {
                await Task.Yield();

                finished = true;
            })
            .BuildAsync();

        // Assert
        Assert.True(finished);
    }

    [Fact]
    public void Configure_WhenTheCallbackIsNull_ShouldThrow()
    {
        // Arrange
        var pipeline = CreateBuilder().ToPipeline();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => pipeline.ConfigureBuilder(null!));
        Assert.Throws<ArgumentNullException>(() => pipeline.ConfigureServices((Action<IServiceCollection>)null!));
        Assert.Throws<ArgumentNullException>(
            () => pipeline.ConfigureServices((Action<IServiceCollection, IConfiguration>)null!)
        );
        Assert.Throws<ArgumentNullException>(
            () => pipeline.ConfigureServices((Action<IServiceCollection, IConfiguration, IWebHostEnvironment>)null!)
        );
        Assert.Throws<ArgumentNullException>(() => pipeline.ConfigureApp((Action<WebApplication>)null!));
        Assert.Throws<ArgumentNullException>(() => pipeline.ConfigureApp((Func<WebApplication, Task>)null!));
    }

    [Fact]
    public async Task BuildAsync_WhenNothingWasAdded_ShouldStillBuild()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        await using var app = await builder
            .ToPipeline()
            .BuildAsync();

        // Assert
        Assert.NotNull(app);
    }

    internal static WebApplicationBuilder CreateBuilder(string? environmentName = null)
        => WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environmentName ?? Environments.Production
        });

    internal sealed class MarkerService;

    internal sealed class MarkerModule : ModuleBase
    {
        public override void ConfigureBuilder(WebApplicationBuilder builder)
            => builder.Services.AddSingleton<MarkerService>();
    }

    private sealed class RecordingModule(string name, List<string> steps) : ModuleBase
    {
        public override void ConfigureBuilder(WebApplicationBuilder builder)
            => steps.Add($"{name}:builder");

        public override Task ConfigureAppAsync(WebApplication app)
        {
            steps.Add($"{name}:app");

            return Task.CompletedTask;
        }
    }
}

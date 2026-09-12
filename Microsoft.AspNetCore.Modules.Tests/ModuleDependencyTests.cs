using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Modules.Tests;

public sealed class ModuleDependencyTests
{
    [Fact]
    public async Task BuildAsync_WhenTheDependencyIsAnotherModule_ShouldBuild()
    {
        // Arrange
        var builder = ModulePipelineTests.CreateBuilder();

        // Act
        await using var app = await builder
            .ToPipeline()
            .AddModule<DependentModule>()
            .AddModule<ModulePipelineTests.MarkerModule>()
            .BuildAsync();

        // Assert
        Assert.NotNull(app);
    }

    [Fact]
    public async Task BuildAsync_WhenTheDependencyIsARegisteredService_ShouldBuild()
    {
        // Arrange
        var builder = ModulePipelineTests.CreateBuilder();

        // Act
        await using var app = await builder
            .ToPipeline()
            .ConfigureServices(services => services.AddSingleton<ServiceDependency>())
            .AddModule<ServiceDependentModule>()
            .BuildAsync();

        // Assert
        Assert.NotNull(app);
    }

    [Fact]
    public async Task BuildAsync_WhenTheDependencyIsScoped_ShouldNotResolveItToCheck()
    {
        // Arrange
        var builder = ModulePipelineTests.CreateBuilder();

        // Act
        await using var app = await builder
            .ToPipeline()
            .ConfigureServices(services => services.AddScoped<ServiceDependency>())
            .AddModule<ServiceDependentModule>()
            .BuildAsync();

        // Assert
        Assert.NotNull(app);
        Assert.Equal(0, ServiceDependency.Created);
    }

    [Fact]
    public async Task BuildAsync_WhenTheDependencyIsMissing_ShouldThrowNamingBothTypes()
    {
        // Arrange
        var builder = ModulePipelineTests.CreateBuilder();

        var pipeline = builder
            .ToPipeline()
            .AddModule<DependentModule>();

        // Act
        var error = await Assert.ThrowsAsync<InvalidOperationException>(pipeline.BuildAsync);

        // Assert
        Assert.Contains(nameof(DependentModule), error.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(ModulePipelineTests.MarkerModule), error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BuildAsync_WhenTheDependencyIsMissing_ShouldThrowBeforeAnyModuleRuns()
    {
        // Arrange
        var ran = false;
        var builder = ModulePipelineTests.CreateBuilder();

        var pipeline = builder
            .ToPipeline()
            .ConfigureApp(_ => ran = true)
            .AddModule<DependentModule>();

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(pipeline.BuildAsync);

        // Assert
        Assert.False(ran);
    }

    [Fact]
    public async Task BuildAsync_WhenTheDependingModuleIsDroppedByABranch_ShouldBuild()
    {
        // Arrange
        var builder = ModulePipelineTests.CreateBuilder();

        // Act
        await using var app = await builder
            .ToPipeline()
            .When(false)
                .AddModule<DependentModule>()
            .BuildAsync();

        // Assert
        Assert.NotNull(app);
    }

    private sealed class DependentModule : ModuleBase
    {
        public override IEnumerable<Type> Dependencies { get; } = [typeof(ModulePipelineTests.MarkerModule)];
    }

    private sealed class ServiceDependentModule : ModuleBase
    {
        public override IEnumerable<Type> Dependencies { get; } = [typeof(ServiceDependency)];
    }

    private sealed class ServiceDependency
    {
        public static int Created { get; private set; }

        public ServiceDependency()
            => Created++;
    }
}

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Modules.Tests;

public sealed class WhenBranchTests
{
    [Fact]
    public async Task When_WhenConditionHolds_ShouldKeepEverythingWrittenAfterIt()
    {
        // Arrange
        var steps = new List<string>();
        var builder = ModulePipelineTests.CreateBuilder();

        // Act
        await using var app = await builder
            .ToPipeline()
            .When(true)
                .ConfigureBuilder(_ => steps.Add("builder"))
                .ConfigureServices(_ => steps.Add("services"))
                .ConfigureApp(_ => steps.Add("app"))
            .BuildAsync();

        // Assert
        Assert.Equal(["builder", "services", "app"], steps);
    }

    [Fact]
    public async Task When_WhenConditionDoesNotHold_ShouldDropEverythingWrittenAfterIt()
    {
        // Arrange
        var steps = new List<string>();
        var builder = ModulePipelineTests.CreateBuilder();

        // Act
        await using var app = await builder
            .ToPipeline()
            .When(false)
                .ConfigureBuilder(_ => steps.Add("builder"))
                .ConfigureServices(_ => steps.Add("services"))
                .ConfigureApp(_ => steps.Add("app"))
                .AddModule<ModulePipelineTests.MarkerModule>()
            .BuildAsync();

        // Assert
        Assert.Empty(steps);
        Assert.Null(app.Services.GetService<ModulePipelineTests.MarkerService>());
    }

    [Fact]
    public async Task When_WhenASecondBranchOpens_ShouldNotInheritTheFirstCondition()
    {
        // Arrange
        var steps = new List<string>();
        var builder = ModulePipelineTests.CreateBuilder();

        // Act
        await using var app = await builder
            .ToPipeline()
            .When(false)
                .ConfigureApp(_ => steps.Add("dropped"))
            .When(true)
                .ConfigureApp(_ => steps.Add("kept"))
            .BuildAsync();

        // Assert
        Assert.Equal(["kept"], steps);
    }

    [Fact]
    public async Task Always_WhenWrittenAfterABranch_ShouldGoBackToKeepingEverything()
    {
        // Arrange
        var steps = new List<string>();
        var builder = ModulePipelineTests.CreateBuilder();

        // Act
        await using var app = await builder
            .ToPipeline()
            .When(false)
                .ConfigureApp(_ => steps.Add("dropped"))
            .Always()
                .ConfigureApp(_ => steps.Add("kept"))
            .BuildAsync();

        // Assert
        Assert.Equal(["kept"], steps);
    }

    [Fact]
    public async Task BuildAsync_WhenWrittenInsideABranch_ShouldStillBuildTheApplication()
    {
        // Arrange
        var builder = ModulePipelineTests.CreateBuilder();

        // Act
        await using var app = await builder
            .ToPipeline()
            .When(false)
                .ConfigureApp(_ => { })
                .BuildAsync();

        // Assert
        Assert.NotNull(app);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task When_WhenGivenAPredicate_ShouldEvaluateIt(bool condition, int expected)
    {
        // Arrange
        var steps = new List<string>();
        var builder = ModulePipelineTests.CreateBuilder();

        // Act
        await using var app = await builder
            .ToPipeline()
            .When(() => condition)
                .ConfigureApp(_ => steps.Add("app"))
            .BuildAsync();

        // Assert
        Assert.Equal(expected, steps.Count);
    }

    [Fact]
    public async Task When_WhenGivenAConfigurationPredicate_ShouldReadTheLoadedConfiguration()
    {
        // Arrange
        var steps = new List<string>();
        var builder = ModulePipelineTests.CreateBuilder();
        builder.Configuration["Features:Caching"] = "true";

        // Act
        await using var app = await builder
            .ToPipeline()
            .When(configuration => configuration.GetValue<bool>("Features:Caching"))
                .ConfigureApp(_ => steps.Add("caching"))
            .When(configuration => configuration.GetValue<bool>("Features:Missing"))
                .ConfigureApp(_ => steps.Add("missing"))
            .BuildAsync();

        // Assert
        Assert.Equal(["caching"], steps);
    }

    [Fact]
    public void When_WhenThePredicateIsNull_ShouldThrow()
    {
        // Arrange
        var pipeline = ModulePipelineTests.CreateBuilder().ToPipeline();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => pipeline.When((Func<bool>)null!));
        Assert.Throws<ArgumentNullException>(() => pipeline.When((Func<IConfiguration, bool>)null!));
    }

    [Fact]
    public async Task WhenEnvironment_WhenTheNameMatchesApartFromCase_ShouldKeepTheBranch()
    {
        // Arrange
        var steps = new List<string>();
        var builder = ModulePipelineTests.CreateBuilder(Environments.Development);

        // Act
        await using var app = await builder
            .ToPipeline()
            .WhenEnvironment("development")
                .ConfigureApp(_ => steps.Add("app"))
            .BuildAsync();

        // Assert
        Assert.Equal(["app"], steps);
    }

    [Fact]
    public async Task WhenEnvironment_WhenTheNameDiffers_ShouldDropTheBranch()
    {
        // Arrange
        var steps = new List<string>();
        var builder = ModulePipelineTests.CreateBuilder(Environments.Production);

        // Act
        await using var app = await builder
            .ToPipeline()
            .WhenEnvironment(Environments.Development)
                .ConfigureApp(_ => steps.Add("app"))
            .BuildAsync();

        // Assert
        Assert.Empty(steps);
    }

    [Fact]
    public void WhenEnvironment_WhenTheNameIsEmpty_ShouldThrow()
    {
        // Arrange
        var pipeline = ModulePipelineTests.CreateBuilder().ToPipeline();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => pipeline.WhenEnvironment(" "));
    }

    [Fact]
    public async Task WhenDevelopment_WhenRunningInDevelopment_ShouldKeepTheBranch()
    {
        // Arrange
        var steps = new List<string>();
        var builder = ModulePipelineTests.CreateBuilder(Environments.Development);

        // Act
        await using var app = await builder
            .ToPipeline()
            .WhenDevelopment()
                .ConfigureApp(_ => steps.Add("development"))
            .WhenProduction()
                .ConfigureApp(_ => steps.Add("production"))
            .BuildAsync();

        // Assert
        Assert.Equal(["development"], steps);
    }

    [Fact]
    public async Task WhenProduction_WhenRunningInProduction_ShouldKeepTheBranch()
    {
        // Arrange
        var steps = new List<string>();
        var builder = ModulePipelineTests.CreateBuilder(Environments.Production);

        // Act
        await using var app = await builder
            .ToPipeline()
            .WhenProduction()
                .ConfigureApp(_ => steps.Add("production"))
            .WhenDevelopment()
                .ConfigureApp(_ => steps.Add("development"))
            .BuildAsync();

        // Assert
        Assert.Equal(["production"], steps);
    }

    [Fact]
    public async Task When_WhenBranchesAreMixedWithRootCalls_ShouldKeepTheOrderTheyWereWritten()
    {
        // Arrange
        var steps = new List<string>();
        var builder = ModulePipelineTests.CreateBuilder();

        // Act
        await using var app = await builder
            .ToPipeline()
            .ConfigureApp(_ => steps.Add("first"))
            .When(true)
                .ConfigureApp(_ => steps.Add("second"))
            .Always()
                .ConfigureApp(_ => steps.Add("third"))
            .BuildAsync();

        // Assert
        Assert.Equal(["first", "second", "third"], steps);
    }

    [Fact]
    public async Task AddModule_WhenTheBranchHolds_ShouldKeepTheModule()
    {
        // Arrange
        var builder = ModulePipelineTests.CreateBuilder();

        // Act
        await using var app = await builder
            .ToPipeline()
            .When(true)
                .AddModule<ModulePipelineTests.MarkerModule>()
            .BuildAsync();

        // Assert
        Assert.NotNull(app.Services.GetService<ModulePipelineTests.MarkerService>());
    }

    [Fact]
    public void WhenEnvironment_WhenTheNameIsNull_ShouldThrow()
    {
        // Arrange
        var pipeline = ModulePipelineTests.CreateBuilder().ToPipeline();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => pipeline.WhenEnvironment(null!));
    }

    [Fact]
    public void AddModule_WhenTheBranchIsDropped_ShouldStillRejectNull()
    {
        // Arrange
        var branch = ModulePipelineTests.CreateBuilder().ToPipeline().When(false);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => branch.AddModule(null!));
    }
}

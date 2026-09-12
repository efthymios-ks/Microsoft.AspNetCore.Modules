# Microsoft.AspNetCore.Modules

`Program.cs` cut into modules, plus a fluent pipeline that builds the application from them.
```
ModuleBase.cs                      ConfigureBuilder, ConfigureAppAsync, Dependencies
IModulePipeline.cs                 AddModule, Configure*, When*, Always, BuildAsync
WebApplicationBuilderExtensions.cs ToPipeline()
Internal/ModulePipeline.cs         the root: owns the builder, the modules and the build
Internal/ModuleBranch.cs           one When and everything written after it
Internal/LambdaModule.cs           what inline configuration becomes
```

## A module

```csharp
public sealed class DatabaseModule : ModuleBase
{
    public override void ConfigureBuilder(WebApplicationBuilder builder)
        => builder.Services.AddDbContext<ShopDbContext>(options
            => options.UseSqlServer(builder.Configuration.GetConnectionString("Shop")));

    public override async Task ConfigureAppAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<ShopDbContext>()
            .Database
            .MigrateAsync();
    }
}
```

`ConfigureBuilder` registers, `ConfigureAppAsync` runs once the application exists. Every module
registers before any module runs, so a module can use what a later one registered and the order they
were added in does not decide what works.

## The pipeline

```csharp
var app = await WebApplication
    .CreateBuilder(args)
    .ToPipeline()
    .AddModule<DatabaseModule>()
    .AddModule(new MessagingModule(queueName: "orders"))

    .ConfigureServices(services => services.AddAuthentication())
    .ConfigureServices((services, configuration) => services.Configure<EmailOptions>(configuration.GetSection("Email")))
    .ConfigureServices((services, configuration, environment) => services.AddDataProtection(environment, configuration))
    .ConfigureBuilder(builder => builder.Host.UseSerilog())

    .ConfigureApp(current => current.UseAuthentication())
    .ConfigureApp(async current => await current.SeedAsync())

    .BuildAsync();

await app.RunAsync();
```

| Call | Does |
| --- | --- |
| `AddModule(module)` / `AddModule<TModule>()` | adds a module, either built by you or by its parameterless constructor |
| `ConfigureBuilder(builder)` | inline configuration that needs the builder itself |
| `ConfigureServices(services)` | the same, with configuration and environment as further overloads |
| `ConfigureApp(app)` / `ConfigureApp(async app)` | inline configuration that runs after the application is built |
| `BuildAsync()` | registers everything, builds, checks dependencies, then runs each module |

Inline configuration is a module with no name — it keeps its place in the order, so a `ConfigureApp`
written between two modules runs between them.

## Conditions

```csharp
var app = await WebApplication
    .CreateBuilder(args)
    .ToPipeline()
    .WhenDevelopment()
        .ConfigureServices(services => services.AddOpenApi())
        .ConfigureApp(current => current.MapOpenApi())
        .ConfigureApp(current => current.UseDeveloperExceptionPage())

    .When(configuration => configuration.GetValue<bool>("Features:Caching"))
        .AddModule<CachingModule>()

    .Always()
        .ConfigureApp(current => current.MapControllers())

    .BuildAsync();
```

A `When` opens a branch, and **everything written after it belongs to that branch** — not only the
next call. The branch closes at the next `When`, at `Always()`, or at `BuildAsync()`. Branches do not
nest: each `When` starts fresh, so a second one is independent of the first rather than an `and`.

| Call | Keeps the branch when |
| --- | --- |
| `When(bool)` / `When(() => …)` | the condition holds |
| `When(configuration => …)` | the predicate accepts the configuration, which is already loaded |
| `WhenEnvironment(name)` | the environment name matches, ignoring case |
| `WhenDevelopment()` / `WhenProduction()` | the environment is that one |
| `Always()` | always — it leaves the current branch |

A dropped branch is dropped where it is written, so nothing it configures is registered and nothing
it adds is built.

## Dependencies

```csharp
public sealed class ReportingModule : ModuleBase
{
    public override IEnumerable<Type> Dependencies { get; } = [typeof(DatabaseModule), typeof(IClock)];
}
```

A dependency is satisfied by another module of that type, or by a registered service. All of them
are checked once, after the application is built and before any module runs, so a miss cannot leave
the application half configured:

```
Module 'ReportingModule' depends on 'DatabaseModule', which is neither a registered module nor a
registered service.
```

The check asks the provider whether a type can be resolved rather than resolving it, so nothing is
built early and a scoped dependency is not refused.

## License

MIT.

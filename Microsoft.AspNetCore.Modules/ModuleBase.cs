using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Modules;

/// <summary>
/// One slice of an application's start-up: what it registers, and what it does once the application
/// exists. Every module registers before any module runs, so registration order does not matter.
/// </summary>
public abstract class ModuleBase
{
    /// <summary>
    /// Types this module cannot work without. Each is satisfied by another module of that type, or
    /// by a service someone registered. Checked once, after the application is built.
    /// </summary>
    public virtual IEnumerable<Type> Dependencies { get; } = [];

    /// <summary>Registers what the module needs. Runs before the application is built.</summary>
    public virtual void ConfigureBuilder(WebApplicationBuilder builder)
    {
    }

    /// <summary>
    /// Adds to the request pipeline, or does start-up work such as a migration. Runs after the
    /// application is built, in the order modules were added.
    /// </summary>
    public virtual Task ConfigureAppAsync(WebApplication app)
        => Task.CompletedTask;
}

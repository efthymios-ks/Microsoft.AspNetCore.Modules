using Microsoft.AspNetCore.Modules.Internal;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Modules;

public static class WebApplicationBuilderExtensions
{
    /// <summary>Starts a module pipeline over this builder.</summary>
    public static IModulePipeline ToPipeline(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return new ModulePipeline(builder);
    }
}

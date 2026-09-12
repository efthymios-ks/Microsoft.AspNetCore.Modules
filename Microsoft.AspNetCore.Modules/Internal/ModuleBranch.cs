namespace Microsoft.AspNetCore.Modules.Internal;

/// <summary>
/// A <c>When</c> and everything written after it. A branch that does not hold drops what it is
/// given, and either way it returns itself, so the whole chain stays under the same condition.
/// </summary>
internal sealed class ModuleBranch(ModulePipeline root, bool condition) : ModulePipelineBase
{
    private readonly bool _condition = condition;

    protected override ModulePipeline Root { get; } = root;

    public override IModulePipeline AddModule(ModuleBase module)
    {
        ArgumentNullException.ThrowIfNull(module);

        if (_condition)
        {
            Root.AddModule(module);
        }

        return this;
    }
}

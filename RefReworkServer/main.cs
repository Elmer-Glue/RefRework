using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Helpers.Server;
using System.Reflection;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace RefReworkServer;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.elmerglue.refrework";
    public string Name { get; init; } = "Ref Rework";
    public string Author { get; init; } = "Elmer Glue";
    public List<string>? Contributors { get; init; } = ["DrunkGeko", "marbL-"];
    public SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; } = "";
    public string License { get; init; } = "MIT";
}

[Injectable(TypePriority = OnLoadOrder.Preload + 1)]
public class PreSPTLoader(
        ISptLogger<PreSPTLoader> logger,
        ModHelper modHelper,
        Context context
    ) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        var config = modHelper.GetJsonDataFromFile<RefGPConfig>(pathToMod, "config.json5");

        context.PreInitialize(config, logger);

        return Task.CompletedTask;
    }
}

[Injectable(TypePriority = OnLoadOrder.TraderRegistration + 100)]
public class PostDBLoader(
    Context context,
    ISptLogger<PostDBLoader> logger,
    TradersTable tradersTable,
    TemplateTable templateTable
) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        if (!context.IsInitialized)
        {
            throw new Exception("Context was not initialized!");
        }

        context.PostInitialize(tradersTable, templateTable);

        if (context.config.enable)
        {
            try
            {
                RefChanges.Apply(context);
                logger.Success("Ref Rework: applied Ref dogtag/Lega medal -> GP coin changes.");
            }
            catch (Exception ex)
            {
                logger.Error($"Ref Rework failed to apply changes: {ex.Message}");
                if (context.config.dev.showFullError)
                {
                    logger.Error(ex.StackTrace ?? "");
                }
            }
        }

        return Task.CompletedTask;
    }
}

using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;

namespace RefReworkServer;

[Injectable(InjectionType = InjectionType.Singleton)]
public class Context
{
    public TradersTable traders = null!;
    public TemplateTable templates = null!;
    public RefGPConfig config = null!;
    public ISptLogger<PreSPTLoader> logger = null!;

    public bool IsInitialized => config != null;

    public void PreInitialize(RefGPConfig _config, ISptLogger<PreSPTLoader> _logger)
    {
        config = _config;
        logger = _logger;
    }

    public void PostInitialize(TradersTable _traders, TemplateTable _templates)
    {
        traders = _traders;
        templates = _templates;
    }
}

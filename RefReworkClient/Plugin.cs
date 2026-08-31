using System;
using System.IO;
using BepInEx;
using HarmonyLib;

namespace RefReworkClient;

[BepInPlugin("com.elmerglue.refreworkclient", "Ref Rework", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    public static DogtagValueTable ValueTable = null!;

    private FileSystemWatcher? _watcher;

    private void Awake()
    {
        var pluginDirectory = Path.GetDirectoryName(Info.Location);
        if (string.IsNullOrEmpty(pluginDirectory))
        {
            pluginDirectory = AppDomain.CurrentDomain.BaseDirectory;
            Logger.LogWarning("[RefReworkClient] Could not determine plugin directory from " +
                               $"Info.Location; falling back to '{pluginDirectory}'.");
        }

        ValueTable = new DogtagValueTable(Logger, pluginDirectory);
        ValueTable.LoadOrCreateDefault();

        SetupHotReload(pluginDirectory);

        var harmony = new Harmony("com.elmerglue.refreworkclient");
        harmony.PatchAll();

        Logger.LogInfo("Ref Rework loaded (flat dogtag value table + GP icon fix).");
    }

    private void SetupHotReload(string pluginDirectory)
    {
        try
        {
            _watcher = new FileSystemWatcher(pluginDirectory, "dogtag_values.json")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true,
            };

            _watcher.Changed += (_, _) => DebouncedReload();
            _watcher.Created += (_, _) => DebouncedReload();
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"[RefReworkClient] Could not watch dogtag_values.json for changes " +
                               $"(edits will need a game restart): {ex.Message}");
        }
    }

    private DateTime _lastReloadRequest;

    private void DebouncedReload()
    {
        _lastReloadRequest = DateTime.UtcNow;
        var requestedAt = _lastReloadRequest;

        System.Threading.Tasks.Task.Delay(250).ContinueWith(_ =>
        {
            if (_lastReloadRequest == requestedAt)
            {
                ValueTable.Reload();
            }
        });
    }
}

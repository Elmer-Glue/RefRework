using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using Newtonsoft.Json;

namespace RefReworkClient;

public sealed class DogtagValueTable
{
    private readonly ManualLogSource _log;
    private readonly string _path;
    private SortedList<int, int> _levels = new();
    private int _fallbackAmount = 1;

    public DogtagValueTable(ManualLogSource log, string pluginDirectory)
    {
        _log = log;
        _path = Path.Combine(pluginDirectory, "dogtag_values.json");
    }

    public string FilePath => _path;

    public void LoadOrCreateDefault()
    {
        if (!File.Exists(_path))
        {
            WriteDefaultFile();
        }

        Reload();
    }

    public void Reload()
    {
        try
        {
            var json = File.ReadAllText(_path);
            var data = JsonConvert.DeserializeObject<DogtagValueTableFile>(json);

            if (data?.levels == null || data.levels.Count == 0)
            {
                _log.LogWarning(
                    $"[RefReworkClient] dogtag_values.json has no entries under \"levels\" -- " +
                    "every dogtag will use the fallbackAmount.");
                _levels = new SortedList<int, int>();
            }
            else
            {
                var parsed = new SortedList<int, int>();
                foreach (var kvp in data.levels)
                {
                    if (!int.TryParse(kvp.Key, out var level) || level < 0)
                    {
                        _log.LogWarning($"[RefReworkClient] Ignoring invalid level key \"{kvp.Key}\" in dogtag_values.json.");
                        continue;
                    }

                    parsed[level] = Math.Max(0, kvp.Value);
                }

                _levels = parsed;
            }

            _fallbackAmount = Math.Max(0, data?.fallbackAmount ?? 1);

            _log.LogInfo(
                $"[RefReworkClient] Loaded dogtag value table: {_levels.Count} level(s) defined, " +
                $"fallback = {_fallbackAmount} GP.");
        }
        catch (Exception ex)
        {
            _log.LogError($"[RefReworkClient] Failed to load dogtag_values.json: {ex.Message}. " +
                           "Using fallbackAmount for every dogtag until this is fixed.");
            _levels = new SortedList<int, int>();
        }
    }

    public int GetAmountForLevel(int level)
    {
        if (_levels.Count == 0)
        {
            return _fallbackAmount;
        }

        var candidate = _levels.Keys.Where(k => k <= level).Cast<int?>().Max();

        if (candidate == null)
        {
            return _fallbackAmount;
        }

        return _levels[candidate.Value];
    }

    private void WriteDefaultFile()
    {
        const string defaultJson = @"{
  ""_comment"": ""GP coins Ref pays for a dogtag of the given level (kills). Levels not listed use the value of the nearest lower listed level. Add/remove/edit levels freely -- no restart needed, the table hot-reloads."",
  ""fallbackAmount"": 1,
  ""levels"": {
    ""1"": 400,
    ""5"": 900,
    ""10"": 1600,
    ""15"": 2400,
    ""20"": 3200,
    ""30"": 4800,
    ""40"": 6400,
    ""50"": 8000
  }
}
";
        File.WriteAllText(_path, defaultJson);
    }

    private sealed class DogtagValueTableFile
    {
        public Dictionary<string, int>? levels { get; set; }
        public int fallbackAmount { get; set; } = 1;
    }
}

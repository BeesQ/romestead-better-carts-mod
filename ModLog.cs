using System.Collections.Generic;
using BepInEx.Logging;

namespace BetterCarts;

// TEMPORARY diagnostic layer for the v1.3.0 Cart Capacity investigation. Remove, or default the toggle to false, before release
internal static class ModLog {
    private const string Tag = "[CC] ";

    private static ManualLogSource _log;
    private static readonly Dictionary<string, string> LastSeen = new Dictionary<string, string>();

    internal static void Init(ManualLogSource log) {
        _log = log;
    }

    internal static bool Enabled {
        get { return _log != null && ModConfig.CartCapacityLogging != null && ModConfig.CartCapacityLogging.Value; }
    }

    internal static void Info(string message) {
        if (Enabled) {
            _log.LogInfo(Tag + message);
        }
    }

    // per-tick paths call this; the line is only emitted when its content actually changes
    internal static void OnChange(string key, string message) {
        if (!Enabled) {
            return;
        }
        if (LastSeen.TryGetValue(key, out string previous) && previous == message) {
            return;
        }
        LastSeen[key] = message;
        _log.LogInfo(Tag + message);
    }

    internal static void Reset(string key) {
        LastSeen.Remove(key);
    }
}

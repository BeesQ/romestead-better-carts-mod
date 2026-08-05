using System.Collections.Generic;
using BepInEx.Logging;

namespace BetterCarts;

// diagnostic layer for Cart Capacity. It stays in the codebase on purpose - it is what found the client-callback and the measurement-race bugs. Both toggles ship false
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

    // the per-Cart and per-pickup firehose. Gated on Enabled too, so the basic toggle still silences everything on its own
    internal static bool AdvancedEnabled {
        get {
            return Enabled && ModConfig.CartCapacityAdvancedLogging != null
                && ModConfig.CartCapacityAdvancedLogging.Value;
        }
    }

    internal static void Info(string message) {
        if (Enabled) {
            _log.LogInfo(Tag + message);
        }
    }

    internal static void Advanced(string message) {
        if (AdvancedEnabled) {
            _log.LogInfo(Tag + message);
        }
    }

    // per-tick paths call this; the line is only emitted when its content actually changes
    internal static void OnChange(string key, string message) {
        Emit(Enabled, key, message);
    }

    internal static void AdvancedOnChange(string key, string message) {
        Emit(AdvancedEnabled, key, message);
    }

    private static void Emit(bool enabled, string key, string message) {
        if (!enabled) {
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

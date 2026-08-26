using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BepInEx.Configuration;
using CandideServer;
using Shared;
using Shared.Entity;
using Shared.Helpers;

namespace BetterCarts;

internal sealed class CartTypeRecord {
    internal Guid Id;
    internal string Name = string.Empty;

    internal ConfigEntry<int> Setting;
}

internal static class CartCapacity {
    internal const int VanillaBase = 4;
    internal const int VanillaBlessed = 5;

    private const int DefaultCapacity = 4;
    private const int SliderMax = 64;
    private const string SectionName = "Cart Capacity";
    private const string EntryDescription = "The default capacity of this cart is 4.";

    private static readonly CartTypeRecord[] Types = {
        new CartTypeRecord { Id = new Guid("5af0ea10-21de-404a-a869-b0079653ee0b"), Name = "Wooden Cart" },
        new CartTypeRecord { Id = new Guid("4f26d74b-6fea-4ce9-b369-3bd4507dfff6"), Name = "Bronze Cart" }
    };

    private static readonly Dictionary<Guid, CartTypeRecord> Records = new Dictionary<Guid, CartTypeRecord>();

    private static bool _flagsLogged;

    internal static bool Enforcing {
        get { return ModConfig.Enabled.Value && ModConfig.CartCapacityEnabled.Value; }
    }

    internal static bool Ejecting {
        get {
            return Enforcing && (ModConfig.CartCapacityEjectOverflow == null
                || ModConfig.CartCapacityEjectOverflow.Value);
        }
    }

    internal static bool Blessed {
        get { return WorldFlagsHelper.HasFlag(ServerGameState.WorldFlags, WorldFlagNames.MercuryCartCapacity); }
    }

    private static int BlessingBonus {
        get {
            return ModConfig.CartCapacityBlessingBonus == null ? 1 : ModConfig.CartCapacityBlessingBonus.Value;
        }
    }

    internal static void BindTypeEntries(ConfigFile config) {
        bool hidden = !ModConfig.CartCapacityEnabled.Value;
        Records.Clear();
        int order = 3;
        foreach (CartTypeRecord record in Types) {
            record.Setting = config.Bind(SectionName, record.Id.ToString(), DefaultCapacity,
                new ConfigDescription(EntryDescription, new AcceptableValueRange<int>(0, SliderMax),
                    ModConfig.EntryTag(record.Name + " Capacity", order, hidden)));
            Records[record.Id] = record;
            order++;
        }
    }

    internal static void LogStartup() {
        ModLog.Info("=== STARTUP ===");
        ModLog.Info("Enabled=" + ModConfig.Enabled.Value + " CartCapacityEnabled=" + ModConfig.CartCapacityEnabled.Value
            + " bonus=" + BlessingBonus + " types=" + Records.Count);
        foreach (CartTypeRecord record in Types) {
            ModLog.Info("  type " + record.Id + " name=\"" + record.Name + "\" slider="
                + (record.Setting == null ? "UNBOUND" : record.Setting.Value.ToString(CultureInfo.InvariantCulture)));
        }
        ModLog.Info("=== END STARTUP ===");
    }

    internal static void NoteWorldLoaded(string origin) {
        if (_flagsLogged || !ModLog.Enabled) {
            return;
        }
        _flagsLogged = true;
        LogWorldFlags(origin);
    }

    private static void LogWorldFlags(string origin) {
        object flags = ServerGameState.WorldFlags;
        string dump;
        if (flags == null) {
            dump = "null";
        } else if (flags is System.Collections.IEnumerable items && !(flags is string)) {
            dump = string.Join(",", items.Cast<object>().Select(o => o == null ? "null" : o.ToString()));
        } else {
            dump = flags.ToString();
        }
        ModLog.Info("WorldFlags(" + origin + ") key=\"" + WorldFlagNames.MercuryCartCapacity
            + "\" HasFlag=" + Blessed + " raw=[" + dump + "]");
    }

    internal static bool TryGetEnforcedCapacity(EntityWrapper cartEntity, out int capacity) {
        capacity = 0;
        if (cartEntity == null || !Enforcing) {
            return false;
        }
        if (!Records.TryGetValue(cartEntity.BaseGuid, out CartTypeRecord record) || record.Setting == null) {
            ModLog.AdvancedOnChange("cap:" + cartEntity.BaseGuid, "CAPACITY type=" + cartEntity.BaseGuid
                + " is not a vanilla Cart type - Cart Capacity does not apply to it");
            return false;
        }
        int configured = record.Setting.Value;
        int bonus = Blessed ? BlessingBonus : 0;
        capacity = configured + bonus;
        ModLog.AdvancedOnChange("cap:" + record.Id, "CAPACITY " + record.Name + " base=" + configured + " bonus=" + bonus
            + " blessed=" + Blessed + " -> " + capacity);
        return true;
    }

    internal static int GetKnownCapacity(EntityWrapper cartEntity) {
        if (TryGetEnforcedCapacity(cartEntity, out int enforced)) {
            return enforced;
        }
        return Blessed ? VanillaBlessed : VanillaBase;
    }
}

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

    private const int SliderMax = 20;
    private const string SectionName = "Cart Capacity";
    private const string EntryDescription = "0 = Default, which leaves this cart at its normal capacity of 4 (5 with the Mercury blessing). Any other value replaces that base of 4.";

    private static readonly CartTypeRecord[] Types = {
        new CartTypeRecord { Id = new Guid("5af0ea10-21de-404a-a869-b0079653ee0b"), Name = "Wooden Cart" },
        new CartTypeRecord { Id = new Guid("4f26d74b-6fea-4ce9-b369-3bd4507dfff6"), Name = "Bronze Cart" }
    };

    private static readonly Dictionary<Guid, CartTypeRecord> Records = new Dictionary<Guid, CartTypeRecord>();

    private static bool _flagsLogged;

    internal static bool Enforcing {
        get { return ModConfig.Enabled.Value && ModConfig.CartCapacityEnabled.Value; }
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
        int order = 2;
        foreach (CartTypeRecord record in Types) {
            record.Setting = config.Bind(SectionName, record.Id.ToString(), 0,
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

    internal static int GetEnforcedCapacity(EntityWrapper cartEntity) {
        if (cartEntity == null || !Enforcing) {
            return 0;
        }
        if (!Records.TryGetValue(cartEntity.BaseGuid, out CartTypeRecord record) || record.Setting == null) {
            ModLog.AdvancedOnChange("cap:" + cartEntity.BaseGuid, "CAPACITY type=" + cartEntity.BaseGuid
                + " is not a vanilla Cart type - Cart Capacity does not apply to it");
            return 0;
        }
        int configured = record.Setting.Value;
        if (configured <= 0) {
            ModLog.AdvancedOnChange("cap:" + record.Id, "CAPACITY " + record.Name + " type=" + record.Id
                + " slider=0 (Default) - not enforced");
            return 0;
        }
        int bonus = Blessed ? BlessingBonus : 0;
        ModLog.AdvancedOnChange("cap:" + record.Id, "CAPACITY " + record.Name + " base=" + configured + " bonus=" + bonus
            + " blessed=" + Blessed + " -> " + (configured + bonus));
        return configured + bonus;
    }

    internal static int GetKnownCapacity(EntityWrapper cartEntity) {
        int enforced = GetEnforcedCapacity(cartEntity);
        if (enforced > 0) {
            return enforced;
        }
        return Blessed ? VanillaBlessed : VanillaBase;
    }
}

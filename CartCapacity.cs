using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BepInEx.Configuration;
using Candide.Database.Doodad;
using Candide.Entities.Controllers.Other;
using CandideServer;
using CandideServer.Entities;
using CandideServer.Entities.Controllers;
using Shared;
using Shared.Data;
using Shared.Entity;
using Shared.Helpers;
using Shared.Text;

namespace BetterCarts;

internal sealed class CartTypeRecord {
    internal Guid Id;
    internal string Name = string.Empty;
    internal int ExactUnblessed;
    internal int ExactBlessed;
    internal int SeenUnblessed;
    internal int SeenBlessed;
    internal ConfigEntry<int> Setting;
}

internal static class CartCapacity {
    internal const int VanillaBase = 4;
    internal const int VanillaBlessed = 5;

    private const int SeedBonus = 1;
    private const int SliderMax = 20;
    private const int DiscoveryRetryMs = 1000;
    private const string SectionName = "Cart Capacity";
    private const string UnknownName = "Modded Cart";

    private static readonly Dictionary<Guid, CartTypeRecord> Records = new Dictionary<Guid, CartTypeRecord>();

    private static bool _discovered;
    private static bool _dirty;
    private static long _nextDiscoveryTick;

    internal static bool Enforcing {
        get { return ModConfig.Enabled.Value && ModConfig.CartCapacityEnabled.Value; }
    }

    internal static bool Blessed {
        get { return WorldFlagsHelper.HasFlag(ServerGameState.WorldFlags, WorldFlagNames.MercuryCartCapacity); }
    }

    internal static void LoadCache(string raw) {
        Records.Clear();
        if (string.IsNullOrEmpty(raw)) {
            return;
        }
        foreach (string entry in raw.Split(';')) {
            if (entry.Length == 0) {
                continue;
            }
            string[] fields = entry.Split('|');
            if (fields.Length < 4 || !Guid.TryParse(fields[0], out Guid id)) {
                continue;
            }
            Records[id] = new CartTypeRecord {
                Id = id,
                Name = fields[1],
                ExactUnblessed = ParseCount(fields[2]),
                ExactBlessed = ParseCount(fields[3]),
                SeenUnblessed = fields.Length > 4 ? ParseCount(fields[4]) : 0,
                SeenBlessed = fields.Length > 5 ? ParseCount(fields[5]) : 0
            };
        }
    }

    internal static void BindTypeEntries(ConfigFile config) {
        bool hidden = !ModConfig.CartCapacityEnabled.Value;
        List<CartTypeRecord> ordered = new List<CartTypeRecord>(Records.Values);
        ordered.Sort(Compare);
        int order = 2;
        foreach (CartTypeRecord record in ordered) {
            record.Setting = config.Bind(SectionName, record.Id.ToString(), 0,
                new ConfigDescription(
                    "0 = Auto (this Cart type keeps its own capacity). 1 or more sets the BASE capacity; in-game capacity upgrades are added on top of it.",
                    new AcceptableValueRange<int>(0, Math.Max(SliderMax, Highest(record))),
                    ModConfig.EntryTag(Label(record), order, hidden)));
            order++;
        }
    }

    internal static void EnsureDiscovered() {
        if (_discovered) {
            return;
        }
        long now = Environment.TickCount64;
        if (now < _nextDiscoveryTick) {
            return;
        }
        _nextDiscoveryTick = now + DiscoveryRetryMs;
        if (DiscoverServer() || DiscoverClient()) {
            _discovered = true;
            FlushCache();
        }
    }

    internal static void NoteLiveType(Guid typeId) {
        if (typeId != Guid.Empty) {
            Track(typeId);
        }
    }

    // a refusal proves the exact number: every slot the Cart knows about was taken
    internal static void ObserveExact(Guid typeId, bool blessed, int count) {
        if (count <= 0 || typeId == Guid.Empty) {
            return;
        }
        CartTypeRecord record = Track(typeId);
        if (blessed) {
            if (count != record.ExactBlessed) {
                record.ExactBlessed = count;
                _dirty = true;
            }
            return;
        }
        if (count != record.ExactUnblessed) {
            record.ExactUnblessed = count;
            _dirty = true;
        }
    }

    // seeing N items on a Cart only proves capacity >= N, so this never feeds the label
    internal static void ObserveSeen(Guid typeId, bool blessed, int count) {
        if (count <= 0 || typeId == Guid.Empty) {
            return;
        }
        CartTypeRecord record = Track(typeId);
        if (blessed) {
            if (count > record.SeenBlessed) {
                record.SeenBlessed = count;
                _dirty = true;
            }
            return;
        }
        if (count > record.SeenUnblessed) {
            record.SeenUnblessed = count;
            _dirty = true;
        }
    }

    internal static void FlushCache() {
        if (!_dirty) {
            return;
        }
        _dirty = false;
        ModConfig.KnownCarts.Value = Serialize();
    }

    // 0 means "do not interfere": the master toggle, the feature toggle or the per-type value is off / Auto
    internal static int GetEnforcedCapacity(EntityWrapper cartEntity) {
        if (cartEntity == null || !Enforcing) {
            return 0;
        }
        if (!Records.TryGetValue(cartEntity.BaseGuid, out CartTypeRecord record) || record.Setting == null) {
            return 0;
        }
        int configured = record.Setting.Value;
        if (configured <= 0) {
            return 0;
        }
        return configured + Bonus(record, Blessed);
    }

    // best available answer to "how much fits on this Cart", used by the free-slot rule even when the feature is off
    internal static int GetKnownCapacity(EntityWrapper cartEntity) {
        int enforced = GetEnforcedCapacity(cartEntity);
        if (enforced > 0) {
            return enforced;
        }
        bool blessed = Blessed;
        if (cartEntity != null && Records.TryGetValue(cartEntity.BaseGuid, out CartTypeRecord record)) {
            int unblessed = Math.Max(record.ExactUnblessed, record.SeenUnblessed);
            int observed = blessed ? Math.Max(Math.Max(record.ExactBlessed, record.SeenBlessed), unblessed) : unblessed;
            if (observed > 0) {
                return observed;
            }
        }
        return blessed ? VanillaBlessed : VanillaBase;
    }

    private static bool DiscoverServer() {
        Dictionary<Guid, EntitySystem> systems = ServerEntityDataManager.EntitySystems;
        if (systems == null || systems.Count == 0) {
            return false;
        }
        foreach (Guid id in new List<Guid>(systems.Keys)) {
            if (ServerEntityDataManager.TryGetEntityBaseData(id, out EntityWrapper wrapper) && IsCart(wrapper)) {
                Track(id);
            }
        }
        return true;
    }

    private static bool DiscoverClient() {
        Dictionary<Guid, EntitySystem> systems = DoodadDatabaseManager.EntitySystems;
        if (systems == null || systems.Count == 0) {
            return false;
        }
        foreach (Guid id in new List<Guid>(systems.Keys)) {
            if (DoodadDatabaseManager.TryGetEntityBaseData(id, out EntityWrapper wrapper) && IsCart(wrapper)) {
                Track(id);
            }
        }
        return true;
    }

    private static bool IsCart(EntityWrapper wrapper) {
        if (wrapper == null) {
            return false;
        }
        return wrapper.Controller is ServerCart2Controller || wrapper.Controller is Cart2Controller;
    }

    private static CartTypeRecord Track(Guid id) {
        if (!Records.TryGetValue(id, out CartTypeRecord record)) {
            record = new CartTypeRecord { Id = id };
            Records[id] = record;
            _dirty = true;
        }
        if (string.IsNullOrEmpty(record.Name)) {
            string name = ResolveName(id);
            if (!string.IsNullOrEmpty(name)) {
                record.Name = name;
                _dirty = true;
            }
        }
        return record;
    }

    // several constructions can point at ONE doodad Guid (a joiner resolves Iron Cart's cart:2 to the bronze doodad), so a name is only trustworthy when exactly one matches
    private static string ResolveName(Guid id) {
        var map = ConstructionDataBase.DataMap;
        if (map == null) {
            return string.Empty;
        }
        string label = string.Empty;
        int matches = 0;
        foreach (var model in map.Values) {
            if (string.IsNullOrEmpty(model.SpawnedId)) {
                continue;
            }
            if (!Guid.TryParse(model.SpawnedId, out Guid spawned) || spawned != id) {
                continue;
            }
            matches++;
            if (matches > 1) {
                return string.Empty;
            }
            label = Translate(model.Name.Id);
        }
        return matches == 1 ? label : string.Empty;
    }

    // a StringId is EITHER a translation key OR a raw literal; GetTranslation would wrap a literal in angle brackets
    private static string Translate(string key) {
        if (string.IsNullOrEmpty(key)) {
            return string.Empty;
        }
        return StringDefinitions.TryGetString(key, out string translated) ? translated : key;
    }

    private static string Label(CartTypeRecord record) {
        string name = string.IsNullOrEmpty(record.Name) ? UnknownName : record.Name;
        int known = KnownBase(record);
        if (known <= 0) {
            return name + " Capacity - Auto";
        }
        return name + " Capacity - " + known.ToString(CultureInfo.InvariantCulture) + " (Vanilla)";
    }

    private static int KnownBase(CartTypeRecord record) {
        if (record.ExactUnblessed > 0) {
            return record.ExactUnblessed;
        }
        if (record.ExactBlessed > 0) {
            return Math.Max(1, record.ExactBlessed - SeedBonus);
        }
        return 0;
    }

    private static int Bonus(CartTypeRecord record, bool blessed) {
        if (!blessed) {
            return 0;
        }
        if (record.ExactUnblessed > 0 && record.ExactBlessed > record.ExactUnblessed) {
            return record.ExactBlessed - record.ExactUnblessed;
        }
        return SeedBonus;
    }

    private static int Highest(CartTypeRecord record) {
        int unblessed = Math.Max(record.ExactUnblessed, record.SeenUnblessed);
        int blessed = Math.Max(record.ExactBlessed, record.SeenBlessed);
        return Math.Max(unblessed, blessed);
    }

    private static string Serialize() {
        StringBuilder builder = new StringBuilder();
        foreach (CartTypeRecord record in Records.Values) {
            if (builder.Length > 0) {
                builder.Append(';');
            }
            builder.Append(record.Id.ToString()).Append('|')
                .Append(Sanitize(record.Name)).Append('|')
                .Append(record.ExactUnblessed.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(record.ExactBlessed.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(record.SeenUnblessed.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(record.SeenBlessed.ToString(CultureInfo.InvariantCulture));
        }
        return builder.ToString();
    }

    private static string Sanitize(string name) {
        if (string.IsNullOrEmpty(name)) {
            return string.Empty;
        }
        return name.Replace('|', ' ').Replace(';', ' ');
    }

    private static int ParseCount(string value) {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) && parsed > 0
            ? parsed
            : 0;
    }

    private static int Compare(CartTypeRecord left, CartTypeRecord right) {
        int byName = string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
        return byName != 0 ? byName : left.Id.CompareTo(right.Id);
    }
}

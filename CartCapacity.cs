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

    // refusal-proven and CACHED: a Cart that refused an item has every slot it knows about taken, so the count is its capacity
    internal int ExactUnblessed;
    internal int ExactBlessed;

    // lower bounds, RUNTIME ONLY: seeing N items proves capacity >= N and nothing more. Persisting that is what once made the menu claim an Iron Cart holds 2
    internal int SeenUnblessed;
    internal int SeenBlessed;

    internal ConfigEntry<int> Setting;
}

internal static class CartCapacity {
    internal const int VanillaBase = 4;
    internal const int VanillaBlessed = 5;

    private const string CacheVersion = "v2";
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

    // the version token discards anything an older build wrote rather than reinterpreting it; misreading old lower bounds as exact numbers is what produced a wrong vanilla figure in the menu
    internal static void LoadCache(string raw) {
        Records.Clear();
        if (string.IsNullOrEmpty(raw)) {
            return;
        }
        string[] parts = raw.Split(';');
        if (parts.Length == 0 || !string.Equals(parts[0], CacheVersion, StringComparison.Ordinal)) {
            _dirty = true;
            return;
        }
        for (int i = 1; i < parts.Length; i++) {
            if (parts[i].Length == 0) {
                continue;
            }
            string[] fields = parts[i].Split('|');
            if (fields.Length < 4 || !Guid.TryParse(fields[0], out Guid id)) {
                continue;
            }
            Records[id] = new CartTypeRecord {
                Id = id,
                Name = fields[1],
                ExactUnblessed = ParseCount(fields[2]),
                ExactBlessed = ParseCount(fields[3])
            };
        }
    }

    internal static void BindTypeEntries(ConfigFile config) {
        bool hidden = !ModConfig.CartCapacityEnabled.Value;
        List<CartTypeRecord> ordered = new List<CartTypeRecord>(Records.Values);
        ordered.Sort(Compare);
        int order = 2;
        foreach (CartTypeRecord record in ordered) {
            int max = Math.Max(SliderMax, Math.Max(record.ExactUnblessed, record.ExactBlessed));
            record.Setting = config.Bind(SectionName, record.Id.ToString(), 0,
                new ConfigDescription(Describe(record), new AcceptableValueRange<int>(0, max),
                    ModConfig.EntryTag(DisplayName(record), order, hidden)));
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

    // assignment, not Math.Max: a refusal is authoritative, so a cart mod that changes its capacity in an update corrects itself on the next refusal
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

    internal static void ObserveSeen(Guid typeId, bool blessed, int count) {
        if (count <= 0 || typeId == Guid.Empty) {
            return;
        }
        CartTypeRecord record = Track(typeId);
        if (blessed) {
            record.SeenBlessed = Math.Max(record.SeenBlessed, count);
            return;
        }
        record.SeenUnblessed = Math.Max(record.SeenUnblessed, count);
    }

    internal static void FlushCache() {
        if (!_dirty) {
            return;
        }
        _dirty = false;
        ModConfig.KnownCarts.Value = Serialize();
    }

    // 0 means "do not interfere": the master toggle, the feature toggle or the per-type value is off / Auto. A configured number is EXACT - the blessing adds nothing on top of it
    internal static int GetEnforcedCapacity(EntityWrapper cartEntity) {
        if (cartEntity == null || !Enforcing) {
            return 0;
        }
        if (!Records.TryGetValue(cartEntity.BaseGuid, out CartTypeRecord record) || record.Setting == null) {
            return 0;
        }
        return Math.Max(0, record.Setting.Value);
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

    // several constructions can point at ONE doodad Guid, so a name is only trustworthy when exactly one matches
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

    private static string DisplayName(CartTypeRecord record) {
        return (string.IsNullOrEmpty(record.Name) ? UnknownName : record.Name) + " Capacity";
    }

    // states only what was MEASURED. Deriving one figure from the other needs a guess at the blessing bonus, and that guess is wrong for any mod whose bonus is not +1
    private static string Describe(CartTypeRecord record) {
        const string Head = "0 = Auto. ";
        const string Tail = " 1 or more sets the exact capacity for this Cart type; the Mercury blessing adds nothing on top of it.";
        bool hasUnblessed = record.ExactUnblessed > 0;
        bool hasBlessed = record.ExactBlessed > 0;
        if (hasUnblessed && hasBlessed) {
            return Head + "This Cart type normally holds " + Count(record.ExactUnblessed) + " items, "
                + Count(record.ExactBlessed) + " with the Mercury blessing." + Tail;
        }
        if (hasUnblessed) {
            return Head + "This Cart type normally holds " + Count(record.ExactUnblessed) + " items." + Tail;
        }
        if (hasBlessed) {
            return Head + "This Cart type normally holds " + Count(record.ExactBlessed)
                + " items with the Mercury blessing." + Tail;
        }
        return Head + "This Cart type's capacity has not been measured yet." + Tail;
    }

    private static string Serialize() {
        StringBuilder builder = new StringBuilder(CacheVersion);
        foreach (CartTypeRecord record in Records.Values) {
            builder.Append(';')
                .Append(record.Id.ToString()).Append('|')
                .Append(Sanitize(record.Name)).Append('|')
                .Append(Count(record.ExactUnblessed)).Append('|')
                .Append(Count(record.ExactBlessed));
        }
        return builder.ToString();
    }

    private static string Sanitize(string name) {
        if (string.IsNullOrEmpty(name)) {
            return string.Empty;
        }
        return name.Replace('|', ' ').Replace(';', ' ');
    }

    private static string Count(int value) {
        return value.ToString(CultureInfo.InvariantCulture);
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

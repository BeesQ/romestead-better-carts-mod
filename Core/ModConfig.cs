using BepInEx.Configuration;

namespace BetterCarts;

internal static class ModConfig {
    internal static ConfigEntry<bool> Enabled;
    internal static ConfigEntry<bool> ChainOverflowEnabled;
    internal static ConfigEntry<bool> DepositRangeEnabled;
    internal static ConfigEntry<int> DepositRange;
    internal static ConfigEntry<bool> CollectRangeEnabled;
    internal static ConfigEntry<int> CollectRange;
    internal static ConfigEntry<bool> ConnectRangeEnabled;
    internal static ConfigEntry<int> ConnectRange;
    internal static ConfigEntry<bool> BucketPriorityEnabled;
    internal static ConfigEntry<bool> CartReleaseFixEnabled;
    internal static ConfigEntry<bool> CartCapacityEnabled;
    internal static ConfigEntry<bool> CartCapacityLogging;
    internal static ConfigEntry<int> CartCapacityBlessingBonus;
    internal static ConfigEntry<bool> CartCapacityEjectOverflow;
    internal static ConfigEntry<bool> CartCapacityAdvancedLogging;
    internal static ConfigEntry<bool> StockpileRangeEnabled;
    internal static ConfigEntry<int> StockpileRange;
    internal static ConfigEntry<bool> StockpileWhilePulled;
    internal static ConfigEntry<bool> StockpileWhileParked;

    internal static void Init(ConfigFile config) {
        Enabled = config.Bind("General", "Enabled", true,
            new ConfigDescription("Master on/off for the whole mod.", null,
                SectionTag("General", 0), EntryTag("All features", 0)));
        ChainOverflowEnabled = config.Bind("Chain Overflow", "Enabled", true,
            new ConfigDescription("When a full cart picks up an item, the item is passed to the next cart in the chain with a free slot.", null,
                SectionTag("Chain Overflow", 1), EntryTag("Pass overflow along the chain", 0)));
        CollectRangeEnabled = config.Bind("Collect Range", "Enabled", true,
            new ConfigDescription("Carts automatically pick up loose items within range.", null,
                SectionTag("Collect Range", 5), EntryTag("Automatic pickup", 0)));
        CollectRange = config.Bind("Collect Range", "Range", 2,
            new ConfigDescription("Collect reach in tiles per side. 0 = vanilla (touch only).",
                new AcceptableValueRange<int>(0, 10),
                EntryTag("Range", 1)));
        DepositRangeEnabled = config.Bind("Deposit Range", "Enabled", true,
            new ConfigDescription("Carts deposit matching cargo into Material Storages within range.", null,
                SectionTag("Deposit Range", 6), EntryTag("Automatic deposit", 0)));
        DepositRange = config.Bind("Deposit Range", "Range", 2,
            new ConfigDescription("Deposit reach in tiles per side, 0 = vanilla (park on the storage).",
                new AcceptableValueRange<int>(0, 10),
                EntryTag("Range", 1)));
        ConnectRangeEnabled = config.Bind("Connect Range", "Enabled", true,
            new ConfigDescription("A free cart is pulled toward a cart the player is pulling once it is within range, so they connect without touching.", null,
                SectionTag("Connect Range", 7), EntryTag("Automatic connect", 0)));
        ConnectRange = config.Bind("Connect Range", "Range", 2,
            new ConfigDescription("Connect reach in tiles per side. 0 = vanilla (touch only).",
                new AcceptableValueRange<int>(0, 10),
                EntryTag("Range", 1)));
        BucketPriorityEnabled = config.Bind("Bucket Priority", "Enabled", true,
            new ConfigDescription("When taking an item off a cart, prefer grabbing an empty bucket over other cargo.", null,
                SectionTag("Bucket Priority", 2), EntryTag("Prefer empty buckets", 0)));
        CartReleaseFixEnabled = config.Bind("Cart Release Fix", "Enabled", true,
            new ConfigDescription("Releasing a pulled cart with the interact key never grabs a different cart on the same press.", null,
                SectionTag("Cart Release Fix", 3), EntryTag("Release without re-grabbing", 0)));
        CartCapacityEnabled = config.Bind("Cart Capacity", "Enabled", true,
            new ConfigDescription("Sets how many items the vanilla carts can carry - modded carts are not supported. A cart's value is its base capacity, and the Mercury blessing adds the bonus below on top, so 0 means a cart that carries nothing. High values can cause stutter and stack the cargo into a tall tower above the cart. Lowering a value stops a cart picking up more straight away, and the next time you load that world the cart drops whatever no longer fits in a circle beside itself. In multiplayer the host's values apply to everyone, and every player needs the mod installed to SEE cargo beyond the normal four. Raising a cart above its normal capacity is the only thing this mod writes to your save: change the cart values and the blessing bonus back to vanilla, leave Eject Overflow on, and load each affected world once before uninstalling.", null,
                SectionTag("Cart Capacity", 4), EntryTag("Set capacity per cart type", 0)));
        CartCapacityEjectOverflow = config.Bind("Cart Capacity", "Eject Overflow", true,
            new ConfigDescription("When a world loads, a cart carrying more than its capacity drops the surplus in a circle beside itself. Turn this off to leave that cargo on the cart, where it stays until you unload it by hand.", null,
                EntryTag("Eject Overflow", 1, hidden: !CartCapacityEnabled.Value)));
        CartCapacityBlessingBonus = config.Bind("Cart Capacity", "Blessing Bonus", 1,
            new ConfigDescription("How much the Mercury cart-capacity blessing adds on top of a cart's base capacity.", new AcceptableValueRange<int>(0, 64),
                EntryTag("Blessing Bonus", 2, hidden: !CartCapacityEnabled.Value)));
        CartCapacityLogging = config.Bind("Cart Capacity", "Logging", false,
            new ConfigDescription("DIAGNOSTIC. Writes the Cart Capacity startup and world lines to BepInEx/LogOutput.log. Off by default; turn it on in this file only when reporting a bug.", null,
                EntryTag("Logging (Diagnostic)", 97, hidden: true)));
        CartCapacityAdvancedLogging = config.Bind("Cart Capacity", "Advanced Logging", false,
            new ConfigDescription("DIAGNOSTIC. Adds a per-cart and per-pickup trace to the log, which makes it very large. Does nothing while Logging is off.", null,
                EntryTag("Advanced Logging (Diagnostic)", 98, hidden: true)));
        CartCapacity.BindTypeEntries(config);
        StockpileRangeEnabled = config.Bind("Stockpile Range", "Enabled", true,
            new ConfigDescription("Carts take resources from building output stockpiles within range. Solid resources go into free slots, bucket resources fill empty buckets on the cart.", null,
                SectionTag("Stockpile Range", 8), EntryTag("Take from stockpiles", 0)));
        StockpileRange = config.Bind("Stockpile Range", "Range", 2,
            new ConfigDescription("Stockpile reach in tiles per side. 0 = vanilla (off).",
                new AcceptableValueRange<int>(0, 10),
                EntryTag("Range", 1)));
        StockpileWhilePulled = config.Bind("Stockpile Range", "While Pulled", true,
            new ConfigDescription("Take resources while a player is pulling the cart or its chain.", null,
                EntryTag("While Pulled", 2)));
        StockpileWhileParked = config.Bind("Stockpile Range", "While Parked", false,
            new ConfigDescription("Take resources while the cart is parked (not pulled by a player).", null,
                EntryTag("While Parked", 3)));
    }

    private static object SectionTag(string section, int order) {
        return new { Section = section, DisplayName = section, Order = order };
    }

    internal static object EntryTag(string displayName, int order) {
        return new { DisplayName = displayName, Order = order };
    }

    internal static object EntryTag(string displayName, int order, bool hidden) {
        return new { DisplayName = displayName, Order = order, Hidden = hidden };
    }
}

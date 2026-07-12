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
    internal static ConfigEntry<bool> StockpileRangeEnabled;
    internal static ConfigEntry<int> StockpileRange;
    internal static ConfigEntry<bool> StockpileWhilePulled;
    internal static ConfigEntry<bool> StockpileWhileParked;

    internal static void Init(ConfigFile config) {
        Enabled = config.Bind("General", "Enabled", true,
            new ConfigDescription("Master on/off for the whole mod.", null,
                SectionTag("General", 0), EntryTag("Enabled", 0)));
        ChainOverflowEnabled = config.Bind("Chain Overflow", "Enabled", true,
            new ConfigDescription("When a full cart picks up an item, the item is passed to the next cart in the chain with a free slot.", null,
                SectionTag("Chain Overflow", 1), EntryTag("Enabled", 0)));
        CollectRangeEnabled = config.Bind("Collect Range", "Enabled", true,
            new ConfigDescription("Carts automatically pick up loose items within range.", null,
                SectionTag("Collect Range", 4), EntryTag("Enabled", 0)));
        CollectRange = config.Bind("Collect Range", "Range", 2,
            new ConfigDescription("Collect reach in tiles per side. 0 = vanilla (touch only).",
                new AcceptableValueRange<int>(0, 10),
                EntryTag("Range", 1)));
        DepositRangeEnabled = config.Bind("Deposit Range", "Enabled", true,
            new ConfigDescription("Carts deposit matching cargo into Material Storages within range.", null,
                SectionTag("Deposit Range", 5), EntryTag("Enabled", 0)));
        DepositRange = config.Bind("Deposit Range", "Range", 2,
            new ConfigDescription("Deposit reach in tiles per side, 0 = vanilla (park on the storage).",
                new AcceptableValueRange<int>(0, 10),
                EntryTag("Range", 1)));
        ConnectRangeEnabled = config.Bind("Connect Range", "Enabled", true,
            new ConfigDescription("A free cart is pulled toward a cart the player is pulling once it is within range, so they connect without touching.", null,
                SectionTag("Connect Range", 6), EntryTag("Enabled", 0)));
        ConnectRange = config.Bind("Connect Range", "Range", 2,
            new ConfigDescription("Connect reach in tiles per side. 0 = vanilla (touch only).",
                new AcceptableValueRange<int>(0, 10),
                EntryTag("Range", 1)));
        BucketPriorityEnabled = config.Bind("Bucket Priority", "Enabled", true,
            new ConfigDescription("When taking an item off a cart, prefer grabbing an empty bucket over other cargo.", null,
                SectionTag("Bucket Priority", 2), EntryTag("Enabled", 0)));
        CartReleaseFixEnabled = config.Bind("Cart Release Fix", "Enabled", true,
            new ConfigDescription("Releasing a pulled cart with the interact key never grabs a different cart on the same press.", null,
                SectionTag("Cart Release Fix", 3), EntryTag("Enabled", 0)));
        StockpileRangeEnabled = config.Bind("Stockpile Range", "Enabled", true,
            new ConfigDescription("Carts take resources from building output stockpiles within range. Solid resources go into free slots, bucket resources fill empty buckets on the cart.", null,
                SectionTag("Stockpile Range", 7), EntryTag("Enabled", 0)));
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

    private static object EntryTag(string displayName, int order) {
        return new { DisplayName = displayName, Order = order };
    }
}

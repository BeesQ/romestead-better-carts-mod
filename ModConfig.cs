using BepInEx.Configuration;

namespace BetterCarts;

internal static class ModConfig {
    internal static ConfigEntry<bool> Enabled;
    internal static ConfigEntry<bool> ChainOverflowEnabled;
    internal static ConfigEntry<bool> DepositRangeEnabled;
    internal static ConfigEntry<int> DepositRange;
    internal static ConfigEntry<bool> CollectRangeEnabled;
    internal static ConfigEntry<int> CollectRange;

    internal static void Init(ConfigFile config) {
        Enabled = config.Bind("General", "Enabled", true,
            "Master on/off for the whole mod.");
        ChainOverflowEnabled = config.Bind("Chain Overflow", "Enabled", true,
            "When a full cart picks up an item, the item is passed to the next cart in the chain with a free slot.");
        CollectRangeEnabled = config.Bind("Collect Range", "Enabled", true,
            "Carts automatically pick up loose items within range.");
        CollectRange = config.Bind("Collect Range", "Range", 2,
            new ConfigDescription("Collect reach in tiles per side. 0 = vanilla (touch only).",
                new AcceptableValueRange<int>(0, 10)));
        DepositRangeEnabled = config.Bind("Deposit Range", "Enabled", true,
            "Carts deposit matching cargo into Material Storages within range.");
        DepositRange = config.Bind("Deposit Range", "Range", 2,
            new ConfigDescription("Deposit reach in tiles per side, 0 = vanilla (park on the storage).",
                new AcceptableValueRange<int>(0, 10)));
    }
}
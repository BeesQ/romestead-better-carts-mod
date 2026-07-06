using BepInEx.Configuration;

namespace BetterCarts;

internal static class ModConfig
{
    internal static ConfigEntry<bool> Enabled;
    internal static ConfigEntry<bool> ChainOverflowEnabled;
    internal static ConfigEntry<bool> DepositRangeEnabled;
    internal static ConfigEntry<int> DepositRange;
    internal static ConfigEntry<bool> CollectRangeEnabled;
    internal static ConfigEntry<int> CollectRange;

    internal static void Init(ConfigFile config)
    {
        Enabled = config.Bind("General", "Enabled", true,
            "Master on/off for the whole mod.");
        ChainOverflowEnabled = config.Bind("FeatureA", "ChainOverflowEnabled", true,
            "Toggle chain overflow on pickup.");
        DepositRangeEnabled = config.Bind("FeatureB", "DepositRangeEnabled", true,
            "Toggle ranged deposit into storage.");
        DepositRange = config.Bind("FeatureB", "DepositRange", 1,
            new ConfigDescription("Deposit reach in tiles per side. 0 = vanilla.",
                new AcceptableValueRange<int>(0, 10)));
        CollectRangeEnabled = config.Bind("FeatureB", "CollectRangeEnabled", true,
            "Toggle ranged collect of loose items.");
        CollectRange = config.Bind("FeatureB", "CollectRange", 1,
            new ConfigDescription("Collect reach in tiles per side. 0 = vanilla.",
                new AcceptableValueRange<int>(0, 10)));
    }
}

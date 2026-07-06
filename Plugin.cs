using BepInEx;
using BepInEx.NET.Common;
using HarmonyLib;

namespace BetterCarts;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public class BetterCartsPlugin : BasePlugin
{
    public const string PluginGuid = "com.beesq.romestead.bettercarts";
    public const string PluginName = "Better Carts";
    public const string PluginVersion = "1.0.0";

    public override void Load()
    {
        ModConfig.Init(Config);
        new Harmony(PluginGuid).PatchAll();
        Log.LogInfo(PluginName + " " + PluginVersion + " loaded.");
    }
}

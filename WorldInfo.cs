using System.Reflection;
using Candide.World;
using HarmonyLib;

namespace BetterCarts;

internal static class WorldInfo
{
    private static readonly FieldInfo WorldField = AccessTools.Field(typeof(ExteriorWorldHandler), "World");
    private static readonly PropertyInfo WorldProperty = AccessTools.Property(typeof(ExteriorWorldHandler), "World");

    internal static float TileSize
    {
        get
        {
            object worldObject = WorldField != null ? WorldField.GetValue(null) : WorldProperty?.GetValue(null);
            if (worldObject is ChunkedWorld world && world.TileSize.X > 0)
            {
                return world.TileSize.X;
            }
            return ChunkedWorld.DefaultWorld.TileSize.X;
        }
    }
}

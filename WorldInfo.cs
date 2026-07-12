using Candide.World;

namespace BetterCarts;

internal static class WorldInfo
{
    internal static float TileSize
    {
        get
        {
            ChunkedWorld world = ExteriorWorldHandler.World;
            if (world != null && world.TileSize.X > 0)
            {
                return world.TileSize.X;
            }
            return ChunkedWorld.DefaultWorld.TileSize.X;
        }
    }
}

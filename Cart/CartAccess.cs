using System;
using CandideServer.Entities.Controllers;
using HarmonyLib;
using Shared.Entity;

namespace BetterCarts;

internal static class CartAccess
{
    internal static readonly Func<ServerCart2Controller, EntityWrapper, bool> PickupEntity =
        AccessTools.MethodDelegate<Func<ServerCart2Controller, EntityWrapper, bool>>(
            AccessTools.Method(typeof(ServerCart2Controller), "PickupEntity"));
}

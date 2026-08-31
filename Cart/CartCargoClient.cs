using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Candide.Entities.Controllers.Other;
using Candide.GameModels;
using Candide.Sound;
using CandideCreator.Shared.Helpers;
using Microsoft.Xna.Framework;
using Shared.Entity;

namespace BetterCarts;

// the host runs a SEPARATE EntityWrapper per item on each side; a server-side pin is invisible to the client, which keeps simulating the item as loose cargo on the ground. This class is the client half of Cart Capacity - without it extras become ghost items
internal static class CartCargoClient {
    private const float RingRadius = 2.5f;
    private const float RingHeight = 6f;
    private const float RingJitter = 0.02f;
    private const int SeatsPerLayer = 4;
    private const float LayerStep = 6f;

    // the ring puts seats Right and Left at world Y 0, and a radius jitter cannot separate them because their offset is scaled by sin(angle), which is 0 there. This nudges each item along the DEPTH axis in world space, after the cart rotation, so the separation survives every cart facing. 0.02 matches the margin vanilla's own four items get from their radius jitter
    private const float DepthNudge = 0.02f;

    // vanilla emits this from Cart2Controller.UpdateSlot whenever a c1..c5 parameter changes to a new item, and nothing on the server plays it. Extras ride bc_cargo instead of cN, so UpdateSlot never sees them and the sound has to be replicated here or cargo past the vanilla slots lands silently
    private const string PickupSound = "event:/hits/impact/impact_storage";

    private static readonly ConditionalWeakTable<Cart2Controller, List<Guid>> Slots =
        new ConditionalWeakTable<Cart2Controller, List<Guid>>();

    private static readonly List<Guid> ReuseIncoming = new List<Guid>();
    private static readonly ConditionalWeakTable<Cart2Controller, string[]> LastRaw =
        new ConditionalWeakTable<Cart2Controller, string[]>();

    // Cart2Controller.OnServerSetState re-reads c1..c5 after every parameter sync; bc_cargo rides the same message
    internal static void SyncSlots(Cart2Controller cart) {
        List<Guid> slots = Slots.GetOrCreateValue(cart);
        var parameters = cart.Parameters;
        if (parameters == null) {
            return;
        }
        string stored = parameters.GetString(CartCargoSync.CargoKey, string.Empty);
        string[] previous = LastRaw.GetValue(cart, _ => new string[1]);
        if (string.Equals(previous[0], stored, StringComparison.Ordinal)) {
            return;
        }
        // the very first sync for a cart is adoption from the save, not a pickup - without this a cart loaded holding extras would fire one sound per item on world load
        bool firstSync = previous[0] == null;
        previous[0] = stored;
        ModLog.Advanced("CLIENT SYNC cart=" + cart.Entity.Id + " bc_cargo=\"" + stored + "\" had=" + slots.Count);
        CartCargoSync.Unpack(stored, ReuseIncoming);
        foreach (Guid id in slots) {
            if (!ReuseIncoming.Contains(id)) {
                ReleaseOne(cart, id);
            }
        }
        bool gained = false;
        foreach (Guid id in ReuseIncoming) {
            if (!slots.Contains(id)) {
                gained = true;
                break;
            }
        }
        slots.Clear();
        foreach (Guid id in ReuseIncoming) {
            slots.Add(id);
        }
        if (gained && !firstSync) {
            cart.Entity.EmitSoundOneShot(PickupSound);
        }
    }

    internal static void UpdateSlots(Cart2Controller cart) {
        // SyncSlots also runs from the OnServerSetState postfix; polling here as well catches any parameter change that arrives without it, and costs one string compare when nothing moved
        SyncSlots(cart);
        List<Guid> slots = Slots.GetOrCreateValue(cart);
        if (slots.Count == 0) {
            return;
        }
        for (int i = slots.Count - 1; i >= 0; i--) {
            if (!GameState.Entities.TryGetValue(slots[i], out EntityWrapper item)) {
                slots.RemoveAt(i);
                continue;
            }
            // the local player grabbed it; vanilla UpdateSlot defers to the player the same way, and the server drops it from bc_cargo on its next sweep
            if (item.CarrierId == GameState.LocalPlayer.EntityId) {
                ModLog.Advanced("CLIENT yield " + item.Id + " to local player");
                slots.RemoveAt(i);
                continue;
            }
            item.IsThrown = false;
            item.ThrowerId = null;
            item.NoEntityCollision = true;
            item.NoTerrainCollision = true;
            item.CarrierId = cart.Entity.Id;
        }
        for (int i = 0; i < slots.Count; i++) {
            if (GameState.Entities.TryGetValue(slots[i], out EntityWrapper item)) {
                Place(cart, item, i);
            }
        }
    }

    internal static void ReleaseAll(Cart2Controller cart) {
        List<Guid> slots = Slots.GetOrCreateValue(cart);
        foreach (Guid id in slots) {
            ReleaseOne(cart, id);
        }
        slots.Clear();
    }

    // the same reset vanilla's RemoveEntityFromCart performs, which is what stops a released item from staying collisionless
    private static void ReleaseOne(Cart2Controller cart, Guid id) {
        if (!GameState.Entities.TryGetValue(id, out EntityWrapper item)) {
            return;
        }
        item.NoEntityCollision = false;
        item.NoTerrainCollision = false;
        if (item.CarrierId == cart.Entity.Id) {
            item.CarrierId = null;
        }
    }

    // vanilla puts its four on the DIAGONALS of a ring of radius 2.5 at Z 6 (direction units 0.5, 1.5, 2.5, 3.5 of a 4-unit circle), so extras take the four CARDINALS and stack in layers above once those are used
    private static void Place(Cart2Controller cart, EntityWrapper item, int index) {
        int layer = index / SeatsPerLayer;
        int seat = index % SeatsPerLayer;
        float direction = seat;

        // the jitter is per ITEM, not per seat: seat 0 layer 0 and seat 0 layer 1 would otherwise share an X and Y and differ only in Z, which two sprites cannot be told apart by. Vanilla gets this free because its four sit on distinct diagonals
        Vector3 offset = DirectionToVector3(direction) * (RingRadius + index * RingJitter);
        offset.Z = RingHeight + layer * LayerStep;
        offset = VectorExtension.ToXzy(offset);

        Vector3 rotated;
        Vector3.Transform(ref offset, ref cart.Entity.MeshTransformMatrixRef, out rotated);

        item.Position = VectorExtension.ToXzy(rotated) + cart.Entity.Position
            + cart.Entity.Velocity * cart.Entity.Fdt
            + new Vector3(0f, index * DepthNudge, 0f);
        item.Velocity = cart.Entity.Velocity;
        item.System.CollisionGroup.UpdatePositionAndVelocity(item);
    }

    // CandideDirection8: Right 0, Down 1, Left 2, Up 3 - a full circle is 4, not 2 PI
    private static Vector3 DirectionToVector3(float direction) {
        float radians = direction * MathHelper.PiOver2;
        return new Vector3((float)Math.Cos(radians), (float)Math.Sin(radians), 0f);
    }
}

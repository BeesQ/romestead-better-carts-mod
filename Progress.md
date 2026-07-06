# Progress.md

Changelog + next actions for the Romestead "Better Carts" mod. Read after
Project_Brief.md and CLAUDE.md.

Status legend: ✅ done | ❌ to do | ⚠️ attention | 🔮 future / revisit later

## ✅ Done
- ✅ Chose loader: BepInEx 6 CoreCLR + HarmonyX.
- ✅ Confirmed engine: MonoGame / .NET (not Unity).
- ✅ Split the game into 3 assemblies (client Romestead.dll / server
      CandideServer.dll / shared Shared.dll) and got the full folder trees.
- ✅ Confirmed cart pickup/slot/chain/deposit is server-authoritative.
- ✅ Read the client cart controller (Cart2Controller) fully.
- ✅ Wrote the project docs: Project_Brief.md, DLL_REFERENCES.md, Progress.md,
      MODDING_ENV.md, and the earlier general guide
      (Romestead_Modding_Guide_Carts.md).
- ✅ Server (CandideServer.*) and shared (Shared.*) source indexed in Project
      Knowledge and searchable. ⚠️ EXCEPTION found at code-time: the
      MaterialStorage deposit files never surface in search (see Blockers).
- ✅ Confirmed the Feature A authority in ServerCart2Controller: CanBeAutoPicked /
      CanBePickedUp, HandleAutopickup, PickupEntity / TryPickupEntity (the c1..c5
      loop; c5 gated by worship_flag:cart_capacity), Attach / Detach /
      HandleConnectToOtherCart (the "following" chain via FollowingId +
      FollowerCartId).
- ✅ Read the FULL ServerCart2Controller body at code-time. New findings:
      - Server Update(GameTime) ALREADY runs a circular pickup scan every tick:
        GetEntitiesTouchingCircleArea(Position2, BoundingBox.Width/2 - 5, Z,
        Z + 8) with CanBePickedUp -> PickupEntity. So Feature B collect is a
        postfix on Update adding a second, wider scan (radius =
        CollectRange * TileSize) filtered by CanBeAutoPicked (stricter than the
        vanilla touch scan: rejects non-thrown Health entities, so the ranged
        scan does not vacuum animals).
      - Client Cart2Controller.OnEntityCollision returns true for ANY
        CanBeAutoPicked entity regardless of slot state, so NO client patch is
        needed for Feature A or Feature B collect (prediction never fights the
        server; slot state arrives via Parameters in OnServerSetState).
      - EntitySystem.GetEntityById(Guid?) is available server-side (used by
        ServerChairFurnitureController), so the chain walk resolves carts via
        Entity.System without touching ServerGameState directly.
- ✅ TileSize source confirmed at code-time: ChunkedWorld.TileSize (public
      Point; comes from server config via ExteriorWorldHandler.SyncFullGameState;
      engine default Point(16,16) in ChunkedWorld.DefaultWorld). The mod reads
      ExteriorWorldHandler.World at runtime with a DefaultWorld fallback -
      nothing hardcoded.
- ✅ Wrote the mod v0.9.0 "Better Carts" (new BetterCarts project):
      - Plugin skeleton: BasePlugin + [BepInPlugin] + Harmony(guid).PatchAll().
      - Config.Bind: full locked settings table (General.Enabled,
        FeatureA.ChainOverflowEnabled, FeatureB.DepositRangeEnabled/DepositRange,
        FeatureB.CollectRangeEnabled/CollectRange, ranges int 0..10 default 1).
      - Feature A: postfix on ServerCart2Controller.PickupEntity - when the hit
        cart is full, walk followers (FollowerCartId) then leaders (FollowingId,
        cart-type only), invoke each cart's own PickupEntity via an AccessTools
        delegate. Visited set + cap 16 + [ThreadStatic] re-entrancy guard.
        Reuses the game's own slot/capacity/c5 logic; no per-frame rescans
        (runs only when a pickup fails).
      - Feature B collect: postfix on ServerCart2Controller.Update - circular
        scan radius CollectRange * TileSize, same Z band as vanilla (Z..Z+8),
        CanBeAutoPicked filter, route through PickupEntity (chain overflow
        applies automatically), early break once the whole chain is full.
      - .csproj with all compile-time references (DLL_REFERENCES.md section 7),
        Private=false, GamePath property, optional CopyToGamePlugins target.

## 🔒 Locked decisions
- Mod name = "Better Carts" (standard BepInEx plugin, [BepInPlugin] guid + name +
  version). Nexus release planned for later.
- Loader = BepInEx 6 CoreCLR (Ice Box Studio build, Romestead Nexus mods/1).
- Features: A = chain overflow on pickup; B = Deposit Range + Collect Range. Both
  user-configurable in-game.
- Feature B range scale (applies to BOTH DepositRange and CollectRange):
  - Integer 0..10, default 1.
  - 0 = vanilla: no ranged behavior (collect needs physical collision; deposit
    needs the cart on the storage). At 0 the mod leaves vanilla behavior alone.
  - N (>=1) = an area N tiles on each side of the cart, i.e. the (2N+1) x (2N+1)
    tile grid centered on the cart. 1 -> 3x3, 2 -> 5x5, ... 10 -> 21x21.
- Feature B scan shape = CIRCULAR (the game's own convention for proximity).
  - Radius = N * World.TileSize (runtime value; never hardcode a tile size).
  - The circle sits inside the (2N+1) grid (value 1 = a circle that fits a 3x3
    grid, value 2 = 5x5, etc). Only the radius scales with N.
  - Use GetEntitiesTouchingCircleArea (server-side: CandideServer.BM/
    TryGetEntitiesInCircleArea). The exact radius constant (N vs N+0.5 tiles) can
    be nudged during testing to feel right; N * TileSize is the baseline.
- Hosting scopes for v1: singleplayer + multiplayer listen-server (host-driven:
  the host runs the authoritative CandideServer.dll in-process; joiners are
  client-only). One BepInEx install per machine.
- Tunable values via BepInEx Config.Bind. Mod Settings Menu shows them
  automatically; keep it a SOFT dependency (no [BepInDependency]) so Better Carts
  loads with or without it. For v1 use PLAIN Config.Bind (no MSM presentation
  metadata yet - that is a future item). Ranges: integer AcceptableValueRange(0, 10).
- Localization: English-only for v1 (no Localization API dependency).
- ASCII only in mod code and shipped mod files; reference game DLLs at compile
  time only. (Project Knowledge docs may use emoji.)

## ⚠️ Pending on user
- ❌ Provide the MaterialStorage source files (see Blockers) so Feature B deposit
      can be implemented without guessing method bodies.
- ❌ Plugin GUID author part: current placeholder "com.bettercarts.romestead" -
      swap in the author handle if wanted (one constant in Plugin.cs + rename of
      the generated .cfg).
- ❌ Confirm TargetFramework: net8.0 assumed. Check
      Romestead\Romestead.runtimeconfig.json ("tfm" / framework version) and
      adjust BetterCarts.csproj if it differs.

## 🔮 Future / revisit later
- 🔮 Feature B deposit ships as v1.0.0 once the storage source is readable
      (entry point + "must overlap" check -> extend reach by
      DepositRange * TileSize). Config keys are already bound in v0.9.0 so the
      user config file stays stable across the update.
- 🔮 Dedicated server: Romestead has no standalone dedicated-server binary today,
      so it is out of scope for v1. If the game later ships one, the same server
      patches apply; only the loader would need to target that server binary
      (the current community loader targets Romestead.exe).
- 🔮 Mod Settings Menu presentation metadata: nicer display names, description,
      icon, ordering, and Nexus link via soft anonymous tags. v1 ships plain
      Config.Bind (still auto-detected and shown by MSM).
- 🔮 Localization API (mods/53) integration: translated config labels via the API
      (hard dependency, 18 locales incl pl_PL). Defer to a later Nexus update.
- 🔮 Client-prediction polish for Feature B (items visibly sliding toward the cart).

## 🎯 Next for Claude (in order)
1. ❌ Read the deposit source once the user adds it to the decompiled-dll repo /
      Project Knowledge: ServerMaterialStorageStackController.cs,
      ServerMaterialStoragePitController.cs,
      ServerMaterialStorageFluidContainerController.cs,
      MaterialStorageController.cs, DepositEntityIntoStorageMessage.cs,
      ItemDepositFallbackStrategy.cs, LogisticalBuildingHelper.cs. Extract the
      deposit entry point + the current "must overlap" check.
2. ❌ Implement Feature B deposit (reach = DepositRange * TileSize) as a new
      patch file in BetterCarts, bump version to 1.0.0.
3. ❌ Nexus packaging: readme, requirements listing (BepInEx loader mods/1),
      zip layout, permissions note (no game binaries shipped).

## 🚨 Blockers
- ❌ MaterialStorage deposit source is NOT retrievable from Project Knowledge:
      8 targeted searches (exact class names, message name, helper names, and
      content-term variants) never surfaced those files, while other small
      CandideServer files (DetachEntityMessage, FurnitureLootedMessage,
      GetPickupFurnitureMessage) rank fine. Conclusion: those files are most
      likely missing from the index.
      USER ACTION: export the files listed in "Next for Claude" step 1 with
      dnSpyEx from CandideServer.dll and add them to the decompiled-dll repo /
      Project Knowledge.
      Per CLAUDE.md ("do not guess method bodies"), deposit is NOT implemented
      from guesswork; v0.9.0 binds the Deposit config keys (stable config file)
      and logs "not active yet" instead of patching blind.

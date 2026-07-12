<p align="center">
  <img src="https://github.com/BeesQ/romestead-better-carts-mod/blob/65600df9b08e004ccec391ff0c2d4c62f6e7ac38/packaging/assets/icon.png" width="256" alt="Better Carts icon">
</p>

# Better Carts

A [BepInEx 6 CoreCLR](https://www.nexusmods.com/romestead/mods/1) mod for **Romestead** that makes hauling with Carts more pleasant with quality-of-life features, all configurable in-game

## Features

- **Chain Overflow** - when a full Cart picks up an item, it is passed to the next Cart in the chain with a free slot. Nothing is left behind until every chained Cart is full
- **Bucket Priority** - prefer grabbing an empty Bucket when unloading a Cart
- **Cart Release Fix** - releasing a pulled Cart never grabs a different Cart on the same press
- **Collect Range** - Carts automatically pick up loose items within a configurable range (0-10 tiles, default 2). 0 = vanilla
- **Deposit Range** - Carts deposit matching cargo into Material Storages within range (0-10 tiles, default 2). 0 = vanilla
- **Connect Range** - free Carts are pulled toward a Cart the player is pulling once in range (0-10 tiles, default 2). 0 = vanilla
- **Stockpile Range** - Carts take resources from building Output stockpiles within range, into free slots and empty Buckets on the Cart (0-10 tiles, default 2). 0 = vanilla

Server-authoritative features: only the host needs the mod, joining players do not need anything installed. Bucket Priority and Cart Release Fix are client-side: they apply to each player that has the mod installed

## Configuration

Settings live under **Settings -> Mod Settings** in-game (via [Mod Settings Menu](https://www.nexusmods.com/romestead/mods/8)), or in `BepInEx/config/com.beesq.romestead.bettercarts.cfg`

| Section          | Key                                           | Default                 | Meaning                                                          |
| ---------------- | --------------------------------------------- | ----------------------- | ---------------------------------------------------------------- |
| General          | Enabled                                       | true                    | Master on/off for the whole mod                                  |
| Chain Overflow   | Enabled                                       | true                    | Pass overflow to the next chained Cart                           |
| Bucket Priority  | Enabled                                       | true                    | Prioritize grabbing an empty Bucket when unloading a Cart        |
| Cart Release Fix | Enabled                                       | true                    | Releasing a pulled Cart never grabs a different Cart             |
| Collect Range    | Enabled / Range                               | true / 2                | Ranged pickup of loose items (0-10 tiles, 0 = vanilla)           |
| Deposit Range    | Enabled / Range                               | true / 2                | Ranged deposit into Material Storages (0-10 tiles, 0 = vanilla)  |
| Connect Range    | Enabled / Range                               | true / 2                | Ranged Cart pulling (0-10 tiles, 0 = vanilla)                    |
| Stockpile Range  | Enabled / Range / While Pulled / While Parked | true / 2 / true / false | Take from Output stockpiles into Carts (0-10 tiles, 0 = vanilla) |

## Compatibility

- [Iron Cart](https://www.nexusmods.com/romestead/mods/92) by burdock12 - compatible, tested together

## Install (players)

Recommended: grab it from a mod site, which also lists the Requirements below for you

- **Nexus Mods**: https://www.nexusmods.com/romestead/mods/91
- **Thunderstore**: https://thunderstore.io/c/romestead/p/BeesQ/BetterCarts (supports Install with Mod Manager)

Manual (works with a build from any source, including this repo's [Releases](https://github.com/BeesQ/romestead-better-carts-mod/releases)):

1. Install the [Romestead BepInEx Mod Loader](https://www.nexusmods.com/romestead/mods/1) and [Mod Settings Menu](https://www.nexusmods.com/romestead/mods/8).
2. Drop `BetterCarts.dll` and `icon.png` into `Romestead/BepInEx/plugins/BetterCarts/`.
3. Launch the game through Steam.

## Build (developers)

Requires the .NET 8 SDK and a local Romestead install with the BepInEx loader already set up (the project references DLLs from the game folder)

1. Set the game path once, then restart your terminal/IDE:
   ```
   setx ROMESTEAD_PATH "C:\Path\To\steamapps\common\romestead"
   ```
2. Build:
   ```
   dotnet build -c Release
   ```

`BetterCarts.dll` is produced in `bin/Release/` and, by default, auto-copied into `BepInEx/plugins/BetterCarts` together with `icon.png`. Pass `-p:CopyToGamePlugins=false` to skip the copy, or `-p:GamePath="..."` to override the path for a single build

No game or loader assemblies are redistributed - they are referenced from your local install at compile time only

## License

Released under the [MIT License](LICENSE)

## Bug Reports and Feedback

Please [open an issue](https://github.com/BeesQ/romestead-better-carts-mod/issues) on this repo

## Links

- Nexus Mods: https://www.nexusmods.com/romestead/mods/91
- Thunderstore: https://thunderstore.io/c/romestead/p/BeesQ/BetterCarts
- More from me: https://solo.to/BeesQ

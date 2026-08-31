[![Release](https://img.shields.io/github/v/release/BeesQ/Romestead-BetterCarts-Mod?style=for-the-badge&logo=github&logoColor=white&color=blue "Latest release version&#10;Click to view GitHub Releases")](https://github.com/BeesQ/Romestead-BetterCarts-Mod/releases)
[![Romestead](https://img.shields.io/badge/Romestead-0.26.1__2-blue?style=for-the-badge&logo=steam&logoColor=white "Currently supported Romestead version&#10;Click to view Romestead on Steam")](https://store.steampowered.com/app/1805320/Romestead)
[![Thunderstore Downloads](https://img.shields.io/thunderstore/dt/BeesQ/BetterCarts?style=for-the-badge&logo=thunderstore&logoColor=white&color=brightgreen "Total downloads from Thunderstore&#10;Click to view Better Carts on Thunderstore")](https://thunderstore.io/c/romestead/p/BeesQ/BetterCarts)
[![License](https://img.shields.io/github/license/BeesQ/Romestead-BetterCarts-Mod?style=for-the-badge&logo=github&logoColor=white&color=orange "Project license&#10;Click to view LICENSE.txt")](LICENSE.txt)

<p align="center">
  <img src="https://raw.githubusercontent.com/BeesQ/Romestead-BetterCarts-Mod/939b0f4339ef7f592d6611eab6e476e13f23dcda/packaging/assets/banner.gif" width="100%" alt="Better Carts banner">
</p>

A [BepInEx 6 CoreCLR](https://www.nexusmods.com/romestead/mods/1) mod for game **[Romestead](https://store.steampowered.com/app/1805320/Romestead)** that makes hauling with Carts more pleasant with quality-of-life features, all configurable in-game

## Features

- **Chain Overflow** - when a full Cart picks up an item, it is passed to the next Cart in the chain with a free slot. Nothing is left behind until every chained Cart is full
- **Bucket Priority** - prefer grabbing an empty Bucket when unloading a Cart
- **Cart Release Fix** - releasing a pulled Cart never grabs a different Cart on the same press
- **Cart Capacity** - set how many items the vanilla Carts can carry (0-64, default 4). The Mercury cart-capacity blessing adds a configurable bonus on top (0-64, default 1), and Eject Overflow drops anything above the limit beside the Cart when a world loads. Modded Cart types are not covered
- **Cart Overlays** - the item count is shown above a Cart carrying more than 5 items, and a Cart that comes loose from the chain by itself says so. Counts for ordinary capacity and for empty Carts can be turned on too
- **Collect Range** - Carts automatically pick up loose items within a configurable range (0-10 tiles, default 2). 0 = vanilla
- **Deposit Range** - Carts deposit matching cargo into Material Storages within range (0-10 tiles, default 2). 0 = vanilla
- **Connect Range** - free Carts are pulled toward a Cart the player is pulling once in range (0-10 tiles, default 2). 0 = vanilla
- **Stockpile Range** - Carts take resources from building Output stockpiles within range, into free slots and empty Buckets on the Cart (0-10 tiles, default 2). 0 = vanilla

## Configuration

All settings (master toggle, per-feature toggles etc.) are configured from the **Mod Settings** button in the main menu (added by **Mod Settings Menu**), or in **BepInEx/config/com.beesq.romestead.bettercarts.cfg**

| Section          | Key                                                                   | Default                            | Meaning                                                          |
| ---------------- | --------------------------------------------------------------------- | ---------------------------------- | ---------------------------------------------------------------- |
| General          | Enabled                                                               | true                               | Master on/off for the whole mod                                  |
| Chain Overflow   | Enabled                                                               | true                               | Pass overflow to the next chained Cart                           |
| Bucket Priority  | Enabled                                                               | true                               | Prioritize grabbing an empty Bucket when unloading a Cart        |
| Cart Release Fix | Enabled                                                               | true                               | Releasing a pulled Cart never grabs a different Cart             |
| Cart Capacity    | Enabled / Eject Overflow / Blessing Bonus / Wooden Cart / Bronze Cart | true / true / 1 / 4 / 4            | Per-Cart-type carry capacity for the vanilla Carts (0-64)        |
| Cart Overlays    | Enabled / Show Above Vanilla Capacity / Show For Vanilla Capacity / Show For Empty Carts / Disconnect Message | true / true / false / false / true | Cargo count above a Cart, and a message when a Cart disconnects  |
| Collect Range    | Enabled / Range                                                       | true / 2                           | Ranged pickup of loose items (0-10 tiles, 0 = vanilla)           |
| Deposit Range    | Enabled / Range                                                       | true / 2                           | Ranged deposit into Material Storages (0-10 tiles, 0 = vanilla)  |
| Connect Range    | Enabled / Range                                                       | true / 2                           | Ranged Cart pulling (0-10 tiles, 0 = vanilla)                    |
| Stockpile Range  | Enabled / Range / While pulled / While parked                         | true / 2 / true / false            | Take from Output stockpiles into Carts (0-10 tiles, 0 = vanilla) |

## Requirements

- [BepinEx 6 For Romestead (Mod Loader)](https://www.nexusmods.com/romestead/mods/1) by Ice Box Studio
- [Mod Settings Menu (Settings Menu)](https://www.nexusmods.com/romestead/mods/8) by Ice Box Studio

## Multiplayer

**Server-side** - only the host needs the mod, joining players do not need anything installed

- Chain Overflow
- Cart Capacity - the host's values apply to everyone, but each player needs the mod to SEE cargo beyond the vanilla slots
- Collect Range
- Deposit Range
- Connect Range
- Stockpile Range

**Client-side** - applies to each player that has the mod installed

- Bucket Priority
- Cart Release Fix
- Cart Overlays - the cargo count appears for every player with the mod, and the disconnect message is shown to whoever was pulling the Cart

## Compatibility

- [Iron Cart](https://www.nexusmods.com/romestead/mods/92) by burdock12 - compatible but not supported by the Cart Capacity feature
- [Cart Capacity](https://www.nexusmods.com/romestead/mods/34) by Specsfo/Encordeo - not compatible

## Install

Recommended: grab it from a mod site, which also lists the Requirements above for you

- **Nexus Mods**: https://www.nexusmods.com/romestead/mods/91
- **Thunderstore**: https://thunderstore.io/c/romestead/p/BeesQ/BetterCarts (supports Install with Mod Manager)

Manual, from this repo's [Releases](https://github.com/BeesQ/Romestead-BetterCarts-Mod/releases):

1. Install the [Romestead BepInEx Mod Loader](https://www.nexusmods.com/romestead/mods/1) and [Mod Settings Menu](https://www.nexusmods.com/romestead/mods/8).
2. Open the zip and drop the `BetterCarts` folder, found under `BepInEx/plugins/`, into your own `Romestead/BepInEx/plugins/`.
3. Launch the game through Steam.

## Build (developers)

Requires the .NET 8 SDK and a local Romestead install with the BepInEx 6 CoreCLR loader already set up (the project references DLLs from the game folder)

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

## Notes

- The mod stores nothing in your save unless a Cart actually carries more than the game allows on its own - more than 4 items, or more than 5 with the Mercury blessing
- Eject Overflow is the only part of the mod that moves your cargo: on world load, a Cart over its capacity drops the surplus beside itself
- High capacity values can cause stutter and stack cargo into a tall tower above the Cart
- Before removing the mod, change Carts Capacity and Mercury's blessing values back to vanilla, leave Eject Overflow on, and load each affected world once so extra cargo is released
- Deposit Range takes only matching resources from Cart cargo
- Stockpile Range takes only from Output stockpiles, a building's input storage is never drained

## Bug Reports and Feedback

Please submit through GitHub Issues on this repo

## Credits

Thanks to [Beartwigs](https://beartwigs.com) for creating [Romestead](https://store.steampowered.com/app/1805320/Romestead)

## AI Disclosure

- Claude AI does the writing - the code and all the text that comes with it
- I write the rules it follows: what gets built, how it works, how it reads
- I test every build

## License

Released under the [MIT License](LICENSE.txt)

## Links

- Nexus Mods: https://www.nexusmods.com/romestead/mods/91
- Thunderstore: https://thunderstore.io/c/romestead/p/BeesQ/BetterCarts
- More from me: https://solo.to/BeesQ

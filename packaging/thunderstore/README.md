[![Release](https://img.shields.io/github/v/release/BeesQ/Romestead-BetterCarts-Mod?style=for-the-badge&logo=github&logoColor=white&color=blue "Latest release version&#10;Click to view GitHub Releases")](https://github.com/BeesQ/Romestead-BetterCarts-Mod/releases)
[![Romestead](https://img.shields.io/badge/Romestead-0.25.1__12-blue?style=for-the-badge&logo=steam&logoColor=white "Currently supported Romestead version&#10;Click to view Romestead on Steam")](https://store.steampowered.com/app/1805320/Romestead)
[![Thunderstore Downloads](https://img.shields.io/thunderstore/dt/BeesQ/BetterCarts?style=for-the-badge&logo=thunderstore&logoColor=white&color=brightgreen "Total downloads from Thunderstore&#10;Click to view Better Carts on Thunderstore")](https://thunderstore.io/c/romestead/p/BeesQ/BetterCarts)
[![License](https://img.shields.io/github/license/BeesQ/Romestead-BetterCarts-Mod?style=for-the-badge&logo=github&logoColor=white&color=orange "Project license&#10;Click to view LICENSE.txt")](https://raw.githubusercontent.com/BeesQ/Romestead-BetterCarts-Mod/939b0f4339ef7f592d6611eab6e476e13f23dcda/LICENSE.txt)

<p align="center">
  <img src="https://raw.githubusercontent.com/BeesQ/Romestead-BetterCarts-Mod/939b0f4339ef7f592d6611eab6e476e13f23dcda/packaging/assets/banner.gif" width="100%" alt="Better Carts banner">
</p>

Better Carts makes hauling with Carts more pleasant with quality-of-life features, all configurable in-game

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

## Requirements

- [BepInExPack_Romestead (Mod Loader)](https://thunderstore.io/c/romestead/p/Romestead_Modding/BepInExPack_Romestead) by Ice Box Studio
- [ModSettingsMenu (Settings Menu)](https://thunderstore.io/c/romestead/p/Ice_Box_Studio_Romestead/ModSettingsMenu) by Ice Box Studio

They are installed automatically as dependencies by your mod manager

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

Automatic (recommended): click Install with Mod Manager on this page, works with Thunderstore Mod Manager, r2modman, or Gale

Manual: download the zip and extract its contents into Romestead/BepInEx/plugins/

## Notes

- The mod stores nothing in your save unless a Cart actually carries more than the game allows on its own - more than 4 items, or more than 5 with the Mercury blessing
- Eject Overflow is the only part of the mod that moves your cargo: on world load, a Cart over its capacity drops the surplus beside itself
- High capacity values can cause stutter and stack cargo into a tall tower above the Cart
- Before removing the mod, change Carts Capacity and Mercury's blessing values back to vanilla, leave Eject Overflow on, and load each affected world once so extra cargo is released
- Deposit Range takes only matching resources from Cart cargo
- Stockpile Range takes only from Output stockpiles, a building's input storage is never drained

## Bug Reports and Feedback

Please submit through GitHub Issues on the source repo (link below)

## AI Disclosure

- Claude AI does the writing - the code and all the text that comes with it
- I write the rules it follows: what gets built, how it works, how it reads
- I test every build

## Links

- Source code (MIT): https://github.com/BeesQ/Romestead-BetterCarts-Mod
- Also on Nexus Mods: https://www.nexusmods.com/romestead/mods/91
- More from me: https://solo.to/BeesQ

Better Carts makes hauling with Carts more pleasant with quality-of-life features, all configurable in-game

## Features

- **Chain Overflow** - when a full Cart picks up an item, it is passed to the next Cart in the chain with a free slot. Nothing is left behind until every chained Cart is full
- **Bucket Priority** - prefer grabbing an empty Bucket when unloading a Cart
- **Cart Release Fix** - releasing a pulled Cart never grabs a different Cart on the same press
- **Cart Capacity** - set how many items the vanilla Carts can carry (0-64, default 4). The Mercury cart-capacity blessing adds a configurable bonus on top (0-64, default 1), and Eject Overflow drops anything above the limit beside the Cart when a world loads. Modded Cart types are not covered
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

Server-authoritative features: only the host needs the mod, joining players do not need anything installed. Bucket Priority and Cart Release Fix are client-side: they apply to each player that has the mod installed

Cart Capacity is server-authoritative: the host's values apply to everyone, but each player needs the mod to SEE cargo beyond the vanilla slots

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

## Links

- Source code (MIT): https://github.com/BeesQ/romestead-better-carts-mod
- Also on Nexus Mods: https://www.nexusmods.com/romestead/mods/91
- More from me: https://solo.to/BeesQ

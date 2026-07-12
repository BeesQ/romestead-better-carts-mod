Better Carts makes hauling with Carts more pleasant with quality-of-life features, all configurable in-game

## Features

- **Chain Overflow** - when a full Cart picks up an item, it is passed to the next Cart in the chain with a free slot. Nothing is left behind until every chained Cart is full
- **Bucket Priority** - prefer grabbing an empty Bucket when unloading a Cart
- **Cart Release Fix** - releasing a pulled Cart never grabs a different Cart on the same press
- **Collect Range** - Carts automatically pick up loose items within a configurable range (0-10 tiles, default 2). 0 = vanilla
- **Deposit Range** - Carts deposit matching cargo into Material Storages within range (0-10 tiles, default 2). 0 = vanilla
- **Connect Range** - free Carts are pulled toward a Cart the player is pulling once in range (0-10 tiles, default 2). 0 = vanilla
- **Stockpile Range** - Carts take resources from building Output stockpiles within range, into free slots and empty Buckets on the Cart (0-10 tiles, default 2). 0 = vanilla

## Configuration

All settings (master toggle, per-feature toggles etc.) are in-game under Settings -> Mod Settings, or in BepInEx/config/com.beesq.romestead.bettercarts.cfg

## Requirements

- [BepInExPack_Romestead (Mod Loader)](https://thunderstore.io/c/romestead/p/Romestead_Modding/BepInExPack_Romestead)
- [ModSettingsMenu (Settings Menu)](https://thunderstore.io/c/romestead/p/Ice_Box_Studio_Romestead/ModSettingsMenu)

They are installed automatically as dependencies by your mod manager

## Multiplayer

Server-authoritative features: only the host needs the mod, joining players do not need anything installed. Bucket Priority and Cart Release Fix are client-side: they apply to each player that has the mod installed

## Compatibility

- [Iron Cart](https://www.nexusmods.com/romestead/mods/92) by burdock12 - compatible, tested together

## Install

Automatic (recommended): click Install with Mod Manager on this page, works with Thunderstore Mod Manager, r2modman, or Gale

Manual: download the zip and extract its contents into Romestead/BepInEx/plugins/

## Notes

- Safe to add or remove at any time, the mod stores nothing in your save
- Deposit Range takes only matching resources from Cart cargo
- Stockpile Range takes only from Output stockpiles, a building's input storage is never drained

## Bug Reports and Feedback

Please submit through GitHub Issues on the source repo (link below)

## Links

- Source code (MIT): https://github.com/BeesQ/romestead-better-carts-mod
- Also on Nexus Mods: https://www.nexusmods.com/romestead/mods/91
- More from me: https://solo.to/BeesQ

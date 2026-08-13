## 1.3.0

- Added Cart Capacity: set how many items the vanilla Carts can carry (0-64, default 4). Modded Cart types are not covered
- Added Blessing Bonus: how much the Mercury cart-capacity blessing adds on top of a Cart's base capacity (0-64, default 1)
- Added Eject Overflow (on by default): on world load, a Cart over its capacity drops the surplus beside itself
- Stockpile Range now fills a Cart up to its configured capacity instead of stopping at the vanilla slots
- Items collected beyond the vanilla slots now play the Cart pickup sound
- Settings toggles are now labelled by what each feature does instead of "Enabled"
- Corrected the settings location: Mod Settings is its own button in the main menu, not a page inside Settings

## 1.2.0

- Added Cart Release Fix: releasing a pulled Cart never grabs a different Cart on the same press (client-side, applies per player with the mod)
- Added Stockpile Range: Carts take resources from building Output stockpiles within range - solid resources go into free slots, bucket resources fill empty Buckets on the Cart (0-10 tiles, default 2). 0 = vanilla. While Pulled / While Parked toggles decide when it runs; Chain Overflow applies to taken items
- Added Mod Settings Menu integration: named and ordered settings, mod icon, Nexus link, and an update check when Mod Settings Menu is installed. The mod still works without it
- The mod now ships icon.png next to BetterCarts.dll

## 1.1.0

- Added Bucket Priority: prefer grabbing an empty Bucket when unloading a Cart
- Added Connect Range: free Carts are pulled toward a Cart the player is pulling once in range (0-10 tiles, default 2). 0 = vanilla

## 1.0.0

- Initial release: Chain Overflow, Collect Range, Deposit Range - all configurable in-game

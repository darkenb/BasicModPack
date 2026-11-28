# Changelog

## 1.2.0

- New switch **Auto replenish**: the arrow button on drone, vessel and bot slots is on for every logistics station and distributor: new, copied and loaded. Only the planet you are on is filled from your inventory, one building after another.
- New switch **Distributor item from depot**: a copied distributor placed on a depot that holds items picks its item from the depot instead of keeping the copied one.
- New switch **Control panel copy/paste**: a copy/paste button on control panel rows of stations, distributors and advanced mining machines copies settings, item slots and filters onto other buildings of the same type, one by one; right click on any of these buttons cancels the copy.

## 1.1.2

- Best chain recipe: an input could be given a recipe that needs an item made from that input, for example tar from coal and hydrogen while the hydrogen was made from tar. The replicator could then not make the input at all: with GenesisBook, deuterium for plasma capsules showed 0 although it could be made from crude oil. Such loops are no longer chosen.

## 1.1.1

- Best chain recipe: inputs the replicator makes on the way now count when recipes are compared. Enriched stone bricks are chosen when you carry stone that can be crushed into dust, not only when you carry the dust itself.
- Storage limit in range: the slot box no longer shows the name of the last opened station.

## 1.1.0

- New switch **Best chain recipe** (Always on, ON by default): when the replicator makes a missing input along the way, it uses the recipe that can make the most of it from what you carry, among every recipe that makes it, other mods' included. Dark Fog drops are spent on basic materials only, never on finished products.

## 1.0.0

- First release: pack panel (Ctrl+Shift+B) in two parts.
- One-time actions, each with Apply, plus Apply all: upgrade belts and sorters, fill stations with drones, vessels and warpers, fuel generators, clear ruins, set storage limits, all within construction range.
- Always on, each with ON/OFF: pick up drops in range (Apply while off), station templates, power warning, station stock warning, build counter.
- Texts in English, Turkish and Chinese, chosen on the panel.

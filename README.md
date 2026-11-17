# BUG_BasicModPack

Small quality-of-life features in one mod. Press **Ctrl+Shift+B** in game to open the pack panel. It has two parts: **One-time actions**, each with an Apply button and one Apply all button for the whole list, and **Always on** features, each with an ON/OFF switch. Switches apply at once and are kept in the config file. The EN / TR / 中文 buttons in the title bar switch the pack's texts between English (default), Turkish and Chinese.

## One-time actions

All of them work on the buildings within your mecha's construction range, so mods that widen the range widen them too. **Apply all** runs every action and shows the results in one message (Storage limit only when a number is typed in).

- **Upgrade in range**: every belt and sorter becomes the best tier you carry, the same way the game's upgrade tool does it: the new item comes from your inventory and the old one goes back to it. Only unlocked items are used and nothing is downgraded. If your inventory fills up, the upgrade stops there.
- **Fill stations in range**: logistics drones, logistics vessels and space warpers from your inventory into the stations, up to each station's capacity. The game itself does this only when a station is built. Vessels and warpers go to interstellar stations only, warpers once warp drive is researched.
- **Fuel generators in range**: tops up the fuel of thermal power plants, mini fusion power plants, artificial stars and other burning generators to one full stack. A generator that holds no fuel gets the fitting fuel you carry the most of.
- **Clear ruins in range**: removes the rubble that destroyed buildings leave behind, and Dark Fog base remains that the game allows to clear (as when a blueprint is placed on them). Ruins hold no items, so nothing comes back.
- **Storage limit in range**: type the number of open slots in the box, then Apply: every storage gets its red limit bar at that slot. 0 removes the limit.

## Always on

- **Pick up drops in range**: items lying on the ground within your construction range fly to you, by the game's own pickup (which on its own reaches only 50 m). Switched ON, it picks up by itself twice a second and follows the item filter like the game's automatic pickup: only items set to be picked up. Switched OFF, its Apply button picks up once everything the filter does not exclude, as clicking an item does; Apply all includes it then. Nothing is picked up while the inventory is full. The **Item filter** buttons in the same card set the game's item filter for every item in one click: **Pick all** (every item picked up automatically), **Pick none** (no item picked up, not even by hand), **Invert** (items picked up automatically are no longer picked up, all others are) and **Random** (each item at random, picked up automatically or not at all). They change the same filter as the game's Item Filter window, so the setting is kept in the save.
- **Station templates**: while a logistics station window is open, a small "Station templates" window lets you save the station's settings under a name and apply them to another station of the same type with one click. Drag it by its title to wherever you like: it opens there next time. With three or more templates, the – button next to the close button folds the list to one row; a long list scrolls with the mouse wheel. The close button hides it until you close the station window. A template holds the slot items, local/remote modes, limits, slot directions, drone and vessel ranges, warp settings, charging power and auto-replenish settings, the same data the game's own copy and paste carries. Items no longer kept in a slot are handed back to you, as when you change a slot by hand. Templates are shared by all saves and stored in `BepInEx/config/BUG_BasicModPack.templates.txt`. Orbital collectors and advanced mining machines are not covered.
- **Power warning**: a message and a sound when a power network on the planet you are on meets less than 90% of its demand. It warns again only after the network has recovered to 95%.
- **Station stock warning**: names the interstellar station that has run out of vessels, or of warpers while it still has vessels. Checked every five seconds on all planets; each station is reported once until it is restocked. Stations that never had vessels are reported once when a save is loaded.
- **Build counter**: while building, a line beside the cursor shows how many of the held building you carry; while dragging a belt line, also how many pieces it takes and how many you are short.
- **Best chain recipe**: when the replicator is missing an input, it makes it along the way (chain production) with the one recipe the game links to that item. Switched ON, that recipe is instead the one whose inputs you carry enough of to make the most of the item (inventory, delivery slots and what the queue has left over), chosen among every unlocked recipe that makes it and can be made in the replicator: the game's, and those of any mod that adds recipes, BUG mods included. When you carry the inputs of none of them, the game's own recipe is kept. Dark Fog drops are only spent on basic items (raw resources and materials such as ingots, graphite or refined liquids): a recipe that turns them into a finished product, such as Dyson sphere parts (frame material and solar sails included, whatever type a mod gives them), fuel rods, ammunition, combat units or buildings, is never chosen. The same rule picks the recipes of that recipe's inputs, down the chain. Each choice depends only on what you carry, so it changes only when your stock does. Items an order needs are taken from the inventory when the order is placed, as the game always does, so they never count for another order. The choice is renewed as your inventory changes and stays fixed while one order is planned, so the replicator's counts, its red marks and the queue always agree. A recipe that uses the item itself is never chosen. Switched OFF, every item gets back its own recipe at once.

## Configuration

`BepInEx/config/bug.dsp.basicmodpack.cfg`: the panel hotkey, each switch, the storage limit value, the language, where the panel and the template window were left and whether it is folded.

## Compatibility

No other mods are needed. The pack adds no items, recipes or buildings, so it can be removed at any time without harming saves. Best chain recipe changes only which recipe the replicator uses for an input, never a recipe itself, and is not stored in the save.

## Building from source

You need a current .NET SDK and the .NET Framework 4.7.2 targeting pack. The game, Unity and BepInEx assemblies are referenced from your own installation and are not part of this repository.

```text
dotnet build -c Release "-p:DSPGamePath=D:\Games\Dyson Sphere Program" "-p:BepInExCorePath=C:\path\to\BepInEx\core"
```

The output is `bin/Release/net472/BUG_BasicModPack.dll`. Copy it to `BepInEx/plugins/BUG_BasicModPack/`.

## License

MIT, see [LICENSE](LICENSE).

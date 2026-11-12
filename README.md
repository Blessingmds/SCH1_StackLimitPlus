# 🧱 StackLimit Plus

**Stack sizes, cash and station limits for Schedule I, all set in-game.**

StackLimit Plus lets you decide how much fits in a slot: for every item, per category, per item, per place, or for one specific rack or station. Press **F6** and change it on the spot. No config files, no restarts.

👉 [NexusMods page](https://www.nexusmods.com/schedule1/mods/623)

---

## ✨ Features

### 🎛 In-game menu (F6)
Open it anywhere. Changes apply right away and are saved for you.

- **Items**: one stack limit for every item, a limit per category (like product), or a limit per item. Search or filter by type (weed, meth, storage, tools, …) to find one item fast. Unstackable items can be included too.
- **Places**: choose where your limits apply: pockets, storage, vehicle trunks (with a separate setting per vehicle type if you want), delivery vans, employees and dealers, and each station (drying rack, brick press, cauldron, chemistry station, lab oven, mixing station, packaging station). Each place uses your item limits, the vanilla size, or one custom number. Presets: *Pockets only*, *Everywhere except stations*, *Everywhere*.
- **Cash**: how much cash fits in one storage slot, and the ATM weekly deposit limit.
- **Stations**: how many items a drying rack works on at once, and how much a mixing station mixes in one go.
- **Settings**: change the menu key, replay the tutorial, or reset everything back to vanilla.

### 🎯 This object
Look at a rack, station or storage and you'll see **[F6] Change limit** right under the game's own prompt. Press it to set limits for **just that one object**: its slot stack size and, for racks and mixers, its capacity. A worker's bed gets its own cash-per-slot limit for their cash case.

### 📱 Phone app
StackLimit Plus is also an app on your in-game phone, so you can check and change your limits without the hotkey.

### 🧭 Tutorial
New to the mod? The first time you open the menu, a short hands-on tour walks you through it. You change a limit, find an item, set up one object and open the phone app. Anything you change during the tour is undone at the end. Skip it any time, replay it from Settings, or press **?** on any page for a tour of just that page.

### 👥 Multiplayer
The host's settings are sent to everyone in the lobby, so all players use the same limits. Clients can view them but not change them. Your own menu key and window size stay yours.

---

## 📦 Installation

1. Install [MelonLoader](https://melonwiki.xyz/) (0.7 or newer) for Schedule I.
2. Download the file for your game branch: **IL2CPP** for the normal game, **Mono** for the `alternate` beta branch.
3. Extract it into your `Schedule I` folder. It puts `SCH1_StackLimitPlus.dll` in `Mods/`, and MDSAPI (which it needs, included in the download) in `Plugins/` and `Mods/MDSAPI/`.
4. Start the game, load a save and press **F6**.

Settings are saved to `Schedule I/UserData/StackLimitPlus.json`.

When a new version is out, a notice on the main menu tells you; click it to open the download page. MDSAPI checks GitHub for this at most once a day and never downloads anything itself. Don't want it? Click **Opt out of automated checks** on the notice.

### Upgrading from 1.x
Your old stack limit and hotkey are carried over automatically on first launch.

---

## ❓ FAQ

**My items didn't change in storage.**
Check the **Places** tab: storage may be set to *Vanilla*. Use the *Everywhere* preset to apply your limits everywhere.

**I only want bigger stacks in my pockets.**
Use the *Pockets only* preset in **Places**.

**How do I go back to normal?**
**Settings → Reset everything** puts every limit back to vanilla. Your menu key stays.

**My mixed products won't stack higher than vanilla.**
Update to 2.0.1. Older versions sometimes missed mixes when a save loaded, until you changed a setting.

---

## 📝 Changelog

### 2.1.1
- **New:** runs on the `alternate` (Mono) beta branch as well as the normal game.
- **Changed:** built on MDSAPI, which is included in the download. When a game update moves something the mod relies on, the fix lands in MDSAPI and the mod keeps working. If a part does break, the log names it and the rest keeps running.
- **New:** a notice on the main menu tells you when an update is available.
- **Includes** the 2.0.1 fixes for mixed products.
- Your settings file carries over as-is.

### 2.0.1
- **Fixed:** mixed products sometimes stayed at the vanilla stack size after loading a save. New mixes now get your limits right away, both when a save loads and when you make one.
- **Fixed:** a mix made after the game had loaded could keep a stack size you had already lowered, and ignored places set to *Vanilla*.
- **Fixed:** an error in the log on every save load.

---

## 🔓 Open source

Bug reports and pull requests are welcome.

**Credits:** BlessingMds, Prowiler

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
2. Drop `SCH1_StackLimitPlus.dll` into `Schedule I/Mods/`.
3. Start the game, load a save and press **F6**.

Settings are saved to `Schedule I/UserData/StackLimitPlus.json`.

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

---

## 🔓 Open source

Bug reports and pull requests are welcome.

**Credits:** BlessingMds, Prowiler

# Acts from the Past: Config Sub-Mod

A small companion mod for **Acts from the Past** (Slay the Spire 2) that adds the configuration the base mod is missing:

* **Enemy scaling sliders, per player count** (solo / 2 / 3 / 4 players) for
  * enemy **HP**
  * enemy **attack damage**
  * enemy **Block**
  * enemy **buffs** (the ones the game scales in co-op: Regen, Metallicize, Plated Armor, ...)
  * **debuffs put on players** (Weak, Vulnerable, Frail, ...)
* The HP slider also covers the two Acts from the Past enemies that scale themselves (Awakened One's phase 2 HP
  and the Transient's damage).
* Sliders show the **total multiplier** compared to a solo enemy, and their defaults are the base game's own
  co-op values (e.g. 2 players = HP x2.20), so you can see what the game does today and simply lower it.
* **Co-op safe:** the host's settings are sent to everyone in the lobby automatically, so all players use the same numbers.
* By default the sliders only apply inside the Slay the Spire 1 acts (Exordium, The City, The Beyond); one
  switch makes them apply everywhere.
* **Shop music fix:** shops inside the Slay the Spire 1 acts play the regular Slay the Spire 2 merchant music
  (choose per-act matching music or a fixed act's music). Acts from the Past 1.0.7+ in this repo fixes shop
  music on its own too, so you can switch this option off if you prefer the built-in version.

**Requires:** Slay the Spire 2 beta v0.113.0+, BaseLib 3.4.8+, and Acts from the Past 1.0.8+ (the version in this repo).

Everything is in **Settings → Mod Configuration → "Acts from the Past: Config Sub-Mod"**.

---

## Installing - easiest way (no Visual Studio)

1. Unzip the `ActsFromThePastConfig` folder somewhere easy, e.g. your Desktop.
2. Double-click **`BUILD.bat`**. A black window opens - you don't need to type anything.
   The first time it downloads Microsoft's free .NET 9 SDK (about 200 MB) into a `.dotnet` folder next to the file.
3. When it says **DONE**, the mod is already in `Slay the Spire 2\mods\ActsFromThePastConfig`. Press any key to close.
   If it says **BUILD FAILED**, take a screenshot of the window and open an issue on this repository.

If Windows shows "Windows protected your PC", click **More info → Run anyway** (the file is plain text; you can
open it in Notepad to see exactly what it does).

## Installing with Visual Studio (alternative)

The mod is C# code, so it has to be turned into a `.dll` once on your PC. Visual Studio does this with one click
and even copies the finished mod into the game's `mods` folder for you.

### 1. Things you need first

1. **Slay the Spire 2** installed through Steam (the beta branch, same as Acts from the Past requires).
2. Subscribed on the Steam Workshop to **BaseLib** and **Acts from the Past** (let Steam finish downloading them).
3. **Visual Studio 2022 Community** (free): <https://visualstudio.microsoft.com/vs/community/>
   * Run the installer. On the **Workloads** page tick **".NET desktop development"**, then click **Install**.
   * That workload includes the .NET 9 SDK the mod needs. (If your Visual Studio is older than version 17.12,
     use *Help → Check for Updates* first.)

### 2. Build the mod

1. Put the `ActsFromThePastConfig` folder somewhere easy, e.g. `Documents\ActsFromThePastConfig`.
2. Double-click **`ActsFromThePastConfig.sln`**. Visual Studio opens.
3. In the menu bar click **Build → Build Solution** (or press `Ctrl + Shift + B`).
4. Watch the **Output** window at the bottom. When it ends with
   `Build: 1 succeeded` you are done - it also printed a line like
   `Copying ActsFromThePastConfig.dll and ActsFromThePastConfig.json to C:\...\Slay the Spire 2\mods\ActsFromThePastConfig\`.
5. Start the game, enable the mod in the mod list if it isn't already, and open
   **Settings → Mod Configuration → Acts from the Past: Config Sub-Mod**.

You only have to build again if you change the code or if a big game update breaks the mod.

### If the build complains

| Message | What to do |
|---|---|
| `Slay the Spire 2 was not found at ...` | Open `Directory.Build.props` in Notepad, remove the `<!--` and `-->` around the `<Sts2Path>` line and put in your own game folder (the folder containing `mods`). Use forward slashes `/`. Save, then build again. |
| `BaseLib.dll was not found` | Make sure you are subscribed to BaseLib on the Workshop and Steam has finished downloading it (start the game once). If you installed BaseLib manually instead, its `BaseLib.dll` must be in `Slay the Spire 2\mods\BaseLib\`. |
| `Access to the path ... is denied` while copying | Close Visual Studio, right-click its shortcut and choose **Run as administrator**, then build again. |
| Red squiggles in the code *before* building | Normal - they disappear after the first build. Only the **Output** window result matters. |

Build problems that mention a specific method name (for example `does not contain a definition for ...`) mean the
game or BaseLib changed their code since this mod was written; the error names exactly what to look at.

---

## How the settings work

* **Only change enemies in Acts from the Past acts** (default ON) - sliders apply only in Exordium / The City / The Beyond.
  Turn OFF to apply them in every act.
* **Scale up slightly in later acts** (default ON) - the base game scales co-op enemies by x1.1 per player in Act 1,
  x1.2 in Act 2 and x1.3 for the Act 3 boss. With this ON your sliders are the *Act 1* values and later acts rise by
  the same proportion. OFF uses the exact slider value everywhere.
* **Per-player-count sections** - the section matching the number of players in the run is used
  (4 players is also used for larger modded lobbies). The *Solo* section is at the bottom and left at x1.00.
* **Base game reference values (Act 1):**

  | Players | HP | Block | Buffs | Damage | Debuffs |
  |---|---|---|---|---|---|
  | 2 | x2.20 | x2.00 | x2.20 | x1.00 | x1.00 |
  | 3 | x3.30 | x3.30 | x3.30 | x1.00 | x1.00 |
  | 4 | x4.40 | x4.40 | x4.40 | x1.00 | x1.00 |

  *Restore Defaults* at the bottom of the page puts everything back to these.

* **Co-op:** every player needs this mod installed (the game checks). The host's settings are what count - they are
  sent when the run starts or loads, and again if the host changes a slider during the run. To be safe, change
  settings between fights rather than in the middle of one.

---

## For the curious: what the mod patches

| Game method | What we do |
|---|---|
| `Creature.ScaleHpForMultiplayer` | Replace the co-op HP result with `base HP x slider` |
| `Creature.ScaleMonsterHpForMultiplayer` | Solo HP slider (the game skips scaling at 1 player) |
| `MultiplayerScalingModel.ModifyBlockMultiplicative` | Replace the enemy block multiplier |
| `PowerModel.GetScaledAmountForMultiplayer` (and its overrides) | Re-scale the extra buff amount the game added |
| `Hook.ModifyDamage` | Multiply enemy base attack damage |
| `Hook.ModifyPowerAmountReceived` | Multiply debuffs enemies apply to players |
| `AwakenedOne.Phase2Hp` (Acts from the Past) | Phase 2 HP follows your HP slider (AFTP sets it by hand, bypassing the game's scaling) |
| `Transient.CurrentAttackDamage` (Acts from the Past) | AFTP scales Transient's damage like HP in co-op; it now follows your HP slider |
| `MultiplayerScalingModel.GetMultiplayerScaling` | Only *read*, to reproduce the game's act-to-act progression |
| `NRunMusicController.UpdateTrack / UpdateMusic / StopMusic / ToggleMerchantTrack` | Shop music fix |
| `RunManager.SetUpNewMultiplayer / SetUpSavedMultiplayer` | Host sends its settings to the lobby |
| `LocManager.GetTable` | Injects the settings text so no `.pck` file is required |

Each patch is applied separately and logs an error instead of crashing if the game changes a method.

## Compatibility notes

* Built and checked against **Slay the Spire 2 v0.113.0 (beta)** and **BaseLib 3.4.8**: every patched game method,
  its parameter names and types, and every private field the mod reads were verified against that game build.
* Built for the Acts from the Past version in this repository (1.0.8). The patches that touch Acts from the Past
  directly (Awakened One, Transient, its shop music) look their targets up by name; if one is missing they log a
  warning in the game's log and switch themselves off instead of crashing.
* Every player in a co-op lobby needs the same version of this mod.

## Credits

* Acts from the Past by Cany0udance.
* BaseLib by Alchyr (config UI, FMOD helpers, co-op messages).
* `Sts2PathDiscovery.props` adapted from the ScalingMod template (MIT).

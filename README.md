# Acts From The Past (updated fork) + Config Sub-Mod

This is a fork of [Acts From The Past](https://github.com/Cany0udance/ActsFromThePast) by **Cany0udance**, kept up to
date with the latest **beta branch** of Slay the Spire 2. The original repository no longer receives releases, so this
fork carries the fixes needed for newer game versions, plus a companion **Config Sub-Mod** that adds the multiplayer
scaling options the base mod lacks.

| | |
|---|---|
| **Acts From The Past version** | 1.0.9 |
| **Config Sub-Mod version** | 1.1.0 |
| **Slay the Spire 2** | v0.113.0 (beta branch) |
| **Requires** | [BaseLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3737335127) 3.4.8 or newer |

All credit for the acts, enemies, events, art and audio goes to Cany0udance and the translators listed below. This
fork only changes code so it keeps working (and the sub-mod is new work on top).

## What's new in this fork

### 1.0.9: Dread and Blitz modes
**Dread:** each Slay the Spire 1 boss now has its own *dominion*, the effect that a Dread boss spreads to every other
fight in its act (shown on the boss icon at the top of the screen). They are themed on the boss and sized like the
base game's dominions for the same act:

| Act | Boss | Dominion |
|---|---|---|
| Exordium | Slime Boss | Each combat, shuffle 2 **Slimed** into your draw pile |
| Exordium | Hexaghost | Each combat, shuffle 2 **Burns** into your draw pile |
| Exordium | The Guardian | Enemies have 2 **Thorns** |
| The City | The Collector | Each combat, start with 2 **Weak**, 2 **Vulnerable** and 2 **Frail** |
| The City | The Champ | Enemies have 2 **Strength** |
| The City | Bronze Automaton | Enemies have 2 **Artifact** |
| The Beyond | Awakened One | Enemies have 2 **Curiosity** (gain 2 Strength whenever you play a Power) |
| The Beyond | Time Eater | The toughest enemy in each combat has **Time Warp** |
| The Beyond | Donu and Deca | Enemies have 1 **Strength** and 4 **Plating** |

Like the base game's bosses, the Slay the Spire 1 bosses are also tougher in Dread: +10% max HP.

**Blitz:** the acts, enemies and events work with Blitz's shorter map. Two fixes: the Secret Portal (Rebalanced Mode)
no longer replaces the map in Blitz, which would have stopped Blitz from moving you along it (it teleports you to the
boss instead), and The Box's free purchase now also works in Blitz shops.

### 1.0.8: Slay the Spire 2 v0.113.0 (beta)
The November 12 beta patch (The Fool, Blitz and Dread) renamed or reshaped a lot of game code. Fixed:
* Events and enemies that remove max HP, hand out relics, or react to deaths (Vampires, Council of Ghosts, Golden
  Idol, Winding Halls, Moai Head, Purifier, Transmogrifier, Big Fish, Scrap Ooze, Dead Adventurer, The Mausoleum,
  Pleading Vagrant, Cursed Tome, We Meet Again, Hexaghost, and the Constricted, Life Link, Spore Cloud and Unawakened
  powers).
* Dead or fleeing enemies becoming unclickable (Darklings, Gremlin Leader's gremlins, Looter, Mugger).
* Necronomicurse's transform sound, and The Box's free purchase in shops.
* Two map-generation patches (Darv, shrine events) that would have stopped the whole mod from loading, and
  Nloth's Gift's rarity patch.

### 1.0.7: Slay the Spire 2 v0.111.0
* **Shop and rest-site music works again.** The game renamed the function that loads its music, so these rooms were
  silent. They now play the regular game music, matched to the act (Exordium uses Act 1's music, The City Act 2's,
  The Beyond Act 3's).
* Lagavulin's "asleep" music check, the end-of-combat music handling (Mind Bloom, boss victory) and a duplicated shop
  price patch for The Box.

## Config Sub-Mod

The [`ActsFromThePastConfig`](ActsFromThePastConfig) folder is a separate, optional mod that adds an in-game settings
page (**Settings → Mod Configuration → "Acts from the Past: Config Sub-Mod"**):

* **Enemy multiplayer scaling sliders, per player count** (solo, 2, 3 and 4 players) for enemy **HP**, **attack
  damage**, **Block**, **buffs** (Regen, Metallicize, Plated Armor and the other buffs the game scales in co-op) and
  **debuffs put on players** (Weak, Vulnerable, Frail...). Each slider shows the total multiplier compared to a solo
  run, and the defaults are the base game's own values (for example 2 players = HP ×2.20), so you can see what the
  game does and turn it down.
* The HP slider also covers the two Acts From The Past enemies that do their own scaling: **Awakened One's phase 2
  HP** and the **Transient's damage**.
* **Only in Acts From The Past acts** (on by default): the sliders apply only in Exordium, The City and The Beyond.
  Switch it off to apply them in every act.
* **Scale up in later acts** (on by default): keeps the base game's small act-to-act increase on top of your values.
* **Co-op safe:** the host's settings are sent to every player automatically, so everyone uses the same numbers.
* An optional shop music fix for the Slay the Spire 1 acts (no longer needed with 1.0.7+, but available).

Every player in a co-op lobby needs both mods. See the [sub-mod's README](ActsFromThePastConfig/README.md) for all
the details and build instructions.

## Installing

1. Make sure **BaseLib** is subscribed on the Steam Workshop and up to date, and that Slay the Spire 2 is on the
   **beta branch** (Steam → right-click the game → Properties → Betas).
2. If you are subscribed to the original Acts From The Past on the Workshop, unsubscribe, and delete any old
   `ActsFromThePast` folder in your game's `mods` folder. Two copies of the same mod confuse the game.
3. Put the `ActsFromThePast` folder (and `ActsFromThePastConfig`, if you want it) in
   `Slay the Spire 2\mods`.

**Note on building:** this repository holds the code. Acts From The Past also needs its `ActsFromThePast.pck` file
(art, sounds, animations, text), which is not stored here; it comes from the original release. The Config Sub-Mod is
code only and builds into a single `.dll`.

---

## About Acts From The Past

Acts From The Past is a work-in-progress port of the Acts from Slay the Spire 1, including Exordium, The City, and The Beyond.

<img width="1369" height="770" alt="image" src="https://github.com/user-attachments/assets/ad195a97-9743-435c-8107-a6fa4c242568" />

This mod includes ALL events from the first game besides A Note For Yourself.

<img width="1369" height="770" alt="image" src="https://github.com/user-attachments/assets/76f0bb64-0f6d-41c6-8833-62322f85d733" />

Some enemies have been given visual & auditory touchups to help them feel a bit more fresh in the new game environment.

<img width="1369" height="770" alt="image" src="https://github.com/user-attachments/assets/3a74cc2e-afcd-4616-9db9-f729147533b7" />

### Rebalanced Mode

Rebalanced Mode is a config (disabled by default) that changes Slay the Spire 1 events to bring them up to Slay the Spire 2 standards. The goals of this mode include removing ALL leave buttons and generally buffing events to make them more positive on average.

<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/30d7f286-888c-4364-a964-cd1863c1b49c" />

<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/9e374caa-02c7-4155-966f-8994fb56e71e" />

(oh yeah, and this mod also has [Googly Eyes](https://steamcommunity.com/sharedfiles/filedetails/?id=3745864998) support)

---

## Credits

* **Cany0udance**: Acts From The Past.
* **Nitablade** (Russian), **snumodder** (Korean), **Sashimi_knife** (Japanese), **floca** (Brazilian Portuguese),
  **jjkkllike, ImLingCheng & Entropy ManG0** (Simplified Chinese): translations.
* **flounder**: art for the Blood Bank relic.
* **Alchyr**: [BaseLib](https://github.com/Alchyr/BaseLib-StS2).

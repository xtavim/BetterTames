# BetterTames

A taming overhaul. Tames recover instead of dying, come with you through portals and into dungeons, teleport back to you when they get lost, gain stars from kills and eat from your chests. Adds follow/stay all keys, taming, breeding and growth timers, and settings for taming time, growth time and breeding. Works with every tameable creature, modded ones included, and syncs with ServerSync.

## Features

### 🛡️ Protection

- A tame that would die lies down and recovers instead **[Configurable]**
- It gets up at full health after 60 seconds **[Configurable]**
- While recovering it takes no damage, enemies ignore it, and it doesn't eat or breed
- A butcher knife still kills it
- **Left Alt + E** turns protection on or off for the tame you are looking at **[Configurable]**
- The name above its health bar is blue when protected, yellow while recovering

### ❤️ Healing

- Tames heal from empty to full in 1 hour **[Configurable]**

### 🌀 Teleport

- A following tame teleports to you when it falls 30m behind **[Configurable]**
- When you die, every tame that was following you teleports to where you respawn
- **Left Alt + H** teleports every follower to you **[Configurable]**
- Following tames come with you into dungeons and back out. Big tames, like lox, wait outside **[Configurable]**
- Tames near you come through portals with you **[Configurable]**

### 🐺 Commands

- **Left Alt + F**: tames within 30m follow you **[Configurable]**
- **Left Alt + G**: tames following you stay **[Configurable]**
- Any tame can be told to follow or stay, not only the ones the game allows **[Configurable]**

### 🥩 Taming

- Every creature takes 30 minutes to tame, modded ones included **[Configurable]**

### 🐣 Breeding

- A tame gets pregnant after 3 love points **[Configurable]**
- Breeding stops when 6 of the same kind are within 10m **[Configurable]**
- Offspring grow up in 30 minutes **[Configurable]**
- A newborn has a 25% chance to be one star above its parent, up to 2 stars **[Configurable]**
- A recovering tame doesn't breed, and doesn't count as a partner

### ⭐ Stars

- A tame that kills an enemy has a 10% chance to gain a star, up to 2 stars **[Configurable]**
- +5% for each star the enemy has over the tame **[Configurable]**
- A tame that gains a star is healed to full **[Configurable]**

### 🍖 Feeding

- Hungry tames eat from player-built chests within 10m when there is no food on the ground **[Configurable]**
- Creatures you are taming do the same **[Configurable]**
- Only eat from chests that hold nothing but food they eat **[Configurable]**
- Dungeon and world chests are never touched

### ⏱️ Timers

- Time left to tame, shown as paused while the creature is hungry or frightened **[Configurable]**
- Time left in a pregnancy, or the tame's love points **[Configurable]**
- Time left before offspring grow up **[Configurable]**
- Time left for a recovering tame

### ⚔️ Tames fight their own kind

- A tamed wolf fights wild wolves that are attacking. The game normally keeps them on the same side **[Configurable]**
- Calm wild ones, and ones you are taming, are left alone

## Installation

Install with your mod manager, or drop `BetterTames.dll` into `BepInEx/plugins`.

## Configuration

Everything below can be changed in-game with Configuration Manager, or in `BepInEx/config/xtav1m.BetterTames.cfg`.

### General

| Setting | Default | What it does |
| --- | --- | --- |
| Lock Configuration | On | Only server admins can change the settings |
| Tames Fight Their Own Kind | On | Tames fight wild creatures of their own kind that are attacking |

### Timers

| Setting | Default | What it does |
| --- | --- | --- |
| Show Taming Timer | On | Time left to tame |
| Show Breeding Timer | On | Time left in a pregnancy, or love points |
| Show Growth Timer | On | Time left to grow up |

### Taming

| Setting | Default | What it does |
| --- | --- | --- |
| Taming Time | 30 | Minutes of feeding to tame a creature |

### Breeding

| Setting | Default | What it does |
| --- | --- | --- |
| Love Points Needed | 3 | Love points before a tame gets pregnant |
| Growth Time | 30 | Minutes for offspring to grow up |
| Crowd Limit | 6 | Breeding stops when this many of the same kind are nearby |
| Crowd Range | 10 | Meters the crowd limit counts |
| Offspring Star Chance | 25 | Percent chance a newborn gets an extra star |

### Commands

| Setting | Default | What it does |
| --- | --- | --- |
| Command Any Tame | On | Any tame can follow or stay |
| Follow All Key | Left Alt + F | Tames near you follow you |
| Stay All Key | Left Alt + G | Tames following you stay |
| Follow/Stay All Range | 30 | Meters those keys reach |

### Teleport

| Setting | Default | What it does |
| --- | --- | --- |
| Teleport Lost Tames | On | Followers teleport to you when they fall behind or you respawn |
| Teleport Distance | 30 | Meters a follower can fall behind |
| Teleport Tames Key | Left Alt + H | Teleport every follower to you |
| Big Tames Enter Dungeons | **Off** | On lets big tames into dungeons too, where they can get stuck |
| Tames Follow Through Portals | On | Followers near a portal come through it with you |

### Leveling

| Setting | Default | What it does |
| --- | --- | --- |
| Gain Stars From Kills | On | Tames can gain a star from kills |
| Star Chance | 10 | Percent chance per kill |
| Bonus Chance Per Star | 5 | Percent added per star the enemy has over the tame |
| Heal On New Star | On | Heal to full on a new star |

### Health

| Setting | Default | What it does |
| --- | --- | --- |
| Full Heal Time | 3600 | Seconds to heal from empty to full |
| Protect All Tames | On | Tames recover instead of dying |
| Recovery Time | 60 | Seconds a tame recovers before getting up |
| Protection Key | Left Alt + E | Turn protection on or off for one tame |
| Show Recovering Tames | On | Show a recovering tame's name and health bar when you look at it |

### Feeding

| Setting | Default | What it does |
| --- | --- | --- |
| Wild Creatures Eat From Chests | On | Creatures you are taming eat from chests |
| Tames Eat From Chests | On | Tames eat from chests |
| Chest Range | 10 | Meters a creature reaches for a chest |
| Only Food Chests | Off | Only eat from chests that hold nothing but food it eats |

## Compatibility

- Requires **BepInExPack Valheim 5.4.2350** or newer.
- In multiplayer, every player should install it. A creature is run by the player nearest to it, and only gets these features if that player has the mod.
- Install it on the server to sync settings with ServerSync. The server's values win while **Lock Configuration** is on.
- Safe to remove. Tames keep their stars, and everything else goes back to vanilla.

## Planned for 1.0

- 🎯 A key to send your tames after what you are looking at

## Changelog

See [CHANGELOG.md](changelog) for version history.

## Credits

- **ServerSync** - server-authoritative configuration syncing, merged into the plugin DLL.

## License

This mod is provided as-is for the Valheim community.

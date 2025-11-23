# KillaDome Plugin Suite

A full COD-style Rust server experience with modular plugin architecture. Now split into two plugins for better maintainability and separation of concerns.

## 🎮 Plugin Architecture

### KillaDome.cs (Main Plugin - 2,460 lines)
The core game logic plugin that handles:
- Player progression and weapon systems
- Blood Token economy
- Match management and queuing
- Data persistence and player profiles
- Weapon/attachment systems
- VFX/SFX management
- Store API and purchase logic

### KillaUI.cs (UI Plugin - 1,092 lines)
The user interface plugin that provides:
- Lobby UI with 5 tabs (Play, Loadouts, Store, Stats, Settings)
- Drag-and-drop loadout editor
- Store interface for weapons, skins, and outfits
- Stats display and settings menu
- All UI console commands

## 📦 Installation

1. Place **both** `KillaDome.cs` and `KillaUI.cs` in your Oxide plugins folder
2. (Optional) Install [ImageLibrary](https://umod.org/plugins/image-library) plugin for weapon/skin images
3. Configure image URLs in the `GunConfig` section of KillaDome.cs
4. Restart server or reload plugins

**Important:** KillaUI depends on KillaDome and will not function without it. Load order doesn't matter as Oxide handles plugin dependencies automatically.

## 🔧 Recent Updates: Modular Architecture

### What Changed?

The plugin has been refactored from a single 4,222-line file into two focused plugins:

- **KillaDome.cs**: 2,460 lines (42% smaller) - Game logic only
- **KillaUI.cs**: 1,092 lines - UI rendering only

This separation provides:
- ✅ **Better Maintainability** - Edit UI without touching game logic
- ✅ **Independent Reloading** - Reload UI plugin without restarting game systems
- ✅ **Team Development** - Multiple developers can work simultaneously
- ✅ **Cleaner Code** - Well-defined plugin communication interface

## 🎯 How to Add New Weapons

#### Step 1: Add Gun Definition
Navigate to the `GunConfig` class (around line 68) and add your gun to the `Guns` dictionary:

```csharp
["newgun"] = new GunDefinition
{
    Id = "newgun",                          // Internal ID (lowercase, no spaces)
    DisplayName = "New Gun Display Name",    // Name shown to players
    RustItemShortname = "item.shortname",    // Actual Rust item shortname
    ImageUrl = "https://i.imgur.com/image.png" // Direct URL to gun image
}
```

**Result:** Gun automatically appears in the Loadout Tab for player selection!

#### Step 2: Add Skin(s) for the Gun
In the same `GunConfig` class, add skin(s) to the `Skins` list:

```csharp
new SkinDefinition
{
    Name = "Cool Skin Name",                     // Display name
    SkinId = "skin_id_or_workshop_id",           // Rust skin ID
    WeaponId = "newgun",                         // Must match gun Id above
    ImageUrl = "https://i.imgur.com/skin.png",   // Preview image URL
    Cost = 450,                                  // Price in Blood Tokens
    Tag = "NEW",                                 // Badge: "NEW", "POPULAR", "HOT", or ""
    Rarity = "Epic"                              // "Common", "Rare", "Epic", "Legendary"
}
```

**Result:** Skin automatically appears in the Store Tab with correct pricing, tag, and rarity!

### Example: Adding Thompson

The plugin now includes a Thompson example:

**Gun Definition:**
```csharp
["thompson"] = new GunDefinition
{
    Id = "thompson",
    DisplayName = "Thompson",
    RustItemShortname = "smg.thompson",
    ImageUrl = "https://i.imgur.com/YourThompsonImage.png"
}
```

**Skin Definition:**
```csharp
new SkinDefinition
{
    Name = "Thompson Dragon",
    SkinId = "skin_thompson_dragon",
    WeaponId = "thompson",
    ImageUrl = "https://i.imgur.com/YourThompsonDragonSkin.png",
    Cost = 550,
    Tag = "HOT",
    Rarity = "Epic"
}
```

### Centralized Configuration Benefits

✅ **Single Source of Truth:** Edit guns/skins in ONE place only
✅ **Automatic Updates:** Changes instantly reflect in all tabs
✅ **No Code Duplication:** Maintainable and clean
✅ **Predictable Behavior:** Explicit pricing, tags, and rarity
✅ **Easy to Extend:** Just add entries to the config dictionaries

### Property Defaults

When adding skins, these are the default values if not specified:
- `Cost`: 300
- `Tag`: "" (no tag)
- `Rarity`: "Common"

### Technical Details

- **KillaUI** reads from KillaDome's `_gunConfig` via public API methods
- **Store Tab** dynamically displays weapons/skins from centralized config
- **Loadout Tab** uses `GetAllGunIds()` to show available weapons
- **Plugin Communication** via Oxide's Call() method for inter-plugin communication
- Tag badges only display if tag is not null/empty/whitespace
- All changes are automatically saved to player profiles via KillaDome

## 🔌 Plugin Communication

KillaUI communicates with KillaDome using Oxide's plugin reference system:

```csharp
// In KillaUI.cs
[PluginReference]
private Plugin KillaDome;

// Example: Call KillaDome API
KillaDome?.Call("CycleWeapon", player, "primary", 1);
var sessionData = KillaDome?.Call("GetSessionData", player.userID);
```

### Public API Methods (KillaDome)

KillaDome exposes these methods for KillaUI to call:
- `AddToQueue(ulong steamId)` - Add player to match queue
- `CycleWeapon(BasePlayer player, string slot, int direction)` - Change weapon selection
- `PurchaseItem(ulong steamId, string itemId, int cost)` - Purchase store items
- `PurchaseArmor(ulong steamId, string itemShortname, int cost)` - Purchase armor
- `ApplySkin(ulong steamId, string weapon, string skinId)` - Apply weapon skin
- `ApplyAttachment(ulong steamId, string weapon, string slot, string attachmentId)` - Apply attachment
- `CycleArmor(ulong steamId, string slot, int direction)` - Change armor selection
- `GetSessionData(ulong steamId)` - Get player session data
- `GetCurrentLoadout(ulong steamId)` - Get player's current loadout
- `GetWeaponInfo(string weaponId)` - Get weapon display information
- `GetAvailableGuns()` - Get list of available weapons for store
- `GetPlayerProfile(ulong steamId)` - Get player profile stats

## 📋 Configuration

The plugin creates a `KillaDome.json` config file with settings for:
- Starting Blood Tokens
- Tokens per kill
- Spawn positions
- Tebex integration
- And more...

## 💻 Commands

**Player Commands:**
- `/kd` - Show help
- `/kd open` - Open lobby UI (requires KillaUI plugin)
- `/kd stats` - View your stats
- `/dice <bet>` - Play dice mini-game
- `/kdlobby set` - (Admin) Set lobby spawn position
- `/kdspawn set <number>` - (Admin) Set arena spawn position

**Admin Console Commands:**
- `kd.open` - Open lobby UI for yourself
- `kd.start` - Start a match
- `kd.giveskin <steamid> <skinid>` - Give skin to player
- `kd.resetprogress <steamid>` - Reset player progress

**UI Console Commands** (handled by KillaUI plugin):
- `killadome.close` - Close the UI
- `killadome.tab <tabname>` - Switch to a specific tab
- `killadome.weapon.next/prev <slot>` - Cycle weapons
- `killadome.purchase <itemid> <cost>` - Purchase item
- And many more for loadout/store interactions

## 🐛 Troubleshooting

### KillaUI not working
- **Problem:** UI doesn't appear or console shows "KillaUI plugin not loaded"
- **Solution:** Ensure both KillaDome.cs and KillaUI.cs are in your plugins folder and loaded

### Images not showing
- **Problem:** Weapon/skin images are blank in the UI
- **Solution:** 
  1. Install the [ImageLibrary](https://umod.org/plugins/image-library) plugin
  2. Verify image URLs in `GunConfig` are accessible
  3. Check Oxide logs for image loading errors

### Plugin load order issues
- **Problem:** One plugin loads before the other causing errors
- **Solution:** Oxide handles plugin dependencies automatically. Just ensure both plugins are present.

## 🔄 Migrating from Single-File Version

If upgrading from the old single-file KillaDome.cs (4,222 lines):

1. **Backup** your existing KillaDome.cs and player data
2. **Replace** the old KillaDome.cs with the new version
3. **Add** KillaUI.cs to your plugins folder
4. **Reload** plugins: `oxide.reload KillaDome` then `oxide.reload KillaUI`
5. **Test** that UI works by running `/kd open`

Player data is fully compatible - no migration needed!

## 📝 Support

For issues or questions:
1. Check that both plugins are loaded: `oxide.plugins`
2. Verify Gun IDs match between Guns dictionary and Skins list
3. Ensure Image URLs are accessible
4. Check Rust item shortnames are correct
5. Verify SkinId values are unique
6. Review Oxide console logs for errors

## 🎨 Customization

### Adding New UI Tabs
Edit `KillaUI.cs` - Add new tab buttons in `ShowLobbyUIWithTab()` and create rendering methods

### Modifying Game Logic
Edit `KillaDome.cs` - All game systems are isolated from UI code

### Changing UI Colors/Layout
Edit `KillaUI.cs` - All CUI elements are in the tab rendering methods

---

**Version:** 1.0.0  
**Architecture:** Modular (2 plugins)  
**Author:** KillaDome Dev Team  
**Total Lines:** 3,552 (KillaDome: 2,460 + KillaUI: 1,092)

# KillaDome Plugin

A full COD-style Rust server experience plugin with lobby, loadouts, and progression.

## Recent Updates: Auto-Update Store Tab Feature

### What Changed?

The Store Tab now **automatically updates** when you add guns or skins to the centralized `GunConfig` section. No more editing multiple places in the code!

### How to Add New Weapons

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

- **Store Tab** reads from `_gunConfig.Skins` dynamically
- **Loadout Tab** reads from `_gunConfig.GetAllGunIds()` dynamically  
- **GiveWeapon** method uses `_gunConfig.Guns` to map IDs to Rust item shortnames
- Tag badges only display if tag is not null/empty/whitespace
- All changes are automatically saved to player profiles

### Installation

1. Place `KillaDome.cs` in your Oxide plugins folder
2. (Optional) Install ImageLibrary plugin for weapon/skin images
3. Configure image URLs in the `GunConfig` section
4. Restart server or reload plugin

### Configuration

The plugin creates a `KillaDome.json` config file with settings for:
- Starting Blood Tokens
- Tokens per kill
- Spawn positions
- Tebex integration
- And more...

### Commands

**Player Commands:**
- `/kd` - Show help
- `/kd open` - Open lobby UI
- `/kd stats` - View your stats
- `/dice <bet>` - Play dice mini-game

**Admin Commands:**
- `kd.open` - Open lobby UI
- `kd.start` - Start a match
- `kd.giveskin <steamid> <skinid>` - Give skin to player
- `kd.resetprogress <steamid>` - Reset player progress

### Support

For issues or questions about the auto-update Store Tab feature, check:
1. Gun IDs match between Guns dictionary and Skins list
2. Image URLs are accessible
3. Rust item shortnames are correct
4. SkinId values are unique

---

**Version:** 1.0.0  
**Author:** KillaDome Dev Team

/*
 * KillaDome.cs - Full COD-Style Rust Server Experience Plugin
 * 
 * Features:
 * - Lobby system with comprehensive UI (Play/Loadouts/Store/Stats tabs)
 * - Drag-and-drop loadout editor with image library
 * - Persistent weapon progression and attachment upgrades
 * - Custom VFX/SFX for bullets and attachments
 * - Store integration (Tebex-compatible)
 * - High performance, GC-friendly architecture
 * 
 * Version: 1.0.0
 * Author: KillaDome Dev Team
 */

using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;

namespace Oxide.Plugins
{
    [Info("KillaDome", "KillaDome", "1.0.0")]
    [Description("Full COD-style server experience with lobby, loadouts, and progression")]
    public class KillaDome : RustPlugin
    {
        #region Fields
        
        [PluginReference]
        private Plugin ImageLibrary;
        
        private DomeManager _domeManager;
        private LobbyUI _lobbyUI;
        private LoadoutEditor _loadoutEditor;
        private AttachmentSystem _attachmentSystem;
        private WeaponProgression _weaponProgression;
        private VFXManager _vfxManager;
        private SFXManager _sfxManager;
        private ForgeStationSystem _forgeStation;
        private BloodTokenEconomy _tokenEconomy;
        private StoreAPI _storeAPI;
        private SaveManager _saveManager;
        private AntiExploit _antiExploit;
        private TelemetrySystem _telemetry;
        
        private PluginConfig _config;
        private GunConfig _gunConfig;
        private OutfitConfig _outfitConfig;
        private Dictionary<ulong, PlayerSession> _activeSessions = new Dictionary<ulong, PlayerSession>();
        
        private const string PERMISSION_ADMIN = "killadome.admin";
        private const string PERMISSION_VIP = "killadome.vip";
        
        #endregion
        
        #region Gun & Image Configuration
        
        /// <summary>
        /// CENTRALIZED GUN AND IMAGE CONFIGURATION
        /// This is the ONLY place you need to add/edit guns and their images!
        /// 
        /// AUTOMATIC FEATURES:
        /// - When you add a new gun to Guns dictionary, it automatically appears in the Loadout Tab
        /// - When you add a new skin to Skins list, it automatically appears in the Store Tab
        /// - No need to edit any other code - everything updates automatically!
        /// 
        /// Changes here automatically apply to both Store Tab and Loadout Tab.
        /// </summary>
        public class GunConfig
        {
            // ===== GUNS CONFIGURATION =====
            // Add or modify guns here. Each gun needs:
            // - Id: Internal identifier (lowercase, no spaces)
            // - DisplayName: Name shown to players
            // - RustItemShortname: The actual Rust item shortname
            // - ImageUrl: Direct URL to the gun's image
            
            public Dictionary<string, GunDefinition> Guns = new Dictionary<string, GunDefinition>
            {
                ["ak47"] = new GunDefinition
                {
                    Id = "ak47",
                    DisplayName = "AK-47",
                    RustItemShortname = "rifle.ak",
                    ImageUrl = "https://i.imgur.com/YourAK47Image.png"
                },
                ["lr300"] = new GunDefinition
                {
                    Id = "lr300",
                    DisplayName = "LR-300",
                    RustItemShortname = "rifle.lr300",
                    ImageUrl = "https://i.imgur.com/YourLR300Image.png"
                },
                ["m249"] = new GunDefinition
                {
                    Id = "m249",
                    DisplayName = "M249",
                    RustItemShortname = "lmg.m249",
                    ImageUrl = "https://i.imgur.com/YourM249Image.png"
                },
                ["mp5"] = new GunDefinition
                {
                    Id = "mp5",
                    DisplayName = "MP5A4",
                    RustItemShortname = "smg.mp5",
                    ImageUrl = "https://i.imgur.com/YourMP5Image.png"
                },
                ["thompson"] = new GunDefinition
                {
                    Id = "thompson",
                    DisplayName = "Thompson",
                    RustItemShortname = "smg.thompson",
                    ImageUrl = "https://i.imgur.com/YourThompsonImage.png"
                },
                ["python"] = new GunDefinition
                {
                    Id = "python",
                    DisplayName = "Python Revolver",
                    RustItemShortname = "pistol.python",
                    ImageUrl = "https://i.imgur.com/YourPythonImage.png"
                },
                ["bolt"] = new GunDefinition
                {
                    Id = "bolt",
                    DisplayName = "Bolt Action Rifle",
                    RustItemShortname = "rifle.bolt",
                    ImageUrl = "https://i.imgur.com/YourBoltImage.png"
                },
                ["sarpistol"] = new GunDefinition
                {
                    Id = "sarpistol",
                    DisplayName = "Semi-Auto Pistol",
                    RustItemShortname = "pistol.semiauto",
                    ImageUrl = "https://i.imgur.com/YourSARImage.png"
                },
                ["custom"] = new GunDefinition
                {
                    Id = "custom",
                    DisplayName = "Custom SMG",
                    RustItemShortname = "smg.2",
                    ImageUrl = "https://i.imgur.com/YourCustomImage.png"
                },
                ["m39"] = new GunDefinition
                {
                    Id = "m39",
                    DisplayName = "M39 Rifle",
                    RustItemShortname = "rifle.m39",
                    ImageUrl = "https://i.imgur.com/YourM39Image.png"
                }
            };
            
            // ===== SKINS CONFIGURATION =====
            // Add or modify weapon skins here. Each skin needs:
            // - Name: Display name for the skin
            // - SkinId: Rust workshop skin ID or custom identifier
            // - WeaponId: Which gun this skin is for (must match a gun Id above)
            // - ImageUrl: Direct URL to the skin preview image
            // - Cost: Price in Blood Tokens (default: 300)
            // - Tag: Optional badge like "NEW", "POPULAR", "HOT" (default: "")
            // - Rarity: Rarity tier like "Common", "Rare", "Epic", "Legendary" (default: "Common")
            
            public List<SkinDefinition> Skins = new List<SkinDefinition>
            {
                // AK-47 Skins
                new SkinDefinition
                {
                    Name = "AK-47 Tempered",
                    SkinId = "3602286295",
                    WeaponId = "ak47",
                    ImageUrl = "https://i.imgur.com/YourAK47TemperedSkin.png",
                    Cost = 650,
                    Tag = "NEW",
                    Rarity = "Legendary"
                },
                new SkinDefinition
                {
                    Name = "AK-47 Neon",
                    SkinId = "3102802323",
                    WeaponId = "ak47",
                    ImageUrl = "https://i.imgur.com/YourAK47NeonSkin.png",
                    Cost = 500,
                    Tag = "POPULAR",
                    Rarity = "Epic"
                },
                new SkinDefinition
                {
                    Name = "AK-47 Classic",
                    SkinId = "skin_ak47_classic",
                    WeaponId = "ak47",
                    ImageUrl = "https://i.imgur.com/YourAK47ClassicSkin.png",
                    Cost = 400,
                    Tag = "",
                    Rarity = "Rare"
                },
                
                // M249 Skins
                new SkinDefinition
                {
                    Name = "M249 Chrome",
                    SkinId = "skin_m249_chrome",
                    WeaponId = "m249",
                    ImageUrl = "https://i.imgur.com/YourM249ChromeSkin.png",
                    Cost = 450,
                    Tag = "",
                    Rarity = "Epic"
                },
                
                // Pistol Skins
                new SkinDefinition
                {
                    Name = "Pistol Black",
                    SkinId = "skin_pistol_black",
                    WeaponId = "pistol",
                    ImageUrl = "https://i.imgur.com/YourPistolBlackSkin.png",
                    Cost = 250,
                    Tag = "",
                    Rarity = "Common"
                },
                
                // LR-300 Skins
                new SkinDefinition
                {
                    Name = "LR-300 Gold",
                    SkinId = "skin_lr300_gold",
                    WeaponId = "lr300",
                    ImageUrl = "https://i.imgur.com/YourLR300GoldSkin.png",
                    Cost = 600,
                    Tag = "",
                    Rarity = "Legendary"
                },
                
                // MP5 Skins
                new SkinDefinition
                {
                    Name = "MP5 Tactical",
                    SkinId = "skin_mp5_tactical",
                    WeaponId = "mp5",
                    ImageUrl = "https://i.imgur.com/YourMP5TacticalSkin.png",
                    Cost = 350,
                    Tag = "",
                    Rarity = "Rare"
                },
                
                // Example: Add a new skin here and it will automatically appear in Store Tab!
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
            };
            
            // ===== HELPER METHODS =====
            
            public string GetGunImageUrl(string gunId)
            {
                return Guns.ContainsKey(gunId) ? Guns[gunId].ImageUrl : "";
            }
            
            public string GetSkinImageUrl(string skinId)
            {
                var skin = Skins.FirstOrDefault(s => s.SkinId == skinId);
                return skin?.ImageUrl ?? "";
            }
            
            public string[] GetAllGunIds()
            {
                return Guns.Keys.ToArray();
            }
            
            public SkinDefinition[] GetSkinsForWeapon(string weaponId)
            {
                return Skins.Where(s => s.WeaponId == weaponId).ToArray();
            }
        }
        
        public class GunDefinition
        {
            public string Id { get; set; }
            public string DisplayName { get; set; }
            public string RustItemShortname { get; set; }
            public string ImageUrl { get; set; }
        }
        
        public class SkinDefinition
        {
            public string Name { get; set; }
            public string SkinId { get; set; }
            public string WeaponId { get; set; }
            public string ImageUrl { get; set; }
            public int Cost { get; set; } = 300; // Default cost
            public string Tag { get; set; } = ""; // Optional tag like "NEW", "POPULAR", etc.
            public string Rarity { get; set; } = "Common"; // Rarity tier
        }
        
        // ===== OUTFIT/ARMOR CONFIGURATION =====
        public class OutfitConfig
        {
            public List<ArmorItem> Armors = new List<ArmorItem>
            {
                // Head Armor
                new ArmorItem
                {
                    Name = "Metal Facemask",
                    ItemShortname = "metal.facemask",
                    Slot = "head",
                    SkinId = "0",
                    ImageUrl = "https://i.imgur.com/mVY2Uav.png",
                    Cost = 300,
                    Rarity = "Common"
                },
                new ArmorItem
                {
                    Name = "Coffee Can Helmet",
                    ItemShortname = "coffeecan.helmet",
                    Slot = "head",
                    SkinId = "0",
                    ImageUrl = "https://i.imgur.com/YourCoffeeCanImage.png",
                    Cost = 250,
                    Rarity = "Common"
                },
                
                // Chest Armor
                new ArmorItem
                {
                    Name = "Metal Chest Plate",
                    ItemShortname = "metal.plate.torso",
                    Slot = "chest",
                    SkinId = "0",
                    ImageUrl = "https://i.imgur.com/YourMetalChestImage.png",
                    Cost = 400,
                    Rarity = "Rare"
                },
                new ArmorItem
                {
                    Name = "Road Sign Jacket",
                    ItemShortname = "roadsign.jacket",
                    Slot = "chest",
                    SkinId = "0",
                    ImageUrl = "https://i.imgur.com/YourRoadSignImage.png",
                    Cost = 300,
                    Rarity = "Common"
                },
                
                // Legs Armor
                new ArmorItem
                {
                    Name = "Heavy Plate Pants",
                    ItemShortname = "heavy.plate.pants",
                    Slot = "legs",
                    SkinId = "0",
                    ImageUrl = "https://i.imgur.com/YourHeavyPantsImage.png",
                    Cost = 400,
                    Rarity = "Rare"
                },
                new ArmorItem
                {
                    Name = "Road Sign Kilt",
                    ItemShortname = "roadsign.kilt",
                    Slot = "legs",
                    SkinId = "0",
                    ImageUrl = "https://i.imgur.com/YourRoadSignKiltImage.png",
                    Cost = 300,
                    Rarity = "Common"
                },
                
                // Hands/Gloves
                new ArmorItem
                {
                    Name = "Tactical Gloves",
                    ItemShortname = "tactical.gloves",
                    Slot = "hands",
                    SkinId = "0",
                    ImageUrl = "https://i.imgur.com/YourTacticalGlovesImage.png",
                    Cost = 200,
                    Rarity = "Common"
                },
                
                // Feet/Boots
                new ArmorItem
                {
                    Name = "Heavy Plate Boots",
                    ItemShortname = "shoes.boots",
                    Slot = "feet",
                    SkinId = "0",
                    ImageUrl = "https://i.imgur.com/YourBootsImage.png",
                    Cost = 250,
                    Rarity = "Common"
                }
            };
            
            public ArmorItem[] GetArmorsBySlot(string slot)
            {
                return Armors.Where(a => a.Slot == slot).ToArray();
            }
        }
        
        public class ArmorItem
        {
            public string Name { get; set; }
            public string DisplayName => Name; // Alias for consistency
            public string ItemShortname { get; set; }
            public string Slot { get; set; } // "head", "chest", "legs", "hands", "feet"
            public string SkinId { get; set; } = "0";
            public string ImageUrl { get; set; }
            public int Cost { get; set; } = 300;
            public string Rarity { get; set; } = "Common";
            public string Tag { get; set; } = "";
        }
        
        #endregion
        
        #region Configuration
        
        internal class PluginConfig
        {
            [JsonProperty("Lobby Spawn Position")]
            public Vector3 LobbySpawnPosition { get; set; } = new Vector3(0, 100, 0);
            
            [JsonProperty("Arena Spawn Position")]
            public Vector3 ArenaSpawnPosition { get; set; } = new Vector3(0, 100, 500);
            
            [JsonProperty("Starting Blood Tokens")]
            public int StartingTokens { get; set; } = 500;
            
            [JsonProperty("Tokens Per Kill")]
            public int TokensPerKill { get; set; } = 10;
            
            [JsonProperty("Enable Tebex Integration")]
            public bool EnableTebex { get; set; } = false;
            
            [JsonProperty("Tebex Secret Key")]
            public string TebexSecretKey { get; set; } = "YOUR_SECRET_KEY_HERE";
            
            [JsonProperty("Max Weapon Level")]
            public int MaxWeaponLevel { get; set; } = 10;
            
            [JsonProperty("Max Attachment Level")]
            public int MaxAttachmentLevel { get; set; } = 5;
            
            [JsonProperty("UI Update Throttle MS")]
            public int UIUpdateThrottleMS { get; set; } = 100;
            
            [JsonProperty("Auto Save Interval Seconds")]
            public float AutoSaveInterval { get; set; } = 300f;
            
            [JsonProperty("Enable Debug Logging")]
            public bool EnableDebugLogging { get; set; } = false;
        }
        
        protected override void LoadDefaultConfig()
        {
            _config = new PluginConfig();
            SaveConfig();
        }
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<PluginConfig>();
                if (_config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                PrintError("Configuration file is corrupt. Loading defaults...");
                LoadDefaultConfig();
            }
            SaveConfig();
        }
        
        protected override void SaveConfig() => Config.WriteObject(_config, true);
        
        #endregion
        
        #region Oxide Hooks
        
        private void Init()
        {
            permission.RegisterPermission(PERMISSION_ADMIN, this);
            permission.RegisterPermission(PERMISSION_VIP, this);
            
            // Initialize gun configuration
            _gunConfig = new GunConfig();
            _outfitConfig = new OutfitConfig();
            
            // Initialize all systems
            _saveManager = new SaveManager(this, _config);
            _antiExploit = new AntiExploit(this);
            _tokenEconomy = new BloodTokenEconomy(this, _config);
            _attachmentSystem = new AttachmentSystem(this, _config);
            _weaponProgression = new WeaponProgression(this, _config);
            _vfxManager = new VFXManager(this);
            _sfxManager = new SFXManager(this);
            _forgeStation = new ForgeStationSystem(this, _config, _tokenEconomy, _attachmentSystem, _weaponProgression);
            _loadoutEditor = new LoadoutEditor(this, _attachmentSystem);
            _storeAPI = new StoreAPI(this, _config, _tokenEconomy);
            _lobbyUI = new LobbyUI(this, _loadoutEditor, _forgeStation, _storeAPI);
            _domeManager = new DomeManager(this, _config);
            _telemetry = new TelemetrySystem(this);
            
            LogDebug("KillaDome initialized successfully");
        }
        
        private void OnServerInitialized()
        {
            timer.Every(_config.AutoSaveInterval, () => AutoSaveAllPlayers());
            LogDebug("Auto-save timer started");
            
            // Load images after server is ready
            timer.Once(5f, () => LoadImages());
        }
        
        private void LoadImages()
        {
            if (ImageLibrary == null || !ImageLibrary.IsLoaded)
            {
                PrintWarning("ImageLibrary not loaded. Images will not display. Please install ImageLibrary plugin.");
                return;
            }
            
            // Load gun images
            foreach (var gun in _gunConfig.Guns.Values)
            {
                if (!string.IsNullOrEmpty(gun.ImageUrl))
                {
                    ImageLibrary.Call("AddImage", gun.ImageUrl, gun.ImageUrl);
                }
            }
            
            // Load skin images
            foreach (var skin in _gunConfig.Skins)
            {
                if (!string.IsNullOrEmpty(skin.ImageUrl))
                {
                    ImageLibrary.Call("AddImage", skin.ImageUrl, skin.ImageUrl);
                }
            }
            
            // Load armor images
            foreach (var armor in _outfitConfig.Armors)
            {
                if (!string.IsNullOrEmpty(armor.ImageUrl))
                {
                    ImageLibrary.Call("AddImage", armor.ImageUrl, armor.ImageUrl);
                }
            }
            
            Puts($"Loaded {_gunConfig.Guns.Count} gun images, {_gunConfig.Skins.Count} skin images, and {_outfitConfig.Armors.Count} armor images into ImageLibrary");
        }
        
        private void Unload()
        {
            // Clean up all UI
            foreach (var player in BasePlayer.activePlayerList)
            {
                _lobbyUI?.DestroyUI(player);
            }
            
            // Save all player data
            foreach (var session in _activeSessions.Values)
            {
                _saveManager?.SavePlayerProfile(session.Profile);
            }
            
            _activeSessions.Clear();
            
            LogDebug("KillaDome unloaded and cleaned up");
        }
        
        private void OnPlayerConnected(BasePlayer player)
        {
            if (player == null || _saveManager == null || _lobbyUI == null) return;
            
            NextTick(() =>
            {
                if (player == null || !player.IsConnected) return;
                
                var profile = _saveManager.LoadPlayerProfile(player.userID);
                var session = new PlayerSession(player, profile);
                _activeSessions[player.userID] = session;
                
                // Teleport to lobby
                TeleportToLobby(player);
                
                // Show lobby UI
                timer.Once(1f, () =>
                {
                    if (player != null && player.IsConnected)
                    {
                        _lobbyUI.ShowLobbyUI(player);
                    }
                });
                
                LogDebug($"Player {player.displayName} ({player.userID}) connected");
            });
        }
        
        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player == null) return;
            
            _lobbyUI?.DestroyUI(player);
            
            if (_activeSessions.TryGetValue(player.userID, out var session))
            {
                _saveManager?.SavePlayerProfile(session.Profile);
                _activeSessions.Remove(player.userID);
            }
            
            LogDebug($"Player {player.displayName} disconnected: {reason}");
        }
        
        private void OnEntityDeath(BasePlayer victim, HitInfo info)
        {
            if (victim == null || _tokenEconomy == null || _telemetry == null) return;
            
            var attacker = info?.InitiatorPlayer;
            if (attacker != null && attacker != victim && attacker.IsConnected)
            {
                // Award tokens for kill
                _tokenEconomy.AwardTokens(attacker.userID, _config.TokensPerKill);
                
                // Track telemetry
                _telemetry.RecordKill(attacker.userID, victim.userID);
                
                LogDebug($"{attacker.displayName} killed {victim.displayName}");
            }
            
            // Respawn victim in lobby after delay
            timer.Once(3f, () =>
            {
                if (victim != null && victim.IsConnected)
                {
                    TeleportToLobby(victim);
                    victim.Respawn();
                }
            });
        }
        
        #endregion
        
        #region Helper Methods
        
        private void TeleportToLobby(BasePlayer player)
        {
            if (player == null || !player.IsConnected) return;
            player.Teleport(_config.LobbySpawnPosition);
        }
        
        private void TeleportToArena(BasePlayer player)
        {
            if (player == null || !player.IsConnected) return;
            player.Teleport(_config.ArenaSpawnPosition);
            
            // Apply loadout when entering arena
            ApplyLoadout(player);
        }
        
        private void ApplyLoadout(BasePlayer player)
        {
            var session = GetSession(player.userID);
            if (session == null || session.Profile.Loadouts.Count == 0) return;
            
            var loadout = session.Profile.Loadouts[0];
            
            // Strip existing items
            player.inventory.Strip();
            
            // Give primary weapon
            GiveWeapon(player, loadout.Primary, loadout.PrimaryAttachments, loadout.Skins);
            
            // Give secondary weapon
            GiveWeapon(player, loadout.Secondary, loadout.SecondaryAttachments, loadout.Skins);
            
            // Give equipped armor/outfit
            GiveArmor(player, loadout);
            
            LogDebug($"Applied loadout to {player.displayName}");
        }
        
        private void GiveWeapon(BasePlayer player, string weaponName, Dictionary<string, string> attachments, Dictionary<string, string> skins)
        {
            if (string.IsNullOrEmpty(weaponName)) return;
            
            // Get weapon info from centralized config
            string itemName = "rifle.ak"; // Default fallback
            if (_gunConfig?.Guns != null && _gunConfig.Guns.TryGetValue(weaponName, out var gunDef))
            {
                itemName = gunDef.RustItemShortname;
            }
            
            var item = ItemManager.CreateByName(itemName, 1);
            if (item == null)
            {
                LogDebug($"Failed to create weapon: {itemName}");
                return;
            }
            
            // Apply skin if exists
            if (skins != null && skins.TryGetValue(weaponName, out string skinId))
            {
                if (ulong.TryParse(skinId, out ulong skin))
                {
                    item.skin = skin;
                    item.MarkDirty(); // Mark for network update
                }
            }
            
            // Apply attachments if exists
            if (attachments != null && attachments.Count > 0)
            {
                var heldEntity = item.GetHeldEntity() as BaseProjectile;
                if (heldEntity != null && item.contents != null)
                {
                    foreach (var attachmentEntry in attachments)
                    {
                        string attachmentId = attachmentEntry.Value;
                        if (!string.IsNullOrEmpty(attachmentId))
                        {
                            var attachmentItem = ItemManager.CreateByName(attachmentId, 1);
                            if (attachmentItem != null)
                            {
                                // Add attachment to weapon's content container
                                if (!attachmentItem.MoveToContainer(item.contents))
                                {
                                    attachmentItem.Remove(); // Clean up if can't add
                                }
                            }
                        }
                    }
                }
            }
            
            // Give item to player
            if (!player.inventory.GiveItem(item))
            {
                LogDebug($"Failed to give weapon {itemName} to {player.displayName} - inventory full?");
                item.Remove(); // Clean up item if can't give
                return;
            }
            
            // If item was given to belt, ensure visual update
            var heldItem = item.GetHeldEntity();
            if (heldItem != null)
            {
                heldItem.skinID = item.skin;
                heldItem.SendNetworkUpdate();
            }
            
            // Give ammo
            string ammoType = weaponName == "pistol" ? "ammo.pistol" : "ammo.rifle";
            var ammo = ItemManager.CreateByName(ammoType, 250);
            if (ammo != null)
            {
                player.inventory.GiveItem(ammo);
            }
        }
        
        private void GiveArmor(BasePlayer player, Loadout loadout)
        {
            if (player == null || loadout == null) return;
            
            // Give head armor
            if (!string.IsNullOrEmpty(loadout.ArmorHead))
            {
                GiveArmorPiece(player, loadout.ArmorHead);
            }
            
            // Give chest armor
            if (!string.IsNullOrEmpty(loadout.ArmorChest))
            {
                GiveArmorPiece(player, loadout.ArmorChest);
            }
            
            // Give legs armor
            if (!string.IsNullOrEmpty(loadout.ArmorLegs))
            {
                GiveArmorPiece(player, loadout.ArmorLegs);
            }
            
            // Give hands armor
            if (!string.IsNullOrEmpty(loadout.ArmorHands))
            {
                GiveArmorPiece(player, loadout.ArmorHands);
            }
            
            // Give feet armor
            if (!string.IsNullOrEmpty(loadout.ArmorFeet))
            {
                GiveArmorPiece(player, loadout.ArmorFeet);
            }
            
            LogDebug($"Applied armor to {player.displayName}");
        }
        
        private void GiveArmorPiece(BasePlayer player, string armorShortname)
        {
            if (string.IsNullOrEmpty(armorShortname)) return;
            
            var item = ItemManager.CreateByName(armorShortname, 1);
            if (item == null)
            {
                LogDebug($"Failed to create armor item: {armorShortname}");
                return;
            }
            
            // Try to move to wear container (for clothing/armor)
            if (!item.MoveToContainer(player.inventory.containerWear))
            {
                // If wear container is full or item can't be worn, give to main inventory
                player.inventory.GiveItem(item);
            }
            
            LogDebug($"Gave armor piece {armorShortname} to {player.displayName}");
        }
        
        private void AutoSaveAllPlayers()
        {
            if (_saveManager == null) return;
            
            int saved = 0;
            foreach (var session in _activeSessions.Values)
            {
                if (session?.Profile != null)
                {
                    _saveManager.SavePlayerProfile(session.Profile);
                    saved++;
                }
            }
            LogDebug($"Auto-saved {saved} player profiles");
        }
        
        private void LogDebug(string message)
        {
            if (_config?.EnableDebugLogging == true)
            {
                Puts($"[DEBUG] {message}");
            }
        }
        
        internal PlayerSession GetSession(ulong steamId)
        {
            _activeSessions.TryGetValue(steamId, out var session);
            return session;
        }
        
        #endregion
        
        #region Console Commands
        
        [ConsoleCommand("kd.open")]
        private void CmdOpen(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            if (!permission.UserHasPermission(player.UserIDString, PERMISSION_ADMIN))
            {
                SendReply(arg, "You don't have permission to use this command");
                return;
            }
            
            _lobbyUI.ShowLobbyUI(player);
            SendReply(arg, "Lobby UI opened");
        }
        
        [ConsoleCommand("kd.start")]
        private void CmdStart(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            if (!permission.UserHasPermission(player.UserIDString, PERMISSION_ADMIN))
            {
                SendReply(arg, "You don't have permission to use this command");
                return;
            }
            
            _domeManager.StartMatch();
            SendReply(arg, "Match started");
        }
        
        [ConsoleCommand("kd.giveskin")]
        private void CmdGiveSkin(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(2))
            {
                SendReply(arg, "Usage: kd.giveskin <steamid> <skinid>");
                return;
            }
            
            if (!permission.UserHasPermission(player.UserIDString, PERMISSION_ADMIN))
            {
                SendReply(arg, "You don't have permission to use this command");
                return;
            }
            
            if (!ulong.TryParse(arg.Args[0], out ulong targetId))
            {
                SendReply(arg, "Invalid Steam ID");
                return;
            }
            
            string skinId = arg.Args[1];
            
            if (_activeSessions.TryGetValue(targetId, out var session))
            {
                session.Profile.OwnedSkins.Add(skinId);
                _saveManager.SavePlayerProfile(session.Profile);
                SendReply(arg, $"Granted skin {skinId} to player {targetId}");
            }
            else
            {
                SendReply(arg, "Player not found or not online");
            }
        }
        
        [ConsoleCommand("kd.resetprogress")]
        private void CmdResetProgress(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(1))
            {
                SendReply(arg, "Usage: kd.resetprogress <steamid>");
                return;
            }
            
            if (!permission.UserHasPermission(player.UserIDString, PERMISSION_ADMIN))
            {
                SendReply(arg, "You don't have permission to use this command");
                return;
            }
            
            if (!ulong.TryParse(arg.Args[0], out ulong targetId))
            {
                SendReply(arg, "Invalid Steam ID");
                return;
            }
            
            var newProfile = new PlayerProfile(targetId, _config.StartingTokens);
            _saveManager.SavePlayerProfile(newProfile);
            
            if (_activeSessions.TryGetValue(targetId, out var session))
            {
                session.Profile = newProfile;
            }
            
            SendReply(arg, $"Reset progress for player {targetId}");
        }
        
        #endregion
        
        #region Chat Commands
        
        [ChatCommand("kd")]
        private void CmdKD(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                SendReply(player, "KillaDome Commands:\n" +
                    "/kd open - Open lobby UI\n" +
                    "/kd stats - View your stats\n" +
                    "/kd help - Show this help");
                return;
            }
            
            switch (args[0].ToLower())
            {
                case "open":
                    _lobbyUI.ShowLobbyUI(player);
                    SendReply(player, "Lobby UI opened");
                    break;
                    
                case "stats":
                    if (_activeSessions.TryGetValue(player.userID, out var session))
                    {
                        SendReply(player, $"Blood Tokens: {session.Profile.Tokens}\n" +
                            $"VIP Status: {(session.Profile.IsVIP ? "Active" : "Inactive")}");
                    }
                    break;
                    
                case "help":
                    SendReply(player, "KillaDome - Full COD Experience\n" +
                        "Use /kd open to access the lobby");
                    break;
                    
                default:
                    SendReply(player, "Unknown command. Use /kd help");
                    break;
            }
        }
        
        #endregion
        
        #region UI Console Commands
        
        [ConsoleCommand("killadome.close")]
        private void CmdUIClose(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            _lobbyUI.DestroyUI(player);
        }
        
        [ConsoleCommand("killadome.tab")]
        private void CmdUITab(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(1)) return;
            
            string tab = arg.Args[0].ToLower();
            _lobbyUI.ShowLobbyUIWithTab(player, tab);
            
            LogDebug($"Player {player.displayName} opened tab: {tab}");
        }
        
        [ConsoleCommand("killadome.joinqueue")]
        private void CmdUIJoinQueue(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            _domeManager.AddToQueue(player.userID);
            SendReply(player, "You have joined the queue!");
        }
        
        [ConsoleCommand("killadome.weapon.prev")]
        private void CmdWeaponPrev(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(1)) return;
            
            if (!_antiExploit.CheckRateLimit(player.userID))
            {
                SendReply(player, "Please slow down!");
                return;
            }
            
            string slot = arg.Args[0]; // "primary" or "secondary"
            CycleWeapon(player, slot, -1);
            _lobbyUI.ShowLobbyUIWithTab(player, "loadouts");
        }
        
        [ConsoleCommand("killadome.weapon.next")]
        private void CmdWeaponNext(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(1)) return;
            
            if (!_antiExploit.CheckRateLimit(player.userID))
            {
                SendReply(player, "Please slow down!");
                return;
            }
            
            string slot = arg.Args[0]; // "primary" or "secondary"
            CycleWeapon(player, slot, 1);
            _lobbyUI.ShowLobbyUIWithTab(player, "loadouts");
        }
        
        [ConsoleCommand("killadome.purchase")]
        private void CmdPurchase(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(2)) return;
            
            if (!_antiExploit.CheckRateLimit(player.userID))
            {
                SendReply(player, "Please slow down!");
                return;
            }
            
            string itemId = arg.Args[0];
            if (!int.TryParse(arg.Args[1], out int cost))
            {
                SendReply(player, "Invalid cost");
                return;
            }
            
            var session = GetSession(player.userID);
            if (session == null)
            {
                // Create session if it doesn't exist
                var profile = _saveManager.LoadPlayerProfile(player.userID);
                session = new PlayerSession(player, profile);
                _activeSessions[player.userID] = session;
            }
            
            if (session.Profile.Tokens < cost)
            {
                SendReply(player, $"Insufficient tokens! You need {cost} but only have {session.Profile.Tokens}.");
                return;
            }
            
            if (_storeAPI.PurchaseItem(player.userID, itemId, cost))
            {
                SendReply(player, $"Successfully purchased {itemId}!");
                _saveManager.SavePlayerProfile(session.Profile);
                _lobbyUI.ShowLobbyUIWithTab(player, "store");
            }
            else
            {
                SendReply(player, "Purchase failed!");
            }
        }
        
        [ConsoleCommand("killadome.purchase.armor")]
        private void CmdPurchaseArmor(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(2)) return;
            
            if (!_antiExploit.CheckRateLimit(player.userID))
            {
                SendReply(player, "Please slow down!");
                return;
            }
            
            string itemShortname = arg.Args[0];
            if (!int.TryParse(arg.Args[1], out int cost))
            {
                SendReply(player, "Invalid cost");
                return;
            }
            
            var session = GetSession(player.userID);
            if (session == null)
            {
                // Create session if it doesn't exist
                var profile = _saveManager.LoadPlayerProfile(player.userID);
                session = new PlayerSession(player, profile);
                _activeSessions[player.userID] = session;
            }
            
            // Check if already owned
            if (session.Profile.OwnedArmor.Contains(itemShortname))
            {
                SendReply(player, "You already own this armor piece!");
                return;
            }
            
            if (session.Profile.Tokens < cost)
            {
                SendReply(player, $"Insufficient tokens! You need {cost} but only have {session.Profile.Tokens}.");
                return;
            }
            
            // Deduct cost and add armor
            session.Profile.Tokens -= cost;
            session.Profile.OwnedArmor.Add(itemShortname);
            
            // Find armor name for message
            var armor = _outfitConfig.Armors.FirstOrDefault(a => a.ItemShortname == itemShortname);
            string armorName = armor != null ? armor.DisplayName : itemShortname;
            
            SendReply(player, $"Successfully purchased {armorName}!");
            _saveManager.SavePlayerProfile(session.Profile);
            _lobbyUI.ShowLobbyUIWithTab(player, "store");
        }
        
        [ConsoleCommand("killadome.applyskin")]
        private void CmdApplySkin(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(2)) return;
            
            if (!_antiExploit.CheckRateLimit(player.userID))
            {
                SendReply(player, "Please slow down!");
                return;
            }
            
            string weapon = arg.Args[0]; // "primary" or "secondary"
            string skinId = arg.Args[1];
            
            var session = GetSession(player.userID);
            if (session == null || session.Profile.Loadouts.Count == 0) return;
            
            // Check if player owns the skin
            if (!session.Profile.OwnedSkins.Contains(skinId))
            {
                SendReply(player, "You don't own this skin!");
                return;
            }
            
            var loadout = session.Profile.Loadouts[0];
            string weaponName = weapon == "primary" ? loadout.Primary : loadout.Secondary;
            
            // Apply skin to weapon
            loadout.Skins[weaponName] = skinId;
            _saveManager.SavePlayerProfile(session.Profile);
            
            SendReply(player, $"Skin applied to {weaponName}!");
            _lobbyUI.ShowLobbyUIWithTab(player, "loadouts");
        }
        
        [ConsoleCommand("killadome.applyattachment")]
        private void CmdApplyAttachment(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(3)) return;
            
            if (!_antiExploit.CheckRateLimit(player.userID))
            {
                SendReply(player, "Please slow down!");
                return;
            }
            
            string weapon = arg.Args[0]; // "primary" or "secondary"
            string attachmentSlot = arg.Args[1]; // "optic", "barrel", "magazine", "grip"
            string attachmentId = arg.Args[2];
            
            var session = GetSession(player.userID);
            if (session == null || session.Profile.Loadouts.Count == 0) return;
            
            // Check if player owns the attachment (stored in OwnedSkins list for simplicity)
            if (!session.Profile.OwnedSkins.Contains(attachmentId))
            {
                SendReply(player, "You don't own this attachment!");
                return;
            }
            
            var loadout = session.Profile.Loadouts[0];
            var attachments = weapon == "primary" ? loadout.PrimaryAttachments : loadout.SecondaryAttachments;
            
            // Apply attachment
            attachments[attachmentSlot] = attachmentId;
            _saveManager.SavePlayerProfile(session.Profile);
            
            SendReply(player, $"Attachment applied!");
            _lobbyUI.ShowLobbyUIWithTab(player, "loadouts");
        }
        
        #endregion
        
        #region Helper Methods
        
        private void CycleWeapon(BasePlayer player, string slot, int direction)
        {
            var session = GetSession(player.userID);
            if (session == null || session.Profile.Loadouts.Count == 0) return;
            
            var loadout = session.Profile.Loadouts[0];
            string[] availableWeapons = _gunConfig.GetAllGunIds(); // Get weapons from centralized config
            
            string currentWeapon = slot == "primary" ? loadout.Primary : loadout.Secondary;
            int currentIndex = Array.IndexOf(availableWeapons, currentWeapon);
            
            if (currentIndex == -1) currentIndex = 0;
            
            int newIndex = (currentIndex + direction + availableWeapons.Length) % availableWeapons.Length;
            string newWeapon = availableWeapons[newIndex];
            
            if (slot == "primary")
            {
                loadout.Primary = newWeapon;
            }
            else
            {
                loadout.Secondary = newWeapon;
            }
            
            _saveManager.SavePlayerProfile(session.Profile);
            LogDebug($"Player {player.displayName} changed {slot} weapon to {newWeapon}");
        }
        
        [ConsoleCommand("killadome.attachcat")]
        private void CmdAttachmentCategory(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(1)) return;
            
            string category = arg.Args[0].ToLower(); // "scopes", "silencers", "underbarrel"
            if (category != "scopes" && category != "silencers" && category != "underbarrel") return;
            
            var session = GetSession(player.userID);
            if (session == null) return;
            
            session.SelectedAttachmentCategory = category;
            _lobbyUI.ShowLobbyUIWithTab(player, "loadouts");
        }
        
        [ConsoleCommand("killadome.editweapon")]
        private void CmdEditWeapon(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(1)) return;
            
            string slot = arg.Args[0].ToLower(); // "primary" or "secondary"
            if (slot != "primary" && slot != "secondary") return;
            
            var session = GetSession(player.userID);
            if (session == null) return;
            
            session.EditingWeaponSlot = slot;
            _lobbyUI.ShowLobbyUIWithTab(player, "loadouts");
        }
        
        [ConsoleCommand("killadome.storecat")]
        private void CmdStoreCategory(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(1)) return;
            
            string category = arg.Args[0].ToLower(); // "guns", "skins", or "outfits"
            if (category != "guns" && category != "skins" && category != "outfits") return;
            
            var session = GetSession(player.userID);
            if (session == null) return;
            
            session.SelectedStoreCategory = category;
            // Reset page when switching categories
            session.GunsStorePage = 0;
            session.SkinsStorePage = 0;
            _lobbyUI.ShowLobbyUIWithTab(player, "store");
        }
        
        [ConsoleCommand("killadome.storepage")]
        private void CmdStorePage(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(1)) return;
            
            string direction = arg.Args[0].ToLower(); // "next" or "prev"
            
            var session = GetSession(player.userID);
            if (session == null) return;
            
            string category = session.SelectedStoreCategory ?? "guns";
            
            if (category == "guns")
            {
                // Calculate max pages to prevent overflow
                int itemsPerPage = 6;
                int totalItems = _gunConfig.Guns.Count;
                int maxPage = (int)Math.Ceiling((double)totalItems / itemsPerPage) - 1;
                
                if (direction == "next" && session.GunsStorePage < maxPage)
                    session.GunsStorePage++;
                else if (direction == "prev" && session.GunsStorePage > 0)
                    session.GunsStorePage--;
            }
            else if (category == "skins")
            {
                // Calculate max pages to prevent overflow
                int itemsPerPage = 12;
                int totalItems = _gunConfig.Skins.Count + 3; // weapon skins + outfit skins
                int maxPage = (int)Math.Ceiling((double)totalItems / itemsPerPage) - 1;
                
                if (direction == "next" && session.SkinsStorePage < maxPage)
                    session.SkinsStorePage++;
                else if (direction == "prev" && session.SkinsStorePage > 0)
                    session.SkinsStorePage--;
            }
            
            _lobbyUI.ShowLobbyUIWithTab(player, "store");
        }
        
        [ConsoleCommand("killadome.loadouttab")]
        private void CmdLoadoutTab(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(1)) return;
            
            string tab = arg.Args[0].ToLower(); // "weapons" or "outfit"
            if (tab != "weapons" && tab != "outfit") return;
            
            var session = GetSession(player.userID);
            if (session == null) return;
            
            session.SelectedLoadoutTab = tab;
            _lobbyUI.ShowLobbyUIWithTab(player, "loadouts");
        }
        
        [ConsoleCommand("killadome.armor.next")]
        private void CmdArmorNext(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(1)) return;
            
            string slot = arg.Args[0].ToLower(); // "head", "chest", "legs", "hands", "feet"
            
            var session = GetSession(player.userID);
            if (session == null || session.Profile.Loadouts.Count == 0) return;
            
            var loadout = session.Profile.Loadouts[0];
            
            // Get owned armor for this slot
            var ownedArmor = _outfitConfig.Armors
                .Where(a => a.Slot == slot && session.Profile.OwnedArmor.Contains(a.ItemShortname))
                .ToArray();
            
            if (ownedArmor.Length == 0) return;
            
            // Get current armor shortname
            string currentArmorShortname = slot switch
            {
                "head" => loadout.ArmorHead,
                "chest" => loadout.ArmorChest,
                "legs" => loadout.ArmorLegs,
                "hands" => loadout.ArmorHands,
                "feet" => loadout.ArmorFeet,
                _ => null
            };
            
            // Find current index and move to next
            int currentIndex = Array.FindIndex(ownedArmor, a => a.ItemShortname == currentArmorShortname);
            if (currentIndex == -1) currentIndex = 0;
            
            int nextIndex = (currentIndex + 1) % ownedArmor.Length;
            string nextArmor = ownedArmor[nextIndex].ItemShortname;
            
            // Update loadout
            switch (slot)
            {
                case "head": loadout.ArmorHead = nextArmor; break;
                case "chest": loadout.ArmorChest = nextArmor; break;
                case "legs": loadout.ArmorLegs = nextArmor; break;
                case "hands": loadout.ArmorHands = nextArmor; break;
                case "feet": loadout.ArmorFeet = nextArmor; break;
            }
            
            _lobbyUI.ShowLobbyUIWithTab(player, "loadouts");
        }
        
        [ConsoleCommand("killadome.armor.prev")]
        private void CmdArmorPrev(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(1)) return;
            
            string slot = arg.Args[0].ToLower(); // "head", "chest", "legs", "hands", "feet"
            
            var session = GetSession(player.userID);
            if (session == null || session.Profile.Loadouts.Count == 0) return;
            
            var loadout = session.Profile.Loadouts[0];
            
            // Get owned armor for this slot
            var ownedArmor = _outfitConfig.Armors
                .Where(a => a.Slot == slot && session.Profile.OwnedArmor.Contains(a.ItemShortname))
                .ToArray();
            
            if (ownedArmor.Length == 0) return;
            
            // Get current armor shortname
            string currentArmorShortname = slot switch
            {
                "head" => loadout.ArmorHead,
                "chest" => loadout.ArmorChest,
                "legs" => loadout.ArmorLegs,
                "hands" => loadout.ArmorHands,
                "feet" => loadout.ArmorFeet,
                _ => null
            };
            
            // Find current index and move to prev
            int currentIndex = Array.FindIndex(ownedArmor, a => a.ItemShortname == currentArmorShortname);
            if (currentIndex == -1) currentIndex = 0;
            
            int prevIndex = (currentIndex - 1 + ownedArmor.Length) % ownedArmor.Length;
            string prevArmor = ownedArmor[prevIndex].ItemShortname;
            
            // Update loadout
            switch (slot)
            {
                case "head": loadout.ArmorHead = prevArmor; break;
                case "chest": loadout.ArmorChest = prevArmor; break;
                case "legs": loadout.ArmorLegs = prevArmor; break;
                case "hands": loadout.ArmorHands = prevArmor; break;
                case "feet": loadout.ArmorFeet = prevArmor; break;
            }
            
            _lobbyUI.ShowLobbyUIWithTab(player, "loadouts");
        }
        
        [ChatCommand("dice")]
        private void CmdDiceGame(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            
            var session = GetSession(player.userID);
            if (session == null)
            {
                SendReply(player, "Session not found! Please rejoin.");
                return;
            }
            
            // Check if player is in cooldown
            if (session.LastDiceGame != default && (DateTime.UtcNow - session.LastDiceGame).TotalSeconds < 30)
            {
                int remaining = 30 - (int)(DateTime.UtcNow - session.LastDiceGame).TotalSeconds;
                SendReply(player, $"<color=#FF8A00>Dice Game:</color> Wait {remaining}s before playing again!");
                return;
            }
            
            if (args.Length == 0)
            {
                SendReply(player, "<color=#FF8A00>Dice Game:</color> Roll the dice! Win 2x your bet!");
                SendReply(player, "Usage: /dice <bet> (10-100 tokens)");
                SendReply(player, $"Your tokens: <color=#FF8A00>{session.Profile.Tokens}</color>");
                return;
            }
            
            if (!int.TryParse(args[0], out int bet))
            {
                SendReply(player, "<color=#FF8A00>Dice Game:</color> Invalid bet amount!");
                return;
            }
            
            if (bet < 10 || bet > 100)
            {
                SendReply(player, "<color=#FF8A00>Dice Game:</color> Bet must be between 10-100 tokens!");
                return;
            }
            
            if (session.Profile.Tokens < bet)
            {
                SendReply(player, $"<color=#FF8A00>Dice Game:</color> Not enough tokens! You have {session.Profile.Tokens}");
                return;
            }
            
            // Deduct bet
            session.Profile.Tokens -= bet;
            session.LastDiceGame = DateTime.UtcNow;
            
            // Roll dice (1-6 for player, 1-6 for house)
            int playerRoll = UnityEngine.Random.Range(1, 7);
            int houseRoll = UnityEngine.Random.Range(1, 7);
            
            SendReply(player, $"<color=#FF8A00>╔═══════════════════╗</color>");
            SendReply(player, $"<color=#FF8A00>║</color>   DICE GAME    <color=#FF8A00>║</color>");
            SendReply(player, $"<color=#FF8A00>╚═══════════════════╝</color>");
            SendReply(player, $"Your roll: <color=#4CFF4C>[{playerRoll}]</color>");
            SendReply(player, $"House roll: <color=#FF4C4C>[{houseRoll}]</color>");
            
            if (playerRoll > houseRoll)
            {
                int winnings = bet * 2;
                session.Profile.Tokens += winnings;
                SendReply(player, $"<color=#4CFF4C>★ YOU WIN! ★</color> +{winnings} tokens");
                SendReply(player, $"Balance: <color=#FF8A00>{session.Profile.Tokens}</color> tokens");
                Effect.server.Run("assets/prefabs/deployable/vendingmachine/effects/buy.prefab", player.transform.position);
            }
            else if (playerRoll < houseRoll)
            {
                SendReply(player, $"<color=#FF4C4C>✖ YOU LOSE!</color> -{bet} tokens");
                SendReply(player, $"Balance: <color=#FF8A00>{session.Profile.Tokens}</color> tokens");
                Effect.server.Run("assets/prefabs/deployable/vendingmachine/effects/deny.prefab", player.transform.position);
            }
            else
            {
                // Tie - return bet
                session.Profile.Tokens += bet;
                SendReply(player, $"<color=#FFD700>═ TIE! ═</color> Bet returned");
                SendReply(player, $"Balance: <color=#FF8A00>{session.Profile.Tokens}</color> tokens");
            }
            
            _saveManager.SavePlayerProfile(session.Profile);
        }
        
        [ChatCommand("tokengame")]
        private void CmdTokenGameHelp(BasePlayer player, string command, string[] args)
        {
            SendReply(player, "<color=#FF8A00>╔═══════════════════════════╗</color>");
            SendReply(player, "<color=#FF8A00>║</color>  BLOOD TOKEN GAMES     <color=#FF8A00>║</color>");
            SendReply(player, "<color=#FF8A00>╚═══════════════════════════╝</color>");
            SendReply(player, "");
            SendReply(player, "<color=#4CFF4C>/dice <bet></color> - Roll dice vs house");
            SendReply(player, "  • Bet: 10-100 tokens");
            SendReply(player, "  • Win: 2x your bet");
            SendReply(player, "  • Cooldown: 30 seconds");
            SendReply(player, "");
            SendReply(player, "<color=#FFD700>More games coming soon!</color>");
        }
        
        #endregion
        
        #region Data Models
        
        internal class PlayerSession
        {
            public BasePlayer Player { get; set; }
            public PlayerProfile Profile { get; set; }
            public DateTime LastAction { get; set; }
            public string SelectedItem { get; set; }
            public bool IsInMatch { get; set; }
            public string EditingWeaponSlot { get; set; } // "primary" or "secondary"
            public string SelectedAttachmentCategory { get; set; } // "scopes", "silencers", "underbarrel"
            public string SelectedStoreCategory { get; set; } // "guns", "skins", or "outfits"
            public int GunsStorePage { get; set; } // Current page for gun store
            public int SkinsStorePage { get; set; } // Current page for skins store
            public DateTime LastDiceGame { get; set; } // Cooldown for dice game
            public string SelectedLoadoutTab { get; set; } // "weapons" or "outfit"
            
            internal PlayerSession(BasePlayer player, PlayerProfile profile)
            {
                Player = player;
                Profile = profile;
                LastAction = DateTime.UtcNow;
                EditingWeaponSlot = "primary"; // Default to editing primary
                SelectedAttachmentCategory = "scopes"; // Default to scopes tab
                SelectedStoreCategory = "guns"; // Default to guns store
                GunsStorePage = 0; // Start at first page
                SkinsStorePage = 0; // Start at first page
                SelectedLoadoutTab = "weapons"; // Default to weapons tab
            }
        }
        
        public class PlayerProfile
        {
            public ulong SteamID { get; set; }
            public List<Loadout> Loadouts { get; set; }
            public Dictionary<string, int> WeaponLevels { get; set; }
            public Dictionary<string, int> AttachmentLevels { get; set; }
            public List<string> OwnedSkins { get; set; }
            public List<string> OwnedArmor { get; set; } // List of owned armor shortnames
            public int Tokens { get; set; }
            public bool IsVIP { get; set; }
            public DateTime LastUpdated { get; set; }
            public int TotalKills { get; set; }
            public int TotalDeaths { get; set; }
            public int MatchesPlayed { get; set; }
            
            public PlayerProfile()
            {
                Loadouts = new List<Loadout>();
                WeaponLevels = new Dictionary<string, int>();
                AttachmentLevels = new Dictionary<string, int>();
                OwnedSkins = new List<string>();
                OwnedArmor = new List<string>();
            }
            
            public PlayerProfile(ulong steamId, int startingTokens) : this()
            {
                SteamID = steamId;
                Tokens = startingTokens;
                LastUpdated = DateTime.UtcNow;
                
                // Create default loadout
                Loadouts.Add(new Loadout
                {
                    Name = "Default",
                    Primary = "ak47",
                    Secondary = "pistol",
                    PrimaryAttachments = new Dictionary<string, string>(),
                    Skins = new Dictionary<string, string>()
                });
            }
        }
        
        public class Loadout
        {
            public string Name { get; set; }
            public string Primary { get; set; }
            public string Secondary { get; set; }
            public Dictionary<string, string> PrimaryAttachments { get; set; }
            public Dictionary<string, string> SecondaryAttachments { get; set; }
            public Dictionary<string, string> Skins { get; set; }
            public string Lethal { get; set; }
            public string Tactical { get; set; }
            public List<string> Perks { get; set; }
            // Outfit/Armor slots
            public string ArmorHead { get; set; }
            public string ArmorChest { get; set; }
            public string ArmorLegs { get; set; }
            public string ArmorHands { get; set; }
            public string ArmorFeet { get; set; }
            
            public Loadout()
            {
                PrimaryAttachments = new Dictionary<string, string>();
                SecondaryAttachments = new Dictionary<string, string>();
                Skins = new Dictionary<string, string>();
                Perks = new List<string>();
            }
        }
        
        #endregion
        
        #region Module: DomeManager
        
        internal class DomeManager
        {
            private KillaDome _plugin;
            private PluginConfig _config;
            private Match _currentMatch;
            private List<ulong> _matchQueue = new List<ulong>();
            
            internal DomeManager(KillaDome plugin, PluginConfig config)
            {
                _plugin = plugin;
                _config = config;
            }
            
            public void StartMatch()
            {
                if (_currentMatch != null && _currentMatch.IsActive)
                {
                    _plugin.PrintWarning("Match already in progress");
                    return;
                }
                
                _currentMatch = new Match
                {
                    MatchId = Guid.NewGuid().ToString(),
                    StartTime = DateTime.UtcNow,
                    IsActive = true
                };
                
                // Teleport queued players to arena
                foreach (var steamId in _matchQueue)
                {
                    var player = BasePlayer.FindByID(steamId);
                    if (player != null && player.IsConnected)
                    {
                        _plugin.TeleportToArena(player);
                        
                        var session = _plugin.GetSession(steamId);
                        if (session != null)
                        {
                            session.IsInMatch = true;
                        }
                    }
                }
                
                _plugin.Puts($"Match {_currentMatch.MatchId} started with {_matchQueue.Count} players");
                _matchQueue.Clear();
            }
            
            public void EndMatch()
            {
                if (_currentMatch == null || !_currentMatch.IsActive)
                {
                    return;
                }
                
                _currentMatch.IsActive = false;
                _currentMatch.EndTime = DateTime.UtcNow;
                
                // Return players to lobby
                foreach (var player in BasePlayer.activePlayerList)
                {
                    var session = _plugin.GetSession(player.userID);
                    if (session != null && session.IsInMatch)
                    {
                        _plugin.TeleportToLobby(player);
                        session.IsInMatch = false;
                    }
                }
                
                _plugin.Puts($"Match {_currentMatch.MatchId} ended");
            }
            
            public void AddToQueue(ulong steamId)
            {
                if (!_matchQueue.Contains(steamId))
                {
                    _matchQueue.Add(steamId);
                }
            }
            
            public void RemoveFromQueue(ulong steamId)
            {
                _matchQueue.Remove(steamId);
            }
        }
        
        internal class Match
        {
            public string MatchId { get; set; }
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }
            public bool IsActive { get; set; }
            public List<ulong> Participants { get; set; } = new List<ulong>();
        }
        
        #endregion
        
        
        #region Module: LobbyUI
        
        internal class LobbyUI
        {
            private KillaDome _plugin;
            private LoadoutEditor _loadoutEditor;
            private ForgeStationSystem _forgeStation;
            private StoreAPI _storeAPI;
            
            private const string UI_MAIN = "KillaDome.Main";
            private const string UI_TAB_CONTAINER = "KillaDome.TabContainer";
            
            internal LobbyUI(KillaDome plugin, LoadoutEditor loadoutEditor, ForgeStationSystem forgeStation, StoreAPI storeAPI)
            {
                _plugin = plugin;
                _loadoutEditor = loadoutEditor;
                _forgeStation = forgeStation;
                _storeAPI = storeAPI;
            }
            
            public void ShowLobbyUI(BasePlayer player)
            {
                ShowLobbyUIWithTab(player, "play");
            }
            
            public void ShowLobbyUIWithTab(BasePlayer player, string tab)
            {
                DestroyUI(player);
                
                var container = new CuiElementContainer();
                
                // Main background
                container.Add(new CuiPanel
                {
                    Image = { Color = "0 0 0 0.95" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    CursorEnabled = true
                }, "Overlay", UI_MAIN);
                
                // Title - optimized to use less space
                container.Add(new CuiLabel
                {
                    Text = { Text = "KILLADOME", FontSize = 34, Align = TextAnchor.MiddleCenter, Color = "1 0.5 0 1" },
                    RectTransform = { AnchorMin = "0.3 0.88", AnchorMax = "0.7 0.96" }
                }, UI_MAIN);
                
                // Tab buttons - increased height and spacing
                AddTabButton(container, UI_MAIN, "PLAY", 0, player, "killadome.tab play");
                AddTabButton(container, UI_MAIN, "LOADOUTS", 1, player, "killadome.tab loadouts");
                AddTabButton(container, UI_MAIN, "STORE", 2, player, "killadome.tab store");
                AddTabButton(container, UI_MAIN, "STATS", 3, player, "killadome.tab stats");
                AddTabButton(container, UI_MAIN, "SETTINGS", 4, player, "killadome.tab settings");
                
                // Close button
                container.Add(new CuiButton
                {
                    Button = { Color = "0.8 0.2 0.2 1", Command = "killadome.close" },
                    RectTransform = { AnchorMin = "0.92 0.92", AnchorMax = "0.98 0.98" },
                    Text = { Text = "X", FontSize = 20, Align = TextAnchor.MiddleCenter }
                }, UI_MAIN);
                
                // Tab content container - increased height for better use of space
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.1 0.1 0.1 0.9" },
                    RectTransform = { AnchorMin = "0.1 0.08", AnchorMax = "0.9 0.78" }
                }, UI_MAIN, UI_TAB_CONTAINER);
                
                // Show appropriate tab content
                switch (tab.ToLower())
                {
                    case "play":
                        ShowPlayTab(container, player);
                        break;
                    case "loadouts":
                        ShowLoadoutsTab(container, player);
                        break;
                    case "store":
                        ShowStoreTab(container, player);
                        break;
                    case "stats":
                        ShowStatsTab(container, player);
                        break;
                    case "settings":
                        ShowSettingsTab(container, player);
                        break;
                    default:
                        ShowPlayTab(container, player);
                        break;
                }
                
                CuiHelper.AddUi(player, container);
            }
            
            private void AddTabButton(CuiElementContainer container, string parent, string text, int index, BasePlayer player, string command)
            {
                float width = 0.15f;
                float spacing = 0.02f;
                float startX = 0.1f;
                float minX = startX + (width + spacing) * index;
                float maxX = minX + width;
                
                container.Add(new CuiButton
                {
                    Button = { Color = "0.3 0.3 0.3 1", Command = command },
                    RectTransform = { AnchorMin = $"{minX} 0.80", AnchorMax = $"{maxX} 0.86" },
                    Text = { Text = text, FontSize = 14, Align = TextAnchor.MiddleCenter }
                }, parent);
            }
            
            private void ShowPlayTab(CuiElementContainer container, BasePlayer player)
            {
                container.Add(new CuiLabel
                {
                    Text = { Text = "READY TO PLAY?", FontSize = 24, Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = "0.3 0.6", AnchorMax = "0.7 0.7" }
                }, UI_TAB_CONTAINER);
                
                // Join Queue button
                container.Add(new CuiButton
                {
                    Button = { Color = "0.2 0.8 0.2 1", Command = "killadome.joinqueue" },
                    RectTransform = { AnchorMin = "0.35 0.4", AnchorMax = "0.65 0.5" },
                    Text = { Text = "JOIN QUEUE", FontSize = 18, Align = TextAnchor.MiddleCenter }
                }, UI_TAB_CONTAINER);
                
                // Stats preview
                var session = _plugin.GetSession(player.userID);
                if (session != null)
                {
                    container.Add(new CuiLabel
                    {
                        Text = { Text = $"Blood Tokens: {session.Profile.Tokens}", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 0.8 0 1" },
                        RectTransform = { AnchorMin = "0.3 0.3", AnchorMax = "0.7 0.35" }
                    }, UI_TAB_CONTAINER);
                }
            }
            
            private void ShowLoadoutsTab(CuiElementContainer container, BasePlayer player)
            {
                var session = _plugin.GetSession(player.userID);
                if (session == null)
                {
                    var profile = _plugin._saveManager.LoadPlayerProfile(player.userID);
                    session = new PlayerSession(player, profile);
                    _plugin._activeSessions[player.userID] = session;
                }
                
                if (session.Profile.Loadouts.Count == 0)
                {
                    session.Profile.Loadouts.Add(new Loadout
                    {
                        Name = "Default",
                        Primary = "ak47",
                        Secondary = "pistol",
                        PrimaryAttachments = new Dictionary<string, string>(),
                        SecondaryAttachments = new Dictionary<string, string>(),
                        Skins = new Dictionary<string, string>()
                    });
                }
                
                var loadout = session.Profile.Loadouts[0];
                string editingSlot = session.EditingWeaponSlot ?? "primary";
                string currentWeapon = editingSlot == "primary" ? loadout.Primary : loadout.Secondary;
                
                if (string.IsNullOrEmpty(currentWeapon))
                {
                    currentWeapon = editingSlot == "primary" ? "ak47" : "pistol";
                }
                
                // === HEADER === (8% height, optimized positioning)
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.08 0.08 0.12 0.95" },
                    RectTransform = { AnchorMin = "0.05 0.90", AnchorMax = "0.95 0.98" }
                }, UI_TAB_CONTAINER, "LoadoutHeader");
                
                container.Add(new CuiPanel
                {
                    Image = { Color = "1 0.6 0.2 0.6" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.05" }
                }, "LoadoutHeader");
                
                container.Add(new CuiLabel
                {
                    Text = { Text = "LOADOUT EDITOR", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = "1 0.9 0.7 1" },
                    RectTransform = { AnchorMin = "0 0.05", AnchorMax = "1 1" }
                }, "LoadoutHeader");
                
                // === SUB-TABS === (Weapons vs Outfit Editor)
                string selectedLoadoutTab = session.SelectedLoadoutTab ?? "weapons";
                
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.06 0.06 0.08 0.9" },
                    RectTransform = { AnchorMin = "0.05 0.84", AnchorMax = "0.95 0.88" }
                }, UI_TAB_CONTAINER, "LoadoutSubTabs");
                
                // Weapons Tab
                bool isWeaponsActive = selectedLoadoutTab == "weapons";
                container.Add(new CuiButton
                {
                    Button = { Command = "killadome.loadouttab weapons", Color = isWeaponsActive ? "0.2 0.6 0.8 0.9" : "0.12 0.12 0.16 0.9" },
                    Text = { Text = "⚔ WEAPONS", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = isWeaponsActive ? "1 1 1 1" : "0.6 0.6 0.6 1" },
                    RectTransform = { AnchorMin = "0.02 0.1", AnchorMax = "0.35 0.9" }
                }, "LoadoutSubTabs");
                
                // Outfit Tab
                bool isOutfitActive = selectedLoadoutTab == "outfit";
                container.Add(new CuiButton
                {
                    Button = { Command = "killadome.loadouttab outfit", Color = isOutfitActive ? "0.2 0.6 0.8 0.9" : "0.12 0.12 0.16 0.9" },
                    Text = { Text = "👕 OUTFIT", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = isOutfitActive ? "1 1 1 1" : "0.6 0.6 0.6 1" },
                    RectTransform = { AnchorMin = "0.37 0.1", AnchorMax = "0.70 0.9" }
                }, "LoadoutSubTabs");
                
                // Show appropriate content based on selected tab
                if (selectedLoadoutTab == "outfit")
                {
                    ShowOutfitEditorContent(container, player, session, loadout);
                }
                else
                {
                    ShowWeaponsEditorContent(container, player, session, loadout, editingSlot, currentWeapon);
                }
            }
            
            private void ShowWeaponsEditorContent(CuiElementContainer container, BasePlayer player, PlayerSession session, Loadout loadout, string editingSlot, string currentWeapon)
            {
                
                // === WEAPON SELECTION === (16% height, adjusted for sub-tabs)
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.06 0.06 0.08 0.9" },
                    RectTransform = { AnchorMin = "0.05 0.66", AnchorMax = "0.95 0.82" }
                }, UI_TAB_CONTAINER, "WeaponSelection");
                
                container.Add(new CuiLabel
                {
                    Text = { Text = "SELECT WEAPONS", FontSize = 11, Align = TextAnchor.UpperLeft, Color = "0.8 0.8 0.8 1" },
                    RectTransform = { AnchorMin = "0.02 0.92", AnchorMax = "0.30 1" }
                }, "WeaponSelection");
                
                // PRIMARY WEAPON
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.12 0.12 0.16 0.95" },
                    RectTransform = { AnchorMin = "0.02 0.05", AnchorMax = "0.49 0.88" }
                }, "WeaponSelection", "PrimaryBox");
                
                container.Add(new CuiPanel
                {
                    Image = { Color = "1 0.6 0.2 0.4" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0.015 1" }
                }, "PrimaryBox");
                
                container.Add(new CuiLabel
                {
                    Text = { Text = "PRIMARY WEAPON", FontSize = 10, Align = TextAnchor.UpperCenter, Color = "1 0.8 0.5 1" },
                    RectTransform = { AnchorMin = "0.05 0.88", AnchorMax = "0.95 0.98" }
                }, "PrimaryBox");
                
                // Weapon image - using centralized config
                string primaryImageUrl = _plugin._gunConfig.GetGunImageUrl(loadout.Primary);
                container.Add(new CuiElement
                {
                    Parent = "PrimaryBox",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string)_plugin.ImageLibrary?.Call("GetImage", primaryImageUrl) },
                        new CuiRectTransformComponent { AnchorMin = "0.25 0.35", AnchorMax = "0.75 0.80" }
                    }
                });
                
                // Get display name from config
                string primaryDisplayName = _plugin._gunConfig.Guns.ContainsKey(loadout.Primary) 
                    ? _plugin._gunConfig.Guns[loadout.Primary].DisplayName 
                    : loadout.Primary.ToUpper();
                
                // Add background for better visibility
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.08 0.08 0.12 0.9" },
                    RectTransform = { AnchorMin = "0.05 0.20", AnchorMax = "0.95 0.30" }
                }, "PrimaryBox", "PrimaryNameBg");
                
                container.Add(new CuiLabel
                {
                    Text = { Text = primaryDisplayName, FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 0.9 0.5 1" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
                }, "PrimaryNameBg");
                
                container.Add(new CuiButton
                {
                    Button = { Command = "killadome.weapon.prev primary", Color = "0.2 0.6 0.8 0.9" },
                    Text = { Text = "< PREV", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.05 0.06", AnchorMax = "0.47 0.22" }
                }, "PrimaryBox");
                
                container.Add(new CuiButton
                {
                    Button = { Command = "killadome.weapon.next primary", Color = "0.2 0.6 0.8 0.9" },
                    Text = { Text = "NEXT >", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.53 0.06", AnchorMax = "0.95 0.22" }
                }, "PrimaryBox");
                
                // SECONDARY WEAPON
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.12 0.12 0.16 0.95" },
                    RectTransform = { AnchorMin = "0.51 0.05", AnchorMax = "0.98 0.88" }
                }, "WeaponSelection", "SecondaryBox");
                
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.5 0.7 1.0 0.4" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0.015 1" }
                }, "SecondaryBox");
                
                container.Add(new CuiLabel
                {
                    Text = { Text = "SECONDARY WEAPON", FontSize = 10, Align = TextAnchor.UpperCenter, Color = "0.7 0.9 1.0 1" },
                    RectTransform = { AnchorMin = "0.05 0.88", AnchorMax = "0.95 0.98" }
                }, "SecondaryBox");
                
                // Weapon image - using centralized config
                string secondaryImageUrl = _plugin._gunConfig.GetGunImageUrl(loadout.Secondary);
                container.Add(new CuiElement
                {
                    Parent = "SecondaryBox",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string)_plugin.ImageLibrary?.Call("GetImage", secondaryImageUrl) },
                        new CuiRectTransformComponent { AnchorMin = "0.25 0.35", AnchorMax = "0.75 0.80" }
                    }
                });
                
                // Get display name from config
                string secondaryDisplayName = _plugin._gunConfig.Guns.ContainsKey(loadout.Secondary) 
                    ? _plugin._gunConfig.Guns[loadout.Secondary].DisplayName 
                    : loadout.Secondary.ToUpper();
                
                // Add background for better visibility
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.08 0.08 0.12 0.9" },
                    RectTransform = { AnchorMin = "0.05 0.20", AnchorMax = "0.95 0.30" }
                }, "SecondaryBox", "SecondaryNameBg");
                
                container.Add(new CuiLabel
                {
                    Text = { Text = secondaryDisplayName, FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "0.7 0.9 1.0 1" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
                }, "SecondaryNameBg");
                
                container.Add(new CuiButton
                {
                    Button = { Command = "killadome.weapon.prev secondary", Color = "0.5 0.3 0.8 0.9" },
                    Text = { Text = "< PREV", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.05 0.06", AnchorMax = "0.47 0.22" }
                }, "SecondaryBox");
                
                container.Add(new CuiButton
                {
                    Button = { Command = "killadome.weapon.next secondary", Color = "0.5 0.3 0.8 0.9" },
                    Text = { Text = "NEXT >", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.53 0.06", AnchorMax = "0.95 0.22" }
                }, "SecondaryBox");
                
                // === EDITOR SECTION === (58% height, adjusted)
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.06 0.06 0.08 0.9" },
                    RectTransform = { AnchorMin = "0.05 0.06", AnchorMax = "0.95 0.64" }
                }, UI_TAB_CONTAINER, "EditorArea");
                
                container.Add(new CuiLabel
                {
                    Text = { Text = $"CUSTOMIZE: {currentWeapon.ToUpper()}", FontSize = 12, Align = TextAnchor.UpperLeft, Color = "0.8 0.8 0.8 1" },
                    RectTransform = { AnchorMin = "0.02 0.96", AnchorMax = "0.50 1" }
                }, "EditorArea");
                
                bool isPrimaryActive = editingSlot == "primary";
                
                container.Add(new CuiButton
                {
                    Button = { Command = "killadome.editweapon primary", Color = isPrimaryActive ? "0.2 0.6 0.8 0.9" : "0.15 0.15 0.18 0.9" },
                    Text = { Text = "PRIMARY", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = isPrimaryActive ? "1 1 1 1" : "0.6 0.6 0.6 1" },
                    RectTransform = { AnchorMin = "0.72 0.96", AnchorMax = "0.84 1" }
                }, "EditorArea");
                
                container.Add(new CuiButton
                {
                    Button = { Command = "killadome.editweapon secondary", Color = !isPrimaryActive ? "0.5 0.3 0.8 0.9" : "0.15 0.15 0.18 0.9" },
                    Text = { Text = "SECONDARY", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = !isPrimaryActive ? "1 1 1 1" : "0.6 0.6 0.6 1" },
                    RectTransform = { AnchorMin = "0.85 0.96", AnchorMax = "0.98 1" }
                }, "EditorArea");
                
                // LEFT PANEL - SKINS (with scrolling)
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.10 0.10 0.14 0.95" },
                    RectTransform = { AnchorMin = "0.02 0.02", AnchorMax = "0.49 0.93" }
                }, "EditorArea", "SkinsPanel");
                
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.15 0.10 0.20 0.9" },
                    RectTransform = { AnchorMin = "0 0.96", AnchorMax = "1 1" }
                }, "SkinsPanel");
                
                container.Add(new CuiLabel
                {
                    Text = { Text = "WEAPON SKINS", FontSize = 11, Align = TextAnchor.MiddleCenter, Color = "0.9 0.8 1.0 1" },
                    RectTransform = { AnchorMin = "0 0.96", AnchorMax = "1 1" }
                }, "SkinsPanel");
                
                // Scrollable content area for skins
                container.Add(new CuiPanel
                {
                    Image = { Color = "0 0 0 0" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.94" }
                }, "SkinsPanel", "SkinsScroll");
                
                // Skins list - Now using centralized configuration!
                var availableSkins = _plugin._gunConfig.GetSkinsForWeapon(currentWeapon);
                
                for (int i = 0; i < availableSkins.Length; i++)
                {
                    var skin = availableSkins[i];
                    float yMin = 0.96f - ((i + 1) * 0.20f);
                    float yMax = yMin + 0.18f;
                    
                    bool isOwned = session.Profile.OwnedSkins.Contains(skin.SkinId);
                    bool isEquipped = loadout.Skins.TryGetValue(skin.WeaponId, out string equippedSkin) && equippedSkin == skin.SkinId;
                    
                    container.Add(new CuiPanel
                    {
                        Image = { Color = isOwned ? "0.14 0.14 0.18 1" : "0.10 0.10 0.14 0.5" },
                        RectTransform = { AnchorMin = $"0.03 {yMin}", AnchorMax = $"0.97 {yMax}" }
                    }, "SkinsScroll", $"SkinCard_{i}");
                    
                    if (isEquipped)
                    {
                        container.Add(new CuiPanel
                        {
                            Image = { Color = "0.6 0.4 1.0 0.6" },
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "0.015 1" }
                        }, $"SkinCard_{i}");
                    }
                    
                    // Skin image - using ImageUrl from centralized config
                    container.Add(new CuiElement
                    {
                        Parent = $"SkinCard_{i}",
                        Components =
                        {
                            new CuiRawImageComponent { Png = (string)_plugin.ImageLibrary?.Call("GetImage", skin.ImageUrl) },
                            new CuiRectTransformComponent { AnchorMin = "0.05 0.05", AnchorMax = "0.35 0.45" }
                        }
                    });
                    
                    container.Add(new CuiLabel
                    {
                        Text = { Text = skin.Name, FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                        RectTransform = { AnchorMin = "0.40 0.70", AnchorMax = "0.95 0.90" }
                    }, $"SkinCard_{i}");
                    
                    string statusText = isEquipped ? "EQUIPPED" : (isOwned ? "OWNED" : "LOCKED");
                    string statusColor = isEquipped ? "0.4 1.0 0.4" : (isOwned ? "0.6 0.8 1.0" : "1.0 0.4 0.4");
                    
                    container.Add(new CuiLabel
                    {
                        Text = { Text = statusText, FontSize = 8, Align = TextAnchor.MiddleLeft, Color = $"{statusColor} 1" },
                        RectTransform = { AnchorMin = "0.40 0.50", AnchorMax = "0.70 0.68" }
                    }, $"SkinCard_{i}");
                    
                    if (isEquipped)
                    {
                        container.Add(new CuiButton
                        {
                            Button = { Command = "", Color = "0.2 0.6 0.2 0.5" },
                            Text = { Text = "EQUIPPED", FontSize = 9, Align = TextAnchor.MiddleCenter, Color = "0.8 1 0.8 1" },
                            RectTransform = { AnchorMin = "0.40 0.10", AnchorMax = "0.95 0.40" }
                        }, $"SkinCard_{i}");
                    }
                    else if (isOwned)
                    {
                        container.Add(new CuiButton
                        {
                            Button = { Command = $"killadome.applyskin {editingSlot} {skin.SkinId}", Color = "0.2 0.6 0.8 0.9" },
                            Text = { Text = "EQUIP", FontSize = 9, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                            RectTransform = { AnchorMin = "0.40 0.10", AnchorMax = "0.95 0.40" }
                        }, $"SkinCard_{i}");
                    }
                    else
                    {
                        container.Add(new CuiButton
                        {
                            Button = { Command = "", Color = "0.15 0.15 0.15 0.5" },
                            Text = { Text = "LOCKED", FontSize = 9, Align = TextAnchor.MiddleCenter, Color = "0.5 0.5 0.5 1" },
                            RectTransform = { AnchorMin = "0.40 0.10", AnchorMax = "0.95 0.40" }
                        }, $"SkinCard_{i}");
                    }
                }
                
                // RIGHT PANEL - ATTACHMENTS (with scrolling)
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.10 0.10 0.14 0.95" },
                    RectTransform = { AnchorMin = "0.51 0.02", AnchorMax = "0.98 0.93" }
                }, "EditorArea", "AttachmentsPanel");
                
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.10 0.15 0.20 0.9" },
                    RectTransform = { AnchorMin = "0 0.96", AnchorMax = "1 1" }
                }, "AttachmentsPanel");
                
                container.Add(new CuiLabel
                {
                    Text = { Text = "WEAPON ATTACHMENTS", FontSize = 11, Align = TextAnchor.MiddleCenter, Color = "0.7 1.0 1.0 1" },
                    RectTransform = { AnchorMin = "0 0.96", AnchorMax = "1 1" }
                }, "AttachmentsPanel");
                
                // Category tabs
                string selectedCategory = session.SelectedAttachmentCategory ?? "scopes";
                string[] categories = { "SCOPES", "BARREL", "UNDERBARREL" };
                
                for (int i = 0; i < categories.Length; i++)
                {
                    float xMin = 0.03f + (i * 0.323f);
                    float xMax = xMin + 0.31f;
                    
                    bool isSelected = categories[i].ToLower() == selectedCategory || 
                                     (categories[i] == "BARREL" && selectedCategory == "silencers");
                    
                    string categoryCommand = categories[i] == "BARREL" ? "silencers" : categories[i].ToLower();
                    
                    container.Add(new CuiButton
                    {
                        Button = { Command = $"killadome.attachcat {categoryCommand}", Color = isSelected ? "0.2 0.6 0.8 0.9" : "0.12 0.12 0.15 0.9" },
                        Text = { Text = categories[i], FontSize = 9, Align = TextAnchor.MiddleCenter, Color = isSelected ? "1 1 1 1" : "0.6 0.6 0.6 1" },
                        RectTransform = { AnchorMin = $"{xMin} 0.91", AnchorMax = $"{xMax} 0.95" }
                    }, "AttachmentsPanel");
                }
                
                // Scrollable content area for attachments
                container.Add(new CuiPanel
                {
                    Image = { Color = "0 0 0 0" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.89" }
                }, "AttachmentsPanel", "AttachmentsScroll");
                
                // Attachments list
                var allAttachments = new[]
                {
                    new { Name = "Small Scope", Id = "weapon.mod.small.scope", ImageId = "small_scope", Category = "scopes" },
                    new { Name = "8x Scope", Id = "weapon.mod.8x.scope", ImageId = "8x_scope", Category = "scopes" },
                    new { Name = "Holo Sight", Id = "weapon.mod.holosight", ImageId = "holo_sight", Category = "underbarrel" },
                    new { Name = "Laser Sight", Id = "weapon.mod.lasersight", ImageId = "laser_sight", Category = "underbarrel" },
                    new { Name = "Soda Can Silencer", Id = "weapon.mod.sodacansilencer", ImageId = "sodacan_silencer", Category = "silencers" },
                    new { Name = "Silencer", Id = "weapon.mod.silencer", ImageId = "silencer", Category = "silencers" },
                    new { Name = "Muzzle Brake", Id = "weapon.mod.muzzlebrake", ImageId = "muzzle_brake", Category = "silencers" },
                    new { Name = "Muzzle Boost", Id = "weapon.mod.muzzleboost", ImageId = "muzzle_boost", Category = "silencers" },
                };
                
                // Only show owned attachments
                var availableAttachments = allAttachments
                    .Where(a => a.Category == selectedCategory && session.Profile.OwnedSkins.Contains(a.Id))
                    .ToArray();
                
                // Show helpful message if no attachments owned in this category
                if (availableAttachments.Length == 0)
                {
                    container.Add(new CuiLabel
                    {
                        Text = { Text = "No attachments owned in this category.\nPurchase attachments in the Store Tab.", FontSize = 11, Align = TextAnchor.MiddleCenter, Color = "0.6 0.6 0.6 1" },
                        RectTransform = { AnchorMin = "0.1 0.35", AnchorMax = "0.9 0.65" }
                    }, "AttachmentsScroll");
                }
                
                for (int i = 0; i < availableAttachments.Length; i++)
                {
                    var att = availableAttachments[i];
                    float yMin = 0.96f - ((i + 1) * 0.20f);
                    float yMax = yMin + 0.18f;
                    
                    var attachments = editingSlot == "primary" ? loadout.PrimaryAttachments : loadout.SecondaryAttachments;
                    bool isEquipped = attachments.ContainsValue(att.Id);
                    
                    container.Add(new CuiPanel
                    {
                        Image = { Color = "0.14 0.14 0.18 1" },
                        RectTransform = { AnchorMin = $"0.03 {yMin}", AnchorMax = $"0.97 {yMax}" }
                    }, "AttachmentsScroll", $"AttCard_{i}");
                    
                    if (isEquipped)
                    {
                        container.Add(new CuiPanel
                        {
                            Image = { Color = "0.4 0.8 1.0 0.6" },
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "0.015 1" }
                        }, $"AttCard_{i}");
                    }
                    
                    // Attachment image
                    container.Add(new CuiElement
                    {
                        Parent = $"AttCard_{i}",
                        Components =
                        {
                            new CuiRawImageComponent { Png = (string)_plugin.ImageLibrary?.Call("GetImage", att.ImageId) },
                            new CuiRectTransformComponent { AnchorMin = "0.05 0.05", AnchorMax = "0.35 0.45" }
                        }
                    });
                    
                    container.Add(new CuiLabel
                    {
                        Text = { Text = att.Name, FontSize = 10, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                        RectTransform = { AnchorMin = "0.40 0.70", AnchorMax = "0.95 0.90" }
                    }, $"AttCard_{i}");
                    
                    string statusText = isEquipped ? "EQUIPPED" : "OWNED";
                    string statusColor = isEquipped ? "0.4 1.0 0.4" : "0.6 0.8 1.0";
                    
                    container.Add(new CuiLabel
                    {
                        Text = { Text = statusText, FontSize = 8, Align = TextAnchor.MiddleLeft, Color = $"{statusColor} 1" },
                        RectTransform = { AnchorMin = "0.40 0.50", AnchorMax = "0.70 0.68" }
                    }, $"AttCard_{i}");
                    
                    if (isEquipped)
                    {
                        container.Add(new CuiButton
                        {
                            Button = { Command = "", Color = "0.2 0.6 0.2 0.5" },
                            Text = { Text = "EQUIPPED", FontSize = 9, Align = TextAnchor.MiddleCenter, Color = "0.8 1 0.8 1" },
                            RectTransform = { AnchorMin = "0.40 0.10", AnchorMax = "0.95 0.40" }
                        }, $"AttCard_{i}");
                    }
                    else
                    {
                        container.Add(new CuiButton
                        {
                            Button = { Command = $"killadome.applyattachment {editingSlot} {att.Category} {att.Id}", Color = "0.2 0.6 0.8 0.9" },
                            Text = { Text = "EQUIP", FontSize = 9, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                            RectTransform = { AnchorMin = "0.40 0.10", AnchorMax = "0.95 0.40" }
                        }, $"AttCard_{i}");
                    }
                }
                
                // === FOOTER === (6% height)
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.08 0.08 0.12 0.9" },
                    RectTransform = { AnchorMin = "0.05 0.01", AnchorMax = "0.95 0.06" }
                }, UI_TAB_CONTAINER, "LoadoutFooter");
                
                container.Add(new CuiPanel
                {
                    Image = { Color = "1 0.6 0.2 0.6" },
                    RectTransform = { AnchorMin = "0 0.90", AnchorMax = "1 1" }
                }, "LoadoutFooter");
                
                container.Add(new CuiLabel
                {
                    Text = { Text = "Select weapons | Customize with skins and attachments | Purchase items in Store Tab", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "0.8 0.8 0.8 1" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.90" }
                }, "LoadoutFooter");
                
                container.Add(new CuiPanel
                {
                    Image = { Color = "1 0.6 0.2 0.4" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.05" }
                }, "LoadoutFooter");
                
                container.Add(new CuiLabel
                {
                    Text = { Text = "VIC", FontSize = 9, Align = TextAnchor.MiddleCenter, Color = "0.8 0.9 1.0 0.9" },
                    RectTransform = { AnchorMin = "0.05 0.15", AnchorMax = "0.95 0.85" }
                }, "LoadoutFooter");
            }
            
            private void ShowOutfitEditorContent(CuiElementContainer container, BasePlayer player, PlayerSession session, Loadout loadout)
            {
                // === OUTFIT CUSTOMIZATION SECTION === (76% height)
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.06 0.06 0.08 0.9" },
                    RectTransform = { AnchorMin = "0.05 0.06", AnchorMax = "0.95 0.82" }
                }, UI_TAB_CONTAINER, "OutfitEditorArea");
                
                container.Add(new CuiLabel
                {
                    Text = { Text = "CUSTOMIZE YOUR OUTFIT", FontSize = 14, Align = TextAnchor.UpperCenter, Color = "0.9 0.8 1.0 1" },
                    RectTransform = { AnchorMin = "0.02 0.96", AnchorMax = "0.98 1" }
                }, "OutfitEditorArea");
                
                // 5 Armor Slots in vertical tower (5 columns)
                string[] armorSlots = { "head", "chest", "legs", "hands", "feet" };
                string[] slotLabels = { "HEAD", "CHEST", "LEGS", "HANDS", "FEET" };
                string[] slotColors = { "1.0 0.3 0.3", "1.0 0.8 0.2", "0.3 0.6 1.0", "0.7 0.4 1.0", "0.4 1.0 0.5" };
                
                for (int i = 0; i < armorSlots.Length; i++)
                {
                    string slot = armorSlots[i];
                    string slotLabel = slotLabels[i];
                    string slotColor = slotColors[i];
                    
                    float xMin = 0.02f + (i * 0.195f);
                    float xMax = xMin + 0.185f;
                    
                    container.Add(new CuiPanel
                    {
                        Image = { Color = "0.10 0.10 0.14 0.95" },
                        RectTransform = { AnchorMin = $"{xMin} 0.02", AnchorMax = $"{xMax} 0.93" }
                    }, "OutfitEditorArea", $"ArmorSlot_{slot}");
                    
                    // Colored accent bar
                    container.Add(new CuiPanel
                    {
                        Image = { Color = $"{slotColor} 0.6" },
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "0.02 1" }
                    }, $"ArmorSlot_{slot}");
                    
                    // Slot label
                    container.Add(new CuiLabel
                    {
                        Text = { Text = slotLabel, FontSize = 11, Align = TextAnchor.UpperCenter, Color = $"{slotColor} 1" },
                        RectTransform = { AnchorMin = "0.05 0.93", AnchorMax = "0.95 0.99" }
                    }, $"ArmorSlot_{slot}");
                    
                    // Get owned armor for this slot
                    var ownedArmor = _plugin._outfitConfig.Armors
                        .Where(a => a.Slot == slot && session.Profile.OwnedArmor.Contains(a.ItemShortname))
                        .ToArray();
                    
                    // Get current armor shortname from loadout
                    string currentArmorShortname = slot switch
                    {
                        "head" => loadout.ArmorHead,
                        "chest" => loadout.ArmorChest,
                        "legs" => loadout.ArmorLegs,
                        "hands" => loadout.ArmorHands,
                        "feet" => loadout.ArmorFeet,
                        _ => null
                    };
                    
                    if (ownedArmor.Length == 0)
                    {
                        // No armor owned in this slot
                        container.Add(new CuiLabel
                        {
                            Text = { Text = $"No {slot}\narmor owned", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "0.5 0.5 0.5 1" },
                            RectTransform = { AnchorMin = "0.05 0.40", AnchorMax = "0.95 0.60" }
                        }, $"ArmorSlot_{slot}");
                        
                        container.Add(new CuiLabel
                        {
                            Text = { Text = "Purchase in\nOutfit Store", FontSize = 8, Align = TextAnchor.MiddleCenter, Color = "0.4 0.6 0.8 1" },
                            RectTransform = { AnchorMin = "0.05 0.25", AnchorMax = "0.95 0.38" }
                        }, $"ArmorSlot_{slot}");
                    }
                    else
                    {
                        // Find current armor in owned list
                        var currentArmor = ownedArmor.FirstOrDefault(a => a.ItemShortname == currentArmorShortname);
                        if (currentArmor == null) currentArmor = ownedArmor[0]; // Default to first owned
                        
                        // Armor image
                        container.Add(new CuiElement
                        {
                            Parent = $"ArmorSlot_{slot}",
                            Components =
                            {
                                new CuiRawImageComponent { Png = (string)_plugin.ImageLibrary?.Call("GetImage", currentArmor.ImageUrl) },
                                new CuiRectTransformComponent { AnchorMin = "0.15 0.45", AnchorMax = "0.85 0.85" }
                            }
                        });
                        
                        // Armor name
                        container.Add(new CuiPanel
                        {
                            Image = { Color = "0.08 0.08 0.12 0.9" },
                            RectTransform = { AnchorMin = "0.05 0.30", AnchorMax = "0.95 0.42" }
                        }, $"ArmorSlot_{slot}", $"ArmorNameBg_{slot}");
                        
                        container.Add(new CuiLabel
                        {
                            Text = { Text = currentArmor.DisplayName, FontSize = 9, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
                        }, $"ArmorNameBg_{slot}");
                        
                        // Status indicator
                        container.Add(new CuiLabel
                        {
                            Text = { Text = $"EQUIPPED", FontSize = 7, Align = TextAnchor.MiddleCenter, Color = "0.4 1.0 0.4 1" },
                            RectTransform = { AnchorMin = "0.05 0.22", AnchorMax = "0.95 0.28" }
                        }, $"ArmorSlot_{slot}");
                        
                        // PREV button
                        container.Add(new CuiButton
                        {
                            Button = { Command = $"killadome.armor.prev {slot}", Color = "0.2 0.5 0.7 0.9" },
                            Text = { Text = "<", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                            RectTransform = { AnchorMin = "0.05 0.05", AnchorMax = "0.47 0.18" }
                        }, $"ArmorSlot_{slot}");
                        
                        // NEXT button
                        container.Add(new CuiButton
                        {
                            Button = { Command = $"killadome.armor.next {slot}", Color = "0.2 0.5 0.7 0.9" },
                            Text = { Text = ">", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                            RectTransform = { AnchorMin = "0.53 0.05", AnchorMax = "0.95 0.18" }
                        }, $"ArmorSlot_{slot}");
                        
                        // Item count indicator
                        int currentIndex = Array.IndexOf(ownedArmor, currentArmor);
                        container.Add(new CuiLabel
                        {
                            Text = { Text = $"{currentIndex + 1}/{ownedArmor.Length}", FontSize = 7, Align = TextAnchor.MiddleCenter, Color = "0.7 0.7 0.7 1" },
                            RectTransform = { AnchorMin = "0.05 0.01", AnchorMax = "0.95 0.04" }
                        }, $"ArmorSlot_{slot}");
                    }
                }
                
                // === FOOTER === (4% height)
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.08 0.08 0.12 0.9" },
                    RectTransform = { AnchorMin = "0.05 0.01", AnchorMax = "0.95 0.04" }
                }, UI_TAB_CONTAINER, "OutfitFooter");
                
                container.Add(new CuiLabel
                {
                    Text = { Text = "Cycle through owned armor | Purchase more items in Outfit Store", FontSize = 9, Align = TextAnchor.MiddleCenter, Color = "0.8 0.8 0.8 1" },
                    RectTransform = { AnchorMin = "0.02 0", AnchorMax = "0.98 1" }
                }, "OutfitFooter");
            }
            
            private void ShowStoreTab(CuiElementContainer container, BasePlayer player)
            {
                var session = _plugin.GetSession(player.userID);
                if (session == null)
                {
                    // Create session if it doesn't exist
                    var profile = _plugin._saveManager.LoadPlayerProfile(player.userID);
                    session = new PlayerSession(player, profile);
                    _plugin._activeSessions[player.userID] = session;
                }
                
                string selectedCategory = session.SelectedStoreCategory ?? "guns";
                
                // ===== HEADER SECTION WITH GRADIENT ===== (Optimized: 14% height)
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.08 0.08 0.12 0.95" },
                    RectTransform = { AnchorMin = "0.05 0.84", AnchorMax = "0.95 0.98" }
                }, UI_TAB_CONTAINER, "StoreHeader");
                
                // Top accent line (cyan glow)
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.2 0.8 1.0 0.6" },
                    RectTransform = { AnchorMin = "0 0.98", AnchorMax = "1 1" }
                }, "StoreHeader");
                
                // Title with shadow effect
                container.Add(new CuiLabel
                {
                    Text = { Text = "━━━  S T O R E  ━━━", FontSize = 28, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.2 0.5", AnchorMax = "0.8 0.9" }
                }, "StoreHeader");
                
                // Token balance with icon and glow effect
                string tokenPanelName = "TokenPanel";
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.12 0.08 0.02 0.9" },
                    RectTransform = { AnchorMin = "0.75 0.15", AnchorMax = "0.95 0.75" }
                }, "StoreHeader", tokenPanelName);
                
                // Glow border for token panel
                container.Add(new CuiElement
                {
                    Parent = tokenPanelName,
                    Components =
                    {
                        new CuiImageComponent { Color = "1 0.6 0 0.4" },
                        new CuiOutlineComponent { Color = "1 0.6 0 0.8", Distance = "2 -2" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                    }
                });
                
                // Token icon (using unicode symbol)
                container.Add(new CuiLabel
                {
                    Text = { Text = "◆", FontSize = 22, Align = TextAnchor.MiddleRight, Color = "1 0.8 0 1" },
                    RectTransform = { AnchorMin = "0.05 0", AnchorMax = "0.35 1" }
                }, tokenPanelName);
                
                // Token amount
                container.Add(new CuiLabel
                {
                    Text = { Text = $"{session.Profile.Tokens}", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = "1 0.9 0.7 1" },
                    RectTransform = { AnchorMin = "0.35 0", AnchorMax = "0.95 1" }
                }, tokenPanelName);
                
                // ===== SUB-TAB BUTTONS ===== (Optimized: 5% height)
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.06 0.06 0.08 0.9" },
                    RectTransform = { AnchorMin = "0.05 0.78", AnchorMax = "0.95 0.83" }
                }, UI_TAB_CONTAINER, "StoreSubTabs");
                
                // Gun Store Sub-Tab
                bool isGunsSelected = selectedCategory == "guns";
                container.Add(new CuiButton
                {
                    Button = { Color = isGunsSelected ? "0.2 0.6 0.8 0.9" : "0.12 0.12 0.15 0.9", Command = "killadome.storecat guns" },
                    Text = { Text = "⚔ GUN STORE", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = isGunsSelected ? "1 1 1 1" : "0.6 0.6 0.6 1" },
                    RectTransform = { AnchorMin = "0.02 0.1", AnchorMax = "0.24 0.9" }
                }, "StoreSubTabs");
                
                // Skins Store Sub-Tab (NEW)
                bool isSkinsSelected = selectedCategory == "skins";
                container.Add(new CuiButton
                {
                    Button = { Color = isSkinsSelected ? "0.2 0.6 0.8 0.9" : "0.12 0.12 0.15 0.9", Command = "killadome.storecat skins" },
                    Text = { Text = "🎨 SKINS STORE", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = isSkinsSelected ? "1 1 1 1" : "0.6 0.6 0.6 1" },
                    RectTransform = { AnchorMin = "0.26 0.1", AnchorMax = "0.48 0.9" }
                }, "StoreSubTabs");
                
                // Outfit Store Sub-Tab
                bool isOutfitsSelected = selectedCategory == "outfits";
                container.Add(new CuiButton
                {
                    Button = { Color = isOutfitsSelected ? "0.2 0.6 0.8 0.9" : "0.12 0.12 0.15 0.9", Command = "killadome.storecat outfits" },
                    Text = { Text = "👕 OUTFIT STORE", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = isOutfitsSelected ? "1 1 1 1" : "0.6 0.6 0.6 1" },
                    RectTransform = { AnchorMin = "0.50 0.1", AnchorMax = "0.72 0.9" }
                }, "StoreSubTabs");
                
                // Display selected category content
                if (selectedCategory == "guns")
                {
                    ShowGunStoreContent(container, session, player);
                }
                else if (selectedCategory == "skins")
                {
                    ShowSkinsStoreContent(container, session, player);
                }
                else if (selectedCategory == "outfits")
                {
                    ShowOutfitStoreContent(container, session, player);
                }
                
                // ===== FOOTER INFO BAR =====
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.08 0.08 0.12 0.9" },
                    RectTransform = { AnchorMin = "0.05 0.02", AnchorMax = "0.95 0.06" }
                }, UI_TAB_CONTAINER, "StoreFooter");
                
                // Bottom accent line
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.2 0.8 1.0 0.4" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.05" }
                }, "StoreFooter");
                
                // Info text with icon
                container.Add(new CuiLabel
                {
                    Text = { Text = "💎 Purchase items with Blood Tokens to enhance your loadout  |  Earn tokens by eliminating enemies", FontSize = 11, Align = TextAnchor.MiddleCenter, Color = "0.8 0.9 1.0 0.9" },
                    RectTransform = { AnchorMin = "0.05 0.2", AnchorMax = "0.95 0.95" }
                }, "StoreFooter");
            }
            
            private void ShowGunStoreContent(CuiElementContainer container, PlayerSession session, BasePlayer player)
            {
                // Load base guns from centralized config
                var baseGuns = _plugin._gunConfig.Guns.Select(g => new
                {
                    Name = g.Value.DisplayName,
                    Cost = 0,
                    Id = g.Value.Id,
                    ImageId = g.Value.ImageUrl,
                    Shortname = g.Value.RustItemShortname
                }).ToArray();
                
                // Pagination for base guns (6 per page - smaller cards)
                int itemsPerPage = 6;
                int currentPage = session.GunsStorePage;
                int totalPages = (int)Math.Ceiling((double)baseGuns.Length / itemsPerPage);
                var pagedGuns = baseGuns.Skip(currentPage * itemsPerPage).Take(itemsPerPage).ToArray();
                
                // ===== BASE GUNS SECTION (LEFT SIDE) - PAGINATED =====
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.06 0.06 0.08 0.85" },
                    RectTransform = { AnchorMin = "0.05 0.08", AnchorMax = "0.48 0.77" }
                }, UI_TAB_CONTAINER, "BaseGunsSection");
                
                // Section header
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.15 0.1 0.2 0.9" },
                    RectTransform = { AnchorMin = "0 0.94", AnchorMax = "1 1" }
                }, "BaseGunsSection");
                
                // Section Title
                container.Add(new CuiLabel
                {
                    Text = { Text = "⚔  B A S E   G U N S", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0.9 0.8 1.0 1" },
                    RectTransform = { AnchorMin = "0.05 0.94", AnchorMax = "0.70 1" }
                }, "BaseGunsSection");
                
                // Page indicator
                if (totalPages > 1)
                {
                    container.Add(new CuiLabel
                    {
                        Text = { Text = $"{currentPage + 1}/{totalPages}", FontSize = 10, Align = TextAnchor.MiddleRight, Color = "0.8 0.8 0.8 1" },
                        RectTransform = { AnchorMin = "0.70 0.94", AnchorMax = "0.95 1" }
                    }, "BaseGunsSection");
                }
                
                // Smaller gun cards
                float cardHeight = 0.14f;
                float cardSpacing = 0.01f;
                float startY = 0.92f;
                
                for (int i = 0; i < pagedGuns.Length; i++)
                {
                    var item = pagedGuns[i];
                    float yMax = startY - (i * (cardHeight + cardSpacing));
                    float yMin = yMax - cardHeight;
                    
                    string cardName = $"BaseGunCard_{i}";
                    
                    container.Add(new CuiPanel
                    {
                        Image = { Color = "0.12 0.12 0.16 0.95" },
                        RectTransform = { AnchorMin = $"0.02 {yMin}", AnchorMax = $"0.98 {yMax}" }
                    }, "BaseGunsSection", cardName);
                    
                    // Preview box
                    string previewName = $"StorePreviewBaseGun_{i}";
                    container.Add(new CuiPanel
                    {
                        Image = { Color = "0.08 0.08 0.12 1" },
                        RectTransform = { AnchorMin = "0.05 0.10", AnchorMax = "0.30 0.90" }
                    }, cardName, previewName);
                    
                    // Try to add image
                    if (_plugin.ImageLibrary != null && _plugin.ImageLibrary.IsLoaded)
                    {
                        string imageId = (string)_plugin.ImageLibrary.Call("GetImage", item.ImageId);
                        if (!string.IsNullOrEmpty(imageId))
                        {
                            container.Add(new CuiElement
                            {
                                Parent = previewName,
                                Components =
                                {
                                    new CuiRawImageComponent { Png = imageId },
                                    new CuiRectTransformComponent { AnchorMin = "0.1 0.1", AnchorMax = "0.9 0.9" }
                                }
                            });
                        }
                    }
                    
                    // Name
                    container.Add(new CuiLabel
                    {
                        Text = { Text = item.Name, FontSize = 10, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" },
                        RectTransform = { AnchorMin = "0.35 0.55", AnchorMax = "0.95 0.85" }
                    }, cardName);
                    
                    // Shortname
                    container.Add(new CuiLabel
                    {
                        Text = { Text = item.Shortname, FontSize = 8, Align = TextAnchor.UpperLeft, Color = "0.6 0.6 0.6 1" },
                        RectTransform = { AnchorMin = "0.35 0.35", AnchorMax = "0.95 0.55" }
                    }, cardName);
                    
                    // Status
                    container.Add(new CuiLabel
                    {
                        Text = { Text = "UNLOCKED", FontSize = 9, Align = TextAnchor.MiddleLeft, Color = "0.4 1.0 0.4 1" },
                        RectTransform = { AnchorMin = "0.35 0.10", AnchorMax = "0.95 0.30" }
                    }, cardName);
                }
                
                // Pagination buttons
                if (totalPages > 1)
                {
                    bool canGoPrev = currentPage > 0;
                    container.Add(new CuiButton
                    {
                        Button = { Color = canGoPrev ? "0.2 0.6 0.8 0.9" : "0.3 0.3 0.3 0.5", Command = canGoPrev ? "killadome.storepage prev" : "" },
                        Text = { Text = "◀ PREV", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = canGoPrev ? "1 1 1 1" : "0.5 0.5 0.5 1" },
                        RectTransform = { AnchorMin = "0.02 0.01", AnchorMax = "0.32 0.05" }
                    }, "BaseGunsSection");
                    
                    bool canGoNext = currentPage < totalPages - 1;
                    container.Add(new CuiButton
                    {
                        Button = { Color = canGoNext ? "0.2 0.6 0.8 0.9" : "0.3 0.3 0.3 0.5", Command = canGoNext ? "killadome.storepage next" : "" },
                        Text = { Text = "NEXT ▶", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = canGoNext ? "1 1 1 1" : "0.5 0.5 0.5 1" },
                        RectTransform = { AnchorMin = "0.68 0.01", AnchorMax = "0.98 0.05" }
                    }, "BaseGunsSection");
                }
                
                // ===== ATTACHMENTS SECTION (RIGHT SIDE) - NO SCROLLING =====
                var attachments = new[]
                {
                    new { Name = "Small Scope", Cost = 250, Id = "weapon.mod.small.scope", ImageId = "small_scope", Category = "Optics" },
                    new { Name = "8x Scope", Cost = 400, Id = "weapon.mod.8x.scope", ImageId = "8x_scope", Category = "Optics" },
                    new { Name = "Holo Sight", Cost = 300, Id = "weapon.mod.holosight", ImageId = "holo_sight", Category = "Optics" },
                    new { Name = "Laser Sight", Cost = 250, Id = "weapon.mod.lasersight", ImageId = "laser_sight", Category = "Tactical" },
                    new { Name = "Soda Can Silencer", Cost = 150, Id = "weapon.mod.sodacansilencer", ImageId = "sodacan_silencer", Category = "Barrel" },
                    new { Name = "Oil Filter Silencer", Cost = 200, Id = "weapon.mod.oilfiltersilencer", ImageId = "oilfilter_silencer", Category = "Barrel" },
                    new { Name = "Silencer", Cost = 400, Id = "weapon.mod.silencer", ImageId = "silencer", Category = "Barrel" },
                    new { Name = "Muzzle Brake", Cost = 300, Id = "weapon.mod.muzzlebrake", ImageId = "muzzle_brake", Category = "Barrel" },
                    new { Name = "Muzzle Boost", Cost = 350, Id = "weapon.mod.muzzleboost", ImageId = "muzzle_boost", Category = "Barrel" }
                };
                
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.06 0.06 0.08 0.85" },
                    RectTransform = { AnchorMin = "0.52 0.08", AnchorMax = "0.95 0.77" }
                }, UI_TAB_CONTAINER, "AttachmentsSection");
                
                // Section header
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.1 0.15 0.2 0.9" },
                    RectTransform = { AnchorMin = "0 0.94", AnchorMax = "1 1" }
                }, "AttachmentsSection");
                
                // Section Title
                container.Add(new CuiLabel
                {
                    Text = { Text = "⚙  A T T A C H M E N T S", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0.7 1.0 1.0 1" },
                    RectTransform = { AnchorMin = "0.05 0.94", AnchorMax = "0.95 1" }
                }, "AttachmentsSection");
                
                // Smaller 3-column grid layout (fits all 9 attachments without scrolling)
                int itemsPerRow = 3;
                float cardWidth = 0.30f;
                float cardHeightAtt = 0.18f; // Smaller cards
                float spacingX = 0.02f;
                float spacingY = 0.01f;
                float startX = 0.02f;
                float startYAtt = 0.92f;
                
                for (int i = 0; i < attachments.Length; i++)
                {
                    var item = attachments[i];
                    int row = i / itemsPerRow;
                    int col = i % itemsPerRow;
                    
                    float xMin = startX + (col * (cardWidth + spacingX));
                    float xMax = xMin + cardWidth;
                    float yMax = startYAtt - (row * (cardHeightAtt + spacingY));
                    float yMin = yMax - cardHeightAtt;
                    
                    string cardName = $"AttCard_{i}";
                    
                    container.Add(new CuiPanel
                    {
                        Image = { Color = "0.10 0.12 0.15 0.95" },
                        RectTransform = { AnchorMin = $"{xMin} {yMin}", AnchorMax = $"{xMax} {yMax}" }
                    }, "AttachmentsSection", cardName);
                    
                    // Preview box (smaller)
                    string previewName = $"StorePreviewAtt_{i}";
                    container.Add(new CuiPanel
                    {
                        Image = { Color = "0.08 0.08 0.10 1" },
                        RectTransform = { AnchorMin = "0.15 0.40", AnchorMax = "0.85 0.85" }
                    }, cardName, previewName);
                    
                    // Try to add image
                    if (_plugin.ImageLibrary != null && _plugin.ImageLibrary.IsLoaded)
                    {
                        string imageId = (string)_plugin.ImageLibrary.Call("GetImage", item.ImageId);
                        if (!string.IsNullOrEmpty(imageId))
                        {
                            container.Add(new CuiElement
                            {
                                Parent = previewName,
                                Components =
                                {
                                    new CuiRawImageComponent { Png = imageId },
                                    new CuiRectTransformComponent { AnchorMin = "0.1 0.1", AnchorMax = "0.9 0.9" }
                                }
                            });
                        }
                    }
                    
                    // Category tag
                    string categoryColor = item.Category == "Optics" ? "0.5 0.7 1.0" : 
                                          item.Category == "Tactical" ? "1.0 0.5 0.5" : "0.5 1.0 0.7";
                    container.Add(new CuiLabel
                    {
                        Text = { Text = item.Category.ToUpper(), FontSize = 6, Align = TextAnchor.MiddleCenter, Color = $"{categoryColor} 0.8" },
                        RectTransform = { AnchorMin = "0.05 0.85", AnchorMax = "0.45 0.95" }
                    }, cardName);
                    
                    // Item name
                    container.Add(new CuiLabel
                    {
                        Text = { Text = item.Name, FontSize = 7, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                        RectTransform = { AnchorMin = "0.05 0.25", AnchorMax = "0.95 0.38" }
                    }, cardName);
                    
                    // Price
                    container.Add(new CuiLabel
                    {
                        Text = { Text = "◆", FontSize = 9, Align = TextAnchor.MiddleRight, Color = "1 0.8 0 1" },
                        RectTransform = { AnchorMin = "0.20 0.12", AnchorMax = "0.40 0.23" }
                    }, cardName);
                    
                    container.Add(new CuiLabel
                    {
                        Text = { Text = $"{item.Cost}", FontSize = 8, Align = TextAnchor.MiddleLeft, Color = "1 0.9 0.7 1" },
                        RectTransform = { AnchorMin = "0.40 0.12", AnchorMax = "0.70 0.23" }
                    }, cardName);
                    
                    // Buy button
                    bool canAfford = session.Profile.Tokens >= item.Cost;
                    string btnColor = canAfford ? "0.2 0.6 0.8" : "0.25 0.25 0.25";
                    string btnText = canAfford ? "BUY" : "🔒";
                    
                    container.Add(new CuiButton
                    {
                        Button = { Color = $"{btnColor} 0.9", Command = canAfford ? $"killadome.purchase {item.Id} {item.Cost}" : "" },
                        Text = { Text = btnText, FontSize = 8, Align = TextAnchor.MiddleCenter, Color = canAfford ? "1 1 1 1" : "0.5 0.5 0.5 1" },
                        RectTransform = { AnchorMin = "0.15 0.02", AnchorMax = "0.85 0.10" }
                    }, cardName);
                }
            }
            
            private void ShowSkinsStoreContent(CuiElementContainer container, PlayerSession session, BasePlayer player)
            {
                // Load weapon skins and outfit skins
                var weaponSkins = _plugin._gunConfig.Skins.Select(skin => new
                {
                    Name = skin.Name,
                    Cost = skin.Cost,
                    Id = skin.SkinId,
                    ImageId = skin.ImageUrl,
                    Tag = skin.Tag,
                    Rarity = skin.Rarity,
                    Type = "Weapon"
                }).ToList();
                
                // Placeholder outfit skins
                var outfitSkins = new[]
                {
                    new { Name = "Tactical Outfit", Cost = 800, Id = "outfit_tactical", ImageId = "outfit_tactical", Tag = "NEW", Rarity = "Epic", Type = "Outfit" },
                    new { Name = "Urban Camo", Cost = 600, Id = "outfit_urban", ImageId = "outfit_urban", Tag = "", Rarity = "Rare", Type = "Outfit" },
                    new { Name = "Desert Gear", Cost = 700, Id = "outfit_desert", ImageId = "outfit_desert", Tag = "HOT", Rarity = "Epic", Type = "Outfit" }
                };
                
                var allSkins = weaponSkins.Concat(outfitSkins).ToArray();
                
                // Pagination (12 items per page: 4 columns × 3 rows)
                int itemsPerPage = 12;
                int currentPage = session.SkinsStorePage;
                int totalPages = (int)Math.Ceiling((double)allSkins.Length / itemsPerPage);
                var pagedSkins = allSkins.Skip(currentPage * itemsPerPage).Take(itemsPerPage).ToArray();
                
                // ===== SKINS SECTION (FULL WIDTH) - PAGINATED =====
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.06 0.06 0.08 0.85" },
                    RectTransform = { AnchorMin = "0.05 0.08", AnchorMax = "0.95 0.77" }
                }, UI_TAB_CONTAINER, "SkinsStoreSection");
                
                // Section header
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.15 0.1 0.2 0.9" },
                    RectTransform = { AnchorMin = "0 0.94", AnchorMax = "1 1" }
                }, "SkinsStoreSection");
                
                // Section Title
                container.Add(new CuiLabel
                {
                    Text = { Text = "🎨  W E A P O N   &   O U T F I T   S K I N S", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1.0 0.9 0.8 1" },
                    RectTransform = { AnchorMin = "0.05 0.94", AnchorMax = "0.70 1" }
                }, "SkinsStoreSection");
                
                // Page indicator
                if (totalPages > 1)
                {
                    container.Add(new CuiLabel
                    {
                        Text = { Text = $"{currentPage + 1}/{totalPages}", FontSize = 10, Align = TextAnchor.MiddleRight, Color = "0.8 0.8 0.8 1" },
                        RectTransform = { AnchorMin = "0.70 0.94", AnchorMax = "0.95 1" }
                    }, "SkinsStoreSection");
                }
                
                // Smaller 4-column grid (more compact)
                int itemsPerRow = 4;
                float cardWidth = 0.23f; // Smaller width
                float cardHeight = 0.28f; // Smaller height
                float spacingX = 0.015f; // Smaller spacing
                float spacingY = 0.01f;
                float startX = 0.02f;
                float startY = 0.92f;
                
                for (int i = 0; i < pagedSkins.Length; i++)
                {
                    var item = pagedSkins[i];
                    int row = i / itemsPerRow;
                    int col = i % itemsPerRow;
                    
                    float xMin = startX + (col * (cardWidth + spacingX));
                    float xMax = xMin + cardWidth;
                    float yMax = startY - (row * (cardHeight + spacingY));
                    float yMin = yMax - cardHeight;
                    
                    string cardName = $"SkinCard_{i}";
                    
                    container.Add(new CuiPanel
                    {
                        Image = { Color = "0.12 0.12 0.16 0.95" },
                        RectTransform = { AnchorMin = $"{xMin} {yMin}", AnchorMax = $"{xMax} {yMax}" }
                    }, "SkinsStoreSection", cardName);
                    
                    // Preview box
                    string previewName = $"SkinPreview_{i}";
                    container.Add(new CuiPanel
                    {
                        Image = { Color = "0.08 0.08 0.12 1" },
                        RectTransform = { AnchorMin = "0.10 0.40", AnchorMax = "0.90 0.85" }
                    }, cardName, previewName);
                    
                    // Try to add image
                    if (_plugin.ImageLibrary != null && _plugin.ImageLibrary.IsLoaded)
                    {
                        string imageId = (string)_plugin.ImageLibrary.Call("GetImage", item.ImageId);
                        if (!string.IsNullOrEmpty(imageId))
                        {
                            container.Add(new CuiElement
                            {
                                Parent = previewName,
                                Components =
                                {
                                    new CuiRawImageComponent { Png = imageId },
                                    new CuiRectTransformComponent { AnchorMin = "0.1 0.1", AnchorMax = "0.9 0.9" }
                                }
                            });
                        }
                    }
                    
                    // Type badge
                    string typeColor = item.Type == "Weapon" ? "0.6 0.4 1.0" : "1.0 0.6 0.4";
                    container.Add(new CuiLabel
                    {
                        Text = { Text = item.Type.ToUpper(), FontSize = 6, Align = TextAnchor.MiddleCenter, Color = $"{typeColor} 0.9" },
                        RectTransform = { AnchorMin = "0.05 0.85", AnchorMax = "0.35 0.92" }
                    }, cardName);
                    
                    // Tag badge
                    if (!string.IsNullOrWhiteSpace(item.Tag))
                    {
                        string tagColor = item.Tag == "POPULAR" ? "1 0.3 0.3" : 
                                         item.Tag == "HOT" ? "1.0 0.5 0.0" : "0.3 1 0.5";
                        container.Add(new CuiPanel
                        {
                            Image = { Color = $"{tagColor} 0.9" },
                            RectTransform = { AnchorMin = "0.65 0.85", AnchorMax = "0.95 0.92" }
                        }, cardName);
                        
                        container.Add(new CuiLabel
                        {
                            Text = { Text = item.Tag, FontSize = 6, Align = TextAnchor.MiddleCenter, Color = "0.1 0.1 0.1 1" },
                            RectTransform = { AnchorMin = "0.65 0.85", AnchorMax = "0.95 0.92" }
                        }, cardName);
                    }
                    
                    // Item name
                    container.Add(new CuiLabel
                    {
                        Text = { Text = item.Name, FontSize = 8, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                        RectTransform = { AnchorMin = "0.05 0.28", AnchorMax = "0.95 0.38" }
                    }, cardName);
                    
                    // Rarity
                    string rarityColor = item.Rarity == "Epic" ? "0.6 0.3 1.0" : 
                                        item.Rarity == "Legendary" ? "1.0 0.6 0.2" :
                                        item.Rarity == "Rare" ? "0.3 0.7 1.0" : "0.5 0.5 0.5";
                    container.Add(new CuiLabel
                    {
                        Text = { Text = $"★ {item.Rarity}", FontSize = 7, Align = TextAnchor.MiddleCenter, Color = $"{rarityColor} 1" },
                        RectTransform = { AnchorMin = "0.05 0.20", AnchorMax = "0.95 0.28" }
                    }, cardName);
                    
                    // Price
                    container.Add(new CuiLabel
                    {
                        Text = { Text = "◆", FontSize = 8, Align = TextAnchor.MiddleRight, Color = "1 0.8 0 1" },
                        RectTransform = { AnchorMin = "0.25 0.10", AnchorMax = "0.45 0.18" }
                    }, cardName);
                    
                    container.Add(new CuiLabel
                    {
                        Text = { Text = $"{item.Cost}", FontSize = 7, Align = TextAnchor.MiddleLeft, Color = "1 0.9 0.7 1" },
                        RectTransform = { AnchorMin = "0.45 0.10", AnchorMax = "0.75 0.18" }
                    }, cardName);
                    
                    // Buy button
                    bool canAfford = session.Profile.Tokens >= item.Cost;
                    string btnColor = canAfford ? "0.2 0.7 0.3" : "0.3 0.3 0.3";
                    string btnText = canAfford ? "BUY" : "🔒";
                    
                    container.Add(new CuiButton
                    {
                        Button = { Color = $"{btnColor} 0.9", Command = canAfford ? $"killadome.purchase {item.Id} {item.Cost}" : "" },
                        Text = { Text = btnText, FontSize = 7, Align = TextAnchor.MiddleCenter, Color = canAfford ? "1 1 1 1" : "0.5 0.5 0.5 1" },
                        RectTransform = { AnchorMin = "0.15 0.02", AnchorMax = "0.85 0.08" }
                    }, cardName);
                }
                
                // Pagination buttons
                if (totalPages > 1)
                {
                    bool canGoPrev = currentPage > 0;
                    container.Add(new CuiButton
                    {
                        Button = { Color = canGoPrev ? "0.2 0.6 0.8 0.9" : "0.3 0.3 0.3 0.5", Command = canGoPrev ? "killadome.storepage prev" : "" },
                        Text = { Text = "◀ PREV", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = canGoPrev ? "1 1 1 1" : "0.5 0.5 0.5 1" },
                        RectTransform = { AnchorMin = "0.02 0.01", AnchorMax = "0.20 0.05" }
                    }, "SkinsStoreSection");
                    
                    bool canGoNext = currentPage < totalPages - 1;
                    container.Add(new CuiButton
                    {
                        Button = { Color = canGoNext ? "0.2 0.6 0.8 0.9" : "0.3 0.3 0.3 0.5", Command = canGoNext ? "killadome.storepage next" : "" },
                        Text = { Text = "NEXT ▶", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = canGoNext ? "1 1 1 1" : "0.5 0.5 0.5 1" },
                        RectTransform = { AnchorMin = "0.80 0.01", AnchorMax = "0.98 0.05" }
                    }, "SkinsStoreSection");
                }
            }
            
            private void ShowOutfitStoreContent(CuiElementContainer container, PlayerSession session, BasePlayer player)
            {
                // ===== OUTFIT STORE SECTION ===== (Optimized height)
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.06 0.06 0.08 0.85" },
                    RectTransform = { AnchorMin = "0.05 0.08", AnchorMax = "0.95 0.77" }
                }, UI_TAB_CONTAINER, "OutfitStoreSection");
                
                // Section header bar
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.2 0.1 0.15 0.9" },
                    RectTransform = { AnchorMin = "0 0.96", AnchorMax = "1 1" }
                }, "OutfitStoreSection");
                
                // Section Title with icon
                container.Add(new CuiLabel
                {
                    Text = { Text = "👕  A R M O R   S T O R E", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1.0 0.8 0.9 1" },
                    RectTransform = { AnchorMin = "0.05 0.96", AnchorMax = "0.95 1" }
                }, "OutfitStoreSection");
                
                // Decorative corner accents
                container.Add(new CuiPanel
                {
                    Image = { Color = "1.0 0.4 0.6 0.3" },
                    RectTransform = { AnchorMin = "0 0.96", AnchorMax = "0.02 1" }
                }, "OutfitStoreSection");
                
                container.Add(new CuiPanel
                {
                    Image = { Color = "1.0 0.4 0.6 0.3" },
                    RectTransform = { AnchorMin = "0.98 0.96", AnchorMax = "1 1" }
                }, "OutfitStoreSection");
                
                // Display armor items in a 4-column grid
                var armors = _plugin._outfitConfig.Armors;
                int cols = 4;
                int rows = 3;
                int itemsPerPage = cols * rows; // 12 items
                float cardWidth = 0.22f;
                float cardHeight = 0.28f;
                float spacingX = 0.02f;
                float spacingY = 0.02f;
                float startX = 0.05f;
                float startY = 0.92f;
                
                for (int i = 0; i < Math.Min(armors.Count, itemsPerPage); i++)
                {
                    var armor = armors[i];
                    int col = i % cols;
                    int row = i / cols;
                    
                    float xMin = startX + (col * (cardWidth + spacingX));
                    float yMax = startY - (row * (cardHeight + spacingY));
                    float xMax = xMin + cardWidth;
                    float yMin = yMax - cardHeight;
                    
                    string cardName = $"ArmorCard_{i}";
                    
                    // Card background
                    container.Add(new CuiPanel
                    {
                        Image = { Color = "0.12 0.08 0.15 0.95" },
                        RectTransform = { AnchorMin = $"{xMin} {yMin}", AnchorMax = $"{xMax} {yMax}" }
                    }, "OutfitStoreSection", cardName);
                    
                    // Armor image
                    if (_plugin.ImageLibrary != null && _plugin.ImageLibrary.IsLoaded)
                    {
                        string imageId = (string)_plugin.ImageLibrary.Call("GetImage", armor.ImageUrl);
                        if (!string.IsNullOrEmpty(imageId))
                        {
                            container.Add(new CuiElement
                            {
                                Parent = cardName,
                                Components =
                                {
                                    new CuiRawImageComponent { Png = imageId },
                                    new CuiRectTransformComponent { AnchorMin = "0.1 0.40", AnchorMax = "0.9 0.90" }
                                }
                            });
                        }
                    }
                    
                    // Armor name
                    container.Add(new CuiLabel
                    {
                        Text = { Text = armor.Name, FontSize = 8, Align = TextAnchor.UpperCenter, Color = "1 0.9 0.95 1" },
                        RectTransform = { AnchorMin = "0.05 0.30", AnchorMax = "0.95 0.38" }
                    }, cardName);
                    
                    // Slot label
                    string slotColor = armor.Slot == "head" ? "0.8 0.5 1.0" :
                                      armor.Slot == "chest" ? "0.5 0.8 1.0" :
                                      armor.Slot == "legs" ? "1.0 0.7 0.5" :
                                      armor.Slot == "hands" ? "0.7 1.0 0.5" : "1.0 0.5 0.5";
                    container.Add(new CuiLabel
                    {
                        Text = { Text = armor.Slot.ToUpper(), FontSize = 6, Align = TextAnchor.MiddleCenter, Color = $"{slotColor} 1" },
                        RectTransform = { AnchorMin = "0.05 0.22", AnchorMax = "0.95 0.30" }
                    }, cardName);
                    
                    // Rarity
                    string rarityColor = armor.Rarity == "Epic" ? "0.6 0.3 1.0" : 
                                        armor.Rarity == "Legendary" ? "1.0 0.6 0.2" :
                                        armor.Rarity == "Rare" ? "0.3 0.7 1.0" : "0.5 0.5 0.5";
                    container.Add(new CuiLabel
                    {
                        Text = { Text = $"★ {armor.Rarity}", FontSize = 7, Align = TextAnchor.MiddleCenter, Color = $"{rarityColor} 1" },
                        RectTransform = { AnchorMin = "0.05 0.14", AnchorMax = "0.95 0.22" }
                    }, cardName);
                    
                    // Price
                    container.Add(new CuiLabel
                    {
                        Text = { Text = "◆", FontSize = 8, Align = TextAnchor.MiddleRight, Color = "1 0.8 0 1" },
                        RectTransform = { AnchorMin = "0.25 0.06", AnchorMax = "0.45 0.14" }
                    }, cardName);
                    
                    container.Add(new CuiLabel
                    {
                        Text = { Text = $"{armor.Cost}", FontSize = 7, Align = TextAnchor.MiddleLeft, Color = "1 0.9 0.7 1" },
                        RectTransform = { AnchorMin = "0.45 0.06", AnchorMax = "0.75 0.14" }
                    }, cardName);
                    
                    // Buy button
                    bool canAfford = session.Profile.Tokens >= armor.Cost;
                    string btnColor = canAfford ? "0.7 0.2 0.5" : "0.3 0.3 0.3";
                    string btnText = canAfford ? "BUY" : "🔒";
                    
                    container.Add(new CuiButton
                    {
                        Button = { Color = $"{btnColor} 0.9", Command = canAfford ? $"killadome.purchase.armor {armor.ItemShortname} {armor.Cost}" : "" },
                        Text = { Text = btnText, FontSize = 7, Align = TextAnchor.MiddleCenter, Color = canAfford ? "1 1 1 1" : "0.5 0.5 0.5 1" },
                        RectTransform = { AnchorMin = "0.15 0.01", AnchorMax = "0.85 0.05" }
                    }, cardName);
                }
            }
            
            private void ShowStatsTab(CuiElementContainer container, BasePlayer player)
            {
                container.Add(new CuiLabel
                {
                    Text = { Text = "YOUR STATS", FontSize = 24, Align = TextAnchor.MiddleCenter, Color = "1 0.8 0 1" },
                    RectTransform = { AnchorMin = "0.3 0.8", AnchorMax = "0.7 0.9" }
                }, UI_TAB_CONTAINER);
                
                var session = _plugin.GetSession(player.userID);
                if (session != null)
                {
                    var profile = session.Profile;
                    
                    string[] stats = 
                    {
                        $"Kills: {profile.TotalKills}",
                        $"Deaths: {profile.TotalDeaths}",
                        $"K/D Ratio: {(profile.TotalDeaths > 0 ? ((float)profile.TotalKills / profile.TotalDeaths).ToString("F2") : profile.TotalKills.ToString())}",
                        $"Blood Tokens: {profile.Tokens}",
                        $"Matches Played: {profile.MatchesPlayed}",
                        $"VIP Status: {(profile.IsVIP ? "YES" : "NO")}"
                    };
                    
                    for (int i = 0; i < stats.Length; i++)
                    {
                        float yPos = 0.65f - (i * 0.08f);
                        container.Add(new CuiLabel
                        {
                            Text = { Text = stats[i], FontSize = 16, Align = TextAnchor.MiddleLeft },
                            RectTransform = { AnchorMin = $"0.25 {yPos}", AnchorMax = $"0.75 {yPos + 0.06f}" }
                        }, UI_TAB_CONTAINER);
                    }
                }
                else
                {
                    container.Add(new CuiLabel
                    {
                        Text = { Text = "No stats available", FontSize = 16, Align = TextAnchor.MiddleCenter },
                        RectTransform = { AnchorMin = "0.3 0.5", AnchorMax = "0.7 0.6" }
                    }, UI_TAB_CONTAINER);
                }
            }
            
            private void ShowSettingsTab(CuiElementContainer container, BasePlayer player)
            {
                container.Add(new CuiLabel
                {
                    Text = { Text = "SETTINGS", FontSize = 24, Align = TextAnchor.MiddleCenter, Color = "1 0.8 0 1" },
                    RectTransform = { AnchorMin = "0.3 0.8", AnchorMax = "0.7 0.9" }
                }, UI_TAB_CONTAINER);
                
                container.Add(new CuiLabel
                {
                    Text = { Text = "Plugin Settings", FontSize = 18, Align = TextAnchor.MiddleLeft },
                    RectTransform = { AnchorMin = "0.2 0.6", AnchorMax = "0.8 0.65" }
                }, UI_TAB_CONTAINER);
                
                string[] settings = 
                {
                    "UI Update Throttle: 100ms",
                    "Auto-Save Interval: 5 minutes",
                    "Max Weapon Level: 10",
                    "Max Attachment Level: 5"
                };
                
                for (int i = 0; i < settings.Length; i++)
                {
                    float yPos = 0.5f - (i * 0.08f);
                    container.Add(new CuiLabel
                    {
                        Text = { Text = settings[i], FontSize = 14, Align = TextAnchor.MiddleLeft },
                        RectTransform = { AnchorMin = $"0.25 {yPos}", AnchorMax = $"0.75 {yPos + 0.06f}" }
                    }, UI_TAB_CONTAINER);
                }
                
                container.Add(new CuiLabel
                {
                    Text = { Text = "Configure via KillaDome.json", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "0.7 0.7 0.7 1" },
                    RectTransform = { AnchorMin = "0.3 0.15", AnchorMax = "0.7 0.2" }
                }, UI_TAB_CONTAINER);
            }
            
            public void DestroyUI(BasePlayer player)
            {
                CuiHelper.DestroyUi(player, UI_MAIN);
            }
        }
        
        #endregion
        
        #region Module: LoadoutEditor
        
        internal class LoadoutEditor
        {
            private KillaDome _plugin;
            private AttachmentSystem _attachmentSystem;
            private Dictionary<ulong, string> _selectedItems = new Dictionary<ulong, string>();
            
            internal LoadoutEditor(KillaDome plugin, AttachmentSystem attachmentSystem)
            {
                _plugin = plugin;
                _attachmentSystem = attachmentSystem;
            }
            
            public void SelectItem(ulong steamId, string itemId)
            {
                _selectedItems[steamId] = itemId;
            }
            
            public string GetSelectedItem(ulong steamId)
            {
                _selectedItems.TryGetValue(steamId, out string itemId);
                return itemId;
            }
            
            public void ClearSelection(ulong steamId)
            {
                _selectedItems.Remove(steamId);
            }
            
            public bool TryEquipItem(ulong steamId, string slotId, string itemId)
            {
                var session = _plugin.GetSession(steamId);
                if (session == null || session.Profile.Loadouts.Count == 0)
                {
                    return false;
                }
                
                var loadout = session.Profile.Loadouts[0]; // Current loadout
                
                // Validate and equip based on slot type
                if (slotId.StartsWith("primary_att_"))
                {
                    string attachmentSlot = slotId.Replace("primary_att_", "");
                    loadout.PrimaryAttachments[attachmentSlot] = itemId;
                    return true;
                }
                
                return false;
            }
        }
        
        #endregion
        
        #region Module: AttachmentSystem
        
        internal class AttachmentSystem
        {
            private KillaDome _plugin;
            private PluginConfig _config;
            private Dictionary<string, AttachmentDefinition> _attachments;
            
            internal AttachmentSystem(KillaDome plugin, PluginConfig config)
            {
                _plugin = plugin;
                _config = config;
                InitializeAttachments();
            }
            
            private void InitializeAttachments()
            {
                _attachments = new Dictionary<string, AttachmentDefinition>
                {
                    ["silencer"] = new AttachmentDefinition
                    {
                        Id = "silencer",
                        Name = "Silencer",
                        Slot = "barrel",
                        MaxLevel = 5,
                        StatModifiers = new Dictionary<string, float>
                        {
                            ["noise_reduction"] = 0.8f,
                            ["damage"] = -0.05f
                        }
                    },
                    ["extended_mag"] = new AttachmentDefinition
                    {
                        Id = "extended_mag",
                        Name = "Extended Magazine",
                        Slot = "mag",
                        MaxLevel = 5,
                        StatModifiers = new Dictionary<string, float>
                        {
                            ["mag_size"] = 1.5f,
                            ["reload_speed"] = -0.1f
                        }
                    },
                    ["reflex"] = new AttachmentDefinition
                    {
                        Id = "reflex",
                        Name = "Reflex Sight",
                        Slot = "optic",
                        MaxLevel = 3,
                        StatModifiers = new Dictionary<string, float>
                        {
                            ["accuracy"] = 1.2f
                        }
                    }
                };
            }
            
            internal AttachmentDefinition GetAttachment(string attachmentId)
            {
                _attachments.TryGetValue(attachmentId, out var attachment);
                return attachment;
            }
            
            public Dictionary<string, float> CalculateWeaponStats(string weaponId, Dictionary<string, string> attachments)
            {
                var stats = new Dictionary<string, float>
                {
                    ["damage"] = 1.0f,
                    ["fire_rate"] = 1.0f,
                    ["accuracy"] = 1.0f,
                    ["mag_size"] = 1.0f,
                    ["reload_speed"] = 1.0f
                };
                
                foreach (var attachment in attachments.Values)
                {
                    var def = GetAttachment(attachment);
                    if (def != null)
                    {
                        foreach (var mod in def.StatModifiers)
                        {
                            if (stats.ContainsKey(mod.Key))
                            {
                                stats[mod.Key] *= mod.Value;
                            }
                            else
                            {
                                stats[mod.Key] = mod.Value;
                            }
                        }
                    }
                }
                
                return stats;
            }
        }
        
        internal class AttachmentDefinition
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Slot { get; set; }
            public int MaxLevel { get; set; }
            public Dictionary<string, float> StatModifiers { get; set; }
            public string VFXTag { get; set; }
            public string SFXTag { get; set; }
        }
        
        #endregion
        
        #region Module: WeaponProgression
        
        internal class WeaponProgression
        {
            private KillaDome _plugin;
            private PluginConfig _config;
            private Dictionary<string, WeaponDefinition> _weapons;
            
            internal WeaponProgression(KillaDome plugin, PluginConfig config)
            {
                _plugin = plugin;
                _config = config;
                InitializeWeapons();
            }
            
            private void InitializeWeapons()
            {
                _weapons = new Dictionary<string, WeaponDefinition>
                {
                    ["ak47"] = new WeaponDefinition
                    {
                        Id = "ak47",
                        Name = "AK-47",
                        MaxLevel = 10,
                        BaseStats = new Dictionary<string, float>
                        {
                            ["damage"] = 35f,
                            ["fire_rate"] = 0.13f,
                            ["accuracy"] = 0.75f
                        }
                    },
                    ["m249"] = new WeaponDefinition
                    {
                        Id = "m249",
                        Name = "M249",
                        MaxLevel = 10,
                        BaseStats = new Dictionary<string, float>
                        {
                            ["damage"] = 30f,
                            ["fire_rate"] = 0.1f,
                            ["accuracy"] = 0.7f
                        }
                    }
                };
            }
            
            public int GetWeaponLevel(ulong steamId, string weaponId)
            {
                var session = _plugin.GetSession(steamId);
                if (session == null) return 0;
                
                session.Profile.WeaponLevels.TryGetValue(weaponId, out int level);
                return level;
            }
            
            public bool UpgradeWeapon(ulong steamId, string weaponId, int cost)
            {
                var session = _plugin.GetSession(steamId);
                if (session == null) return false;
                
                int currentLevel = GetWeaponLevel(steamId, weaponId);
                if (currentLevel >= _config.MaxWeaponLevel)
                {
                    return false;
                }
                
                if (session.Profile.Tokens < cost)
                {
                    return false;
                }
                
                session.Profile.Tokens -= cost;
                session.Profile.WeaponLevels[weaponId] = currentLevel + 1;
                
                return true;
            }
        }
        
        internal class WeaponDefinition
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public int MaxLevel { get; set; }
            public Dictionary<string, float> BaseStats { get; set; }
        }
        
        #endregion
        
        #region Module: VFXManager
        
        internal class VFXManager
        {
            private KillaDome _plugin;
            
            internal VFXManager(KillaDome plugin)
            {
                _plugin = plugin;
            }
            
            public void PlayVFX(BasePlayer player, string vfxTag, Vector3 position)
            {
                // This would trigger client-side VFX
                player.SendConsoleCommand($"killadome.vfx {vfxTag} {position.x} {position.y} {position.z}");
            }
        }
        
        #endregion
        
        #region Module: SFXManager
        
        internal class SFXManager
        {
            private KillaDome _plugin;
            
            internal SFXManager(KillaDome plugin)
            {
                _plugin = plugin;
            }
            
            public void PlaySFX(BasePlayer player, string sfxTag)
            {
                // This would trigger client-side SFX
                player.SendConsoleCommand($"killadome.sfx {sfxTag}");
            }
        }
        
        #endregion
        
        #region Module: ForgeStationSystem
        
        internal class ForgeStationSystem
        {
            private KillaDome _plugin;
            private PluginConfig _config;
            private BloodTokenEconomy _economy;
            private AttachmentSystem _attachmentSystem;
            private WeaponProgression _weaponProgression;
            
            internal ForgeStationSystem(KillaDome plugin, PluginConfig config, BloodTokenEconomy economy, 
                AttachmentSystem attachmentSystem, WeaponProgression weaponProgression)
            {
                _plugin = plugin;
                _config = config;
                _economy = economy;
                _attachmentSystem = attachmentSystem;
                _weaponProgression = weaponProgression;
            }
            
            public int CalculateUpgradeCost(int currentLevel)
            {
                return 100 * (currentLevel + 1);
            }
            
            public bool UpgradeAttachment(ulong steamId, string attachmentId)
            {
                var session = _plugin.GetSession(steamId);
                if (session == null) return false;
                
                int currentLevel = 0;
                session.Profile.AttachmentLevels.TryGetValue(attachmentId, out currentLevel);
                
                if (currentLevel >= _config.MaxAttachmentLevel)
                {
                    return false;
                }
                
                int cost = CalculateUpgradeCost(currentLevel);
                
                if (!_economy.SpendTokens(steamId, cost))
                {
                    return false;
                }
                
                session.Profile.AttachmentLevels[attachmentId] = currentLevel + 1;
                return true;
            }
        }
        
        #endregion
        
        #region Module: BloodTokenEconomy
        
        internal class BloodTokenEconomy
        {
            private KillaDome _plugin;
            private PluginConfig _config;
            
            internal BloodTokenEconomy(KillaDome plugin, PluginConfig config)
            {
                _plugin = plugin;
                _config = config;
            }
            
            public void AwardTokens(ulong steamId, int amount)
            {
                var session = _plugin.GetSession(steamId);
                if (session == null) return;
                
                session.Profile.Tokens += amount;
                _plugin.LogDebug($"Awarded {amount} tokens to {steamId}. New balance: {session.Profile.Tokens}");
            }
            
            public bool SpendTokens(ulong steamId, int amount)
            {
                var session = _plugin.GetSession(steamId);
                if (session == null || session.Profile.Tokens < amount)
                {
                    return false;
                }
                
                session.Profile.Tokens -= amount;
                return true;
            }
            
            public int GetBalance(ulong steamId)
            {
                var session = _plugin.GetSession(steamId);
                return session?.Profile.Tokens ?? 0;
            }
        }
        
        #endregion
        
        #region Module: StoreAPI
        
        internal class StoreAPI
        {
            private KillaDome _plugin;
            private PluginConfig _config;
            private BloodTokenEconomy _economy;
            
            internal StoreAPI(KillaDome plugin, PluginConfig config, BloodTokenEconomy economy)
            {
                _plugin = plugin;
                _config = config;
                _economy = economy;
            }
            
            public bool PurchaseItem(ulong steamId, string itemId, int cost)
            {
                if (!_economy.SpendTokens(steamId, cost))
                {
                    return false;
                }
                
                var session = _plugin.GetSession(steamId);
                if (session == null) return false;
                
                session.Profile.OwnedSkins.Add(itemId);
                _plugin.LogDebug($"Player {steamId} purchased {itemId} for {cost} tokens");
                
                return true;
            }
            
            // Tebex integration stub
            public void ProcessTebexPurchase(ulong steamId, string packageId, string transactionId)
            {
                if (!_config.EnableTebex)
                {
                    _plugin.PrintWarning("Tebex integration is disabled");
                    return;
                }
                
                // TODO: Verify purchase with Tebex API using secret key
                // For now, just log
                _plugin.LogDebug($"Processing Tebex purchase: {steamId}, {packageId}, {transactionId}");
            }
        }
        
        #endregion
        
        #region Module: SaveManager
        
        internal class SaveManager
        {
            private KillaDome _plugin;
            private PluginConfig _config;
            private string _dataDirectory;
            
            internal SaveManager(KillaDome plugin, PluginConfig config)
            {
                _plugin = plugin;
                _config = config;
                _dataDirectory = Path.Combine(Interface.Oxide.DataDirectory, "KillaDome");
                
                if (!Directory.Exists(_dataDirectory))
                {
                    Directory.CreateDirectory(_dataDirectory);
                }
            }
            
            public PlayerProfile LoadPlayerProfile(ulong steamId)
            {
                string filePath = Path.Combine(_dataDirectory, $"{steamId}.json");
                
                if (!File.Exists(filePath))
                {
                    return new PlayerProfile(steamId, _config.StartingTokens);
                }
                
                try
                {
                    string json = File.ReadAllText(filePath);
                    var profile = JsonConvert.DeserializeObject<PlayerProfile>(json);
                    _plugin.LogDebug($"Loaded profile for {steamId}");
                    return profile ?? new PlayerProfile(steamId, _config.StartingTokens);
                }
                catch (Exception ex)
                {
                    _plugin.PrintError($"Failed to load profile for {steamId}: {ex.Message}");
                    return new PlayerProfile(steamId, _config.StartingTokens);
                }
            }
            
            public void SavePlayerProfile(PlayerProfile profile)
            {
                if (profile == null) return;
                
                profile.LastUpdated = DateTime.UtcNow;
                
                string filePath = Path.Combine(_dataDirectory, $"{profile.SteamID}.json");
                string tempPath = filePath + ".tmp";
                
                try
                {
                    string json = JsonConvert.SerializeObject(profile, Formatting.Indented);
                    File.WriteAllText(tempPath, json);
                    
                    // Atomic swap
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    File.Move(tempPath, filePath);
                    
                    _plugin.LogDebug($"Saved profile for {profile.SteamID}");
                }
                catch (Exception ex)
                {
                    _plugin.PrintError($"Failed to save profile for {profile.SteamID}: {ex.Message}");
                }
            }
        }
        
        #endregion
        
        #region Module: AntiExploit
        
        internal class AntiExploit
        {
            private KillaDome _plugin;
            private Dictionary<ulong, RateLimiter> _rateLimiters = new Dictionary<ulong, RateLimiter>();
            
            internal AntiExploit(KillaDome plugin)
            {
                _plugin = plugin;
            }
            
            public bool CheckRateLimit(ulong steamId, int maxActionsPerSecond = 5)
            {
                if (!_rateLimiters.TryGetValue(steamId, out var limiter))
                {
                    limiter = new RateLimiter(maxActionsPerSecond);
                    _rateLimiters[steamId] = limiter;
                }
                
                return limiter.AllowAction();
            }
            
            public bool ValidateAction(ulong steamId, string action)
            {
                if (!CheckRateLimit(steamId))
                {
                    _plugin.PrintWarning($"Rate limit exceeded for {steamId} on action {action}");
                    return false;
                }
                
                return true;
            }
        }
        
        internal class RateLimiter
        {
            private int _maxActions;
            private Queue<DateTime> _actions = new Queue<DateTime>();
            
            internal RateLimiter(int maxActionsPerSecond)
            {
                _maxActions = maxActionsPerSecond;
            }
            
            public bool AllowAction()
            {
                var now = DateTime.UtcNow;
                var cutoff = now.AddSeconds(-1);
                
                // Remove old actions
                while (_actions.Count > 0 && _actions.Peek() < cutoff)
                {
                    _actions.Dequeue();
                }
                
                if (_actions.Count >= _maxActions)
                {
                    return false;
                }
                
                _actions.Enqueue(now);
                return true;
            }
        }
        
        #endregion
        
        #region Module: TelemetrySystem
        
        internal class TelemetrySystem
        {
            private KillaDome _plugin;
            private Dictionary<string, int> _eventCounts = new Dictionary<string, int>();
            
            internal TelemetrySystem(KillaDome plugin)
            {
                _plugin = plugin;
            }
            
            public void RecordKill(ulong attackerId, ulong victimId)
            {
                IncrementEvent("kills");
                
                var session = _plugin.GetSession(attackerId);
                if (session != null)
                {
                    session.Profile.TotalKills++;
                }
                
                var victimSession = _plugin.GetSession(victimId);
                if (victimSession != null)
                {
                    victimSession.Profile.TotalDeaths++;
                }
            }
            
            public void RecordPurchase(ulong steamId, string itemId, int cost)
            {
                IncrementEvent("purchases");
                _plugin.LogDebug($"Telemetry: Purchase - {steamId}, {itemId}, {cost}");
            }
            
            private void IncrementEvent(string eventName)
            {
                if (!_eventCounts.ContainsKey(eventName))
                {
                    _eventCounts[eventName] = 0;
                }
                _eventCounts[eventName]++;
            }
            
            public Dictionary<string, int> GetStats()
            {
                return new Dictionary<string, int>(_eventCounts);
            }
        }
        
        #endregion
    }
}

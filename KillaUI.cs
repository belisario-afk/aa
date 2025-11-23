/*
 * KillaUI.cs - UI Plugin for KillaDome
 * 
 * Features:
 * - Lobby UI system with tabs (Play/Loadouts/Store/Stats/Settings)
 * - Drag-and-drop loadout editor
 * - Store interface for weapons, skins, and outfits
 * - Stats and settings displays
 * 
 * Version: 1.0.0
 * Author: KillaDome Dev Team
 */

using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("KillaUI", "KillaDome", "1.0.0")]
    [Description("UI system for KillaDome plugin")]
    public class KillaUI : RustPlugin
    {
        #region Fields
        
        [PluginReference]
        private Plugin KillaDome;
        
        [PluginReference]
        private Plugin ImageLibrary;
        
        private const string UI_MAIN = "KillaDome.Main";
        private const string UI_TAB_CONTAINER = "KillaDome.TabContainer";
        
        #endregion
        
        #region Oxide Hooks
        
        private void Init()
        {
            LogDebug("KillaUI initialized successfully");
        }
        
        private void OnServerInitialized()
        {
            if (KillaDome == null || !KillaDome.IsLoaded)
            {
                PrintWarning("KillaDome plugin not found! KillaUI requires KillaDome to function.");
                return;
            }
            
            LogDebug("KillaUI connected to KillaDome");
        }
        
        private void Unload()
        {
            // Clean up all UI
            foreach (var player in BasePlayer.activePlayerList)
            {
                DestroyUI(player);
            }
            
            LogDebug("KillaUI unloaded and cleaned up");
        }
        
        #endregion
        
        #region Public API Methods
        
        /// <summary>
        /// Show lobby UI with default (Play) tab
        /// </summary>
        public void ShowLobbyUI(BasePlayer player)
        {
            ShowLobbyUIWithTab(player, "play");
        }
        
        /// <summary>
        /// Test method - shows a simple UI to verify plugin communication works
        /// </summary>
        public void ShowTestUI(BasePlayer player)
        {
            try
            {
                if (player == null)
                {
                    PrintWarning("ShowTestUI: player is null");
                    return;
                }
                
                Puts($"[KillaUI] ShowTestUI called for {player.displayName}");
                
                var container = new CuiElementContainer();
                
                // Simple test panel
                container.Add(new CuiPanel
                {
                    Image = { Color = "1 0 0 0.8" },
                    RectTransform = { AnchorMin = "0.3 0.3", AnchorMax = "0.7 0.7" },
                    CursorEnabled = true
                }, "Overlay", "TestUI");
                
                container.Add(new CuiLabel
                {
                    Text = { Text = "TEST UI - Plugin Working!", FontSize = 24, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0 0.4", AnchorMax = "1 0.6" }
                }, "TestUI");
                
                container.Add(new CuiButton
                {
                    Button = { Close = "TestUI", Color = "0.8 0.2 0.2 1" },
                    RectTransform = { AnchorMin = "0.35 0.2", AnchorMax = "0.65 0.35" },
                    Text = { Text = "CLOSE", FontSize = 16, Align = TextAnchor.MiddleCenter }
                }, "TestUI");
                
                CuiHelper.AddUi(player, container);
                Puts($"[KillaUI] Test UI shown to {player.displayName} with {container.Count} elements");
            }
            catch (Exception ex)
            {
                PrintError($"Error in ShowTestUI: {ex}");
            }
        }
        
        /// <summary>
        /// Show lobby UI with specific tab
        /// </summary>
        public void ShowLobbyUIWithTab(BasePlayer player, string tab)
        {
            try
            {
                if (player == null)
                {
                    PrintWarning("ShowLobbyUIWithTab: player is null");
                    return;
                }
                
                if (KillaDome == null || !KillaDome.IsLoaded)
                {
                    PrintWarning($"ShowLobbyUIWithTab: KillaDome plugin not available for {player.displayName}");
                    player.ChatMessage("KillaDome plugin not loaded. Please contact an admin.");
                    return;
                }
                
                LogDebug($"ShowLobbyUIWithTab called for {player.displayName}, tab: {tab}");
                
                DestroyUI(player);
                
                var container = new CuiElementContainer();
                
                // Main background
                container.Add(new CuiPanel
                {
                    Image = { Color = "0 0 0 0.95" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    CursorEnabled = true
                }, "Overlay", UI_MAIN);
                
                LogDebug($"Added main panel for {player.displayName}");
                
                // Title
                container.Add(new CuiLabel
                {
                    Text = { Text = "KILLADOME", FontSize = 34, Align = TextAnchor.MiddleCenter, Color = "1 0.5 0 1" },
                    RectTransform = { AnchorMin = "0.3 0.88", AnchorMax = "0.7 0.96" }
                }, UI_MAIN);
                
                // Tab buttons
                AddTabButton(container, UI_MAIN, "PLAY", 0, "killadome.tab play");
                AddTabButton(container, UI_MAIN, "LOADOUTS", 1, "killadome.tab loadouts");
                AddTabButton(container, UI_MAIN, "STORE", 2, "killadome.tab store");
                AddTabButton(container, UI_MAIN, "STATS", 3, "killadome.tab stats");
                AddTabButton(container, UI_MAIN, "SETTINGS", 4, "killadome.tab settings");
                
                LogDebug($"Added tab buttons for {player.displayName}");
                
                // Close button
                container.Add(new CuiButton
                {
                    Button = { Color = "0.8 0.2 0.2 1", Command = "killadome.close" },
                    RectTransform = { AnchorMin = "0.92 0.92", AnchorMax = "0.98 0.98" },
                    Text = { Text = "X", FontSize = 20, Align = TextAnchor.MiddleCenter }
                }, UI_MAIN);
                
                // Tab content container
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.1 0.1 0.1 0.9" },
                    RectTransform = { AnchorMin = "0.1 0.08", AnchorMax = "0.9 0.78" }
                }, UI_MAIN, UI_TAB_CONTAINER);
                
                LogDebug($"Added content container for {player.displayName}, about to show tab: {tab}");
                
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
                
                LogDebug($"Tab content added for {player.displayName}, container has {container.Count} elements");
                
                CuiHelper.AddUi(player, container);
                LogDebug($"UI added for {player.displayName}, elements count: {container.Count}");
                Puts($"[KillaUI] Successfully rendered UI for {player.displayName} with {container.Count} elements");
            }
            catch (Exception ex)
            {
                PrintError($"Error in ShowLobbyUIWithTab for {player?.displayName}: {ex}");
                player?.ChatMessage($"Error showing UI: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Destroy all UI elements for a player
        /// </summary>
        public void DestroyUI(BasePlayer player)
        {
            if (player == null) return;
            CuiHelper.DestroyUi(player, UI_MAIN);
        }
        
        #endregion
        
        #region UI Console Commands
        
        [ConsoleCommand("killadome.close")]
        private void CmdUIClose(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            DestroyUI(player);
        }
        
        [ConsoleCommand("killadome.tab")]
        private void CmdUITab(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(1)) return;
            
            string tab = arg.Args[0].ToLower();
            ShowLobbyUIWithTab(player, tab);
            
            LogDebug($"Player {player.displayName} opened tab: {tab}");
        }
        
        [ConsoleCommand("killadome.joinqueue")]
        private void CmdUIJoinQueue(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            if (KillaDome == null || !KillaDome.IsLoaded)
            {
                player.ChatMessage("KillaDome plugin not available!");
                return;
            }
            
            // Call KillaDome plugin to handle queue logic
            KillaDome.Call("AddToQueue", player.userID);
            player.ChatMessage("You have joined the queue!");
        }
        
        [ConsoleCommand("killadome.weapon.prev")]
        private void CmdWeaponPrev(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(1)) return;
            
            if (!CheckRateLimit(player.userID))
            {
                player.ChatMessage("Please slow down!");
                return;
            }
            
            string slot = arg.Args[0];
            KillaDome?.Call("CycleWeapon", player, slot, -1);
            ShowLobbyUIWithTab(player, "loadouts");
        }
        
        [ConsoleCommand("killadome.weapon.next")]
        private void CmdWeaponNext(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(1)) return;
            
            if (!CheckRateLimit(player.userID))
            {
                player.ChatMessage("Please slow down!");
                return;
            }
            
            string slot = arg.Args[0];
            KillaDome?.Call("CycleWeapon", player, slot, 1);
            ShowLobbyUIWithTab(player, "loadouts");
        }
        
        [ConsoleCommand("killadome.purchase")]
        private void CmdPurchase(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(2)) return;
            
            if (!CheckRateLimit(player.userID))
            {
                player.ChatMessage("Please slow down!");
                return;
            }
            
            string itemId = arg.Args[0];
            if (!int.TryParse(arg.Args[1], out int cost))
            {
                player.ChatMessage("Invalid cost");
                return;
            }
            
            var result = KillaDome?.Call("PurchaseItem", player.userID, itemId, cost);
            if (result is bool success && success)
            {
                player.ChatMessage($"Successfully purchased {itemId}!");
                ShowLobbyUIWithTab(player, "store");
            }
            else
            {
                player.ChatMessage("Purchase failed!");
            }
        }
        
        [ConsoleCommand("killadome.purchase.armor")]
        private void CmdPurchaseArmor(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(2)) return;
            
            if (!CheckRateLimit(player.userID))
            {
                player.ChatMessage("Please slow down!");
                return;
            }
            
            string itemShortname = arg.Args[0];
            if (!int.TryParse(arg.Args[1], out int cost))
            {
                player.ChatMessage("Invalid cost");
                return;
            }
            
            var result = KillaDome?.Call("PurchaseArmor", player.userID, itemShortname, cost);
            if (result is bool success && success)
            {
                player.ChatMessage($"Successfully purchased armor!");
                ShowLobbyUIWithTab(player, "store");
            }
            else
            {
                player.ChatMessage("Purchase failed!");
            }
        }
        
        [ConsoleCommand("killadome.applyskin")]
        private void CmdApplySkin(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(2)) return;
            
            if (!CheckRateLimit(player.userID))
            {
                player.ChatMessage("Please slow down!");
                return;
            }
            
            string weapon = arg.Args[0];
            string skinId = arg.Args[1];
            
            var result = KillaDome?.Call("ApplySkin", player.userID, weapon, skinId);
            if (result is bool success && success)
            {
                player.ChatMessage($"Skin applied!");
                ShowLobbyUIWithTab(player, "loadouts");
            }
            else
            {
                player.ChatMessage("Failed to apply skin!");
            }
        }
        
        [ConsoleCommand("killadome.applyattachment")]
        private void CmdApplyAttachment(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(3)) return;
            
            if (!CheckRateLimit(player.userID))
            {
                player.ChatMessage("Please slow down!");
                return;
            }
            
            string weapon = arg.Args[0];
            string attachmentSlot = arg.Args[1];
            string attachmentId = arg.Args[2];
            
            var result = KillaDome?.Call("ApplyAttachment", player.userID, weapon, attachmentSlot, attachmentId);
            if (result is bool success && success)
            {
                player.ChatMessage($"Attachment applied!");
                ShowLobbyUIWithTab(player, "loadouts");
            }
            else
            {
                player.ChatMessage("Failed to apply attachment!");
            }
        }
        
        [ConsoleCommand("killadome.attachcat")]
        private void CmdAttachmentCategory(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(1)) return;
            
            string category = arg.Args[0].ToLower();
            if (category != "scopes" && category != "silencers" && category != "underbarrel") return;
            
            KillaDome?.Call("SetAttachmentCategory", player.userID, category);
            ShowLobbyUIWithTab(player, "loadouts");
        }
        
        [ConsoleCommand("killadome.editweapon")]
        private void CmdEditWeapon(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(1)) return;
            
            string slot = arg.Args[0].ToLower();
            if (slot != "primary" && slot != "secondary") return;
            
            KillaDome?.Call("SetEditingWeaponSlot", player.userID, slot);
            ShowLobbyUIWithTab(player, "loadouts");
        }
        
        [ConsoleCommand("killadome.storecat")]
        private void CmdStoreCategory(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(1)) return;
            
            string category = arg.Args[0].ToLower();
            if (category != "guns" && category != "skins" && category != "outfits") return;
            
            KillaDome?.Call("SetStoreCategory", player.userID, category);
            ShowLobbyUIWithTab(player, "store");
        }
        
        [ConsoleCommand("killadome.storepage")]
        private void CmdStorePage(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(1)) return;
            
            string direction = arg.Args[0].ToLower();
            KillaDome?.Call("ChangeStorePage", player.userID, direction);
            ShowLobbyUIWithTab(player, "store");
        }
        
        [ConsoleCommand("killadome.loadouttab")]
        private void CmdLoadoutTab(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(1)) return;
            
            string tab = arg.Args[0].ToLower();
            if (tab != "weapons" && tab != "outfit") return;
            
            KillaDome?.Call("SetLoadoutTab", player.userID, tab);
            ShowLobbyUIWithTab(player, "loadouts");
        }
        
        [ConsoleCommand("killadome.armor.next")]
        private void CmdArmorNext(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(1)) return;
            
            string slot = arg.Args[0].ToLower();
            KillaDome?.Call("CycleArmor", player.userID, slot, 1);
            ShowLobbyUIWithTab(player, "loadouts");
        }
        
        [ConsoleCommand("killadome.armor.prev")]
        private void CmdArmorPrev(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs(1)) return;
            
            string slot = arg.Args[0].ToLower();
            KillaDome?.Call("CycleArmor", player.userID, slot, -1);
            ShowLobbyUIWithTab(player, "loadouts");
        }
        
        #endregion
        
        #region Tab Rendering Methods
        
        private void AddTabButton(CuiElementContainer container, string parent, string text, int index, string command)
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
            var sessionData = KillaDome?.Call("GetSessionData", player.userID);
            if (sessionData is Dictionary<string, object> data)
            {
                var tokens = data.ContainsKey("Tokens") ? data["Tokens"] : 0;
                container.Add(new CuiLabel
                {
                    Text = { Text = $"Blood Tokens: {tokens}", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 0.8 0 1" },
                    RectTransform = { AnchorMin = "0.3 0.3", AnchorMax = "0.7 0.35" }
                }, UI_TAB_CONTAINER);
            }
        }
        
        private void ShowLoadoutsTab(CuiElementContainer container, BasePlayer player)
        {
            // Get session data from KillaDome
            var sessionData = KillaDome?.Call("GetSessionData", player.userID);
            if (sessionData == null)
            {
                PrintWarning($"GetSessionData returned null for {player.displayName}");
                container.Add(new CuiLabel
                {
                    Text = { Text = "Error loading session data.\nPlease try again.", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 0.3 0.3 1" },
                    RectTransform = { AnchorMin = "0.3 0.4", AnchorMax = "0.7 0.6" }
                }, UI_TAB_CONTAINER);
                return;
            }
            
            var data = sessionData as Dictionary<string, object>;
            if (data == null)
            {
                PrintWarning($"GetSessionData returned invalid type for {player.displayName}");
                container.Add(new CuiLabel
                {
                    Text = { Text = "Error: Invalid session data.", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 0.3 0.3 1" },
                    RectTransform = { AnchorMin = "0.3 0.4", AnchorMax = "0.7 0.6" }
                }, UI_TAB_CONTAINER);
                return;
            }
            
            var loadoutData = KillaDome?.Call("GetCurrentLoadout", player.userID) as Dictionary<string, object>;
            
            if (loadoutData == null)
            {
                PrintWarning($"GetCurrentLoadout returned null for {player.displayName}");
                container.Add(new CuiLabel
                {
                    Text = { Text = "Error loading loadout.\nPlease try again.", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 0.3 0.3 1" },
                    RectTransform = { AnchorMin = "0.3 0.4", AnchorMax = "0.7 0.6" }
                }, UI_TAB_CONTAINER);
                return;
            }
            
            LogDebug($"ShowLoadoutsTab: Got session and loadout data for {player.displayName}");
            
            // Get editing state
            string editingSlot = data.ContainsKey("EditingWeaponSlot") ? data["EditingWeaponSlot"] as string ?? "primary" : "primary";
            string selectedLoadoutTab = data.ContainsKey("SelectedLoadoutTab") ? data["SelectedLoadoutTab"] as string ?? "weapons" : "weapons";
            
            // === HEADER ===
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
            
            // === SUB-TABS ===
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
                ShowOutfitEditorContent(container, player, loadoutData);
            }
            else
            {
                ShowWeaponsEditorContent(container, player, loadoutData, editingSlot);
            }
        }
        
        private void ShowWeaponsEditorContent(CuiElementContainer container, BasePlayer player, Dictionary<string, object> loadoutData, string editingSlot)
        {
            string primaryWeapon = loadoutData.ContainsKey("Primary") ? loadoutData["Primary"] as string ?? "ak47" : "ak47";
            string secondaryWeapon = loadoutData.ContainsKey("Secondary") ? loadoutData["Secondary"] as string ?? "pistol" : "pistol";
            string currentWeapon = editingSlot == "primary" ? primaryWeapon : secondaryWeapon;
            
            // === WEAPON SELECTION ===
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
            RenderWeaponBox(container, "WeaponSelection", player, primaryWeapon, "PRIMARY WEAPON", "primary", 
                "0.02 0.05", "0.49 0.88", "1 0.6 0.2 0.4", "1 0.8 0.5 1", "0.2 0.6 0.8 0.9");
            
            // SECONDARY WEAPON
            RenderWeaponBox(container, "WeaponSelection", player, secondaryWeapon, "SECONDARY WEAPON", "secondary",
                "0.51 0.05", "0.98 0.88", "0.5 0.7 1.0 0.4", "0.7 0.9 1.0 1", "0.5 0.3 0.8 0.9");
            
            // === EDITOR SECTION ===
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
                RectTransform = { AnchorMin = "0.86 0.96", AnchorMax = "0.98 1" }
            }, "EditorArea");
            
            // Placeholder for attachment customization
            container.Add(new CuiLabel
            {
                Text = { Text = "Attachment customization", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "0.7 0.7 0.7 1" },
                RectTransform = { AnchorMin = "0.3 0.4", AnchorMax = "0.7 0.5" }
            }, "EditorArea");
        }
        
        private void RenderWeaponBox(CuiElementContainer container, string parent, BasePlayer player, string weaponId, 
            string title, string slot, string anchorMin, string anchorMax, string accentColor, string titleColor, string buttonColor)
        {
            string boxName = $"{slot}Box";
            
            container.Add(new CuiPanel
            {
                Image = { Color = "0.12 0.12 0.16 0.95" },
                RectTransform = { AnchorMin = anchorMin, AnchorMax = anchorMax }
            }, parent, boxName);
            
            container.Add(new CuiPanel
            {
                Image = { Color = accentColor },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0.015 1" }
            }, boxName);
            
            container.Add(new CuiLabel
            {
                Text = { Text = title, FontSize = 10, Align = TextAnchor.UpperCenter, Color = titleColor },
                RectTransform = { AnchorMin = "0.05 0.88", AnchorMax = "0.95 0.98" }
            }, boxName);
            
            // Get weapon display info from KillaDome
            var weaponInfo = KillaDome?.Call("GetWeaponInfo", weaponId) as Dictionary<string, object>;
            if (weaponInfo != null)
            {
                string displayName = weaponInfo.ContainsKey("DisplayName") ? weaponInfo["DisplayName"] as string ?? weaponId.ToUpper() : weaponId.ToUpper();
                string imageUrl = weaponInfo.ContainsKey("ImageUrl") ? weaponInfo["ImageUrl"] as string ?? "" : "";
                
                // Weapon image
                if (!string.IsNullOrEmpty(imageUrl) && ImageLibrary != null && ImageLibrary.IsLoaded)
                {
                    container.Add(new CuiElement
                    {
                        Parent = boxName,
                        Components =
                        {
                            new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", imageUrl) },
                            new CuiRectTransformComponent { AnchorMin = "0.25 0.35", AnchorMax = "0.75 0.80" }
                        }
                    });
                }
                
                // Weapon name background
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.08 0.08 0.12 0.9" },
                    RectTransform = { AnchorMin = "0.05 0.20", AnchorMax = "0.95 0.30" }
                }, boxName, $"{boxName}NameBg");
                
                container.Add(new CuiLabel
                {
                    Text = { Text = displayName, FontSize = 12, Align = TextAnchor.MiddleCenter, Color = titleColor },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
                }, $"{boxName}NameBg");
            }
            
            // Navigation buttons
            container.Add(new CuiButton
            {
                Button = { Command = $"killadome.weapon.prev {slot}", Color = buttonColor },
                Text = { Text = "< PREV", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.05 0.06", AnchorMax = "0.47 0.22" }
            }, boxName);
            
            container.Add(new CuiButton
            {
                Button = { Command = $"killadome.weapon.next {slot}", Color = buttonColor },
                Text = { Text = "NEXT >", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.53 0.06", AnchorMax = "0.95 0.22" }
            }, boxName);
        }
        
        private void ShowOutfitEditorContent(CuiElementContainer container, BasePlayer player, Dictionary<string, object> loadoutData)
        {
            container.Add(new CuiLabel
            {
                Text = { Text = "OUTFIT EDITOR", FontSize = 14, Align = TextAnchor.UpperCenter, Color = "1 0.8 0 1" },
                RectTransform = { AnchorMin = "0.3 0.7", AnchorMax = "0.7 0.8" }
            }, UI_TAB_CONTAINER);
            
            // Armor slots placeholder
            string[] armorSlots = { "head", "chest", "legs", "hands", "feet" };
            string[] armorLabels = { "HEAD", "CHEST", "LEGS", "HANDS", "FEET" };
            
            for (int i = 0; i < armorSlots.Length; i++)
            {
                float yPos = 0.55f - (i * 0.10f);
                
                container.Add(new CuiLabel
                {
                    Text = { Text = armorLabels[i], FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "0.8 0.8 0.8 1" },
                    RectTransform = { AnchorMin = $"0.15 {yPos}", AnchorMax = $"0.30 {yPos + 0.08f}" }
                }, UI_TAB_CONTAINER);
                
                // Previous button
                container.Add(new CuiButton
                {
                    Button = { Command = $"killadome.armor.prev {armorSlots[i]}", Color = "0.3 0.3 0.4 0.9" },
                    Text = { Text = "<", FontSize = 10, Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0.32 {yPos}", AnchorMax = $"0.40 {yPos + 0.08f}" }
                }, UI_TAB_CONTAINER);
                
                // Armor display area
                container.Add(new CuiPanel
                {
                    Image = { Color = "0.12 0.12 0.16 0.9" },
                    RectTransform = { AnchorMin = $"0.42 {yPos}", AnchorMax = $"0.58 {yPos + 0.08f}" }
                }, UI_TAB_CONTAINER);
                
                // Next button
                container.Add(new CuiButton
                {
                    Button = { Command = $"killadome.armor.next {armorSlots[i]}", Color = "0.3 0.3 0.4 0.9" },
                    Text = { Text = ">", FontSize = 10, Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0.60 {yPos}", AnchorMax = $"0.68 {yPos + 0.08f}" }
                }, UI_TAB_CONTAINER);
            }
        }
        
        private void ShowStoreTab(CuiElementContainer container, BasePlayer player)
        {
            // Get session data
            var sessionData = KillaDome?.Call("GetSessionData", player.userID) as Dictionary<string, object>;
            string selectedCategory = sessionData != null && sessionData.ContainsKey("SelectedStoreCategory") 
                ? sessionData["SelectedStoreCategory"] as string ?? "guns" 
                : "guns";
            
            // Header
            container.Add(new CuiPanel
            {
                Image = { Color = "0.08 0.08 0.12 0.95" },
                RectTransform = { AnchorMin = "0.05 0.88", AnchorMax = "0.95 0.96" }
            }, UI_TAB_CONTAINER, "StoreHeader");
            
            container.Add(new CuiLabel
            {
                Text = { Text = "BLOOD TOKEN STORE", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 0.8 0 1" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
            }, "StoreHeader");
            
            // Category tabs
            container.Add(new CuiPanel
            {
                Image = { Color = "0.06 0.06 0.08 0.9" },
                RectTransform = { AnchorMin = "0.05 0.82", AnchorMax = "0.95 0.86" }
            }, UI_TAB_CONTAINER, "StoreTabs");
            
            AddStoreCategoryTab(container, "StoreTabs", "GUNS", "guns", selectedCategory == "guns", 0);
            AddStoreCategoryTab(container, "StoreTabs", "SKINS", "skins", selectedCategory == "skins", 1);
            AddStoreCategoryTab(container, "StoreTabs", "OUTFITS", "outfits", selectedCategory == "outfits", 2);
            
            // Content area
            container.Add(new CuiPanel
            {
                Image = { Color = "0.05 0.05 0.07 0.9" },
                RectTransform = { AnchorMin = "0.05 0.08", AnchorMax = "0.95 0.80" }
            }, UI_TAB_CONTAINER, "StoreContent");
            
            // Show category-specific content
            switch (selectedCategory)
            {
                case "guns":
                    ShowGunsStoreContent(container, player);
                    break;
                case "skins":
                    ShowSkinsStoreContent(container, player);
                    break;
                case "outfits":
                    ShowOutfitsStoreContent(container, player);
                    break;
            }
        }
        
        private void AddStoreCategoryTab(CuiElementContainer container, string parent, string text, string category, bool isActive, int index)
        {
            float width = 0.30f;
            float spacing = 0.02f;
            float startX = 0.05f;
            float minX = startX + (width + spacing) * index;
            float maxX = minX + width;
            
            container.Add(new CuiButton
            {
                Button = { Command = $"killadome.storecat {category}", Color = isActive ? "0.2 0.6 0.8 0.9" : "0.12 0.12 0.16 0.9" },
                Text = { Text = text, FontSize = 12, Align = TextAnchor.MiddleCenter, Color = isActive ? "1 1 1 1" : "0.6 0.6 0.6 1" },
                RectTransform = { AnchorMin = $"{minX} 0.1", AnchorMax = $"{maxX} 0.9" }
            }, parent);
        }
        
        private void ShowGunsStoreContent(CuiElementContainer container, BasePlayer player)
        {
            container.Add(new CuiLabel
            {
                Text = { Text = "Available Weapons", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 0.8 0 1" },
                RectTransform = { AnchorMin = "0.3 0.7", AnchorMax = "0.7 0.8" }
            }, "StoreContent");
            
            // Get guns list from KillaDome
            var gunsData = KillaDome?.Call("GetAvailableGuns") as List<Dictionary<string, object>>;
            if (gunsData != null && gunsData.Count > 0)
            {
                int itemsPerRow = 3;
                int rows = Math.Min(2, (int)Math.Ceiling((double)gunsData.Count / itemsPerRow));
                
                for (int i = 0; i < Math.Min(gunsData.Count, 6); i++)
                {
                    int row = i / itemsPerRow;
                    int col = i % itemsPerRow;
                    
                    float itemWidth = 0.28f;
                    float itemHeight = 0.28f;
                    float spacingX = 0.03f;
                    float spacingY = 0.03f;
                    float startX = 0.05f;
                    float startY = 0.60f;
                    
                    float minX = startX + (col * (itemWidth + spacingX));
                    float maxX = minX + itemWidth;
                    float maxY = startY - (row * (itemHeight + spacingY));
                    float minY = maxY - itemHeight;
                    
                    var gunData = gunsData[i];
                    string gunId = gunData.ContainsKey("Id") ? gunData["Id"] as string ?? "" : "";
                    string displayName = gunData.ContainsKey("DisplayName") ? gunData["DisplayName"] as string ?? gunId : gunId;
                    int cost = gunData.ContainsKey("Cost") ? Convert.ToInt32(gunData["Cost"]) : 500;
                    
                    RenderStoreItem(container, "StoreContent", gunId, displayName, cost, 
                        minX.ToString("F2"), minY.ToString("F2"), maxX.ToString("F2"), maxY.ToString("F2"), "gun");
                }
                
                // Pagination
                AddStorePagination(container, "StoreContent");
            }
            else
            {
                container.Add(new CuiLabel
                {
                    Text = { Text = "No weapons available", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "0.7 0.7 0.7 1" },
                    RectTransform = { AnchorMin = "0.3 0.4", AnchorMax = "0.7 0.5" }
                }, "StoreContent");
            }
        }
        
        private void ShowSkinsStoreContent(CuiElementContainer container, BasePlayer player)
        {
            container.Add(new CuiLabel
            {
                Text = { Text = "Available Skins", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 0.8 0 1" },
                RectTransform = { AnchorMin = "0.3 0.8", AnchorMax = "0.7 0.9" }
            }, "StoreContent");
            
            container.Add(new CuiLabel
            {
                Text = { Text = "Browse weapon skins here", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "0.7 0.7 0.7 1" },
                RectTransform = { AnchorMin = "0.3 0.4", AnchorMax = "0.7 0.5" }
            }, "StoreContent");
            
            AddStorePagination(container, "StoreContent");
        }
        
        private void ShowOutfitsStoreContent(CuiElementContainer container, BasePlayer player)
        {
            container.Add(new CuiLabel
            {
                Text = { Text = "Available Outfits", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 0.8 0 1" },
                RectTransform = { AnchorMin = "0.3 0.8", AnchorMax = "0.7 0.9" }
            }, "StoreContent");
            
            container.Add(new CuiLabel
            {
                Text = { Text = "Browse armor and outfits here", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "0.7 0.7 0.7 1" },
                RectTransform = { AnchorMin = "0.3 0.4", AnchorMax = "0.7 0.5" }
            }, "StoreContent");
            
            AddStorePagination(container, "StoreContent");
        }
        
        private void RenderStoreItem(CuiElementContainer container, string parent, string itemId, string displayName, 
            int cost, string minX, string minY, string maxX, string maxY, string itemType)
        {
            string itemPanel = $"StoreItem_{itemId}";
            
            container.Add(new CuiPanel
            {
                Image = { Color = "0.12 0.12 0.16 0.95" },
                RectTransform = { AnchorMin = $"{minX} {minY}", AnchorMax = $"{maxX} {maxY}" }
            }, parent, itemPanel);
            
            // Item name
            container.Add(new CuiLabel
            {
                Text = { Text = displayName, FontSize = 11, Align = TextAnchor.UpperCenter, Color = "1 0.9 0.7 1" },
                RectTransform = { AnchorMin = "0.05 0.75", AnchorMax = "0.95 0.95" }
            }, itemPanel);
            
            // Image placeholder
            container.Add(new CuiPanel
            {
                Image = { Color = "0.2 0.2 0.25 0.9" },
                RectTransform = { AnchorMin = "0.15 0.35", AnchorMax = "0.85 0.70" }
            }, itemPanel);
            
            // Cost
            container.Add(new CuiLabel
            {
                Text = { Text = $"{cost} Tokens", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 0.8 0 1" },
                RectTransform = { AnchorMin = "0.1 0.20", AnchorMax = "0.9 0.30" }
            }, itemPanel);
            
            // Purchase button
            container.Add(new CuiButton
            {
                Button = { Command = $"killadome.purchase {itemId} {cost}", Color = "0.2 0.8 0.2 0.9" },
                Text = { Text = "BUY", FontSize = 10, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.15 0.05", AnchorMax = "0.85 0.18" }
            }, itemPanel);
        }
        
        private void AddStorePagination(CuiElementContainer container, string parent)
        {
            container.Add(new CuiButton
            {
                Button = { Command = "killadome.storepage prev", Color = "0.3 0.3 0.4 0.9" },
                Text = { Text = "< PREVIOUS", FontSize = 12, Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = "0.1 0.02", AnchorMax = "0.35 0.08" }
            }, parent);
            
            container.Add(new CuiButton
            {
                Button = { Command = "killadome.storepage next", Color = "0.3 0.3 0.4 0.9" },
                Text = { Text = "NEXT >", FontSize = 12, Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = "0.65 0.02", AnchorMax = "0.9 0.08" }
            }, parent);
        }
        
        private void ShowStatsTab(CuiElementContainer container, BasePlayer player)
        {
            container.Add(new CuiLabel
            {
                Text = { Text = "YOUR STATS", FontSize = 24, Align = TextAnchor.MiddleCenter, Color = "1 0.8 0 1" },
                RectTransform = { AnchorMin = "0.3 0.8", AnchorMax = "0.7 0.9" }
            }, UI_TAB_CONTAINER);
            
            var sessionData = KillaDome?.Call("GetSessionData", player.userID) as Dictionary<string, object>;
            if (sessionData != null)
            {
                var profileData = KillaDome?.Call("GetPlayerProfile", player.userID) as Dictionary<string, object>;
                if (profileData != null)
                {
                    int kills = profileData.ContainsKey("TotalKills") ? Convert.ToInt32(profileData["TotalKills"]) : 0;
                    int deaths = profileData.ContainsKey("TotalDeaths") ? Convert.ToInt32(profileData["TotalDeaths"]) : 0;
                    int tokens = profileData.ContainsKey("Tokens") ? Convert.ToInt32(profileData["Tokens"]) : 0;
                    int matches = profileData.ContainsKey("MatchesPlayed") ? Convert.ToInt32(profileData["MatchesPlayed"]) : 0;
                    bool isVIP = profileData.ContainsKey("IsVIP") && Convert.ToBoolean(profileData["IsVIP"]);
                    
                    float kd = deaths > 0 ? (float)kills / deaths : kills;
                    
                    string[] stats = 
                    {
                        $"Kills: {kills}",
                        $"Deaths: {deaths}",
                        $"K/D Ratio: {kd:F2}",
                        $"Blood Tokens: {tokens}",
                        $"Matches Played: {matches}",
                        $"VIP Status: {(isVIP ? "YES" : "NO")}"
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
        
        #endregion
        
        #region Helper Methods
        
        private Dictionary<ulong, DateTime> _rateLimitCache = new Dictionary<ulong, DateTime>();
        
        private bool CheckRateLimit(ulong steamId)
        {
            // Clean up old entries periodically to prevent memory leaks
            if (_rateLimitCache.Count > 100)
            {
                CleanupRateLimitCache();
            }
            
            if (_rateLimitCache.ContainsKey(steamId))
            {
                var lastAction = _rateLimitCache[steamId];
                if ((DateTime.UtcNow - lastAction).TotalMilliseconds < 100)
                {
                    return false;
                }
            }
            
            _rateLimitCache[steamId] = DateTime.UtcNow;
            return true;
        }
        
        private void CleanupRateLimitCache()
        {
            // Remove entries older than 1 minute for memory efficiency
            var cutoff = DateTime.UtcNow.AddMinutes(-1);
            var keysToRemove = new List<ulong>();
            
            foreach (var kvp in _rateLimitCache)
            {
                if (kvp.Value < cutoff)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _rateLimitCache.Remove(key);
            }
        }
        
        private void LogDebug(string message)
        {
            Puts($"[DEBUG] {message}");
        }
        
        #endregion
    }
}

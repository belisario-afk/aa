/*
 * KillaUIv2.cs - Complete UI Redesign with Modern Layouts
 * 
 * Features:
 * - Full screen design (1920x1080)
 * - 5 main tabs: PLAY, LOADOUTS, STORE, STATS, SETTINGS
 * - Professional layouts with proper pagination
 * - Clean separation of concerns
 * - Easy to maintain and extend
 * 
 * Version: 2.0.0
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
    [Info("KillaUIv2", "KillaDome", "2.0.0")]
    [Description("Modern redesigned UI for KillaDome with full screen layouts")]
    public class KillaUIv2 : RustPlugin
    {
        #region Fields
        
        [PluginReference]
        private Plugin KillaDome;
        
        [PluginReference]
        private Plugin ImageLibrary;
        
        // UI Constants
        private const string UI_MAIN = "KillaUI_Main";
        private const string UI_PANEL = "KillaUI_Panel";
        
        // Screen dimensions (1920x1080 reference)
        private const float SCREEN_WIDTH = 1920f;
        private const float SCREEN_HEIGHT = 1080f;
        
        // Colors
        private const string COLOR_PRIMARY = "0.1 0.1 0.1 0.95";      // Dark background
        private const string COLOR_SECONDARY = "0.15 0.15 0.15 0.95"; // Lighter panels
        private const string COLOR_ACCENT = "0.2 0.6 1 1";            // Blue accent
        private const string COLOR_SUCCESS = "0 0.8 0.2 1";           // Green for buy/success
        private const string COLOR_DANGER = "0.8 0.2 0 1";            // Red for danger
        private const string COLOR_WARNING = "1 0.7 0 1";             // Yellow/orange for currency
        private const string COLOR_TEXT = "1 1 1 1";                  // White text
        private const string COLOR_TEXT_DIM = "0.7 0.7 0.7 1";        // Gray text
        private const string COLOR_BORDER = "0.3 0.3 0.3 1";          // Border color
        
        // Session state tracking
        private Dictionary<ulong, PlayerUIState> _playerStates = new Dictionary<ulong, PlayerUIState>();
        
        #endregion
        
        #region Data Classes
        
        private class PlayerUIState
        {
            public string CurrentTab = "play";
            public string CurrentSubTab = "";
            public string CurrentStoreCategory = "guns";
            public string CurrentStoreSkinTab = "gun_skins";
            public int CurrentStorePage = 0;
            public string CurrentLoadoutTab = "loadout_editor";
            public string CurrentEditingWeaponSlot = "primary";
            public string CurrentAttachmentCategory = "scope";
            public int CurrentAttachmentPage = 0;
        }
        
        #endregion
        
        #region Oxide Hooks
        
        private void Init()
        {
            Puts("[KillaUIv2] [DEBUG] KillaUIv2 initialized");
        }
        
        private void OnServerInitialized()
        {
            if (KillaDome == null || !KillaDome.IsLoaded)
            {
                PrintWarning("[KillaUIv2] KillaDome plugin not found! UI will not function.");
                return;
            }
            
            Puts("[KillaUIv2] [DEBUG] KillaUIv2 connected to KillaDome");
        }
        
        private void Unload()
        {
            // Clean up all UIs
            foreach (var player in BasePlayer.activePlayerList)
            {
                DestroyUI(player);
            }
            
            _playerStates.Clear();
        }
        
        #endregion
        
        #region Public API Methods (Called by KillaDome)
        
        [HookMethod("ShowLobbyUI")]
        public void ShowLobbyUI(BasePlayer player)
        {
            if (player == null || !player.IsConnected) return;
            
            Puts($"[KillaUIv2] ShowLobbyUI called for {player.displayName}");
            
            // Initialize player state if needed
            if (!_playerStates.ContainsKey(player.userID))
            {
                _playerStates[player.userID] = new PlayerUIState();
            }
            
            // Show the main UI with current tab
            var state = _playerStates[player.userID];
            ShowMainUI(player, state.CurrentTab);
        }
        
        [HookMethod("DestroyUI")]
        public void DestroyUI(BasePlayer player)
        {
            if (player == null) return;
            
            CuiHelper.DestroyUi(player, UI_MAIN);
            CuiHelper.DestroyUi(player, UI_PANEL);
            
            _playerStates.Remove(player.userID);
        }
        
        #endregion
        
        #region Main UI Structure
        
        private void ShowMainUI(BasePlayer player, string tab)
        {
            if (player == null || !player.IsConnected) return;
            
            // Destroy existing UI
            CuiHelper.DestroyUi(player, UI_MAIN);
            
            var container = new CuiElementContainer();
            
            // Main background panel
            container.Add(new CuiPanel
            {
                Image = { Color = COLOR_PRIMARY },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                CursorEnabled = true
            }, "Overlay", UI_MAIN);
            
            // Header with title and tab buttons
            AddHeader(container, UI_MAIN, player, tab);
            
            // Content area based on selected tab
            AddTabContent(container, UI_MAIN, player, tab);
            
            // Close button
            AddCloseButton(container, UI_MAIN, player);
            
            CuiHelper.AddUi(player, container);
            
            Puts($"[KillaUIv2] UI rendered for {player.displayName}, tab: {tab}");
        }
        
        private void AddHeader(CuiElementContainer container, string parent, BasePlayer player, string currentTab)
        {
            // Header panel
            var header = container.Add(new CuiPanel
            {
                Image = { Color = COLOR_SECONDARY },
                RectTransform = { AnchorMin = "0 0.92", AnchorMax = "1 1" }
            }, parent, "Header");
            
            // Title
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "🎮 KILLADOME ARENA 🎮",
                    FontSize = 24,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_WARNING
                },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0.3 1" }
            }, header);
            
            // Tab buttons
            string[] tabs = { "PLAY", "LOADOUTS", "STORE", "STATS", "SETTINGS" };
            string[] tabIds = { "play", "loadouts", "store", "stats", "settings" };
            
            float tabWidth = 0.12f;
            float startX = 0.35f;
            
            for (int i = 0; i < tabs.Length; i++)
            {
                float minX = startX + (i * tabWidth);
                float maxX = minX + tabWidth - 0.01f;
                
                bool isActive = tabIds[i] == currentTab;
                
                var tabButton = container.Add(new CuiButton
                {
                    Button = {
                        Color = isActive ? COLOR_ACCENT : "0.2 0.2 0.2 0.95",
                        Command = $"killaui.tab {tabIds[i]}"
                    },
                    RectTransform = { AnchorMin = $"{minX} 0.1", AnchorMax = $"{maxX} 0.9" },
                    Text = {
                        Text = tabs[i],
                        FontSize = 14,
                        Align = TextAnchor.MiddleCenter,
                        Color = COLOR_TEXT
                    }
                }, header);
            }
        }
        
        private void AddCloseButton(CuiElementContainer container, string parent, BasePlayer player)
        {
            container.Add(new CuiButton
            {
                Button = {
                    Color = COLOR_DANGER,
                    Command = "killaui.close"
                },
                RectTransform = { AnchorMin = "0.95 0.93", AnchorMax = "0.99 0.99" },
                Text = {
                    Text = "✖",
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, parent);
        }
        
        private void AddTabContent(CuiElementContainer container, string parent, BasePlayer player, string tab)
        {
            // Content area (below header)
            var content = container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" }, // Transparent
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.92" }
            }, parent, "Content");
            
            // Route to appropriate tab renderer
            switch (tab)
            {
                case "play":
                    RenderPlayTab(container, content, player);
                    break;
                case "loadouts":
                    RenderLoadoutsTab(container, content, player);
                    break;
                case "store":
                    RenderStoreTab(container, content, player);
                    break;
                case "stats":
                    RenderStatsTab(container, content, player);
                    break;
                case "settings":
                    RenderSettingsTab(container, content, player);
                    break;
            }
        }
        
        #endregion
        
        #region PLAY Tab
        
        private void RenderPlayTab(CuiElementContainer container, string parent, BasePlayer player)
        {
            // Get session data from KillaDome
            var sessionData = KillaDome?.Call("GetSessionData", player.userID) as Dictionary<string, object>;
            
            int tokens = 0;
            int kills = 0;
            float kd = 0f;
            
            if (sessionData != null)
            {
                tokens = Convert.ToInt32(sessionData.ContainsKey("tokens") ? sessionData["tokens"] : 0);
                kills = Convert.ToInt32(sessionData.ContainsKey("totalKills") ? sessionData["totalKills"] : 0);
                int deaths = Convert.ToInt32(sessionData.ContainsKey("totalDeaths") ? sessionData["totalDeaths"] : 0);
                kd = deaths > 0 ? (float)kills / deaths : kills;
            }
            
            // Center join button
            var joinPanel = container.Add(new CuiPanel
            {
                Image = { Color = COLOR_SECONDARY },
                RectTransform = { AnchorMin = "0.35 0.5", AnchorMax = "0.65 0.7" }
            }, parent);
            
            // Join title
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "🎯 READY TO DOMINATE?",
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_WARNING
                },
                RectTransform = { AnchorMin = "0 0.7", AnchorMax = "1 0.9" }
            }, joinPanel);
            
            // Join button
            container.Add(new CuiButton
            {
                Button = {
                    Color = COLOR_SUCCESS,
                    Command = "killadome.joinqueue"
                },
                RectTransform = { AnchorMin = "0.25 0.35", AnchorMax = "0.75 0.6" },
                Text = {
                    Text = "JOIN QUEUE\nPlayers: 0/16",
                    FontSize = 16,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, joinPanel);
            
            // Map info
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "Map: Desert Arena",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT_DIM
                },
                RectTransform = { AnchorMin = "0 0.15", AnchorMax = "1 0.3" }
            }, joinPanel);
            
            // Stats panel
            var statsPanel = container.Add(new CuiPanel
            {
                Image = { Color = COLOR_SECONDARY },
                RectTransform = { AnchorMin = "0.1 0.2", AnchorMax = "0.35 0.45" }
            }, parent);
            
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "YOUR STATS",
                    FontSize = 16,
                    Align = TextAnchor.UpperCenter,
                    Color = COLOR_ACCENT
                },
                RectTransform = { AnchorMin = "0 0.8", AnchorMax = "1 1" }
            }, statsPanel);
            
            container.Add(new CuiLabel
            {
                Text = {
                    Text = $"Kills: {kills}\nK/D: {kd:F2}\nTokens: 💰 {tokens}",
                    FontSize = 14,
                    Align = TextAnchor.MiddleLeft,
                    Color = COLOR_TEXT
                },
                RectTransform = { AnchorMin = "0.1 0.2", AnchorMax = "0.9 0.75" }
            }, statsPanel);
            
            // Quick actions panel
            var actionsPanel = container.Add(new CuiPanel
            {
                Image = { Color = COLOR_SECONDARY },
                RectTransform = { AnchorMin = "0.65 0.2", AnchorMax = "0.9 0.45" }
            }, parent);
            
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "QUICK ACTIONS",
                    FontSize = 16,
                    Align = TextAnchor.UpperCenter,
                    Color = COLOR_ACCENT
                },
                RectTransform = { AnchorMin = "0 0.8", AnchorMax = "1 1" }
            }, actionsPanel);
            
            // Quick action buttons
            container.Add(new CuiButton
            {
                Button = {
                    Color = "0.3 0.3 0.3 0.95",
                    Command = "killaui.tab loadouts"
                },
                RectTransform = { AnchorMin = "0.1 0.5", AnchorMax = "0.9 0.7" },
                Text = {
                    Text = "View Loadout",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, actionsPanel);
            
            container.Add(new CuiButton
            {
                Button = {
                    Color = "0.3 0.3 0.3 0.95",
                    Command = "killaui.tab store"
                },
                RectTransform = { AnchorMin = "0.1 0.25", AnchorMax = "0.9 0.45" },
                Text = {
                    Text = "Buy Weapon",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                }
            }, actionsPanel);
        }
        
        #endregion
        
        #region LOADOUTS Tab (Placeholder - to be implemented)
        
        private void RenderLoadoutsTab(CuiElementContainer container, string parent, BasePlayer player)
        {
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "LOADOUTS TAB - Under Construction\n\nFeatures coming:\n• Loadout Editor\n• Outfit Editor\n• Weapon customization\n• Armor selection",
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                },
                RectTransform = { AnchorMin = "0.3 0.3", AnchorMax = "0.7 0.7" }
            }, parent);
        }
        
        #endregion
        
        #region STORE Tab (Placeholder - to be implemented)
        
        private void RenderStoreTab(CuiElementContainer container, string parent, BasePlayer player)
        {
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "STORE TAB - Under Construction\n\nFeatures coming:\n• Guns\n• Attachments\n• Clothing/Armor\n• Skins (Gun & Armor)",
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                },
                RectTransform = { AnchorMin = "0.3 0.3", AnchorMax = "0.7 0.7" }
            }, parent);
        }
        
        #endregion
        
        #region STATS Tab (Placeholder - to be implemented)
        
        private void RenderStatsTab(CuiElementContainer container, string parent, BasePlayer player)
        {
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "STATS TAB - Under Construction\n\nFeatures coming:\n• Combat Performance\n• Match History\n• Progression",
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                },
                RectTransform = { AnchorMin = "0.3 0.3", AnchorMax = "0.7 0.7" }
            }, parent);
        }
        
        #endregion
        
        #region SETTINGS Tab (Placeholder - to be implemented)
        
        private void RenderSettingsTab(CuiElementContainer container, string parent, BasePlayer player)
        {
            container.Add(new CuiLabel
            {
                Text = {
                    Text = "SETTINGS TAB - Under Construction\n\nFeatures coming:\n• Gameplay Settings\n• Notifications\n• Account Management",
                    FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = COLOR_TEXT
                },
                RectTransform = { AnchorMin = "0.3 0.3", AnchorMax = "0.7 0.7" }
            }, parent);
        }
        
        #endregion
        
        #region Console Commands
        
        [ConsoleCommand("killaui.tab")]
        private void CmdChangeTab(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            string tab = arg.GetString(0, "play");
            
            // Update state
            if (!_playerStates.ContainsKey(player.userID))
            {
                _playerStates[player.userID] = new PlayerUIState();
            }
            _playerStates[player.userID].CurrentTab = tab;
            
            // Refresh UI
            ShowMainUI(player, tab);
        }
        
        [ConsoleCommand("killaui.close")]
        private void CmdClose(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            
            DestroyUI(player);
        }
        
        #endregion
        
        #region Helper Methods
        
        private void LogDebug(string message)
        {
            Puts($"[DEBUG] {message}");
        }
        
        #endregion
    }
}

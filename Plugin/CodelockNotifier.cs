/* 
 * --- Copyright ---
 * This resource is licensed and copyrighted, which means, any attempt to copy, modify, merge, publish, distribute, sublicense, 
 * or sell copies of it without the Author's consent, can lead to legal accountability.
 * 
 * 
 * --- Author ---
 * Dana (Dana#5247)
 * 
 * 
 * --- Support ---
 * Email - dana.plugins.business@gmail.com
 * Website - PluginsCraft.com
 * 
 * 
 * --- Donate ---
 * PayPal - ahmadhamid18juli2000@gmail.com
 * 
 * 
 * Copyright (c) 2020 PluginsCraft.com
 */

using Newtonsoft.Json;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("CodeLockNotifier", "Dana", "0.2.0")]
    [Description("Monitors all code locks placed and notifies in case of team limit violation.")]
    public class CodeLockNotifier : RustPlugin
    {
        #region Permissions

        #endregion Permissions

        #region Private Fields
        private PluginConfig _pluginConfig;
        private string _configPath;

        private const string ConsoleMessage = "{0}[{1}] has been authorized in the code lock owned by {2}[{3}] in ({4}) - Current Authorizations ({5})";
        private const string LogFileMessage = "{0}[{1}] has been authorized in the code lock owned by {2}[{3}] in ({4}) - Current Authorizations ({5})";
        #endregion Private Fields

        #region Hooks
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Loading Default Config");
        }
        protected override void LoadConfig()
        {
            _configPath = $"{Manager.ConfigPath}/{Name}.json";
            var newConfig = new DynamicConfigFile(_configPath);
            if (!newConfig.Exists())
            {
                LoadDefaultConfig();
                newConfig.Save();
            }
            try
            {
                newConfig.Load();
            }
            catch (Exception ex)
            {
                RaiseError("Failed to load config file (is the config file corrupt?) (" + ex.Message + ")");
                return;
            }

            newConfig.Settings.DefaultValueHandling = DefaultValueHandling.Populate;
            _pluginConfig = newConfig.ReadObject<PluginConfig>();
            if (_pluginConfig.CodeLockNotifierConfig == null)
            {
                _pluginConfig.CodeLockNotifierConfig = new CodeLockNotifierConfig
                {
                    IsEnabled = true,
                    IsDiscordEnabled = true,
                    DiscordWebHookUrl = "",
                    DiscordEmbedColor = "#2F3136",
                    DiscordEmbedDescription = "{0} has been authorized in the code lock owned by {1}",
                    DiscordEmbedFieldOnlineText = "Online",
                    DiscordEmbedFieldOfflineText = "Offline",
                    DiscordEmbedField1Title = "STATUS",
                    DiscordEmbedField2Title = "PLAYER",
                    DiscordEmbedField3Title = "STEAM ID",
                    DiscordEmbedFooter = "Location {0} • PIN {1}",
                    DiscordMessage = "Team Limit Violation",
                    DiscordMessageMentionEnabled = true,
                    DiscordRoleToMention = "",
                    LogToConsole = true,
                    LogToFile = true,
                    LogToGameChat = true,
                    GameChatLogPlayersText = "{0}. <color=#13ffa2>{1}</color> <color=#FFA500>{2}</color>",
                    IgnoreTeammates = false,
                    MaxAuthorizeLimit = 1
                };
            }
            newConfig.WriteObject(_pluginConfig);
            PrintWarning("Config Loaded");
        }
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [PluginMessages.GameChatMessage] = "{title}\r\n{players}Location {grid}",
                [PluginMessages.Owner] = "Owner",
                [PluginMessages.New] = "New"
            }, this);
        }
        private void Init()
        {
            if (!_pluginConfig.CodeLockNotifierConfig.IsEnabled)
            {
                Unsubscribe("OnCodeEntered");
            }
            PrintWarning("Initialized");
        }
        object OnCodeEntered(CodeLock codeLock, BasePlayer player, string code)
        {
            if (!_pluginConfig.CodeLockNotifierConfig.IsEnabled)
                return null;
            var authorizedPlayers = codeLock.whitelistPlayers ?? new List<ulong>();
            if (codeLock.code != code)
            {
                return null;
            }
            authorizedPlayers.Add(player.userID);
            if (!_pluginConfig.CodeLockNotifierConfig.IgnoreTeammates && _pluginConfig.CodeLockNotifierConfig.MaxAuthorizeLimit < authorizedPlayers.Count)
            {
                ManageMessaging(codeLock.OwnerID, player.userID, authorizedPlayers, player.ServerPosition, codeLock.code);
            }
            else
            {
                var owner = BasePlayer.FindAwakeOrSleeping(codeLock.OwnerID.ToString());
                if (owner.currentTeam == 0 && _pluginConfig.CodeLockNotifierConfig.MaxAuthorizeLimit < authorizedPlayers.Count)
                {
                    ManageMessaging(codeLock.OwnerID, player.userID, authorizedPlayers, player.ServerPosition, codeLock.code);
                }
                else
                {
                    var counter = 0;
                    foreach (var whitelistPlayerId in authorizedPlayers)
                    {
                        var whitelistPlayer = BasePlayer.FindAwakeOrSleeping(whitelistPlayerId.ToString());
                        if (owner.currentTeam != whitelistPlayer.currentTeam)
                            counter++;
                    }
                    if (_pluginConfig.CodeLockNotifierConfig.MaxAuthorizeLimit < counter)
                    {
                        ManageMessaging(codeLock.OwnerID, player.userID, authorizedPlayers, player.ServerPosition, codeLock.code);
                    }
                }
            }

            return null;
        }

        #endregion Hooks

        #region Commands

        #endregion Commands

        #region Methods
        private string GetGrid(Vector3 pos)
        {
            char letter = 'A';
            var x = Mathf.Floor((pos.x + (ConVar.Server.worldsize / 2)) / 146.3f) % 26;
            var count = Mathf.Floor(Mathf.Floor((pos.x + (ConVar.Server.worldsize / 2)) / 146.3f) / 26);
            var z = (Mathf.Floor(ConVar.Server.worldsize / 146.3f)) - Mathf.Floor((pos.z + (ConVar.Server.worldsize / 2)) / 146.3f);
            letter = (char)(letter + x);
            var secondLetter = count <= 0 ? string.Empty : ((char)('A' + (count - 1))).ToString();
            return $"{secondLetter}{letter}{z - 1}";
        }
        private void ManageMessaging(ulong ownerId, ulong newPlayerId, List<ulong> authorizedPlayers, Vector3 codeLockPosition, string codeLockCode)
        {
            var owner = BasePlayer.FindAwakeOrSleeping(ownerId.ToString());
            var newPlayer = BasePlayer.FindAwakeOrSleeping(newPlayerId.ToString());
            if (owner == null || newPlayer == null)
                return;

            var grid = GetGrid(codeLockPosition);
            if (_pluginConfig.CodeLockNotifierConfig.IsDiscordEnabled && !string.IsNullOrWhiteSpace(_pluginConfig.CodeLockNotifierConfig.DiscordWebHookUrl))
            {
                SendDiscordMessage(owner, newPlayer, authorizedPlayers, grid, codeLockCode);
            }

            if (_pluginConfig.CodeLockNotifierConfig.LogToConsole)
            {
                LogToConsole(owner, newPlayer, authorizedPlayers.Count, grid);
            }
            if (_pluginConfig.CodeLockNotifierConfig.LogToFile)
            {
                LogToFile(owner, newPlayer, authorizedPlayers.Count, grid);
            }
            if (_pluginConfig.CodeLockNotifierConfig.LogToGameChat)
            {
                SendToAdmins(owner, newPlayer, authorizedPlayers, grid);
            }
        }

        private void SendDiscordMessage(BasePlayer owner, BasePlayer newPlayer, List<ulong> authorizedPlayers, string grid, string codeLockCode)
        {
            var hexColorNumber = _pluginConfig.CodeLockNotifierConfig.DiscordEmbedColor?.Replace("x", string.Empty);
            int color;
            if (!int.TryParse(hexColorNumber, NumberStyles.HexNumber, null, out color))
                color = 3092790;


            var messageText = string.Format(_pluginConfig.CodeLockNotifierConfig.DiscordEmbedDescription, newPlayer.displayName, owner.displayName);
            var embedBody = new EmbedBody
            {
                Description = $"{messageText}{Environment.NewLine}{Environment.NewLine}\u200b",
                Color = color,
                Fields = new List<FieldBody>()
            };

            var statusList = new List<string>();
            var nameList = new List<string>();
            var idList = new List<ulong>();
            foreach (var playerId in authorizedPlayers)
            {
                var player = BasePlayer.FindAwakeOrSleeping(playerId.ToString());
                if (player == null)
                    continue;

                statusList.Add(player.IsConnected ? _pluginConfig.CodeLockNotifierConfig.DiscordEmbedFieldOnlineText : _pluginConfig.CodeLockNotifierConfig.DiscordEmbedFieldOfflineText);
                nameList.Add($"[{player.displayName}](https://steamcommunity.com/profiles/{player.userID})");
                idList.Add(player.userID);
            }

            embedBody.Fields.Add(new FieldBody
            {
                Name = _pluginConfig.CodeLockNotifierConfig.DiscordEmbedField1Title,
                Value = string.Join(Environment.NewLine, statusList),
                Inline = true
            });
            embedBody.Fields.Add(new FieldBody
            {
                Name = _pluginConfig.CodeLockNotifierConfig.DiscordEmbedField2Title,
                Value = string.Join(Environment.NewLine, nameList),
                Inline = true
            });
            embedBody.Fields.Add(new FieldBody
            {
                Name = _pluginConfig.CodeLockNotifierConfig.DiscordEmbedField3Title,
                Value = string.Join(Environment.NewLine, idList),
                Inline = true
            });
            embedBody.Footer = new FooterBody
            {
                Text = string.Format(_pluginConfig.CodeLockNotifierConfig.DiscordEmbedFooter, grid, codeLockCode)
            };
            var body = new WebHookEmbedBody
            {
                Embeds = new[]
                {
                    embedBody
                }
            };
            var mention = _pluginConfig.CodeLockNotifierConfig.DiscordMessageMentionEnabled
                ? $"<@&{_pluginConfig.CodeLockNotifierConfig.DiscordRoleToMention}> "
                : string.Empty;
            var contentBody = new WebHookContentBody
            {
                Content = $"{mention}{_pluginConfig.CodeLockNotifierConfig.DiscordMessage}"
            };

            webrequest.Enqueue(_pluginConfig.CodeLockNotifierConfig.DiscordWebHookUrl, JsonConvert.SerializeObject(contentBody, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
                (headerCode, headerResult) =>
                {
                    if (headerCode >= 200 && headerCode <= 204)
                    {
                        webrequest.Enqueue(_pluginConfig.CodeLockNotifierConfig.DiscordWebHookUrl, JsonConvert.SerializeObject(body, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
                            (code, result) => { }, this, RequestMethod.POST,
                            new Dictionary<string, string> { { "Content-Type", "application/json" } });
                    }
                }, this, RequestMethod.POST,
                new Dictionary<string, string> { { "Content-Type", "application/json" } });
        }

        private void LogToConsole(BasePlayer owner, BasePlayer newPlayer, int authorizedPlayersCount, string grid)
        {
            PrintWarning(ConsoleMessage, newPlayer.displayName, newPlayer.userID, owner.displayName, owner.userID, grid, authorizedPlayersCount);
        }
        private void LogToFile(BasePlayer owner, BasePlayer newPlayer, int authorizedPlayersCount, string grid)
        {
            LogToFile(string.Empty,
                $"[{DateTime.Now}] {string.Format(LogFileMessage, newPlayer.displayName, newPlayer.userID, owner.displayName, owner.userID, grid, authorizedPlayersCount)}",
                this);
        }
        private void SendToAdmins(BasePlayer owner, BasePlayer newPlayer, List<ulong> authorizedPlayers, string grid)
        {
            var count = 0;
            var sb = new StringBuilder();

            sb.AppendLine($"{string.Format(_pluginConfig.CodeLockNotifierConfig.GameChatLogPlayersText, ++count, owner.displayName, owner.userID)} ({{Owner}})");
            foreach (var playerId in authorizedPlayers)
            {
                if (playerId != owner.userID && playerId != newPlayer.userID)
                {
                    var player = BasePlayer.FindAwakeOrSleeping(playerId.ToString());
                    if (player == null)
                        continue;

                    sb.AppendLine(string.Format(_pluginConfig.CodeLockNotifierConfig.GameChatLogPlayersText, ++count, player.displayName, player.userID));
                }
            }
            sb.AppendLine($"{string.Format(_pluginConfig.CodeLockNotifierConfig.GameChatLogPlayersText, ++count, newPlayer.displayName, newPlayer.userID)} ({{New}})");
            var admins = BasePlayer.allPlayerList.Where(x => x.IsAdmin);
            foreach (var admin in admins)
            {
                var ownerText = Lang(PluginMessages.Owner, admin.UserIDString);
                var newText = Lang(PluginMessages.New, admin.UserIDString);
                var message = lang.GetMessage(PluginMessages.GameChatMessage, this, admin.UserIDString);
                var players = sb.ToString().Replace("{Owner}", ownerText).Replace("{New}", newText);
                message = message.Replace("{title}", $"Team Limit Violation{Environment.NewLine}")
                    .Replace("{players}", players)
                    .Replace("{grid}", grid);
                admin.ChatMessage(message);
            }
        }

        public string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        #endregion Methods

        #region Classes

        private class PluginConfig
        {
            public CodeLockNotifierConfig CodeLockNotifierConfig { get; set; }
        }

        private class CodeLockNotifierConfig
        {
            [JsonProperty(PropertyName = "Plugin - Enabled")]
            public bool IsEnabled { get; set; }

            [JsonProperty(PropertyName = "Discord - Enabled")]
            public bool IsDiscordEnabled { get; set; }

            [JsonProperty(PropertyName = "Discord - Webhook URL")]
            public string DiscordWebHookUrl { get; set; }

            [JsonProperty(PropertyName = "Discord - Embed - Color(HEX)")]
            public string DiscordEmbedColor { get; set; }

            [JsonProperty(PropertyName = "Discord - Embed - Description")]
            public string DiscordEmbedDescription { get; set; }

            [JsonProperty(PropertyName = "Discord - Embed - Field Online Text")]
            public string DiscordEmbedFieldOnlineText { get; set; }

            [JsonProperty(PropertyName = "Discord - Embed - Field Offline Text")]
            public string DiscordEmbedFieldOfflineText { get; set; }

            [JsonProperty(PropertyName = "Discord - Embed - Field 1 Title")]
            public string DiscordEmbedField1Title { get; set; }

            [JsonProperty(PropertyName = "Discord - Embed - Field 2 Title")]
            public string DiscordEmbedField2Title { get; set; }

            [JsonProperty(PropertyName = "Discord - Embed - Field 3 Title")]
            public string DiscordEmbedField3Title { get; set; }

            [JsonProperty(PropertyName = "Discord - Embed - Footer")]
            public string DiscordEmbedFooter { get; set; }

            [JsonProperty(PropertyName = "Discord - Message")]
            public string DiscordMessage { get; set; }

            [JsonProperty(PropertyName = "Discord - Message - Enabled Mention")]
            public bool DiscordMessageMentionEnabled { get; set; }

            [JsonProperty(PropertyName = "Discord - Role to Mention")]
            public string DiscordRoleToMention { get; set; }

            [JsonProperty(PropertyName = "Log - Console")]
            public bool LogToConsole { get; set; }

            [JsonProperty(PropertyName = "Log - File")]
            public bool LogToFile { get; set; }

            [JsonProperty(PropertyName = "Log - Game Chat (Only Admins)")]
            public bool LogToGameChat { get; set; }

            [JsonProperty(PropertyName = "Log - Game Chat Players Text")]
            public string GameChatLogPlayersText { get; set; }

            [JsonProperty(PropertyName = "Ignore Players in Same Team")]
            public bool IgnoreTeammates { get; set; }

            [JsonProperty(PropertyName = "Warning - Max Authorize Limit")]
            public int MaxAuthorizeLimit { get; set; }
        }

        private static class PluginMessages
        {
            public const string GameChatMessage = "GameChatMessage";
            public const string Owner = "Owner";
            public const string New = "New";
        }

        private class WebHookEmbedBody
        {
            [JsonProperty(PropertyName = "embeds")]
            public EmbedBody[] Embeds;
        }

        private class WebHookContentBody
        {
            [JsonProperty(PropertyName = "content")]
            public string Content;
        }

        private class EmbedBody
        {
            [JsonProperty(PropertyName = "title")]
            public string Title;

            [JsonProperty(PropertyName = "type")]
            public string Type = "rich";

            [JsonProperty(PropertyName = "description")]
            public string Description;

            [JsonProperty(PropertyName = "color")]
            public int Color;

            [JsonProperty(PropertyName = "author")]
            public AuthorBody Author;

            [JsonProperty(PropertyName = "fields")]
            public List<FieldBody> Fields;

            [JsonProperty(PropertyName = "footer")]
            public FooterBody Footer;
        }

        public class AuthorBody
        {
            [JsonProperty(PropertyName = "name")]
            public string Name;

            [JsonProperty(PropertyName = "url")]
            public string AuthorURL;

            [JsonProperty(PropertyName = "icon_url")]
            public string AuthorIconURL;
        }

        public class FieldBody
        {
            [JsonProperty(PropertyName = "name")]
            public string Name;

            [JsonProperty(PropertyName = "value")]
            public string Value;

            [JsonProperty(PropertyName = "inline")]
            public bool Inline;
        }

        public class FooterBody
        {
            [JsonProperty(PropertyName = "text")]
            public string Text;

            [JsonProperty(PropertyName = "icon_url")]
            public string IconUrl;

            [JsonProperty(PropertyName = "proxy_icon_url")]
            public string ProxyIconUrl;
        }
        #endregion Classes
    }
}
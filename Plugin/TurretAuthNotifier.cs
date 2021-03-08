﻿using Newtonsoft.Json;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using static System.Net.WebRequestMethods;

namespace Oxide.Plugins
{

    [Info("TurretAuthNotifier", "MikeLitoris", "0.0.3")]
    class TurretAuthNotifier : RustPlugin
    {
        [PluginReference]
        private Plugin DiscordMessages;
        private ConfigData configData;
        class ConfigData
        {
            [JsonProperty(PropertyName = "Max Authorised before notification")]
            public int MaxAuth { get; set; }
            [JsonProperty(PropertyName = "Enable Ingame admin Notification")]
            public bool IngameNotificationEnable { get; set; }
            [JsonProperty(PropertyName = "Enable Discord Webhook")]
            public bool DiscordWebhookEnable { get; set; }
            [JsonProperty(PropertyName = "Discord Webhook")]
            public string DiscordWebhook { get; set; }
            [JsonProperty(PropertyName = "Discord Webhook Message")]
            public string WebhookTitle { get; set; }
        }

        private bool LoadConfigVariables()
        {
            try
            {
                configData = Config.ReadObject<ConfigData>();
            }
            catch
            {
                return false;
            }
            SaveConfig(configData);
            return true;
        }
        void Init()
        {
            if (!LoadConfigVariables())
            {
                Puts("Config File issue detected, kill your config");
                return;
            }
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config");
            configData = new ConfigData { MaxAuth = 1, WebhookTitle = "Max players authorized on turret exceeded", DiscordWebhook = "", DiscordWebhookEnable = true, IngameNotificationEnable = true };
            SaveConfig(configData);
        }

        void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }



        void OnTurretAuthorize(AutoTurret turret, BasePlayer player)
        {

            var nameListLink = new List<string>();
            var nameList = new List<string>();
            var statusList = new List<string>();
            var idList = new List<ulong>();
            var positionList = new List<string>();

            if (turret.authorizedPlayers.Count + 1 > configData.MaxAuth)
            {
                foreach (var authed in turret.authorizedPlayers)
                {
                    nameListLink.Add($"[{authed.username}](https://steamcommunity.com/profiles/{authed.userid})");
                    nameList.Add(authed.username);

                    idList.Add(authed.userid);
                    var authedPlayer = BasePlayer.FindAwakeOrSleeping(authed.ToString());
                    if (authedPlayer == null || authedPlayer.IsConnected == false)
                    {
                        statusList.Add("Offline");
                    }
                    else if (authedPlayer.IsConnected)
                    {
                        statusList.Add("Online");
                    }
                }
                positionList.Add($"teleportpos ({ turret.transform.position.x.ToString().Substring(0, 5)},{ turret.transform.position.y.ToString().Substring(0, 5)},{ turret.transform.position.z.ToString().Substring(0, 5)})");
                nameList.Add(player.displayName);
                nameListLink.Add($"[{player.displayName}](https://steamcommunity.com/profiles/{player.userID})");
                idList.Add(player.userID);
                if (player.IsConnected)
                {
                    statusList.Add("Online");
                }

                if(configData.IngameNotificationEnable == true)
                {
                    string message = $"{configData.WebhookTitle}\n {positionList[0]} ";
                    for (int i = 0; i < nameList.Count; i++)
                    {
                        message += $"{statusList[i]} {nameList[i]} [{idList[i].ToString()}]\n";
                    }
                    
                    var admins = BasePlayer.allPlayerList.Where(p => p.IsAdmin);
                    foreach (var admin in admins)
                    {
                        SendReply(admin, message);
                    }
                }


                if (configData.DiscordWebhookEnable == true && configData.DiscordWebhook != "" && configData.DiscordWebhook != null)
                {
                    SendDiscordMessage(nameListLink, idList, positionList, statusList);
                }
            }
        }

        void SendDiscordMessage(List<string> names, List<ulong> ids, List<string> position, List<string> status)
        {

            object fields = new[]
            {
                new
                {                    
                    name = "Status", value = string.Join("\n", status), inline = true
                },
                new
                {
                name = "Name", value = string.Join("\n", names), inline = true
                },
                new
                {
                name = "ID", value = string.Join("\n", ids), inline = true
                },
                new
                {
                name = "Position", value = string.Join("\n", position), inline = false
                },
            };


            string json = JsonConvert.SerializeObject(fields);
            DiscordMessages?.Call("API_SendFancyMessage", configData.DiscordWebhook, configData.WebhookTitle, 2, json);

        }


    }
}
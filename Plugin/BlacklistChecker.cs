using Newtonsoft.Json;
using Oxide.Core.Libraries;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("BlacklistChecker", "Mikey", "0.0.2")]
    class BlacklistChecker : RustPlugin
    {
        private ConfigData configData;
        #region Config
        class ConfigData
        {
            [JsonProperty(PropertyName = "Check on server save")]
            public bool CheckOnSave { get; set; }
            [JsonProperty(PropertyName = "Get server IP automatically")]
            public bool GetIpBool { get; set; }
            [JsonProperty(PropertyName = "Send Discord Webhook notice")]
            public bool EnableWebhook { get; set; }
            [JsonProperty(PropertyName = "Discord Webhook")]
            public string DiscordWebhook { get; set; }
            [JsonProperty(PropertyName = "Server IP")]
            public string ServerIP { get; set; }
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "Blacklisted", " Has been detected in the Facepunch API, please double-check if your IP is blacklisted here: https://api.facepunch.com/api/public/manifest/?public_key=j0VF6sNnzn9rwt9qTZtI02zTYK8PRdN1 " },
                { "Not Blacklisted", " Has not been blacklisted" }

            }, this);
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
                Puts("Config File issue detected");
                LoadDefaultConfig();

                return;
            }

            if (configData.GetIpBool)
            {
                GetServerIP();
                timer.Once(3, () =>
                { CheckIP(); });                
            }
            else
                CheckIP();

        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config");
            configData = new ConfigData { GetIpBool = true, EnableWebhook = false, CheckOnSave = true, DiscordWebhook = "", ServerIP = "127.0.0.1"};
            SaveConfig(configData);
        }

        void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }
                      
        #endregion Config
        
        void OnServerSave()
        {
            if (configData.CheckOnSave)
            {
                CheckIP();
            }            
        }

        void CheckIP()
        {           
            webrequest.Enqueue("https://api.facepunch.com/api/public/manifest/?public_key=j0VF6sNnzn9rwt9qTZtI02zTYK8PRdN1", null, Callback, this, RequestMethod.GET, null, 5000);
        }

        public void Callback(int code, string response)
        {
            if (code == 200)
            {
                if (response.Contains(configData.ServerIP))
                {
                    if (configData.EnableWebhook)
                    {
                        SendDiscordMessage();
                    }

                    Puts(configData.ServerIP + lang.GetMessage("Blacklisted", this));
                }
                else
                    Puts(configData.ServerIP + lang.GetMessage("Not Blacklisted", this));
            }
            else
                Puts("Error:" + code.ToString());
        }


        void SendDiscordMessage()
        {
            string discordMessage = "{\"content\": \"**" + configData.ServerIP + "**" + lang.GetMessage("Blacklisted", this) + ".\"}";

            webrequest.Enqueue(configData.DiscordWebhook, discordMessage, PostNotice, this, RequestMethod.POST, new Dictionary<string, string> { ["Content-Type"] = "application/json" });
        }

        void PostNotice(int code, string response)
        {
            Puts("Webhook message sent" + response.ToString());
        }
       

        void GetServerIP()
        {
            webrequest.Enqueue("https://api.ipify.org/", null, (code, response) => {
                
                if (code == 200)
                {                    
                    configData.ServerIP = response;
                }
            }, this, RequestMethod.GET, null, 5000);
                    
        }
    }
}   
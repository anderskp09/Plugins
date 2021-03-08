using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Change", "Mikey", "0.0.1")]
    class Change : RustPlugin
    {

        #region config
        private ConfigData configData;
        class ConfigData
        {
            [JsonProperty(PropertyName = "Bool")]
            public bool rep = true;

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

            permission.RegisterPermission("Change.admin",this);
            if (!LoadConfigVariables())
            {
                Puts("Config File issue detected, kill your config");
                return;
            }
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config");
            configData = new ConfigData();
            SaveConfig(configData);
        }

        void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }
        #endregion
        
        [ChatCommand("ConfCheck")]
        void chatconfcheck(BasePlayer player)
        {
            SendReply(player, $"The config value is {configData.rep}");
        }

        [ChatCommand("change")]
        void change(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.userID.ToString(),"Change.admin"))
            {
                SendReply(player, "You do not have the permission for this command");
                return;
            }
            else
            {
                configData.rep = !configData.rep;
                SaveConfig(configData);
                SendReply(player, $"The config data was changed from {!configData.rep} to {configData.rep}");
            }
        }
    }
}
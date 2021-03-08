using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Config","Mikey","0.0.1")]
    class Config : RustPlugin
    {
        private ConfigData configData;
        private const string permissionEnable = "Pluginname.enable";
        #region Config
        class ConfigData
        {

            [JsonProperty(PropertyName = "Config Item")]
            public string ConfigName { get; set; }            


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
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config");
            configData = new ConfigData { ConfigName = "value"};
            SaveConfig(configData);
        }

        void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }



        // print config to console
        [ConsoleCommand("conftest")]
        void confTest(ConsoleSystem.Arg args)
        {
            Puts(configData.ConfigName.ToString());
        }
        #endregion Config
    }


}
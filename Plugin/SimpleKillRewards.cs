using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("SimpleKillRewards", "MikeL", "0.0.5")]
    [Description("Simple Rewards for killing players or NPC's. To be configured in config or with console/ingame command. Killer must have SimpleKillRewards.enable permission to get rewards.")]
    class SimpleKillRewards : RustPlugin
    {
        #region Config 
        private ConfigData configData;
        private const string permissionEnable = "SimpleKillRewards.enable";
        private const string permissionAdmin = "SimpleKillRewards.admin";
        class ConfigData
        {
            [JsonProperty(PropertyName = "Item shorthand")]
            public string Shorthand { get; set; }
            [JsonProperty(PropertyName = "Ammount of item")]
            public int Ammount { get; set; }
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
            permission.RegisterPermission(permissionEnable, this);
            permission.RegisterPermission(permissionAdmin, this);
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
            configData = new ConfigData { Shorthand = "scrap" , Ammount = 20 };
            SaveConfig(configData);
        }

        void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }

        #endregion Config
        #region Commands

        [ConsoleCommand("SKR")]
        void confTest(ConsoleSystem.Arg args)
        {
            if (!(args.Args?.Length == 2) || args.Args[0] == null || args.Args[1] == null)
            {
                Puts($"Currently killing a player will give {configData.Ammount.ToString()} {configData.Shorthand.ToString()}");
            }
            else
            {                             
                try
                {
                    configData = new ConfigData { Shorthand = args.Args[0], Ammount = int.Parse(args.Args[1]) };
                    SaveConfig(configData);
                    Puts($"You sucessfully configured SKR to give {configData.Ammount} {configData.Shorthand} per kill");
                }
                catch
                {
                    Puts($"Incorrect format, follow this syntax '/SKR Scrap 100' Currently 1 kill will give  {configData.Ammount} {configData.Shorthand}");
                }
                
            }
        }

        [ChatCommand("SKR")]
        void SetRewards(BasePlayer player, string command, string[] args)
        {
            if (!(args.Length == 2) || args[0] == null || args[1] == null)
            {
                SendReply(player, $"Currently killing a player will give {configData.Ammount.ToString()} {configData.Shorthand.ToString()}");
            }
            else
            {
                if (!permission.UserHasPermission(player.userID.ToString(), permissionAdmin))
                {
                    SendReply(player, "You dont have permission to acess this command!");
                }
                else
                {
                    try
                    {
                        configData = new ConfigData { Shorthand = args[0], Ammount = int.Parse(args[1]) };
                        SaveConfig(configData);
                        SendReply(player, $"You sucessfully configured SKR to give {configData.Ammount} {configData.Shorthand} per kill");
                    }
                    catch
                    {
                        SendReply(player, $"Incorrect format, follow this syntax '/SKR Scrap 100' Currently 1 kill will give  {configData.Ammount} {configData.Shorthand}");
                    }
                }
            }
        }
        #endregion Commands
        #region Action        

        void OnPlayerDeath(BasePlayer player, HitInfo info)
        {            
            try
            {
                if (!permission.UserHasPermission(info.InitiatorPlayer.userID.ToString(), permissionEnable))
                {
                    return;
                }
                else
                {
                    info.Initiator.GiveItem(ItemManager.CreateByName(configData.Shorthand.ToString(), configData.Ammount));
                }
            }
            catch
            {
                Puts("Error in Config, make sure item shorthand and ammount is correct");
            }            
        }        
        #endregion Action
    }    
}
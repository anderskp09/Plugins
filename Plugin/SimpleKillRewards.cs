using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Oxide.Plugins
{
    [Info("SimpleKillRewards", "MikeL", "0.0.6")]
    [Description("Simple Rewards for killing players or NPC's.")]
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

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["SKR.Current"] = "Currently killing a player will give:",                              
                ["SKR.Set"] = "You sucessfully configured SKR to give:",
                ["SKR.Incorrect"] = "Incorrect format, follow this syntax '/SKR Scrap 100' Currently 1 kill will give:",
                ["SKR.NoPermission"] = "You dont have permision to use this command!",

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
                Puts($"{lang.GetMessage("SKR.Current", this)} {configData.Ammount.ToString()} {configData.Shorthand.ToString()}");
            }
            else
            {                             
                try
                {
                    configData = new ConfigData { Shorthand = args.Args[0], Ammount = int.Parse(args.Args[1]) };
                    SaveConfig(configData);
                    Puts($"{lang.GetMessage("SKR.Set", this)} {configData.Ammount} {configData.Shorthand}");
                }
                catch
                {
                    Puts($"{lang.GetMessage("SKR.Incorrect", this)} {configData.Ammount} {configData.Shorthand}");
                }
                
            }
        }

        [ChatCommand("SKR")]
        void SetRewards(BasePlayer player, string command, string[] args)
        {
            if (!(args.Length == 2) || args[0] == null || args[1] == null)
            {
                SendReply(player, $"{lang.GetMessage("SKR.Current", this)}{configData.Ammount.ToString()} {configData.Shorthand.ToString()}");
            }
            else
            {
                if (!permission.UserHasPermission(player.userID.ToString(), permissionAdmin))
                {
                    SendReply(player, lang.GetMessage("SKR.NoPermission", this));
                }
                else
                {
                    try
                    {
                        configData = new ConfigData { Shorthand = args[0], Ammount = int.Parse(args[1]) };
                        SaveConfig(configData);
                        SendReply(player, $"{lang.GetMessage("SKR.Set", this)} {configData.Ammount} {configData.Shorthand}");
                    }
                    catch
                    {
                        SendReply(player, $"{lang.GetMessage("SKR.Incorrect", this)} {configData.Ammount} {configData.Shorthand}");
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
                Puts(lang.GetMessage("SKR.Incorrect", this));
            }            
        }        
        #endregion Action
    }    
}
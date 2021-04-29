﻿using Newtonsoft.Json;
using Oxide.Core;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("savemyname", "Mikey", "0.0.2")]
    class SaveMyName : RustPlugin
    {
        private ConfigData configData;
        private const string savemyname = "SaveMyName.use";
        private const string savemynameadmin = "SaveMyName.admin";
        #region Config
        class ConfigData
        {
            [JsonProperty(PropertyName = "Config Item")]
            public bool IgnoreAdmins { get; set; }
        }
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["blockedname"] = "The name youre trying to connect with has been blocked, please switch and reconnect",

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

            permission.RegisterPermission(savemyname, this);
            permission.RegisterPermission(savemynameadmin, this);
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
            configData = new ConfigData { IgnoreAdmins = true };
            SaveConfig(configData);
        }

        void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }

        class SavedNames
        {
            public ulong SteamID { get; set; }
            public string Name { get; set; }
        }

        StoredData storedData;

        class StoredData
        {
          public List<SavedNames> savedNames  = new List<SavedNames>();
        }
        
        void Loaded()
        {
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("SaveMyName");
            Interface.Oxide.DataFileSystem.WriteObject("SaveMyName", storedData);
        }

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("SaveMyName", storedData);
        }

        void OnPlayerConnected(BasePlayer player)
        {
            foreach (var storedname in storedData.savedNames)
            {
                if (configData.IgnoreAdmins)
                {
                    if (player.displayName == storedname.Name && player.userID != storedname.SteamID && permission.UserHasPermission(storedname.SteamID.ToString(), savemyname) == true && player.IsAdmin == false)
                    {
                        player.Kick(lang.GetMessage("blockedname", this));
                    }
                }
                else
                {
                    if (player.displayName == storedname.Name && player.userID != storedname.SteamID && permission.UserHasPermission(storedname.SteamID.ToString(), savemyname) == true)
                    {
                        player.Kick(lang.GetMessage("blockedname", this));
                    }
                }
            }
        }

        void OnUserNameUpdated(string id, string oldName, string newName)
        {
            BasePlayer player = BasePlayer.FindAwakeOrSleeping(id);
            foreach (var storedname in storedData.savedNames)
            {
                if (configData.IgnoreAdmins)
                { 
                    if (newName == storedname.Name && ulong.Parse(id) != storedname.SteamID && permission.UserHasPermission(storedname.SteamID.ToString(), savemyname) == true && player.IsAdmin == false)// && player.IsAdmin == false)
                    {                        
                        player.Kick(lang.GetMessage("blockedname", this));
                    }
                }
                else
                {
                    if (newName == storedname.Name && ulong.Parse(id) != storedname.SteamID && permission.UserHasPermission(storedname.SteamID.ToString(), savemyname) == true)
                    {
                        player.Kick(lang.GetMessage("blockedname", this));
                    }
                }
            }
        }

        [ChatCommand("clearmyname")]
        void ClearName(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, savemyname))
            {
                try
                {
                    SavedNames savedName = storedData.savedNames.Find(x => x.SteamID == player.userID);
                    storedData.savedNames.Remove(savedName);
                    SaveData();
                }
                catch (System.Exception)
                {
                    SendReply(player, "No saved name found, use /savemyname to save current name");
                }
            }
        }

        [ChatCommand("savemyname")]
        void SaveName(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, savemyname))
            {
                if (storedData.savedNames.Find(x => x.SteamID == player.userID) != null)
                {
                    SavedNames savedName = storedData.savedNames.Find(x => x.SteamID == player.userID);
                    savedName.Name = player.displayName;                  
                    SaveData();
                    SendReply(player, $"Name {player.displayName} has been saved!");
                    Puts($"Name {player.displayName} has been saved!");
                }
                else
                {
                    SavedNames saved = new SavedNames { SteamID = player.userID, Name = player.displayName };
                    storedData.savedNames.Add(saved);
                    SaveData();
                    SendReply(player, $"Name {player.displayName} has been saved!");
                }                
            }
            
        }
        [ConsoleCommand("savednames")]
        void SavedNamesListCon()
        {
            string line = "\n";
            foreach (var sn in storedData.savedNames)
            {
                line += $"[{sn.SteamID}] {sn.Name}\n";
            }
            Puts(line);

        }
        [ChatCommand("savednames")]
        void SavedNamesList(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, savemynameadmin))
            {
                string line = "";
                foreach (var sn in storedData.savedNames)
                {
                    line += $"[{sn.SteamID}] {sn.Name}\n";
                }
                SendReply(player, line);
                
            }

        }
            #endregion Config
    }


}
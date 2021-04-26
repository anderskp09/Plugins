using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("StorageInvestigator", "Mikey", "0.0.2")]
    class StorageInvestigator : RustPlugin
    {
        
        private ConfigData configData;
        private const string permissionAdmin = "StorageInvestigator.admin";
        #region Config
        class ConfigData
        {
            [JsonProperty(PropertyName = "Container name")]
            public string ContainerName { get; set; }
            [JsonProperty(PropertyName = "Item ID")]
            public int ItemId { get; set; }

        }
        class Contestant
        {
            public int Ammount { get; set; }
            public string Name { get; set; }
            public int ContainerCount { get; set; }
        }
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["AmmountInContainer"] = "There is:",

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
            configData = new ConfigData { ContainerName = "assets/prefabs/deployable/tool cupboard/cupboard.tool.deployed.prefab", ItemId = -932201673 };
            SaveConfig(configData);
        }

        void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }



      
        #endregion Config
        [ChatCommand("SI")]
        void Investigate(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.userID.ToString(), permissionAdmin))
            {
                var contestants = new List<Contestant>();

                foreach (var pl in BasePlayer.allPlayerList)
                {
                    var contestant = new Contestant { Name = pl.displayName };
                    contestants.Add(contestant);
                }
                foreach (StorageContainer sc in Resources.FindObjectsOfTypeAll<StorageContainer>())
                {
                    if (sc.PrefabName.Contains(configData.ContainerName) || sc.PrefabName == configData.ContainerName)
                    {
                        if (BasePlayer.FindAwakeOrSleeping(sc.OwnerID.ToString()) != null)
                        {
                            string name = BasePlayer.FindAwakeOrSleeping(sc.OwnerID.ToString()).displayName;
                            var contestant = contestants.Find(i => i.Name == name);
                            contestant.ContainerCount += 1;

                            ItemContainer inventory = sc.inventory;
                            if (inventory == null) continue;
                            List<Item> list = inventory.itemList.FindAll((Item x) => x.info.itemid == configData.ItemId);
                            int total = 0;
                            for (int i = 0; i < list.Count; i++)
                                total += list[i].amount;
                            contestant.Ammount += total;
                        }
                    }
                }
                List<Contestant> sorted = contestants.OrderBy(c => c.Ammount).ToList();
                string message = "<color=green>And the winner(s) are: </color> \n";
                var i = 0;
                foreach (var contestant in sorted)
                {
                    if (i <= 10)
                    {
                        message += $"{contestant.Name} Had {contestant.Ammount} scrap in {contestant.ContainerCount} TCs \n";
                    }
                    i++;
                }
                Puts(message);
                PrintToChat(message);
            }
        }
    }


}
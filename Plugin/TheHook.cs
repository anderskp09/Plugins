using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("TheHook", "Mikey", "0.0.1")]
    class TheHook : RustPlugin
    {
        private ConfigData configData;
        class ConfigData
        {
            [JsonProperty(PropertyName = "Door net id")]
            public uint door = 0;

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
            permission.RegisterPermission("TheHook.admin", this);
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



        [ChatCommand("MyDoor")]
        void mydoor(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.userID.ToString(), "TheHook.admin"))
            {
                SendReply(player, "you dont have permisions for this command");
                return;
            }
            else
            {
                Door door;
                if (!DOORLOOK(player, out door))
                {
                    SendReply(player, "no door found");
                    return;
                }
                else
                {
                    SendReply(player, $"Found door {door}");
                    configData.door = door.net.ID;
                    SaveConfig(configData);
                }
            }


        }

        void OnPlayerDeath(BasePlayer player, HitInfo info)
        {


            info.Initiator.GiveItem(ItemManager.CreateByName("rock", 1));
            Puts(player.ToString(), info.ToString());
        }

        void OnDoorKnocked(Door door, BasePlayer player)
        {
            if (door.net.ID == configData.door)
            {
                SendReply(player, "this is an admin base go away");
            }
            else 
            {
                return;
            }
        }

        private bool DOORLOOK(BasePlayer player, out Door door)
        {
            RaycastHit hit;
            door = null;
            if(Physics.Raycast(player.eyes.HeadRay(),out hit, 3))
            {
                door = hit.GetEntity() as Door;
            }

            return door != null;
            
        }
    }
}

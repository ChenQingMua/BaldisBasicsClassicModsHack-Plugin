using BepInEx;
using UnityEngine;

namespace UniversalHack
{
    [BepInPlugin("com.universal.plugin", "Baldis Basics Classic Mods Hack", "1.0.0")]
    public class EntryPoint : BaseUnityPlugin
    {
        void Awake()
        {
            
            gameObject.AddComponent<HackMenu>();
            gameObject.AddComponent<ESPPlugin>();
            gameObject.AddComponent<FOVPlugin>();
            gameObject.AddComponent<InvinciblePlugin>();
            gameObject.AddComponent<StaminaPlugin>();
            gameObject.AddComponent<ItemPlugin>();
            gameObject.AddComponent<SpeedPlugin>();
            gameObject.AddComponent<NoclipPlugin>();
            gameObject.AddComponent<AntiPushPlugin>();
            gameObject.AddComponent<EventPlugin>();
            gameObject.AddComponent<MouseFixPlugin>();
            Logger.LogInfo("Hack Plugin Loaded!");
        }
    }
}
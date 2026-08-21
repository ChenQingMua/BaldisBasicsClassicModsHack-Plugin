using BepInEx;
using UnityEngine;

namespace UniversalHack
{
    [BepInPlugin("com.universal.plugin", "Baldis Basics Classic Mods Hack", "1.1.0")]
    public class EntryPoint : BaseUnityPlugin
    {
        void Awake()
        {

            gameObject.AddComponent<PatchManager>();
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
            gameObject.AddComponent<WinPlugin>();
            gameObject.AddComponent<BookPlugin>();
            gameObject.AddComponent<VisualPlugin>();
            
            Logger.LogInfo("Hack Plugin Loaded!");
        }
    }
}
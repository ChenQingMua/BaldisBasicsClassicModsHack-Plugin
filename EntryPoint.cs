using BepInEx;
using UnityEngine;

namespace UniversalHack
{
    [BepInPlugin("com.universal.plugin", "Baldis Basics Classic Mods Hack", "1.3.0")]
    public class EntryPoint : BaseUnityPlugin
    {
        void Awake()
        {
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;
            gameObject.AddComponent<HackMenu>();
            gameObject.AddComponent<NoClipPlugin>();
            gameObject.AddComponent<ESPPlugin>();
            gameObject.AddComponent<FOVPlugin>();
            gameObject.AddComponent<AntiPushPlugin>();
            gameObject.AddComponent<WinPlugin>();
            gameObject.AddComponent<BookPlugin>();
            gameObject.AddComponent<VisualPlugin>();
            gameObject.AddComponent<InvinciblePlugin>();
            gameObject.AddComponent<StaminaPlugin>();
            gameObject.AddComponent<ItemPlugin>();
            gameObject.AddComponent<SpeedPlugin>();
            gameObject.AddComponent<MouseFixPlugin>();
            gameObject.AddComponent<EventPlugin>();
            

            Logger.LogInfo("Hack Plugin Loaded!");
        }
    }
}
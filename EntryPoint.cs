
using BepInEx;
using UnityEngine;

namespace UniversalHack
{
    [BepInPlugin("top.muah.baldi.main", "Baldis Basics Classic Mods Hack", "1.5.0")]
    public class EntryPoint : BaseUnityPlugin
    {
        void Awake()
        {
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;

            gameObject.AddComponent<HackMenu>();
            gameObject.AddComponent<MovementManager>();
            gameObject.AddComponent<VisualManager>();
            gameObject.AddComponent<PlayerManager>();
            gameObject.AddComponent<EventManager>();
            gameObject.AddComponent<ExtensionManager>();
            gameObject.AddComponent<MouseFixPlugin>();
            gameObject.AddComponent<UpdateCheckerPlugin>(); 

            Logger.LogInfo("Hack Plugin Loaded!");
        }
    }
}
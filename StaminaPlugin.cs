using System.Reflection;
using UnityEngine;

namespace UniversalHack
{
    public class StaminaPlugin : MonoBehaviour
    {
        void Awake()
        {
            PatchManager.Register("stamina", "PlayerScript", "StaminaCheck",
                prefix: typeof(Patches).GetMethod("StaminaCheck_Prefix", BindingFlags.Static | BindingFlags.Public));
        }

        public static class Patches
        {
            public static bool StaminaCheck_Prefix() => !HackMenu.ActiveFeatures.Contains("无限体力");
        }
    }
}
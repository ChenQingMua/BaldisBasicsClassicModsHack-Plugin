using System.Reflection;
using UnityEngine;

namespace UniversalHack
{
    public class InvinciblePlugin : MonoBehaviour
    {
        void Awake()
        {
            PatchManager.Register("invincible_enter", "PlayerScript", "OnTriggerEnter",
                prefix: typeof(Patches).GetMethod("OnTriggerEnter_Prefix", BindingFlags.Static | BindingFlags.Public));

            PatchManager.Register("invincible_stay", "PlayerScript", "OnTriggerStay",
                prefix: typeof(Patches).GetMethod("OnTriggerStay_Prefix", BindingFlags.Static | BindingFlags.Public));
        }

        public static class Patches
        {
            public static bool OnTriggerEnter_Prefix() => !HackMenu.ActiveFeatures.Contains("无敌");
            public static bool OnTriggerStay_Prefix() => !HackMenu.ActiveFeatures.Contains("无敌");
        }
    }
}
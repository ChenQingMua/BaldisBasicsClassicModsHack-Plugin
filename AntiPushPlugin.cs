using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace UniversalHack
{
    public class AntiPushPlugin : MonoBehaviour
    {
        void Awake()
        {
            PatchManager.Register(
                "antipush",
                "PlayerScript",
                "OnTriggerStay",
                prefix: typeof(Patches).GetMethod("OnTriggerStay_Prefix", BindingFlags.Static | BindingFlags.Public)
            );
        }

        public static class Patches
        {
            public static bool OnTriggerStay_Prefix()
            {
                return !HackMenu.ActiveFeatures.Contains("无视推动");
            }
        }
    }
}
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace UniversalHack
{
    public class InvinciblePlugin : MonoBehaviour
    {
        private Harmony harmony;

        void Awake()
        {
            harmony = new Harmony("com.universal.invincible");

            var enterMethod = typeof(Patches).GetMethod("OnTriggerEnter_Prefix", BindingFlags.Static | BindingFlags.Public);
            var stayMethod = typeof(Patches).GetMethod("OnTriggerStay_Prefix", BindingFlags.Static | BindingFlags.Public);

            PatchMethod("PlayerScript", "OnTriggerEnter", enterMethod);
            PatchMethod("PlayerScript", "OnTriggerStay", stayMethod);
        }

        void PatchMethod(string typeName, string methodName, MethodInfo prefix)
        {
            try
            {
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.Name == typeName)
                        {
                            var method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            if (method != null)
                            {
                                harmony.Patch(method, prefix != null ? new HarmonyMethod(prefix) : null);
                                return;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        void OnDestroy()
        {
            if (harmony != null)
            {
                try { harmony.UnpatchAll("com.universal.invincible"); } catch { }
            }
        }

        public static class Patches
        {
            public static bool OnTriggerEnter_Prefix() => !HackMenu.ActiveFeatures.Contains("无敌");
            public static bool OnTriggerStay_Prefix() => !HackMenu.ActiveFeatures.Contains("无敌");
        }
    }
}
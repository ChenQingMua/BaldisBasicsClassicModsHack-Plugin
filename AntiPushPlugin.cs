using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace UniversalHack
{
    public class AntiPushPlugin : MonoBehaviour
    {
        private Harmony harmony;

        void Awake()
        {
            harmony = new Harmony("com.universal.antipush");

            var method = typeof(Patches).GetMethod("OnTriggerStay_Prefix", BindingFlags.Static | BindingFlags.Public);
            PatchMethod("PlayerScript", "OnTriggerStay", method);
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
                try { harmony.UnpatchAll("com.universal.antipush"); } catch { }
            }
        }

        public static class Patches
        {
            public static bool OnTriggerStay_Prefix() => !HackMenu.ActiveFeatures.Contains("无视推动");
        }
    }
}
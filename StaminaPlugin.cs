using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace UniversalHack
{
    public class StaminaPlugin : MonoBehaviour
    {
        private Harmony harmony;

        void Awake()
        {
            harmony = new Harmony("com.universal.stamina");

            var method = typeof(Patches).GetMethod("StaminaCheck_Prefix", BindingFlags.Static | BindingFlags.Public);
            PatchMethod("PlayerScript", "StaminaCheck", method);
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
                try { harmony.UnpatchAll("com.universal.stamina"); } catch { }
            }
        }

        public static class Patches
        {
            public static bool StaminaCheck_Prefix() => !HackMenu.ActiveFeatures.Contains("无限体力");
        }
    }
}
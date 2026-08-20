using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace UniversalHack
{
    public class StaminaPlugin : MonoBehaviour
    {
        private static bool patched = false;

        void Awake()
        {
            if (!patched)
            {
                var harmony = new Harmony("com.universal.stamina");
                harmony.PatchAll();
                patched = true;
            }
        }

        [HarmonyPatch]
        public class PlayerScript_StaminaCheck_Patch
        {
            static MethodBase TargetMethod()
            {
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.Name != "PlayerScript") continue;
                        var method = type.GetMethod("StaminaCheck",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (method != null) return method;
                    }
                }
                return null;
            }

            static bool Prefix()
            {
                return !HackMenu.ActiveFeatures.Contains("无限体力");
            }
        }
    }
}
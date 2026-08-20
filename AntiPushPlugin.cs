using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace UniversalHack
{
    public class AntiPushPlugin : MonoBehaviour
    {
        private static bool patched = false;

        void Awake()
        {
            if (!patched)
            {
                var harmony = new Harmony("com.universal.antipush");
                harmony.PatchAll();
                patched = true;
            }
        }

        [HarmonyPatch]
        public class PlayerScript_OnTriggerStay_Patch
        {
            static MethodBase TargetMethod()
            {
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.Name != "PlayerScript") continue;
                        var method = type.GetMethod("OnTriggerStay",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (method != null) return method;
                    }
                }
                return null;
            }

            static bool Prefix()
            {
                return !HackMenu.ActiveFeatures.Contains("无视推动");
            }
        }
    }
}
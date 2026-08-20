using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace UniversalHack
{
    public class ItemPlugin : MonoBehaviour
    {
        private static bool patched = false;

        void Awake()
        {
            if (!patched)
            {
                var harmony = new Harmony("com.universal.item");
                harmony.PatchAll();
                patched = true;
            }
        }

        [HarmonyPatch]
        public class GameControllerScript_ResetItem_Patch
        {
            static MethodBase TargetMethod()
            {
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (!type.Name.Contains("GameController")) continue;
                        var method = type.GetMethod("ResetItem",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (method != null) return method;
                    }
                }
                return null;
            }

            static bool Prefix()
            {
                return !HackMenu.ActiveFeatures.Contains("无限道具");
            }
        }
    }
}
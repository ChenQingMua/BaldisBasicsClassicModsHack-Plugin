using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace UniversalHack
{
    public class SpeedPlugin : MonoBehaviour
    {
        private static bool patched = false;
        private static float originalWalkSpeed;
        private static float originalRunSpeed;
        private static bool speedApplied = false;

        void Awake()
        {
            if (!patched)
            {
                var harmony = new Harmony("com.universal.speed");
                harmony.PatchAll();
                patched = true;
            }
        }

        [HarmonyPatch]
        public class PlayerScript_Start_Patch
        {
            static MethodBase TargetMethod()
            {
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.Name != "PlayerScript") continue;
                        var method = type.GetMethod("Start",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (method != null) return method;
                    }
                }
                return null;
            }

            static void Postfix(object __instance)
            {
                var type = __instance.GetType();
                var walkSpeedField = type.GetField("walkSpeed",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var runSpeedField = type.GetField("runSpeed",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (walkSpeedField != null)
                    originalWalkSpeed = (float)walkSpeedField.GetValue(__instance);
                if (runSpeedField != null)
                    originalRunSpeed = (float)runSpeedField.GetValue(__instance);
            }
        }

        void Update()
        {
            object player = null;
            System.Type playerType = null;

            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in asm.GetTypes())
                {
                    if (type.Name != "PlayerScript") continue;
                    player = FindObjectOfType(type);
                    if (player != null)
                    {
                        playerType = type;
                        break;
                    }
                }
                if (player != null) break;
            }

            if (player == null || playerType == null) return;

            var walkSpeedField = playerType.GetField("walkSpeed",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var runSpeedField = playerType.GetField("runSpeed",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (walkSpeedField == null || runSpeedField == null) return;

            if (HackMenu.ActiveFeatures.Contains("移速"))
            {
                if (!speedApplied)
                {
                    walkSpeedField.SetValue(player, originalWalkSpeed * 2f);
                    runSpeedField.SetValue(player, originalRunSpeed * 2f);
                    speedApplied = true;
                }
            }
            else
            {
                if (speedApplied)
                {
                    walkSpeedField.SetValue(player, originalWalkSpeed);
                    runSpeedField.SetValue(player, originalRunSpeed);
                    speedApplied = false;
                }
            }
        }
    }
}
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace UniversalHack
{
    public class FOVPlugin : MonoBehaviour
    {
        private static float defaultFOV = 60f;
        private static bool patched = false;

        void Awake()
        {
            if (!patched)
            {
                var harmony = new Harmony("com.universal.fov");
                harmony.PatchAll();
                patched = true;
            }
        }

        [HarmonyPatch]
        public class GameControllerScript_Start_Patch
        {
            static MethodBase TargetMethod()
            {
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (!type.Name.Contains("GameController")) continue;
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
                var camField = type.GetField("playerCamera",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (camField == null) return;

                var cam = camField.GetValue(__instance) as Camera;
                if (cam == null) return;

                defaultFOV = cam.fieldOfView;
            }
        }

        [HarmonyPatch]
        public class GameControllerScript_Update_Patch
        {
            static MethodBase TargetMethod()
            {
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (!type.Name.Contains("GameController")) continue;
                        var method = type.GetMethod("Update",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (method != null) return method;
                    }
                }
                return null;
            }

            static void Postfix(object __instance)
            {
                var type = __instance.GetType();
                var camField = type.GetField("playerCamera",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (camField == null) return;

                var cam = camField.GetValue(__instance) as Camera;
                if (cam == null) return;

                if (HackMenu.ActiveFeatures.Contains("增大视野"))
                    cam.fieldOfView = defaultFOV + 50f;
                else
                    cam.fieldOfView = defaultFOV;
            }
        }
    }
}
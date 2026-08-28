using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniversalHack
{
    public class VisualPlugin : MonoBehaviour
    {
        private Harmony harmony;
        private bool redModeApplied = false;
        private static System.Random random = new System.Random();
        private float spinAngle = 0f;

        void Awake()
        {
            harmony = new Harmony("com.universal.visual");
            SceneManager.sceneLoaded += OnSceneLoaded;

            var billboardMethod = typeof(Patches).GetMethod("BillboardLateUpdate_Prefix", BindingFlags.Static | BindingFlags.Public);
            var cameraMethod = typeof(Patches).GetMethod("CameraLateUpdate_Postfix", BindingFlags.Static | BindingFlags.Public);

            PatchMethod("Billboard", "LateUpdate", billboardMethod);
            PatchMethod("CameraScript", "LateUpdate", null, cameraMethod);
        }

        void PatchMethod(string typeName, string methodName, MethodInfo prefix = null, MethodInfo postfix = null)
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
                                harmony.Patch(method,
                                    prefix != null ? new HarmonyMethod(prefix) : null,
                                    postfix != null ? new HarmonyMethod(postfix) : null);
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
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (harmony != null)
            {
                try { harmony.UnpatchAll("com.universal.visual"); } catch { }
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            redModeApplied = false;
            spinAngle = 0f;
        }

        void Update()
        {
            bool enableRed = HackMenu.ActiveFeatures.Contains("红温模式");

            if (enableRed && !redModeApplied)
            {
                RenderSettings.ambientLight = Color.red;
                redModeApplied = true;
            }
            else if (!enableRed && redModeApplied)
            {
                RenderSettings.ambientLight = Color.white;
                redModeApplied = false;
            }

            if (HackMenu.ActiveFeatures.Contains("自转"))
            {
                spinAngle += 911f * Time.unscaledDeltaTime;
                if (spinAngle >= 360f) spinAngle -= 360f;
            }
            else
            {
                spinAngle = 0f;
            }
        }

        public static class Patches
        {
            public static bool BillboardLateUpdate_Prefix(MonoBehaviour __instance)
            {
                if (!HackMenu.ActiveFeatures.Contains("贴图旋转"))
                    return true;

                float rx = (float)(random.NextDouble() * 360f);
                float ry = (float)(random.NextDouble() * 360f);
                float rz = (float)(random.NextDouble() * 360f);

                __instance.transform.rotation = Quaternion.Euler(rx, ry, rz);

                return false;
            }

            public static void CameraLateUpdate_Postfix(MonoBehaviour __instance)
            {
                if (!HackMenu.ActiveFeatures.Contains("自转"))
                    return;

                VisualPlugin plugin = Object.FindObjectOfType<VisualPlugin>();
                if (plugin == null) return;

                Transform camTransform = __instance.transform;
                Vector3 euler = camTransform.eulerAngles;
                euler.y += plugin.spinAngle;
                camTransform.eulerAngles = euler;
            }
        }
    }
}
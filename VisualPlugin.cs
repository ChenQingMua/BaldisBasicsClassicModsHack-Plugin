using HarmonyLib;
using System;
using System.Collections.Generic;
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

        private bool hideHudActive = false;
        private Dictionary<GameObject, bool> hudStates = new Dictionary<GameObject, bool>();

        private static readonly string[] hudComponentNames = new string[]
        {
            "Canvas", "CanvasGroup", "Image", "RawImage", "Text", "TextMeshProUGUI", "TextMeshPro"
        };

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
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
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
            hideHudActive = false;
            hudStates.Clear();
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

            bool wantHideHud = HackMenu.ActiveFeatures.Contains("隐藏界面");

            if (wantHideHud && !hideHudActive)
            {
                HideHUD(true);
                hideHudActive = true;
            }
            else if (!wantHideHud && hideHudActive)
            {
                HideHUD(false);
                hideHudActive = false;
            }
        }

        void HideHUD(bool hide)
        {
            if (hide)
            {
                hudStates.Clear();

                foreach (var go in FindObjectsOfType<GameObject>())
                {
                    if (go == null || !go.activeInHierarchy) continue;
                    if (go.transform.parent != null) continue;

                    if (HasHudComponent(go))
                    {
                        if (!hudStates.ContainsKey(go))
                            hudStates[go] = go.activeSelf;
                        go.SetActive(false);
                    }
                }
            }
            else
            {
                foreach (var kv in hudStates)
                {
                    if (kv.Key != null)
                        kv.Key.SetActive(kv.Value);
                }
                hudStates.Clear();
            }
        }

        bool HasHudComponent(GameObject go)
        {
            if (go == null) return false;

            var components = go.GetComponents<Component>();
            foreach (var c in components)
            {
                if (c == null) continue;
                string typeName = c.GetType().Name;
                foreach (string hud in hudComponentNames)
                {
                    if (typeName == hud) return true;
                }
            }
            return false;
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

                VisualPlugin plugin = UnityEngine.Object.FindObjectOfType<VisualPlugin>();
                if (plugin == null) return;

                Transform camTransform = __instance.transform;
                Vector3 euler = camTransform.eulerAngles;
                euler.y += plugin.spinAngle;
                camTransform.eulerAngles = euler;
            }
        }
    }
}
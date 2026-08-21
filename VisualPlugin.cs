using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniversalHack
{
    public class VisualPlugin : MonoBehaviour
    {
        private bool redModeApplied = false;
        private static System.Random random = new System.Random();
        private float spinAngle = 0f;

        void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;

            PatchManager.Register("billboard_spin", "Billboard", "LateUpdate",
                prefix: typeof(Patches).GetMethod("BillboardLateUpdate_Prefix", BindingFlags.Static | BindingFlags.Public));

            PatchManager.Register("camera_spin", "CameraScript", "LateUpdate",
                postfix: typeof(Patches).GetMethod("CameraLateUpdate_Postfix", BindingFlags.Static | BindingFlags.Public));
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
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
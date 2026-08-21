using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniversalHack
{
    public class ItemPlugin : MonoBehaviour
    {
        private static bool patched = false;
        private Harmony harmony;

        void Awake()
        {
            harmony = new Harmony("com.universal.item");
            SceneManager.sceneLoaded += OnSceneLoaded;

            TryPatch();
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {

            TryPatch();
        }

        void TryPatch()
        {
            if (patched) return;

            var method = FindResetItemMethod();
            if (method == null) return;

            var prefix = typeof(Patches).GetMethod("ResetItem_Prefix",
                BindingFlags.Static | BindingFlags.Public);

            if (prefix != null)
            {
                harmony.Patch(method, new HarmonyMethod(prefix));
                patched = true;

            }
        }

        static MethodBase FindResetItemMethod()
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.Name != "GameControllerScript") continue;

                        var method = type.GetMethod("ResetItem",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                        if (method != null) return method;
                    }
                }
                catch { }
            }
            return null;
        }

        public static class Patches
        {
            public static bool ResetItem_Prefix()
            {
                bool block = HackMenu.ActiveFeatures.Contains("无限道具");
                if (block)
                {

                }
                return !block;
            }
        }
    }
}
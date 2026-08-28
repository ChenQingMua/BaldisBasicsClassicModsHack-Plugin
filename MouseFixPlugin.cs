using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace UniversalHack
{
    public class MouseFixPlugin : MonoBehaviour
    {
        private Harmony harmony;

        void Awake()
        {
            harmony = new Harmony("com.universal.mousefix");

            var cursorMethod = typeof(Patches).GetMethod("CursorUpdate_Prefix", BindingFlags.Static | BindingFlags.Public);
            var mouseMethod = typeof(Patches).GetMethod("MouseAppearing_Prefix", BindingFlags.Static | BindingFlags.Public);

            PatchMethod("InGameCursorController", "Update", cursorMethod);
            PatchMethod("MouseAppearingScript", "Update", mouseMethod);
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
                try { harmony.UnpatchAll("com.universal.mousefix"); } catch { }
            }
        }

        public static class Patches
        {
            public static bool CursorUpdate_Prefix()
            {
                if (HackMenu.ShowMenu)
                {
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                    return false;
                }
                return true;
            }

            public static bool MouseAppearing_Prefix() => !HackMenu.ShowMenu;
        }
    }
}
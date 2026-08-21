using System.Reflection;
using UnityEngine;

namespace UniversalHack
{
    public class MouseFixPlugin : MonoBehaviour
    {
        void Awake()
        {
            PatchManager.Register("mousefix_cursor", "InGameCursorController", "Update",
                prefix: typeof(Patches).GetMethod("CursorUpdate_Prefix", BindingFlags.Static | BindingFlags.Public));

            PatchManager.Register("mousefix_appearing", "MouseAppearingScript", "Update",
                prefix: typeof(Patches).GetMethod("MouseAppearing_Prefix", BindingFlags.Static | BindingFlags.Public));
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
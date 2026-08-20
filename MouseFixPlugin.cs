using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UniversalHack
{
    public class MouseFixPlugin : MonoBehaviour
    {
        [DllImport("user32.dll")]
        static extern bool ClipCursor(IntPtr lpRect);

        private bool forceShowCursor = false;

        void Update()
        {

            if (Input.GetKeyDown(KeyCode.F1))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                ClipCursor(IntPtr.Zero);
            }
        }
    }
}
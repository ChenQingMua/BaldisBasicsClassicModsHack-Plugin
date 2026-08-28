using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniversalHack
{
    public class BookPlugin : MonoBehaviour
    {
        private object gcInstance;
        private Type gcType;
        private bool gcFound;

        private static Type inputType;
        private static MethodInfo getKeyDownMethod;

        static BookPlugin()
        {
            try
            {
                string[] assemblyNames = { "UnityEngine.InputLegacyModule", "UnityEngine", "UnityEngine.CoreModule" };
                foreach (string name in assemblyNames)
                {
                    try
                    {
                        var asm = Assembly.Load(name);
                        if (asm != null)
                        {
                            inputType = asm.GetType("UnityEngine.Input");
                            if (inputType != null)
                            {
                                getKeyDownMethod = inputType.GetMethod("GetKeyDown", new Type[] { typeof(KeyCode) });
                                if (getKeyDownMethod != null) break;
                            }
                        }
                    }
                    catch { }
                }

                if (getKeyDownMethod == null)
                {
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            inputType = asm.GetType("UnityEngine.Input");
                            if (inputType != null)
                            {
                                getKeyDownMethod = inputType.GetMethod("GetKeyDown", new Type[] { typeof(KeyCode) });
                                if (getKeyDownMethod != null) break;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            gcInstance = null;
            gcType = null;
            gcFound = false;
        }

        bool IsKeyDown(KeyCode key)
        {
            if (getKeyDownMethod == null) return false;
            try { return (bool)getKeyDownMethod.Invoke(null, new object[] { key }); }
            catch { return false; }
        }

        void Update()
        {
            if (!HackMenu.ActiveFeatures.Contains("书黑客"))
                return;

            if (!gcFound || gcInstance == null)
            {
                FindGameController();
                if (gcInstance == null) return;
            }

            if (IsKeyDown(KeyCode.Q))
                ModifyNotebooks(1);

            if (IsKeyDown(KeyCode.E))
                ModifyNotebooks(-1);
        }

        void ModifyNotebooks(int delta)
        {
            if (gcType == null || gcInstance == null) return;

            var notebooksField = gcType.GetField("notebooks",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (notebooksField == null)
            {
                notebooksField = gcType.GetField("notebookCount",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }

            if (notebooksField == null) return;

            int current = (int)notebooksField.GetValue(gcInstance);
            int newValue = Mathf.Max(0, current + delta);
            notebooksField.SetValue(gcInstance, newValue);

            var updateMethod = gcType.GetMethod("UpdateNotebookCount",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (updateMethod == null)
            {
                updateMethod = gcType.GetMethod("UpdateNotebooks",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }

            if (updateMethod != null)
            {
                updateMethod.Invoke(gcInstance, null);
            }
        }

        private void FindGameController()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.Name == "GameControllerScript" ||
                            type.Name == "GameController" ||
                            type.Name == "GC" ||
                            type.Name.Contains("GameController"))
                        {
                            var obj = FindObjectOfType(type);
                            if (obj != null)
                            {
                                gcInstance = obj;
                                gcType = type;
                                gcFound = true;
                                return;
                            }
                        }
                    }
                }
                catch { }
            }
        }
    }
}
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniversalHack
{
    public class BookPlugin : MonoBehaviour
    {
        private object gcInstance;
        private System.Type gcType;
        private bool gcFound = false;
        private bool qPressed = false;
        private bool ePressed = false;

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

        void Update()
        {
            if (!HackMenu.ActiveFeatures.Contains("书黑客"))
                return;

            if (!gcFound || gcInstance == null)
            {
                FindGameController();
            }

            if (gcInstance == null) return;

            if (Input.GetKeyDown(KeyCode.Q) && !qPressed)
            {
                qPressed = true;
                ModifyNotebooks(1);
            }
            if (Input.GetKeyUp(KeyCode.Q))
            {
                qPressed = false;
            }

            if (Input.GetKeyDown(KeyCode.E) && !ePressed)
            {
                ePressed = true;
                ModifyNotebooks(-1);
            }
            if (Input.GetKeyUp(KeyCode.E))
            {
                ePressed = false;
            }
        }

        void ModifyNotebooks(int delta)
        {
            var notebooksField = gcType.GetField("notebooks",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var updateMethod = gcType.GetMethod("UpdateNotebookCount",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (notebooksField == null) return;

            int current = (int)notebooksField.GetValue(gcInstance);
            int newValue = Mathf.Max(0, current + delta);
            notebooksField.SetValue(gcInstance, newValue);

            if (updateMethod != null)
            {
                updateMethod.Invoke(gcInstance, null);
            }

        }

        private void FindGameController()
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (!type.Name.Contains("GameController")) continue;
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
                catch { }
            }
        }
    }
}
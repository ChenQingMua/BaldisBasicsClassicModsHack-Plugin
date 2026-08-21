using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniversalHack
{
    public class WinPlugin : MonoBehaviour
    {
        private object exitTriggerInstance;
        private System.Type exitTriggerType;
        private bool found = false;
        private bool secretTriggered = false;
        private bool resultsTriggered = false;

        void Update()
        {
            if (!found || exitTriggerInstance == null)
            {
                FindExitTrigger();
            }

            if (exitTriggerInstance == null) return;

            if (HackMenu.ActiveFeatures.Contains("跳转全错场景") && !secretTriggered)
            {
                var secretSceneField = exitTriggerType.GetField("SecretScene",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (secretSceneField != null)
                {
                    string secretScene = secretSceneField.GetValue(exitTriggerInstance) as string;
                    if (!string.IsNullOrEmpty(secretScene))
                    {
                        SceneManager.LoadScene(secretScene);
                        secretTriggered = true;

                    }
                }
            }

            if (HackMenu.ActiveFeatures.Contains("直接胜利") && !resultsTriggered)
            {
                var resultsSceneField = exitTriggerType.GetField("ResultsScene",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (resultsSceneField != null)
                {
                    string resultsScene = resultsSceneField.GetValue(exitTriggerInstance) as string;
                    if (!string.IsNullOrEmpty(resultsScene))
                    {
                        SceneManager.LoadScene(resultsScene);
                        resultsTriggered = true;

                    }
                }
            }

            if (!HackMenu.ActiveFeatures.Contains("跳转全错场景"))
                secretTriggered = false;

            if (!HackMenu.ActiveFeatures.Contains("直接胜利"))
                resultsTriggered = false;
        }

        private void FindExitTrigger()
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.Name != "ExitTriggerScript") continue;
                        var obj = FindObjectOfType(type);
                        if (obj != null)
                        {
                            exitTriggerInstance = obj;
                            exitTriggerType = type;
                            found = true;
                            return;
                        }
                    }
                }
                catch { }
            }
        }
    }
}
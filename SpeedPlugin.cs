using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniversalHack
{
    public class SpeedPlugin : MonoBehaviour
    {
        private static float originalWalkSpeed;
        private static float originalRunSpeed;
        private static bool speedApplied = false;

        private object playerInstance;
        private System.Type playerType;
        private bool playerFound = false;

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

            playerInstance = null;
            playerType = null;
            playerFound = false;
            speedApplied = false;

            if (HackMenu.ActiveFeatures.Contains("移速"))
            {

            }
        }

        void Update()
        {

            if (!playerFound || playerInstance == null)
            {
                FindPlayer();
            }

            if (playerInstance == null || playerType == null) return;

            var walkSpeedField = playerType.GetField("walkSpeed",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var runSpeedField = playerType.GetField("runSpeed",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (walkSpeedField == null || runSpeedField == null) return;

            if (HackMenu.ActiveFeatures.Contains("移速"))
            {
                if (!speedApplied)
                {

                    if (originalWalkSpeed == 0f)
                        originalWalkSpeed = (float)walkSpeedField.GetValue(playerInstance);
                    if (originalRunSpeed == 0f)
                        originalRunSpeed = (float)runSpeedField.GetValue(playerInstance);

                    walkSpeedField.SetValue(playerInstance, originalWalkSpeed * 2f);
                    runSpeedField.SetValue(playerInstance, originalRunSpeed * 2f);
                    speedApplied = true;
                }
            }
            else
            {
                if (speedApplied)
                {
                    walkSpeedField.SetValue(playerInstance, originalWalkSpeed);
                    runSpeedField.SetValue(playerInstance, originalRunSpeed);
                    speedApplied = false;
                }
            }
        }

        private void FindPlayer()
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.Name != "PlayerScript") continue;
                        var obj = FindObjectOfType(type);
                        if (obj != null)
                        {
                            playerInstance = obj;
                            playerType = type;
                            playerFound = true;

                            var walkField = type.GetField("walkSpeed",
                                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            var runField = type.GetField("runSpeed",
                                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            if (walkField != null)
                                originalWalkSpeed = (float)walkField.GetValue(obj);
                            if (runField != null)
                                originalRunSpeed = (float)runField.GetValue(obj);

                            return;
                        }
                    }
                }
                catch { }
            }
        }
    }
}
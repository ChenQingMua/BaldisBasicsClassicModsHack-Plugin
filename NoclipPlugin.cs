using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniversalHack
{
    public class NoclipPlugin : MonoBehaviour
    {
        private object playerInstance;
        private System.Type playerType;
        private bool playerFound = false;
        private FieldInfo ccField;
        private FieldInfo heightField;
        private Transform playerTransform;

        void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {

            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (playerInstance != null && ccField != null)
            {
                var cc = ccField.GetValue(playerInstance) as CharacterController;
                if (cc != null) cc.enabled = true;
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            playerInstance = null;
            playerType = null;
            playerFound = false;
            ccField = null;
            heightField = null;
            playerTransform = null;
        }

        void Update()
        {
            if (!playerFound || playerInstance == null)
            {
                FindPlayer();
            }

            if (playerInstance == null || ccField == null) return;

            var cc = ccField.GetValue(playerInstance) as CharacterController;
            if (cc == null) return;

            if (HackMenu.ActiveFeatures.Contains("穿墙"))
            {
                cc.enabled = false;
                if (heightField != null)
                    heightField.SetValue(playerInstance, 4f);

                float speed = 20f * Time.deltaTime;
                Vector3 move = Vector3.zero;

                if (Input.GetKey(KeyCode.W)) move += playerTransform.forward;
                if (Input.GetKey(KeyCode.S)) move -= playerTransform.forward;
                if (Input.GetKey(KeyCode.A)) move -= playerTransform.right;
                if (Input.GetKey(KeyCode.D)) move += playerTransform.right;

                playerTransform.position += move.normalized * speed;
            }
            else
            {
                cc.enabled = true;
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
                            playerTransform = (obj as MonoBehaviour).transform;
                            ccField = type.GetField("cc",
                                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            heightField = type.GetField("height",
                                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            playerFound = true;
                            return;
                        }
                    }
                }
                catch { }
            }
        }
    }
}
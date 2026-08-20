using System.Reflection;
using UnityEngine;

namespace UniversalHack
{
    public class NoclipPlugin : MonoBehaviour
    {
        private static bool patched = false;
        private static object playerInstance;
        private static System.Type playerType;
        private static Vector3 lastPosition;

        void Awake()
        {
            if (!patched)
            {
                patched = true;
            }
        }

        void Update()
        {
            if (playerInstance == null)
            {
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.Name != "PlayerScript") continue;
                        playerInstance = FindObjectOfType(type);
                        if (playerInstance != null)
                        {
                            playerType = type;
                            break;
                        }
                    }
                    if (playerInstance != null) break;
                }
            }

            if (playerInstance == null) return;

            var ccField = playerType.GetField("cc",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (ccField == null) return;

            var cc = ccField.GetValue(playerInstance) as CharacterController;
            if (cc == null) return;

            var heightField = playerType.GetField("height",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var transform = (playerInstance as MonoBehaviour).transform;

            if (HackMenu.ActiveFeatures.Contains("穿墙"))
            {
                cc.enabled = false;
                if (heightField != null)
                    heightField.SetValue(playerInstance, 4f);

                float speed = 20f * Time.deltaTime;
                Vector3 move = Vector3.zero;

                if (Input.GetKey(KeyCode.W)) move += transform.forward;
                if (Input.GetKey(KeyCode.S)) move -= transform.forward;
                if (Input.GetKey(KeyCode.A)) move -= transform.right;
                if (Input.GetKey(KeyCode.D)) move += transform.right;

                transform.position += move.normalized * speed;
            }
            else
            {
                cc.enabled = true;
            }
        }

        void OnDestroy()
        {
            if (playerInstance != null)
            {
                var ccField = playerType.GetField("cc",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (ccField != null)
                {
                    var cc = ccField.GetValue(playerInstance) as CharacterController;
                    if (cc != null) cc.enabled = true;
                }
            }
        }
    }
}
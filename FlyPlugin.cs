using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniversalHack
{
    public class FlyPlugin : MonoBehaviour
    {
        private object playerInstance;
        private FieldInfo heightField;
        private Transform playerTransform;

        private bool found = false;
        private bool lastFlyState = false;
        private float currentHeight = 0f;
        private float originalHeight = 0f;
        private bool hasOriginalHeight = false;

        private float flySpeed = 8f;
        private float flyUpSpeed = 32f;
        private float flyDownSpeed = 32f;

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
            found = false;
            playerInstance = null;
            playerTransform = null;
            currentHeight = 0f;
            originalHeight = 0f;
            hasOriginalHeight = false;
        }

        void Update()
        {
            bool flyActive = HackMenu.ActiveFeatures.Contains("飞行");

            if (flyActive != lastFlyState)
            {
                lastFlyState = flyActive;

                if (!flyActive && hasOriginalHeight)
                {
                    if (playerTransform != null && heightField != null)
                    {
                        Vector3 pos = playerTransform.position;
                        pos.y = originalHeight;
                        playerTransform.position = pos;
                        heightField.SetValue(playerInstance, originalHeight);
                    }
                    currentHeight = 0f;
                }
            }

            if (!found || playerInstance == null)
            {
                FindPlayer();
                if (playerInstance == null) return;
            }

            if (flyActive && !hasOriginalHeight && playerTransform != null)
            {
                originalHeight = playerTransform.position.y;
                hasOriginalHeight = true;
            }

            if (!flyActive) return;

            try
            {
                if (playerTransform == null)
                {
                    var mono = playerInstance as MonoBehaviour;
                    if (mono != null)
                        playerTransform = mono.transform;
                }

                if (playerTransform == null) return;

                float horizontal = Input.GetAxis("Horizontal");
                float vertical = Input.GetAxis("Vertical");

                Vector3 moveDirection = Vector3.zero;

                if (playerTransform != null)
                {
                    moveDirection = playerTransform.forward * vertical + playerTransform.right * horizontal;
                }
                else
                {
                    moveDirection = new Vector3(horizontal, 0f, vertical);
                }

                if (moveDirection.magnitude > 1f)
                    moveDirection.Normalize();

                moveDirection *= flySpeed * Time.deltaTime;

                if (Input.GetMouseButton(0))
                {
                    currentHeight += flyUpSpeed * Time.deltaTime;
                }

                if (Input.GetMouseButton(1))
                {
                    currentHeight -= flyDownSpeed * Time.deltaTime;
                }

                float currentPlayerHeight = (float)heightField.GetValue(playerInstance);
                float newHeight = currentPlayerHeight + currentHeight;

                Vector3 newPos = playerTransform.position + moveDirection;
                newPos.y = newHeight;

                playerTransform.position = newPos;
                heightField.SetValue(playerInstance, newPos.y);

                currentHeight = 0f;
            }
            catch { }
        }

        void FindPlayer()
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.Name == "PlayerScript")
                        {
                            var obj = FindObjectOfType(type);
                            if (obj != null)
                            {
                                playerInstance = obj;

                                heightField = type.GetField("height",
                                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                                var mono = playerInstance as MonoBehaviour;
                                if (mono != null)
                                {
                                    playerTransform = mono.transform;
                                }

                                found = true;
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
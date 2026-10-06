using System;
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

        private static MethodInfo getKeyMethod;
        private static MethodInfo getAxisMethod;

        static FlyPlugin()
        {
            try
            {
                string[] assemblyNames = { "UnityEngine.InputLegacyModule", "UnityEngine", "UnityEngine.CoreModule" };
                Type inputType = null;

                foreach (string name in assemblyNames)
                {
                    try
                    {
                        var asm = Assembly.Load(name);
                        if (asm != null)
                        {
                            inputType = asm.GetType("UnityEngine.Input");
                            if (inputType != null) break;
                        }
                    }
                    catch { }
                }

                if (inputType == null)
                {
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            inputType = asm.GetType("UnityEngine.Input");
                            if (inputType != null) break;
                        }
                        catch { }
                    }
                }

                if (inputType != null)
                {
                    getKeyMethod = inputType.GetMethod("GetKey", new Type[] { typeof(KeyCode) });
                    getAxisMethod = inputType.GetMethod("GetAxis", new Type[] { typeof(string) });
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
            found = false;
            playerInstance = null;
            playerTransform = null;
            currentHeight = 0f;
            originalHeight = 0f;
            hasOriginalHeight = false;
        }

        bool IsKey(KeyCode key)
        {
            if (getKeyMethod == null) return false;
            try { return (bool)getKeyMethod.Invoke(null, new object[] { key }); }
            catch { return false; }
        }

        float GetAxis(string axis)
        {
            if (getAxisMethod == null) return 0f;
            try { return (float)getAxisMethod.Invoke(null, new object[] { axis }); }
            catch { return 0f; }
        }

        void Update()
        {
            bool flyActive = HackMenu.ActiveFeatures.Contains("飞行");

            if (flyActive != lastFlyState)
            {
                lastFlyState = flyActive;

                if (!flyActive && hasOriginalHeight)
                {
                    if (playerTransform != null && heightField != null && playerInstance != null)
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

                float horizontal = GetAxis("Horizontal");
                float vertical = GetAxis("Vertical");

                Vector3 moveDirection = playerTransform.forward * vertical + playerTransform.right * horizontal;

                if (moveDirection.magnitude > 1f)
                    moveDirection.Normalize();

                moveDirection *= flySpeed * Time.deltaTime;

                bool upInput = IsKey(KeyCode.R);
                bool downInput = IsKey(KeyCode.F);

                if (upInput)
                {
                    currentHeight += flyUpSpeed * Time.deltaTime;
                }

                if (downInput)
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
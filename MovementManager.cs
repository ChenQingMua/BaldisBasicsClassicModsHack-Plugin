using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace UniversalHack
{
    public class MovementManager : MonoBehaviour
    {
        private object playerInstance;
        private Type playerType;
        private bool playerFound;

        private FieldInfo ccField;
        private FieldInfo heightField;
        private FieldInfo walkSpeedField;
        private FieldInfo runSpeedField;
        private FieldInfo moveDirectionField;

        private Transform playerTransform;
        private CharacterController characterController;

        private bool originalDetectCollisions;
        private float originalHeight;
        private bool hasOriginalHeight;

        private float originalWalkSpeed;
        private float originalRunSpeed;
        private bool speedApplied;

        private bool lastFlyState;
        private float currentHeight;
        private float flySpeed = 8f;
        private float flyUpSpeed = 32f;
        private float flyDownSpeed = 32f;

        private static MethodInfo getKeyMethod;
        private static MethodInfo getAxisMethod;

        private static Harmony harmonyInstance;
        private static bool isHarmonyPatched;

        private const string FeatureNoClip = "穿墙";
        private const string FeatureFly = "飞行";
        private const string FeatureSpeed = "移速";
        private const string FeatureAntiPush = "无视推动";

        static MovementManager()
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
            SceneManager.sceneLoaded += new UnityAction<Scene, LoadSceneMode>(OnSceneLoaded);

            if (!isHarmonyPatched)
            {
                try
                {
                    harmonyInstance = new Harmony("com.universalhack.movement");
                    harmonyInstance.PatchAll();
                    PatchAntiPush();
                    isHarmonyPatched = true;
                }
                catch { }
            }
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= new UnityAction<Scene, LoadSceneMode>(OnSceneLoaded);

            if (characterController != null)
            {
                characterController.detectCollisions = originalDetectCollisions;
            }

            if (isHarmonyPatched && harmonyInstance != null)
            {
                try
                {
                    harmonyInstance.UnpatchAll("com.universalhack.movement");
                    isHarmonyPatched = false;
                }
                catch { }
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            playerInstance = null;
            playerType = null;
            playerFound = false;
            ccField = null;
            heightField = null;
            walkSpeedField = null;
            runSpeedField = null;
            moveDirectionField = null;
            playerTransform = null;
            characterController = null;
            hasOriginalHeight = false;
            speedApplied = false;
            lastFlyState = false;
            currentHeight = 0f;
        }

        void Update()
        {
            if (!playerFound || playerInstance == null)
            {
                FindPlayer();
                if (playerInstance == null) return;
            }

            UpdateNoClip();
            UpdateSpeed();
            UpdateFly();
        }

        void UpdateNoClip()
        {
            if (ccField == null) return;

            characterController = ccField.GetValue(playerInstance) as CharacterController;
            if (characterController == null) return;

            bool active = HackMenu.ActiveFeatures.Contains(FeatureNoClip);

            if (active)
            {
                if (characterController.detectCollisions)
                {
                    originalDetectCollisions = characterController.detectCollisions;
                    characterController.detectCollisions = false;
                }

                if (heightField != null && originalHeight == 0f)
                {
                    originalHeight = (float)heightField.GetValue(playerInstance);
                    heightField.SetValue(playerInstance, 4f);
                }
            }
            else
            {
                if (!characterController.detectCollisions)
                {
                    characterController.detectCollisions = originalDetectCollisions;
                }

                if (heightField != null && originalHeight != 0f)
                {
                    heightField.SetValue(playerInstance, originalHeight);
                    originalHeight = 0f;
                }
            }
        }

        void UpdateSpeed()
        {
            if (walkSpeedField == null || runSpeedField == null) return;

            if (HackMenu.ActiveFeatures.Contains(FeatureSpeed))
            {
                if (!speedApplied)
                {
                    if (originalWalkSpeed == 0f)
                        originalWalkSpeed = (float)walkSpeedField.GetValue(playerInstance);
                    if (originalRunSpeed == 0f)
                        originalRunSpeed = (float)runSpeedField.GetValue(playerInstance);

                    walkSpeedField.SetValue(playerInstance, originalWalkSpeed * 3f);
                    runSpeedField.SetValue(playerInstance, originalRunSpeed * 3f);
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

        void UpdateFly()
        {
            bool flyActive = HackMenu.ActiveFeatures.Contains(FeatureFly);

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

            if (flyActive && !hasOriginalHeight && playerTransform != null)
            {
                originalHeight = playerTransform.position.y;
                hasOriginalHeight = true;
            }

            if (!flyActive) return;
            if (playerTransform == null)
            {
                var mono = playerInstance as MonoBehaviour;
                if (mono != null) playerTransform = mono.transform;
            }
            if (playerTransform == null) return;
            if (heightField == null) return;

            try
            {
                float horizontal = GetAxis("Horizontal");
                float vertical = GetAxis("Vertical");

                Vector3 moveDirection = playerTransform.forward * vertical + playerTransform.right * horizontal;

                if (moveDirection.magnitude > 1f)
                    moveDirection.Normalize();

                moveDirection *= flySpeed * Time.deltaTime;

                bool upInput = IsKey(KeyCode.R);
                bool downInput = IsKey(KeyCode.F);

                if (upInput) currentHeight += flyUpSpeed * Time.deltaTime;
                if (downInput) currentHeight -= flyDownSpeed * Time.deltaTime;

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

        void FindPlayer()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (Type type in assembly.GetTypes())
                    {
                        if (type.Name != "PlayerScript") continue;

                        UnityEngine.Object obj = UnityEngine.Object.FindObjectOfType(type);
                        if (obj == null) continue;

                        playerInstance = obj;
                        playerType = type;
                        playerTransform = (obj as MonoBehaviour).transform;

                        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                        ccField = type.GetField("cc", flags);
                        heightField = type.GetField("height", flags);
                        walkSpeedField = type.GetField("walkSpeed", flags);
                        runSpeedField = type.GetField("runSpeed", flags);
                        moveDirectionField = type.GetField("moveDirection", flags);

                        if (ccField != null)
                        {
                            characterController = ccField.GetValue(playerInstance) as CharacterController;
                            if (characterController != null)
                                originalDetectCollisions = characterController.detectCollisions;
                        }

                        if (heightField != null)
                            originalHeight = (float)heightField.GetValue(playerInstance);

                        if (walkSpeedField != null)
                            originalWalkSpeed = (float)walkSpeedField.GetValue(playerInstance);
                        if (runSpeedField != null)
                            originalRunSpeed = (float)runSpeedField.GetValue(playerInstance);

                        playerFound = true;
                        return;
                    }
                }
                catch { }
            }
        }

        void PatchAntiPush()
        {
            var prefix = typeof(MovementManager).GetMethod("OnTriggerStay_Prefix",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (prefix == null) return;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.Name != "PlayerScript") continue;
                        var method = type.GetMethod("OnTriggerStay",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (method != null)
                        {
                            harmonyInstance.Patch(method, new HarmonyMethod(prefix));
                            return;
                        }
                    }
                }
                catch { }
            }
        }

        static bool OnTriggerStay_Prefix() => !HackMenu.ActiveFeatures.Contains(FeatureAntiPush);
    }

    [HarmonyPatch]
    public static class MovementNoClipPatch
    {
        static MethodInfo TargetMethod()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name == "PlayerScript")
                        {
                            return type.GetMethod("PlayerMove",
                                BindingFlags.Instance | BindingFlags.NonPublic);
                        }
                    }
                }
                catch { }
            }
            return null;
        }

        static FieldInfo GetMoveDirectionField(object instance)
        {
            return instance?.GetType().GetField("moveDirection",
                BindingFlags.Instance | BindingFlags.NonPublic);
        }

        static FieldInfo GetCcField(object instance)
        {
            return instance?.GetType().GetField("cc",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        static FieldInfo GetHeightField(object instance)
        {
            return instance?.GetType().GetField("height",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        static Vector3 lastPosition;
        static bool hasLastPosition;

        [HarmonyPrefix]
        static void Prefix(object __instance)
        {
            if (!HackMenu.ActiveFeatures.Contains("穿墙"))
                return;

            try
            {
                var transform = (__instance as MonoBehaviour)?.transform;
                if (transform == null) return;
                lastPosition = transform.position;
                hasLastPosition = true;
            }
            catch { }
        }

        [HarmonyPostfix]
        static void Postfix(object __instance)
        {
            if (!HackMenu.ActiveFeatures.Contains("穿墙"))
                return;

            try
            {
                if (!hasLastPosition) return;

                var transform = (__instance as MonoBehaviour)?.transform;
                if (transform == null) return;

                var ccField = GetCcField(__instance);
                if (ccField == null) return;
                var cc = ccField.GetValue(__instance) as CharacterController;
                if (cc == null) return;

                var heightField = GetHeightField(__instance);
                if (heightField == null) return;
                float height = (float)heightField.GetValue(__instance);

                var moveDirectionField = GetMoveDirectionField(__instance);
                if (moveDirectionField == null) return;

                Vector3 moveDirection = (Vector3)moveDirectionField.GetValue(__instance);

                Vector3 targetPosition = lastPosition + moveDirection;
                targetPosition.y = height;

                transform.position = targetPosition;
                moveDirectionField.SetValue(__instance, Vector3.zero);
                cc.Move(Vector3.zero);

                hasLastPosition = false;
            }
            catch { }
        }
    }
}
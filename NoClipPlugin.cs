using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace UniversalHack
{
    public class NoClipPlugin : MonoBehaviour
    {
        private object playerInstance;
        private Type playerType;
        private bool playerFound;
        private FieldInfo ccField;
        private FieldInfo heightField;
        private Transform playerTransform;
        private CharacterController characterController;
        private bool originalDetectCollisions;
        private float originalHeight;

        private static Harmony harmonyInstance;
        private static bool isHarmonyPatched = false;

        private void Awake()
        {
            SceneManager.sceneLoaded += new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);

            if (!isHarmonyPatched)
            {
                try
                {
                    harmonyInstance = new Harmony("com.universalhack.noclip");
                    harmonyInstance.PatchAll();
                    isHarmonyPatched = true;
                }
                catch (Exception) { }
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);

            if (isHarmonyPatched && harmonyInstance != null)
            {
                try
                {
                    harmonyInstance.UnpatchAll("com.universalhack.noclip");
                    isHarmonyPatched = false;
                }
                catch (Exception) { }
            }

            if (characterController != null)
            {
                characterController.detectCollisions = originalDetectCollisions;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            this.playerInstance = null;
            this.playerType = null;
            this.playerFound = false;
            this.ccField = null;
            this.heightField = null;
            this.playerTransform = null;
            this.characterController = null;
        }

        private void Update()
        {
            if (!this.playerFound || this.playerInstance == null)
            {
                this.FindPlayer();
            }

            if (this.playerInstance == null || this.ccField == null)
            {
                return;
            }

            this.characterController = this.ccField.GetValue(this.playerInstance) as CharacterController;
            if (this.characterController == null)
            {
                return;
            }

            bool isNoClipActive = HackMenu.ActiveFeatures.Contains("穿墙");

            if (isNoClipActive)
            {
                if (this.characterController.detectCollisions)
                {
                    originalDetectCollisions = this.characterController.detectCollisions;
                    this.characterController.detectCollisions = false;
                }

                if (this.heightField != null && originalHeight == 0f)
                {
                    originalHeight = (float)this.heightField.GetValue(this.playerInstance);
                    this.heightField.SetValue(this.playerInstance, 4f);
                }
            }
            else
            {
                if (!this.characterController.detectCollisions)
                {
                    this.characterController.detectCollisions = originalDetectCollisions;
                }

                if (this.heightField != null && originalHeight != 0f)
                {
                    this.heightField.SetValue(this.playerInstance, originalHeight);
                    originalHeight = 0f;
                }
            }
        }

        private void FindPlayer()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (Type type in assembly.GetTypes())
                    {
                        if (type.Name == "PlayerScript")
                        {
                            UnityEngine.Object @object = UnityEngine.Object.FindObjectOfType(type);
                            if (@object != null)
                            {
                                this.playerInstance = @object;
                                this.playerType = type;
                                this.playerTransform = (@object as MonoBehaviour).transform;
                                this.ccField = type.GetField("cc", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                this.heightField = type.GetField("height", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                this.playerFound = true;

                                if (this.ccField != null)
                                {
                                    this.characterController = this.ccField.GetValue(this.playerInstance) as CharacterController;
                                    if (this.characterController != null)
                                    {
                                        originalDetectCollisions = this.characterController.detectCollisions;
                                    }
                                }

                                if (this.heightField != null)
                                {
                                    originalHeight = (float)this.heightField.GetValue(this.playerInstance);
                                }

                                return;
                            }
                        }
                    }
                }
                catch { }
            }
        }
    }

    [HarmonyPatch]
    public static class NoClipPatch
    {
        private static FieldInfo moveDirectionField;
        private static FieldInfo ccField;
        private static FieldInfo heightField;
        private static Vector3 lastPosition;
        private static bool hasLastPosition;

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
                            moveDirectionField = type.GetField("moveDirection",
                                BindingFlags.Instance | BindingFlags.NonPublic);
                            ccField = type.GetField("cc",
                                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            heightField = type.GetField("height",
                                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                            return type.GetMethod("PlayerMove",
                                BindingFlags.Instance | BindingFlags.NonPublic);
                        }
                    }
                }
                catch { }
            }
            return null;
        }

        [HarmonyPrefix]
        static void Prefix(object __instance)
        {
            if (!HackMenu.ActiveFeatures.Contains("穿墙"))
                return;

            try
            {
                var transform = (__instance as MonoBehaviour)?.transform;
                if (transform == null)
                    return;

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
                if (!hasLastPosition)
                    return;

                var transform = (__instance as MonoBehaviour)?.transform;
                if (transform == null)
                    return;

                if (ccField == null)
                    return;

                var cc = ccField.GetValue(__instance) as CharacterController;
                if (cc == null)
                    return;

                if (heightField == null)
                    return;

                float height = (float)heightField.GetValue(__instance);

                if (moveDirectionField == null)
                    return;

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
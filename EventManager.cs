using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniversalHack
{
    public class EventManager : MonoBehaviour
    {
        private Harmony harmony;
        private Dictionary<string, bool> lastStates = new Dictionary<string, bool>();
        private object gcInstance;
        private Type gcType;
        private bool gcFound;
        private bool initialized;
        private bool angerTriggered;

        private object baldiInstance;
        private Type baldiType;
        private bool baldiFound;

        private object bookGcInstance;
        private Type bookGcType;
        private bool bookGcFound;

        private static Type inputType;
        private static MethodInfo getKeyDownMethod;

        private const string FeatureDeafBaldi = "聋哑巴迪";
        private const string FeatureAnger = "直接激活愤怒";
        private const string FeatureDisableBaldiMove = "禁用巴迪移动";
        private const string FeatureIgnorePrincipal = "无视校长互动";
        private const string FeatureIgnoreCrafters = "无视袜子互动";
        private const string FeatureBookHack = "书黑客";

        static EventManager()
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
            harmony = new Harmony("com.universalhack.event");
            SceneManager.sceneLoaded += OnSceneLoaded;

            var baldiMoveMethod = typeof(EventManager).GetMethod("BaldiMove_Prefix", BindingFlags.Static | BindingFlags.Public);
            var principalMethod = typeof(EventManager).GetMethod("PrincipalTrigger_Prefix", BindingFlags.Static | BindingFlags.Public);
            var craftersMethod = typeof(EventManager).GetMethod("CraftersTrigger_Prefix", BindingFlags.Static | BindingFlags.Public);

            PatchMethod("BaldiScript", "Move", baldiMoveMethod);
            PatchMethod("PrincipalScript", "OnTriggerStay", principalMethod);
            PatchMethod("CraftersScript", "OnTriggerEnter", craftersMethod);
        }

        void PatchMethod(string typeName, string methodName, MethodInfo prefix)
        {
            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.Name != typeName) continue;

                        var method = type.GetMethod(methodName,
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (method != null)
                        {
                            harmony.Patch(method, prefix != null ? new HarmonyMethod(prefix) : null);
                            return;
                        }
                    }
                }
            }
            catch { }
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (harmony != null)
            {
                try { harmony.UnpatchAll("com.universalhack.event"); } catch { }
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            gcInstance = null;
            gcType = null;
            gcFound = false;
            baldiInstance = null;
            baldiType = null;
            baldiFound = false;
            bookGcInstance = null;
            bookGcType = null;
            bookGcFound = false;
            initialized = false;
            angerTriggered = false;
            lastStates.Clear();
        }

        void Update()
        {
            UpdateEvents();
            UpdateBookHack();
        }

        void UpdateEvents()
        {
            if (!gcFound || gcInstance == null)
            {
                FindGameController();
            }

            if (gcInstance == null) return;

            if (!initialized)
            {
                InitializeStates();
                return;
            }

            if (HackMenu.ActiveFeatures.Contains(FeatureAnger) && !angerTriggered)
            {
                TriggerAnger();
            }

            if (HackMenu.ActiveFeatures.Contains(FeatureDeafBaldi))
            {
                ApplyDeafBaldi();
            }

            ToggleObject("无巴迪", "baldi");
            ToggleObject("无校长", "principal");
            ToggleObject("无袜子", "crafters");
            ToggleObject("无欢乐时间", "playtime");
            ToggleObject("无扫把", "gottaSweep");
            ToggleObject("无第一名", "firstPrize");
            ToggleObject("无校霸", "bully");
        }

        void UpdateBookHack()
        {
            if (!HackMenu.ActiveFeatures.Contains(FeatureBookHack))
                return;

            if (!bookGcFound || bookGcInstance == null)
            {
                FindBookGameController();
                if (bookGcInstance == null) return;
            }

            if (IsKeyDown(KeyCode.Q))
                ModifyNotebooks(1);

            if (IsKeyDown(KeyCode.E))
                ModifyNotebooks(-1);
        }

        bool IsKeyDown(KeyCode key)
        {
            if (getKeyDownMethod == null) return false;
            try { return (bool)getKeyDownMethod.Invoke(null, new object[] { key }); }
            catch { return false; }
        }

        void ModifyNotebooks(int delta)
        {
            if (bookGcType == null || bookGcInstance == null) return;

            var notebooksField = bookGcType.GetField("notebooks",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (notebooksField == null)
            {
                notebooksField = bookGcType.GetField("notebookCount",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }

            if (notebooksField == null) return;

            int current = (int)notebooksField.GetValue(bookGcInstance);
            int newValue = Mathf.Max(0, current + delta);
            notebooksField.SetValue(bookGcInstance, newValue);

            var updateMethod = bookGcType.GetMethod("UpdateNotebookCount",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (updateMethod == null)
            {
                updateMethod = bookGcType.GetMethod("UpdateNotebooks",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }

            if (updateMethod != null)
            {
                updateMethod.Invoke(bookGcInstance, null);
            }
        }

        void FindGameController()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
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

                            FindBaldi();
                            return;
                        }
                    }
                }
                catch { }
            }
        }

        void FindBookGameController()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.Name != "GameControllerScript" &&
                            type.Name != "GameController" &&
                            type.Name != "GC" &&
                            !type.Name.Contains("GameController"))
                            continue;

                        var obj = FindObjectOfType(type);
                        if (obj != null)
                        {
                            bookGcInstance = obj;
                            bookGcType = type;
                            bookGcFound = true;
                            return;
                        }
                    }
                }
                catch { }
            }
        }

        void FindBaldi()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.Name != "BaldiScript") continue;
                        var obj = FindObjectOfType(type);
                        if (obj != null)
                        {
                            baldiInstance = obj;
                            baldiType = type;
                            baldiFound = true;
                            return;
                        }
                    }
                }
                catch { }
            }
        }

        void ApplyDeafBaldi()
        {
            if (baldiInstance == null || baldiType == null) return;

            var antiHearingField = baldiType.GetField("antiHearing",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var antiHearingTimeField = baldiType.GetField("antiHearingTime",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (antiHearingField != null)
                antiHearingField.SetValue(baldiInstance, true);

            if (antiHearingTimeField != null)
                antiHearingTimeField.SetValue(baldiInstance, 9999f);
        }

        void TriggerAnger()
        {
            var getAngryMethod = gcType.GetMethod("GetAngry",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (getAngryMethod != null)
            {
                getAngryMethod.Invoke(gcInstance, new object[] { 1f });
                angerTriggered = true;
            }
        }

        void InitializeStates()
        {
            string[] features = { "无巴迪", "无校长", "无袜子", "无欢乐时间", "无扫把", "无第一名", "无校霸" };
            string[] fields = { "baldi", "principal", "crafters", "playtime", "gottaSweep", "firstPrize", "bully" };

            for (int i = 0; i < features.Length; i++)
            {
                var field = gcType.GetField(fields[i],
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                bool currentEnabled = false;

                if (field != null)
                {
                    var obj = field.GetValue(gcInstance) as GameObject;
                    if (obj != null)
                    {
                        bool objActive = obj.activeSelf;
                        bool featureOn = HackMenu.ActiveFeatures.Contains(features[i]);

                        if (featureOn && objActive)
                        {
                            obj.SetActive(false);
                        }

                        currentEnabled = featureOn;
                    }
                }

                lastStates[features[i]] = currentEnabled;
            }

            initialized = true;
        }

        void ToggleObject(string featureName, string fieldName)
        {
            bool current = HackMenu.ActiveFeatures.Contains(featureName);
            if (!lastStates.ContainsKey(featureName)) lastStates[featureName] = false;

            if (current != lastStates[featureName])
            {
                lastStates[featureName] = current;
                var field = gcType.GetField(fieldName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    var obj = field.GetValue(gcInstance) as GameObject;
                    if (obj != null)
                    {
                        obj.SetActive(!current);
                    }
                }
            }
        }

        public static bool BaldiMove_Prefix() => !HackMenu.ActiveFeatures.Contains(FeatureDisableBaldiMove);
        public static bool PrincipalTrigger_Prefix() => !HackMenu.ActiveFeatures.Contains(FeatureIgnorePrincipal);
        public static bool CraftersTrigger_Prefix() => !HackMenu.ActiveFeatures.Contains(FeatureIgnoreCrafters);
    }
}
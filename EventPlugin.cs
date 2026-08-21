using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniversalHack
{
    public class EventPlugin : MonoBehaviour
    {
        private Dictionary<string, bool> lastStates = new Dictionary<string, bool>();
        private object gcInstance;
        private System.Type gcType;
        private bool gcFound = false;
        private bool initialized = false;
        private bool angerTriggered = false;

        private object baldiInstance;
        private System.Type baldiType;
        private bool baldiFound = false;

        void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;

            PatchManager.Register("baldi_move", "BaldiScript", "Move",
                prefix: typeof(Patches).GetMethod("BaldiMove_Prefix", BindingFlags.Static | BindingFlags.Public));

            PatchManager.Register("principal_trigger", "PrincipalScript", "OnTriggerStay",
                prefix: typeof(Patches).GetMethod("PrincipalTrigger_Prefix", BindingFlags.Static | BindingFlags.Public));

            PatchManager.Register("crafters_trigger", "CraftersScript", "OnTriggerEnter",
                prefix: typeof(Patches).GetMethod("CraftersTrigger_Prefix", BindingFlags.Static | BindingFlags.Public));
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
            baldiInstance = null;
            baldiType = null;
            baldiFound = false;
            initialized = false;
            angerTriggered = false;
            lastStates.Clear();
        }

        void Update()
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

            if (HackMenu.ActiveFeatures.Contains("直接激活愤怒") && !angerTriggered)
            {
                TriggerAnger();
            }

            if (HackMenu.ActiveFeatures.Contains("聋哑巴迪"))
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

                            FindBaldi();
                            return;
                        }
                    }
                }
                catch { }
            }
        }

        private void FindBaldi()
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
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

        private void ApplyDeafBaldi()
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

        private void TriggerAnger()
        {
            var getAngryMethod = gcType.GetMethod("GetAngry",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (getAngryMethod != null)
            {
                getAngryMethod.Invoke(gcInstance, new object[] { 1f });
                angerTriggered = true;

            }
        }

        private void InitializeStates()
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

        private void ToggleObject(string featureName, string fieldName)
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
                    if (obj != null) obj.SetActive(!current);
                }
            }
        }

        public static class Patches
        {
            public static bool BaldiMove_Prefix() => !HackMenu.ActiveFeatures.Contains("禁用巴迪移动");
            public static bool PrincipalTrigger_Prefix() => !HackMenu.ActiveFeatures.Contains("无视校长互动");
            public static bool CraftersTrigger_Prefix() => !HackMenu.ActiveFeatures.Contains("无视袜子互动");
        }
    }
}
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using HarmonyLib;

namespace UniversalHack
{
    public class EventPlugin : MonoBehaviour
    {
        private static bool patched = false;
        private Dictionary<string, bool> lastStates = new Dictionary<string, bool>();
        private object gcInstance;
        private System.Type gcType;

        void Awake()
        {
            if (!patched)
            {
                var harmony = new Harmony("com.universal.event");
                harmony.PatchAll();
                patched = true;
            }
        }

        void Update()
        {
            if (gcInstance == null)
            {
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (!type.Name.Contains("GameController")) continue;
                        gcInstance = FindObjectOfType(type);
                        if (gcInstance != null)
                        {
                            gcType = type;
                            break;
                        }
                    }
                    if (gcInstance != null) break;
                }
            }

            if (gcInstance == null) return;

            bool currentBaldi = HackMenu.ActiveFeatures.Contains("无巴迪");
            if (!lastStates.ContainsKey("无巴迪")) lastStates["无巴迪"] = false;
            if (currentBaldi != lastStates["无巴迪"])
            {
                lastStates["无巴迪"] = currentBaldi;
                var baldiField = gcType.GetField("baldi", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (baldiField != null)
                {
                    var baldi = baldiField.GetValue(gcInstance) as GameObject;
                    if (baldi != null) baldi.SetActive(!currentBaldi);
                }
            }

            bool currentPrincipal = HackMenu.ActiveFeatures.Contains("无校长");
            if (!lastStates.ContainsKey("无校长")) lastStates["无校长"] = false;
            if (currentPrincipal != lastStates["无校长"])
            {
                lastStates["无校长"] = currentPrincipal;
                var principalField = gcType.GetField("principal", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (principalField != null)
                {
                    var principal = principalField.GetValue(gcInstance) as GameObject;
                    if (principal != null) principal.SetActive(!currentPrincipal);
                }
            }

            bool currentCrafters = HackMenu.ActiveFeatures.Contains("无袜子");
            if (!lastStates.ContainsKey("无袜子")) lastStates["无袜子"] = false;
            if (currentCrafters != lastStates["无袜子"])
            {
                lastStates["无袜子"] = currentCrafters;
                var craftersField = gcType.GetField("crafters", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (craftersField != null)
                {
                    var crafters = craftersField.GetValue(gcInstance) as GameObject;
                    if (crafters != null) crafters.SetActive(!currentCrafters);
                }
            }

            bool currentPlaytime = HackMenu.ActiveFeatures.Contains("无欢乐时间");
            if (!lastStates.ContainsKey("无欢乐时间")) lastStates["无欢乐时间"] = false;
            if (currentPlaytime != lastStates["无欢乐时间"])
            {
                lastStates["无欢乐时间"] = currentPlaytime;
                var playtimeField = gcType.GetField("playtime", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (playtimeField != null)
                {
                    var playtime = playtimeField.GetValue(gcInstance) as GameObject;
                    if (playtime != null) playtime.SetActive(!currentPlaytime);
                }
            }

            bool currentSweep = HackMenu.ActiveFeatures.Contains("无扫把");
            if (!lastStates.ContainsKey("无扫把")) lastStates["无扫把"] = false;
            if (currentSweep != lastStates["无扫把"])
            {
                lastStates["无扫把"] = currentSweep;
                var gottaSweepField = gcType.GetField("gottaSweep", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (gottaSweepField != null)
                {
                    var gottaSweep = gottaSweepField.GetValue(gcInstance) as GameObject;
                    if (gottaSweep != null) gottaSweep.SetActive(!currentSweep);
                }
            }

            bool currentFirstPrize = HackMenu.ActiveFeatures.Contains("无第一名");
            if (!lastStates.ContainsKey("无第一名")) lastStates["无第一名"] = false;
            if (currentFirstPrize != lastStates["无第一名"])
            {
                lastStates["无第一名"] = currentFirstPrize;
                var firstPrizeField = gcType.GetField("firstPrize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (firstPrizeField != null)
                {
                    var firstPrize = firstPrizeField.GetValue(gcInstance) as GameObject;
                    if (firstPrize != null) firstPrize.SetActive(!currentFirstPrize);
                }
            }

            bool currentBully = HackMenu.ActiveFeatures.Contains("无校霸");
            if (!lastStates.ContainsKey("无校霸")) lastStates["无校霸"] = false;
            if (currentBully != lastStates["无校霸"])
            {
                lastStates["无校霸"] = currentBully;
                var bullyField = gcType.GetField("bully", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (bullyField != null)
                {
                    var bully = bullyField.GetValue(gcInstance) as GameObject;
                    if (bully != null) bully.SetActive(!currentBully);
                }
            }
        }

        [HarmonyPatch]
        public class BaldiScript_Move_Patch
        {
            static MethodBase TargetMethod()
            {
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.Name != "BaldiScript") continue;
                        var method = type.GetMethod("Move",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (method != null) return method;
                    }
                }
                return null;
            }

            static bool Prefix()
            {
                return !HackMenu.ActiveFeatures.Contains("禁用巴迪移动");
            }
        }

        [HarmonyPatch]
        public class PrincipalScript_OnTriggerStay_Patch
        {
            static MethodBase TargetMethod()
            {
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.Name != "PrincipalScript") continue;
                        var method = type.GetMethod("OnTriggerStay",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (method != null) return method;
                    }
                }
                return null;
            }

            static bool Prefix()
            {
                return !HackMenu.ActiveFeatures.Contains("无视校长互动");
            }
        }

        [HarmonyPatch]
        public class CraftersScript_OnTriggerEnter_Patch
        {
            static MethodBase TargetMethod()
            {
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.Name != "CraftersScript") continue;
                        var method = type.GetMethod("OnTriggerEnter",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (method != null) return method;
                    }
                }
                return null;
            }

            static bool Prefix()
            {
                return !HackMenu.ActiveFeatures.Contains("无视袜子互动");
            }
        }
    }
}
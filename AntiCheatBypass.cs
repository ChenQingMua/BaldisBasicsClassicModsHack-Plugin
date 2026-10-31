using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UniversalHack
{
    [BepInPlugin("top.muah.baldi.bypass", "Anti Cheat Bypass", "1.0.0")]
    public class AntiCheatBypass : BaseUnityPlugin
    {
        private Harmony harmony;
        private static string gameRoot;
        private static string bepInExPath;

        private static readonly string[] ClassKeywords =
        {
            "AntiCheat", "AntiHack", "AntiCheating", "CheatDetect",
            "HackDetect", "IntegrityCheck", "SecurityCheck"
        };

        private static readonly string[] MethodKeywords =
        {
            "AntiCheat", "AntiHack", "DetectCheat", "DetectHack",
            "CheckIntegrity", "StartAntiCheat", "VerifyIntegrity"
        };

        private static readonly string[] SensitiveAssemblyKeywords =
        {
            "BepInEx", "UnityExplorer", "MelonLoader", "Harmony"
        };

        private static readonly List<string> PatchedLog = new List<string>();

        private void Awake()
        {
            gameRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            bepInExPath = Path.GetFullPath(Path.Combine(gameRoot, "BepInEx"));

            harmony = new Harmony("com.universal.anticheat.bypass");

            PatchDirectoryExists();
            PatchFileExists();
            PatchDirectoryGetDirectories();
            PatchDirectoryGetFiles();
            PatchDirectoryInfoExists();
            PatchGetAssemblies();

            int scanned = ScanAndPatch();
            PatchApplicationQuit();

            Logger.LogInfo($"[AntiCheatBypass] 初始化完成，扫描 Patch {scanned} 个成员。");
            foreach (var line in PatchedLog) Logger.LogInfo("  " + line);
        }

        private void OnDestroy()
        {
            try { harmony?.UnpatchAll("com.universal.anticheat.bypass"); } catch { }
        }

        private void PatchDirectoryExists()
        {
            try
            {
                var m = typeof(Directory).GetMethod(nameof(Directory.Exists),
                    BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                if (m == null) return;
                harmony.Patch(m, new HarmonyMethod(typeof(BypassPatches)
                    .GetMethod(nameof(BypassPatches.DirectoryExists_Prefix))));
            }
            catch (Exception e) { Logger.LogWarning("Patch Directory.Exists 失败: " + e.Message); }
        }

        private void PatchFileExists()
        {
            try
            {
                var m = typeof(File).GetMethod(nameof(File.Exists),
                    BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                if (m == null) return;
                harmony.Patch(m, new HarmonyMethod(typeof(BypassPatches)
                    .GetMethod(nameof(BypassPatches.FileExists_Prefix))));
            }
            catch (Exception e) { Logger.LogWarning("Patch File.Exists 失败: " + e.Message); }
        }

        private void PatchDirectoryGetDirectories()
        {
            try
            {
                var m = typeof(Directory).GetMethod(nameof(Directory.GetDirectories),
                    BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                if (m == null) return;
                harmony.Patch(m, null, new HarmonyMethod(typeof(BypassPatches)
                    .GetMethod(nameof(BypassPatches.GetDirectories_Postfix))));
            }
            catch (Exception e) { Logger.LogWarning("Patch Directory.GetDirectories 失败: " + e.Message); }
        }

        private void PatchDirectoryGetFiles()
        {
            try
            {
                var m = typeof(Directory).GetMethod(nameof(Directory.GetFiles),
                    BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                if (m == null) return;
                harmony.Patch(m, null, new HarmonyMethod(typeof(BypassPatches)
                    .GetMethod(nameof(BypassPatches.GetFiles_Postfix))));
            }
            catch (Exception e) { Logger.LogWarning("Patch Directory.GetFiles 失败: " + e.Message); }
        }

        private void PatchDirectoryInfoExists()
        {
            try
            {
                var prop = typeof(DirectoryInfo).GetProperty(nameof(DirectoryInfo.Exists),
                    BindingFlags.Public | BindingFlags.Instance);
                if (prop?.GetGetMethod() == null) return;
                harmony.Patch(prop.GetGetMethod(), null, new HarmonyMethod(typeof(BypassPatches)
                    .GetMethod(nameof(BypassPatches.DirectoryInfoExists_Postfix))));
            }
            catch (Exception e) { Logger.LogWarning("Patch DirectoryInfo.Exists 失败: " + e.Message); }
        }

        private void PatchGetAssemblies()
        {
            try
            {
                var m = typeof(AppDomain).GetMethod(nameof(AppDomain.GetAssemblies),
                    BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                if (m == null) return;
                harmony.Patch(m, null, new HarmonyMethod(typeof(BypassPatches)
                    .GetMethod(nameof(BypassPatches.GetAssemblies_Postfix))));
            }
            catch (Exception e) { Logger.LogWarning("Patch AppDomain.GetAssemblies 失败: " + e.Message); }
        }

        private int ScanAndPatch()
        {
            int count = 0;
            var self = typeof(AntiCheatBypass).Assembly;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (string.IsNullOrEmpty(asm.Location)) continue;
                if (asm == self) continue;

                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }
                catch { continue; }

                foreach (var type in types)
                {
                    if (type == null) continue;

                    bool classHit = ClassKeywords.Any(k =>
                        type.Name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);

                    count += PatchTypeMethods(type, classHit ? "类名:" + type.FullName : null);
                }
            }
            return count;
        }

        private int PatchTypeMethods(Type type, string reason)
        {
            int count = 0;
            MethodInfo[] methods;
            try
            {
                methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                          BindingFlags.Static | BindingFlags.Instance |
                                          BindingFlags.DeclaredOnly);
            }
            catch { return 0; }

            foreach (var m in methods)
            {
                if (m == null || m.IsAbstract || m.ContainsGenericParameters) continue;

                bool hit = reason != null ||
                           MethodKeywords.Any(k =>
                               m.Name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
                if (!hit) continue;
                if (!m.IsStatic) continue;

                MethodInfo prefix;
                if (m.ReturnType == typeof(void))
                    prefix = typeof(GenericSkipPatch).GetMethod(nameof(GenericSkipPatch.SkipVoid_Prefix));
                else if (m.ReturnType == typeof(bool))
                    prefix = typeof(GenericSkipPatch).GetMethod(nameof(GenericSkipPatch.SkipBool_Prefix));
                else if (m.ReturnType == typeof(int))
                    prefix = typeof(GenericSkipPatch).GetMethod(nameof(GenericSkipPatch.SkipInt_Prefix));
                else
                    continue;

                try
                {
                    harmony.Patch(m, new HarmonyMethod(prefix));
                    count++;
                    PatchedLog.Add($"[{reason ?? "方法名"}] {type.FullName}.{m.Name} -> {m.ReturnType.Name}");
                }
                catch (Exception e)
                {
                    PatchedLog.Add($"FAIL {type.FullName}.{m.Name}: {e.Message}");
                }
            }
            return count;
        }

        private void PatchApplicationQuit()
        {
            try
            {
                var m = typeof(Application).GetMethod(nameof(Application.Quit),
                    BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                if (m == null) { Logger.LogWarning("未找到 Application.Quit()。"); return; }
                harmony.Patch(m, new HarmonyMethod(typeof(BypassPatches)
                    .GetMethod(nameof(BypassPatches.Quit_Prefix))));
                Logger.LogInfo("已拦截 Application.Quit。");
            }
            catch (Exception e) { Logger.LogWarning("Patch Application.Quit 失败: " + e.Message); }
        }

        internal static bool IsBepInExPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            try
            {
                string full;
                try { full = Path.GetFullPath(path); } catch { full = path; }
                full = full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                string[] exactTargets =
                {
                    Path.Combine(gameRoot, "winhttp.dll"),
                    Path.Combine(gameRoot, "doorstop_config.ini"),
                    Path.Combine(gameRoot, "BepInEx"),
                    Path.Combine(gameRoot, "MelonLoader"),
                };

                foreach (var target in exactTargets)
                {
                    string t = Path.GetFullPath(target)
                        .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    if (string.Equals(full, t, StringComparison.OrdinalIgnoreCase)) return true;
                    if (full.StartsWith(t + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
            catch { }
            return false;
        }

        internal static bool IsSensitiveAssembly(Assembly asm)
        {
            if (asm == null || string.IsNullOrEmpty(asm.Location)) return false;
            string name;
            try { name = asm.GetName().Name; } catch { return false; }
            if (string.IsNullOrEmpty(name)) return false;
            foreach (var kw in SensitiveAssemblyKeywords)
                if (name.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        internal static void Log(string msg)
        {
            try { UnityEngine.Debug.Log("[AntiCheatBypass] " + msg); } catch { }
        }

        public static class BypassPatches
        {
            [HarmonyPriority(Priority.First)]
            public static bool DirectoryExists_Prefix(string path, ref bool __result)
            {
                if (IsBepInExPath(path)) { __result = false; return false; }
                return true;
            }

            [HarmonyPriority(Priority.First)]
            public static bool FileExists_Prefix(string path, ref bool __result)
            {
                if (IsBepInExPath(path)) { __result = false; return false; }
                return true;
            }

            public static void GetDirectories_Postfix(ref string[] __result)
            {
                if (__result == null || __result.Length == 0) return;
                __result = __result.Where(d => !IsBepInExPath(d)).ToArray();
            }

            public static void GetFiles_Postfix(ref string[] __result)
            {
                if (__result == null || __result.Length == 0) return;
                __result = __result.Where(f => !IsBepInExPath(f)).ToArray();
            }

            public static void DirectoryInfoExists_Postfix(DirectoryInfo __instance, ref bool __result)
            {
                if (__result && __instance != null && IsBepInExPath(__instance.FullName))
                    __result = false;
            }

            public static void GetAssemblies_Postfix(ref Assembly[] __result)
            {
                if (__result == null || __result.Length == 0) return;
                __result = __result.Where(a => !IsSensitiveAssembly(a)).ToArray();
            }

            public static bool Quit_Prefix()
            {
                return true;
            }
        }

        public static class GenericSkipPatch
        {
            public static bool SkipVoid_Prefix(MethodBase __originalMethod)
            {
                Log("已跳过 void: " + __originalMethod.DeclaringType?.FullName + "." + __originalMethod.Name);
                return false;
            }

            public static bool SkipBool_Prefix(MethodBase __originalMethod, ref bool __result)
            {
                __result = false;
                Log("已跳过 bool: " + __originalMethod.DeclaringType?.FullName + "." + __originalMethod.Name);
                return false;
            }

            public static bool SkipInt_Prefix(MethodBase __originalMethod, ref int __result)
            {
                __result = 0;
                Log("已跳过 int: " + __originalMethod.DeclaringType?.FullName + "." + __originalMethod.Name);
                return false;
            }
        }
    }
}
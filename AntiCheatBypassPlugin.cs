using BepInEx;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace UniversalHack
{
    public class AntiCheatBypassPlugin : MonoBehaviour
    {
        private Harmony harmony;
        private static string gameRoot;
        private static string bepInExPath;

        void Awake()
        {
            harmony = new Harmony("com.universal.anticheat.bypass");

            gameRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            bepInExPath = Path.GetFullPath(Path.Combine(gameRoot, "BepInEx"));

            PatchDirectoryExists();
            PatchFileExists();
            PatchDirectoryGetDirectories();
            PatchDirectoryGetFiles();
            PatchDirectoryInfoExists();

            Debug.Log("[AntiCheatBypass] 加载（过去式）。");
        }

        void OnDestroy()
        {
            if (harmony != null)
            {
                try { harmony.UnpatchAll("com.universal.anticheat.bypass"); } catch { }
            }
        }

        private void PatchDirectoryExists()
        {
            try
            {
                var method = typeof(Directory).GetMethod(
                    nameof(Directory.Exists),
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(string) },
                    null);

                if (method != null)
                {
                    var prefix = typeof(BypassPatches).GetMethod(
                        nameof(BypassPatches.DirectoryExists_Prefix),
                        BindingFlags.Static | BindingFlags.Public);

                    harmony.Patch(method, new HarmonyMethod(prefix));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AntiCheatBypass] Patch Directory.Exists 失败: " + e.Message);
            }
        }

        private void PatchFileExists()
        {
            try
            {
                var method = typeof(File).GetMethod(
                    nameof(File.Exists),
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(string) },
                    null);

                if (method != null)
                {
                    var prefix = typeof(BypassPatches).GetMethod(
                        nameof(BypassPatches.FileExists_Prefix),
                        BindingFlags.Static | BindingFlags.Public);

                    harmony.Patch(method, new HarmonyMethod(prefix));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AntiCheatBypass] Patch File.Exists 失败: " + e.Message);
            }
        }

        private void PatchDirectoryGetDirectories()
        {
            try
            {
                var method = typeof(Directory).GetMethod(
                    nameof(Directory.GetDirectories),
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(string) },
                    null);

                if (method != null)
                {
                    var postfix = typeof(BypassPatches).GetMethod(
                        nameof(BypassPatches.GetDirectories_Postfix),
                        BindingFlags.Static | BindingFlags.Public);

                    harmony.Patch(method, null, new HarmonyMethod(postfix));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AntiCheatBypass] Patch Directory.GetDirectories 失败: " + e.Message);
            }
        }

        private void PatchDirectoryGetFiles()
        {
            try
            {
                var method = typeof(Directory).GetMethod(
                    nameof(Directory.GetFiles),
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(string) },
                    null);

                if (method != null)
                {
                    var postfix = typeof(BypassPatches).GetMethod(
                        nameof(BypassPatches.GetFiles_Postfix),
                        BindingFlags.Static | BindingFlags.Public);

                    harmony.Patch(method, null, new HarmonyMethod(postfix));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AntiCheatBypass] Patch Directory.GetFiles 失败: " + e.Message);
            }
        }

        private void PatchDirectoryInfoExists()
        {
            try
            {
                var prop = typeof(DirectoryInfo).GetProperty(
                    nameof(DirectoryInfo.Exists),
                    BindingFlags.Public | BindingFlags.Instance);

                if (prop != null && prop.GetGetMethod() != null)
                {
                    var postfix = typeof(BypassPatches).GetMethod(
                        nameof(BypassPatches.DirectoryInfoExists_Postfix),
                        BindingFlags.Static | BindingFlags.Public);

                    harmony.Patch(prop.GetGetMethod(), null, new HarmonyMethod(postfix));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AntiCheatBypass] Patch DirectoryInfo.Exists 失败: " + e.Message);
            }
        }

        internal static bool IsBepInExPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;

            try
            {
                string full;
                try
                {
                    full = Path.GetFullPath(path);
                }
                catch
                {
                    full = path;
                }

                full = full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                if (string.Equals(full, bepInExPath, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (full.StartsWith(bepInExPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            catch { }

            return false;
        }

        public static class BypassPatches
        {
            [HarmonyPriority(Priority.First)]
            public static bool DirectoryExists_Prefix(string path, ref bool __result)
            {
                if (IsBepInExPath(path))
                {
                    __result = false;
                    return false;
                }
                return true;
            }

            [HarmonyPriority(Priority.First)]
            public static bool FileExists_Prefix(string path, ref bool __result)
            {
                if (IsBepInExPath(path))
                {
                    __result = false;
                    return false;
                }
                return true;
            }

            public static void GetDirectories_Postfix(ref string[] __result)
            {
                if (__result == null || __result.Length == 0) return;

                var filtered = new System.Collections.Generic.List<string>();
                foreach (string dir in __result)
                {
                    if (!IsBepInExPath(dir))
                        filtered.Add(dir);
                }
                __result = filtered.ToArray();
            }

            public static void GetFiles_Postfix(ref string[] __result)
            {
                if (__result == null || __result.Length == 0) return;

                var filtered = new System.Collections.Generic.List<string>();
                foreach (string file in __result)
                {
                    if (!IsBepInExPath(file))
                        filtered.Add(file);
                }
                __result = filtered.ToArray();
            }

            public static void DirectoryInfoExists_Postfix(DirectoryInfo __instance, ref bool __result)
            {
                if (__result && __instance != null && IsBepInExPath(__instance.FullName))
                {
                    __result = false;
                }
            }
        }
    }
}
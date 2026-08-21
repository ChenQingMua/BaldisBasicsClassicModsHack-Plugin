using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UniversalHack
{
    public class PatchManager : MonoBehaviour
    {
        private static PatchManager _instance;
        public static PatchManager Instance => _instance;

        private Harmony harmony;
        private readonly List<PendingPatch> pending = new List<PendingPatch>();
        private readonly HashSet<string> patchedIds = new HashSet<string>();

        private class PendingPatch
        {
            public string Id;
            public string TypeName;
            public string MethodName;
            public MethodInfo Prefix;
            public MethodInfo Postfix;
            public BindingFlags Flags;
        }

        void Awake()
        {
            _instance = this;
            harmony = new Harmony("com.universal.patchmanager");
        }

        public static void Register(string id, string typeName, string methodName,
            MethodInfo prefix = null, MethodInfo postfix = null,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        {
            if (_instance == null || _instance.patchedIds.Contains(id)) return;

            _instance.pending.Add(new PendingPatch
            {
                Id = id,
                TypeName = typeName,
                MethodName = methodName,
                Prefix = prefix,
                Postfix = postfix,
                Flags = flags
            });
        }

        void Update()
        {
            if (pending.Count == 0) return;

            for (int i = pending.Count - 1; i >= 0; i--)
            {
                var p = pending[i];
                var method = FindMethod(p.TypeName, p.MethodName, p.Flags);

                if (method != null)
                {
                    try
                    {
                        harmony.Patch(
                            method,
                            p.Prefix != null ? new HarmonyMethod(p.Prefix) : null,
                            p.Postfix != null ? new HarmonyMethod(p.Postfix) : null
                        );
                        patchedIds.Add(p.Id);

                    }
                    catch (Exception ex)
                    {

                    }
                    pending.RemoveAt(i);
                }
            }
        }

        private static MethodBase FindMethod(string typeName, string methodName, BindingFlags flags)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.Name != typeName) continue;
                        var method = type.GetMethod(methodName, flags);
                        if (method != null) return method;
                    }
                }
                catch { }
            }
            return null;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniversalHack
{
    public class PlayerManager : MonoBehaviour
    {
        private Harmony harmony;
        private static bool staminaPatched;
        private static bool itemPatched;

        private object playerInstance;
        private Type playerType;
        private bool playerFound;
        private Transform playerTransform;

        private object gcInstance;
        private Type gcType;
        private bool gcFound;

        private const string FeatureInvincible = "无敌";
        private const string FeatureInfiniteItem = "无限道具";
        private const string FeatureStamina = "无限体力";
        private const string FeatureLeftClickDestroy = "左键破坏";
        private const string FeatureItemEditor = "物品编辑器";

        private float destroyRange = 32f;
        private float destroyRadius = 1.5f;
        private float lastDestroyTime;
        private float destroyInterval = 0.05f;

        private static Type inputType;
        private static MethodInfo getMouseButtonMethod;
        private static MethodInfo getKeyDownMethod;

        private Texture2D whiteTex;
        private Texture2D redFillTex;
        private Texture2D greenFillTex;

        private class DestroyedVisual
        {
            public Bounds bounds;
            public float time;
        }
        private List<DestroyedVisual> destroyedVisuals = new List<DestroyedVisual>();
        private List<Bounds> previewVisuals = new List<Bounds>();
        private const float VISUAL_LIFETIME = 1.5f;

        private bool showItemDialog = false;
        private string itemInput = "";

        static PlayerManager()
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
                                getMouseButtonMethod = inputType.GetMethod("GetMouseButton", new Type[] { typeof(int) });
                                getKeyDownMethod = inputType.GetMethod("GetKeyDown", new Type[] { typeof(KeyCode) });
                                if (getMouseButtonMethod != null && getKeyDownMethod != null) break;
                            }
                        }
                    }
                    catch { }
                }

                if (getMouseButtonMethod == null || getKeyDownMethod == null)
                {
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            inputType = asm.GetType("UnityEngine.Input");
                            if (inputType != null)
                            {
                                if (getMouseButtonMethod == null) getMouseButtonMethod = inputType.GetMethod("GetMouseButton", new Type[] { typeof(int) });
                                if (getKeyDownMethod == null) getKeyDownMethod = inputType.GetMethod("GetKeyDown", new Type[] { typeof(KeyCode) });
                                if (getMouseButtonMethod != null && getKeyDownMethod != null) break;
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
            harmony = new Harmony("com.universalhack.player");
            SceneManager.sceneLoaded += OnSceneLoaded;

            whiteTex = MakeTex(Color.white);
            redFillTex = MakeTex(new Color(1f, 0f, 0f, 0.1f));
            greenFillTex = MakeTex(new Color(0f, 1f, 0f, 0.1f));

            PatchInvincible();
            PatchStamina();
            TryPatchItem();
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (harmony != null)
            {
                try { harmony.UnpatchAll("com.universalhack.player"); } catch { }
            }

            if (whiteTex != null) Destroy(whiteTex);
            if (redFillTex != null) Destroy(redFillTex);
            if (greenFillTex != null) Destroy(greenFillTex);
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            playerInstance = null;
            playerType = null;
            playerFound = false;
            playerTransform = null;
            gcInstance = null;
            gcType = null;
            gcFound = false;
            destroyedVisuals.Clear();
            previewVisuals.Clear();
            showItemDialog = false;
            TryPatchItem();
        }

        void Update()
        {
            if (!gcFound || gcInstance == null) FindGameController();
            if (!playerFound || playerInstance == null) FindPlayer();

            if (HackMenu.ActiveFeatures.Contains(FeatureItemEditor))
            {
                if (IsKeyDown(KeyCode.I)) ToggleItemDialog();
            }
            else if (showItemDialog)
            {
                ToggleItemDialog();
            }

            UpdatePreview();
            UpdateLeftClickDestroy();
            UpdateVisuals();
        }

        void ToggleItemDialog()
        {
            showItemDialog = !showItemDialog;
            itemInput = "";

            if (showItemDialog)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                GUI.FocusControl(null);
            }
        }

        bool IsKeyDown(KeyCode key)
        {
            if (getKeyDownMethod == null) return false;
            try { return (bool)getKeyDownMethod.Invoke(null, new object[] { key }); }
            catch { return false; }
        }

        void FindGameController()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.Name != "GameControllerScript") continue;
                        var obj = UnityEngine.Object.FindObjectOfType(type);
                        if (obj != null)
                        {
                            gcInstance = obj;
                            gcType = type;
                            gcFound = true;
                            return;
                        }
                    }
                }
                catch { }
            }
        }

        void UpdatePreview()
        {
            previewVisuals.Clear();
            if (!HackMenu.ActiveFeatures.Contains(FeatureLeftClickDestroy)) return;
            if (IsMouseButton(0)) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 centerPos = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
            Ray ray = cam.ScreenPointToRay(centerPos);
            RaycastHit[] hits = Physics.SphereCastAll(ray, destroyRadius, destroyRange);

            if (hits == null || hits.Length == 0) return;

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                GameObject target = hit.collider.gameObject;
                if (target == null) continue;

                if (playerTransform != null && target.transform.IsChildOf(playerTransform)) continue;

                string name = target.name.ToLower();
                if (name.Contains("floor") || name.Contains("ceiling")) continue;

                var mr = target.GetComponentInChildren<MeshRenderer>();
                if (mr != null)
                {
                    Transform immediateParent = target.transform.parent;
                    GameObject toDestroy = target;
                    if (immediateParent != null && immediateParent.GetComponent<Collider>() != null && immediateParent.GetComponentInChildren<MeshRenderer>() != null)
                    {
                        toDestroy = immediateParent.gameObject;
                    }

                    var renderers = toDestroy.GetComponentsInChildren<Renderer>();
                    if (renderers.Length > 0)
                    {
                        Bounds combined = renderers[0].bounds;
                        for (int i = 1; i < renderers.Length; i++)
                            combined.Encapsulate(renderers[i].bounds);
                        previewVisuals.Add(combined);
                    }
                    break;
                }
            }
        }

        void UpdateLeftClickDestroy()
        {
            if (!HackMenu.ActiveFeatures.Contains(FeatureLeftClickDestroy)) return;
            if (!IsMouseButton(0)) return;
            if (Time.unscaledTime - lastDestroyTime < destroyInterval) return;
            lastDestroyTime = Time.unscaledTime;

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 centerPos = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
            Ray ray = cam.ScreenPointToRay(centerPos);
            RaycastHit[] hits = Physics.SphereCastAll(ray, destroyRadius, destroyRange);

            if (hits == null || hits.Length == 0) return;

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                GameObject target = hit.collider.gameObject;
                if (target == null) continue;
                if (playerTransform != null && target.transform.IsChildOf(playerTransform)) continue;

                string name = target.name.ToLower();
                if (name.Contains("floor") || name.Contains("ceiling")) continue;

                var mr = target.GetComponentInChildren<MeshRenderer>();
                if (mr != null)
                {
                    Transform immediateParent = target.transform.parent;
                    GameObject toDestroy = target;
                    if (immediateParent != null && immediateParent.GetComponent<Collider>() != null && immediateParent.GetComponentInChildren<MeshRenderer>() != null)
                    {
                        toDestroy = immediateParent.gameObject;
                    }

                    SaveDestroyedVisual(toDestroy);
                    Destroy(toDestroy);
                    break;
                }
            }
        }

        void SaveDestroyedVisual(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds combined = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combined.Encapsulate(renderers[i].bounds);
            }
            destroyedVisuals.Add(new DestroyedVisual { bounds = combined, time = Time.unscaledTime });
        }

        void UpdateVisuals()
        {
            for (int i = destroyedVisuals.Count - 1; i >= 0; i--)
            {
                if (Time.unscaledTime - destroyedVisuals[i].time > VISUAL_LIFETIME)
                {
                    destroyedVisuals.RemoveAt(i);
                }
            }
        }

        void OnGUI()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            if (HackMenu.ActiveFeatures.Contains(FeatureLeftClickDestroy))
            {
                float cx = Screen.width / 2f;
                float cy = Screen.height / 2f;
                GUI.color = new Color(0f, 1f, 0f, 0.8f);
                GUI.DrawTexture(new Rect(cx - 1, cy - 10, 2, 20), whiteTex);
                GUI.DrawTexture(new Rect(cx - 10, cy - 1, 20, 2), whiteTex);

                foreach (var bounds in previewVisuals)
                    DrawBounds(bounds, cam, true);

                foreach (var vis in destroyedVisuals)
                    DrawBounds(vis.bounds, cam, false);

                GUI.color = Color.white;
            }

            if (showItemDialog)
            {
                DrawItemDialog();
            }
        }

        void DrawBounds(Bounds b, Camera cam, bool isPreview)
        {
            Vector3 c = b.center;
            Vector3 e = b.extents;

            Vector3[] corners = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                corners[i] = c + new Vector3(
                    (i & 1) == 0 ? -e.x : e.x,
                    (i & 2) == 0 ? -e.y : e.y,
                    (i & 4) == 0 ? -e.z : e.z
                );
            }

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            bool visible = false;

            foreach (Vector3 corner in corners)
            {
                Vector3 sp = cam.WorldToScreenPoint(corner);
                if (sp.z > 0f) visible = true;
                float sx = sp.x;
                float sy = Screen.height - sp.y;
                if (sx < minX) minX = sx;
                if (sy < minY) minY = sy;
                if (sx > maxX) maxX = sx;
                if (sy > maxY) maxY = sy;
            }

            if (!visible) return;

            float w = maxX - minX;
            float h = maxY - minY;
            if (w < 1f || h < 1f) return;

            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(minX, minY, w, h), isPreview ? redFillTex : greenFillTex);

            GUI.color = isPreview ? Color.red : Color.green;
            float thickness = 2f;
            GUI.DrawTexture(new Rect(minX, minY, w, thickness), whiteTex);
            GUI.DrawTexture(new Rect(minX, minY + h - thickness, w, thickness), whiteTex);
            GUI.DrawTexture(new Rect(minX, minY, thickness, h), whiteTex);
            GUI.DrawTexture(new Rect(minX + w - thickness, minY, thickness, h), whiteTex);
        }

        bool IsMouseButton(int button)
        {
            if (getMouseButtonMethod == null) return false;
            try { return (bool)getMouseButtonMethod.Invoke(null, new object[] { button }); }
            catch { return false; }
        }

        void DrawItemDialog()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            float scale = 0.75f * (0.5f + 50f / 100f);

            GUI.color = new Color(0f, 0f, 0f, 0.54f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTex);

            float dialogWidth = 380f * scale;
            float dialogHeight = 170f * scale;
            float dialogX = (Screen.width - dialogWidth) / 2f;
            float dialogY = (Screen.height - dialogHeight) / 2f;

            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(dialogX, dialogY, dialogWidth, dialogHeight), whiteTex);

            GUI.color = new Color(0.75f, 0.75f, 0.75f, 1f);
            GUI.DrawTexture(new Rect(dialogX, dialogY, dialogWidth, 1f), whiteTex);
            GUI.DrawTexture(new Rect(dialogX, dialogY + dialogHeight - 1f, dialogWidth, 1f), whiteTex);
            GUI.DrawTexture(new Rect(dialogX, dialogY, 1f, dialogHeight), whiteTex);
            GUI.DrawTexture(new Rect(dialogX + dialogWidth - 1f, dialogY, 1f, dialogHeight), whiteTex);
            GUI.color = Color.white;

            GUI.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            GUI.DrawTexture(new Rect(dialogX, dialogY, dialogWidth, 36f * scale), whiteTex);
            GUI.color = Color.white;

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = (int)(20f * scale);
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = Color.white;
            titleStyle.alignment = TextAnchor.MiddleLeft;
            GUI.Label(new Rect(dialogX + 12f * scale, dialogY, dialogWidth - 60f * scale, 36f * scale), "Item Editor", titleStyle);

            float contentY = dialogY + 55f * scale;

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.normal.textColor = Color.black;
            labelStyle.fontSize = (int)(16f * scale);
            labelStyle.alignment = TextAnchor.MiddleLeft;
            GUI.Label(new Rect(dialogX + 20f * scale, contentY, 120f * scale, 32f * scale), "Item ID:", labelStyle);

            float inputWidth = 220f * scale;
            float inputHeight = 32f * scale;
            float inputX = dialogX + 110f * scale;
            float inputY = contentY;

            GUI.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            GUI.DrawTexture(new Rect(inputX, inputY, inputWidth + 2f * scale, inputHeight + 2f * scale), whiteTex);
            GUI.color = new Color(0.95f, 0.95f, 0.95f, 1f);
            GUI.DrawTexture(new Rect(inputX + 1f * scale, inputY + 1f * scale, inputWidth, inputHeight - 2f * scale), whiteTex);
            GUI.color = Color.white;

            GUI.SetNextControlName("ItemInput");
            itemInput = GUI.TextField(new Rect(inputX + 4f * scale, inputY + 2f * scale, inputWidth - 8f * scale, inputHeight - 4f * scale), itemInput, 10);

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
            btnStyle.fontSize = (int)(16f * scale);
            btnStyle.alignment = TextAnchor.MiddleCenter;
            btnStyle.normal.textColor = Color.white;
            btnStyle.hover.textColor = Color.white;
            btnStyle.active.textColor = Color.white;

            float btnWidth = 120f * scale;
            float btnHeight = 30f * scale;
            float btnY = dialogY + dialogHeight - btnHeight - 16f * scale;

            Rect confirmRect = new Rect(dialogX + dialogWidth - btnWidth * 2 - 12f * scale, btnY, btnWidth, btnHeight);
            GUI.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            GUI.DrawTexture(confirmRect, whiteTex);
            GUI.color = Color.white;
            if (GUI.Button(confirmRect, "OK", btnStyle))
            {
                TryGiveItem();
                GUI.FocusControl(null);
            }

            Rect cancelRect = new Rect(dialogX + dialogWidth - btnWidth - 6f * scale, btnY, btnWidth, btnHeight);
            GUI.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            GUI.DrawTexture(cancelRect, whiteTex);
            GUI.color = Color.white;
            if (GUI.Button(cancelRect, "Cancel", btnStyle))
            {
                ToggleItemDialog();
                GUI.FocusControl(null);
            }

            if (Event.current.type == EventType.MouseDown && !new Rect(dialogX, dialogY, dialogWidth, dialogHeight).Contains(Event.current.mousePosition))
            {
                ToggleItemDialog();
                GUI.FocusControl(null);
                Event.current.Use();
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                TryGiveItem();
                GUI.FocusControl(null);
                Event.current.Use();
            }

            if (Event.current.type == EventType.Repaint && GUI.GetNameOfFocusedControl() != "ItemInput")
            {
                GUI.FocusControl("ItemInput");
            }
        }

        void TryGiveItem()
        {
            int id;
            if (!int.TryParse(itemInput, out id)) return;
            if (!gcFound || gcInstance == null || gcType == null) return;

            var collectMethod = gcType.GetMethod("CollectItem", BindingFlags.Instance | BindingFlags.Public);
            if (collectMethod == null) return;

            try
            {
                collectMethod.Invoke(gcInstance, new object[] { id });
                ToggleItemDialog();
            }
            catch { }
        }

        Texture2D MakeTex(Color c)
        {
            Texture2D t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        void PatchInvincible()
        {
            var enterMethod = typeof(PlayerManager).GetMethod("OnTriggerEnter_Prefix", BindingFlags.Static | BindingFlags.Public);
            var stayMethod = typeof(PlayerManager).GetMethod("OnTriggerStay_Prefix", BindingFlags.Static | BindingFlags.Public);
            PatchMethod("PlayerScript", "OnTriggerEnter", enterMethod);
            PatchMethod("PlayerScript", "OnTriggerStay", stayMethod);
        }

        void PatchStamina()
        {
            if (staminaPatched) return;

            var prefix = typeof(PlayerManager).GetMethod("StaminaCheck_Prefix", BindingFlags.Static | BindingFlags.Public);
            if (prefix == null) return;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.Name != "PlayerScript") continue;
                        var method = type.GetMethod("StaminaCheck", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (method != null)
                        {
                            harmony.Patch(method, new HarmonyMethod(prefix));
                            staminaPatched = true;
                            return;
                        }
                    }
                }
                catch { }
            }
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
                        var method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
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

        void TryPatchItem()
        {
            if (itemPatched) return;
            var method = FindResetItemMethod();
            if (method == null) return;
            var prefix = typeof(PlayerManager).GetMethod("ResetItem_Prefix", BindingFlags.Static | BindingFlags.Public);
            if (prefix == null) return;
            harmony.Patch(method, new HarmonyMethod(prefix));
            itemPatched = true;
        }

        static MethodBase FindResetItemMethod()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.Name != "GameControllerScript") continue;
                        var method = type.GetMethod("ResetItem", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (method != null) return method;
                    }
                }
                catch { }
            }
            return null;
        }

        void FindPlayer()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in asm.GetTypes())
                    {
                        if (type.Name != "PlayerScript") continue;
                        UnityEngine.Object obj = UnityEngine.Object.FindObjectOfType(type);
                        if (obj == null) continue;
                        playerInstance = obj;
                        playerType = type;
                        var mono = obj as MonoBehaviour;
                        if (mono != null) playerTransform = mono.transform;
                        playerFound = true;
                        return;
                    }
                }
                catch { }
            }
        }

        public static bool OnTriggerEnter_Prefix() => !HackMenu.ActiveFeatures.Contains(FeatureInvincible);
        public static bool OnTriggerStay_Prefix() => !HackMenu.ActiveFeatures.Contains(FeatureInvincible);
        public static bool StaminaCheck_Prefix() => !HackMenu.ActiveFeatures.Contains(FeatureStamina);
        public static bool ResetItem_Prefix() => !HackMenu.ActiveFeatures.Contains(FeatureInfiniteItem);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniversalHack
{
    public class ExtensionManager : MonoBehaviour
    {
        private const string FeatureSceneJump = "场景跳转";
        private const string FeatureValueHack = "数值修改器";

        private static Type inputType;
        private static MethodInfo getKeyDownMethod;

        private Texture2D whiteTex;

        private bool showSceneDialog = false;
        private bool sceneDropdownOpen = false;
        private int selectedSceneIndex = -1;
        private Vector2 sceneScrollPos = Vector2.zero;
        private List<string> availableScenes = new List<string>();

        private bool showValueDialog = false;

        private string searchClass = "";
        private string searchMember = "";
        private bool classDropdownOpen = false;
        private bool memberDropdownOpen = false;

        private List<Type> allTypes = new List<Type>();
        private List<Type> filteredTypes = new List<Type>();
        private Type selectedType = null;
        private Vector2 classScrollPos = Vector2.zero;

        private List<MemberInfo> allMembers = new List<MemberInfo>();
        private List<MemberInfo> filteredMembers = new List<MemberInfo>();
        private MemberInfo selectedMember = null;
        private Vector2 memberScrollPos = Vector2.zero;

        private string inputValue = "";
        private string statusMessage = "";
        private float resultTime = 0f;

        private const float DIALOG_WIDTH = 460f;
        private const float DROPDOWN_HEIGHT = 180f;

        static ExtensionManager()
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
            whiteTex = new Texture2D(1, 1);
            whiteTex.SetPixel(0, 0, Color.white);
            whiteTex.Apply();
        }

        void OnDestroy()
        {
            if (whiteTex != null) Destroy(whiteTex);
        }

        void Update()
        {
            if (HackMenu.ActiveFeatures.Contains(FeatureSceneJump))
            {
                if (IsKeyDown(KeyCode.J)) ToggleDialog(ref showSceneDialog, showSceneDialog ? null : (Action)RefreshSceneList);
            }
            else if (showSceneDialog) ToggleDialog(ref showSceneDialog, null);

            if (HackMenu.ActiveFeatures.Contains(FeatureValueHack))
            {
                if (IsKeyDown(KeyCode.K)) ToggleDialog(ref showValueDialog, showValueDialog ? null : (Action)(() => RefreshTypes(0)));
            }
            else if (showValueDialog) ToggleDialog(ref showValueDialog, null);
        }

        void ToggleDialog(ref bool state, Action onOpen)
        {
            state = !state;
            if (state)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                onOpen?.Invoke();
            }
            else
            {
                if (!showSceneDialog && !showValueDialog)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }

        bool IsKeyDown(KeyCode key)
        {
            if (getKeyDownMethod == null) return false;
            try { return (bool)getKeyDownMethod.Invoke(null, new object[] { key }); }
            catch { return false; }
        }

        #region 逻辑处理
        void RefreshSceneList()
        {
            availableScenes.Clear();
            int count = SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < count; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
                availableScenes.Add(sceneName);
            }
        }

        void RefreshTypes(int mode)
        {
            allTypes.Clear();
            filteredTypes.Clear();
            selectedType = null;
            allMembers.Clear();
            filteredMembers.Clear();
            selectedMember = null;
            searchClass = "";
            searchMember = "";
            statusMessage = "";
            inputValue = "";

            var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
            if (asm != null)
            {
                allTypes = asm.GetTypes().OrderBy(t => t.Name).ToList();
                filteredTypes = new List<Type>(allTypes);
            }
        }

        void OnTypeSelected(Type t)
        {
            selectedType = t;
            classDropdownOpen = false;
            searchMember = "";
            statusMessage = "";

            allMembers.Clear();
            filteredMembers.Clear();
            selectedMember = null;

            if (t != null)
            {
                var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
                foreach (var f in t.GetFields(flags)) allMembers.Add(f);
                foreach (var p in t.GetProperties(flags)) allMembers.Add(p);

                allMembers = allMembers.OrderBy(m => m.Name).ToList();
                filteredMembers = new List<MemberInfo>(allMembers);
            }
        }

        void OnMemberSelected(MemberInfo m)
        {
            selectedMember = m;
            memberDropdownOpen = false;

            if (m != null)
            {
                inputValue = "";
                Type t = m is FieldInfo fi ? fi.FieldType : (m as PropertyInfo).PropertyType;
                statusMessage = "类型: " + t.Name;
            }
        }

        void ExecuteAction()
        {
            if (selectedType == null || selectedMember == null)
            {
                statusMessage = "未选择类或成员";
                resultTime = Time.time;
                return;
            }

            int count = 0;
            UnityEngine.Object[] instances = null;
            try { instances = FindObjectsOfType(selectedType); } catch { }

            Type memberType = selectedMember is FieldInfo fi ? fi.FieldType : (selectedMember as PropertyInfo).PropertyType;
            object convertedValue = null;

            try
            {
                if (memberType == typeof(bool))
                    convertedValue = (inputValue.ToLower() == "true" || inputValue == "1");
                else if (memberType.IsEnum)
                    convertedValue = Enum.Parse(memberType, inputValue, true);
                else
                    convertedValue = Convert.ChangeType(inputValue, memberType);
            }
            catch
            {
                statusMessage = "输入的值无法转换为 " + memberType.Name;
                resultTime = Time.time;
                return;
            }

            if (selectedMember is FieldInfo field && field.IsStatic)
            {
                field.SetValue(null, convertedValue);
                count++;
            }
            else if (selectedMember is PropertyInfo prop && prop.GetSetMethod() != null && prop.GetSetMethod().IsStatic)
            {
                prop.SetValue(null, convertedValue, null);
                count++;
            }
            else if (instances != null && instances.Length > 0)
            {
                foreach (var inst in instances)
                {
                    if (selectedMember is FieldInfo f) { f.SetValue(inst, convertedValue); count++; }
                    else if (selectedMember is PropertyInfo p && p.GetSetMethod() != null)
                    {
                        try { p.SetValue(inst, convertedValue, null); count++; } catch { }
                    }
                }
            }
            else
            {
                statusMessage = "实例成员需要场景中存在 " + selectedType.Name + " 的实例，但未找到";
                resultTime = Time.time;
                return;
            }

            if (count > 0) statusMessage = "成功执行了 " + count + " 次";
            else statusMessage = "未找到可执行的实例";
            resultTime = Time.time;
        }
        #endregion

        void OnGUI()
        {
            if (showSceneDialog) DrawSceneDialog();
            if (showValueDialog) DrawValueDialog();
        }

        #region UI 绘制
        void DrawSceneDialog()
        {
            float scale = 0.75f * (0.5f + 50f / 100f);
            GUI.color = new Color(0f, 0f, 0f, 0.54f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTex);

            float dialogWidth = 380f * scale;
            float dialogHeight = 170f * scale;
            float dialogX = (Screen.width - dialogWidth) / 2f;
            float dialogY = (Screen.height - dialogHeight) / 2f;

            DrawDialogBackground(dialogX, dialogY, dialogWidth, dialogHeight, scale, "Scene Jump");

            float contentY = dialogY + 55f * scale;
            GUI.Label(new Rect(dialogX + 20f * scale, contentY, 100f * scale, 32f * scale), "Select Scene:", GetLabelStyle(scale));

            float inputWidth = 220f * scale;
            float inputHeight = 32f * scale;
            float inputX = dialogX + 120f * scale;
            float inputY = contentY;

            bool isHoverList = sceneDropdownOpen && new Rect(inputX, inputY + inputHeight + 2f * scale, inputWidth, DROPDOWN_HEIGHT * scale).Contains(Event.current.mousePosition);
            GUI.enabled = !isHoverList;

            DrawDropdownBox(inputX, inputY, inputWidth, inputHeight, scale,
                (selectedSceneIndex >= 0) ? availableScenes[selectedSceneIndex] : "Select a scene...",
                ref sceneDropdownOpen);

            float btnWidth = 120f * scale;
            float btnHeight = 30f * scale;
            float btnY = dialogY + dialogHeight - btnHeight - 16f * scale;

            if (DrawButton(dialogX + dialogWidth - btnWidth * 2 - 12f * scale, btnY, btnWidth, btnHeight, scale, "Jump"))
            {
                if (selectedSceneIndex >= 0)
                {
                    SceneManager.LoadScene(availableScenes[selectedSceneIndex]);
                    ToggleDialog(ref showSceneDialog, null);
                }
            }

            if (DrawButton(dialogX + dialogWidth - btnWidth - 6f * scale, btnY, btnWidth, btnHeight, scale, "Cancel"))
                ToggleDialog(ref showSceneDialog, null);

            GUI.enabled = true;

            if (sceneDropdownOpen)
            {
                float listY = inputY + inputHeight + 2f * scale;
                float listHeight = DROPDOWN_HEIGHT * scale;
                Rect listRect = new Rect(inputX, listY, inputWidth, listHeight);

                DrawListBackground(listRect);
                GUI.BeginGroup(listRect);
                float itemHeight = 30f * scale;
                Rect viewRect = new Rect(0, 0, inputWidth - 20f * scale, Math.Max(availableScenes.Count * itemHeight, listHeight));
                sceneScrollPos = GUI.BeginScrollView(new Rect(0, 0, inputWidth, listHeight), sceneScrollPos, viewRect);

                for (int i = 0; i < availableScenes.Count; i++)
                {
                    if (DrawListItem(new Rect(0, i * itemHeight, inputWidth, itemHeight), scale, availableScenes[i]))
                    {
                        selectedSceneIndex = i;
                        sceneDropdownOpen = false;
                    }
                }
                GUI.EndScrollView();
                GUI.EndGroup();

                if (Event.current.type == EventType.MouseDown && !listRect.Contains(Event.current.mousePosition) && !new Rect(inputX, inputY, inputWidth, inputHeight).Contains(Event.current.mousePosition))
                    sceneDropdownOpen = false;
            }

            bool blockClose = sceneDropdownOpen;
            if (!blockClose && Event.current.type == EventType.MouseDown && !new Rect(dialogX, dialogY, dialogWidth, dialogHeight).Contains(Event.current.mousePosition))
                ToggleDialog(ref showSceneDialog, null);
        }

        void DrawValueDialog()
        {
            float scale = 0.75f * (0.5f + 50f / 100f);
            GUI.color = new Color(0f, 0f, 0f, 0.54f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTex);

            float dialogWidth = DIALOG_WIDTH * scale;
            float dialogHeight = (80f + (20f + 24f + 28f + 10f) * 2 + 60f) * scale;
            float dialogX = (Screen.width - dialogWidth) / 2f;
            float dialogY = (Screen.height - dialogHeight) / 2f;

            DrawDialogBackground(dialogX, dialogY, dialogWidth, dialogHeight, scale, "Value Editor");

            float startY = dialogY + 45f * scale;
            float contentWidth = dialogWidth - 40f * scale;
            float startX = dialogX + 20f * scale;
            float blockHeight = 82f * scale;

            bool isHoverList = classDropdownOpen || memberDropdownOpen;
            GUI.enabled = !isHoverList;

            GUI.Label(new Rect(startX, startY, contentWidth, 20f * scale), "1. Class (Assembly-CSharp):", GetLabelStyle(scale, 14));
            searchClass = GUI.TextField(new Rect(startX, startY + 22f * scale, contentWidth, 24f * scale), searchClass, GetTextFieldStyle(scale));
            if (string.IsNullOrEmpty(searchClass)) filteredTypes = new List<Type>(allTypes);
            else filteredTypes = allTypes.Where(t => t.Name.ToLower().Contains(searchClass.ToLower())).ToList();

            string classDisplay = selectedType != null ? selectedType.Name : "Select a class...";
            if (DrawDropdownBox(startX, startY + 50f * scale, contentWidth, 28f * scale, scale, classDisplay, ref classDropdownOpen))
                memberDropdownOpen = false;

            float memberY = startY + blockHeight;
            GUI.Label(new Rect(startX, memberY, contentWidth, 20f * scale), "2. Member:", GetLabelStyle(scale, 14));
            searchMember = GUI.TextField(new Rect(startX, memberY + 22f * scale, contentWidth, 24f * scale), searchMember, GetTextFieldStyle(scale));
            if (string.IsNullOrEmpty(searchMember)) filteredMembers = new List<MemberInfo>(allMembers);
            else filteredMembers = allMembers.Where(m => m.Name.ToLower().Contains(searchMember.ToLower())).ToList();

            string memberDisplay = selectedMember != null ? selectedMember.Name : "Select a member...";
            if (DrawDropdownBox(startX, memberY + 50f * scale, contentWidth, 28f * scale, scale, memberDisplay, ref memberDropdownOpen))
                classDropdownOpen = false;

            float valueY = memberY + blockHeight;
            GUI.Label(new Rect(startX, valueY, contentWidth, 20f * scale), "3. Value:", GetLabelStyle(scale, 14));
            inputValue = GUI.TextField(new Rect(startX, valueY + 22f * scale, contentWidth, 28f * scale), inputValue, GetTextFieldStyle(scale));

            if (!string.IsNullOrEmpty(statusMessage))
            {
                Color old = GUI.color;
                GUI.color = (Time.time - resultTime < 1.5f && statusMessage.Contains("成功")) ? Color.green : Color.red;
                GUI.Label(new Rect(startX, valueY + 56f * scale, contentWidth, 20f * scale), statusMessage, GetLabelStyle(scale, 12));
                GUI.color = old;
            }

            float btnWidth = 100f * scale;
            float btnHeight = 30f * scale;
            float btnY = dialogY + dialogHeight - btnHeight - 16f * scale;

            if (DrawButton(dialogX + dialogWidth - btnWidth * 2 - 12f * scale, btnY, btnWidth, btnHeight, scale, "Apply"))
                ExecuteAction();

            if (DrawButton(dialogX + dialogWidth - btnWidth - 6f * scale, btnY, btnWidth, btnHeight, scale, "Close"))
                ToggleDialog(ref showValueDialog, null);

            GUI.enabled = true;

            if (classDropdownOpen)
            {
                Rect listRect = new Rect(startX, startY + 80f * scale, contentWidth, DROPDOWN_HEIGHT * scale);
                DrawListBackground(listRect);
                GUI.BeginGroup(listRect);
                float itemHeight = 26f * scale;
                Rect viewRect = new Rect(0, 0, contentWidth - 20f * scale, Math.Max(filteredTypes.Count * itemHeight, listRect.height));
                classScrollPos = GUI.BeginScrollView(new Rect(0, 0, contentWidth, listRect.height), classScrollPos, viewRect);
                for (int i = 0; i < filteredTypes.Count; i++)
                {
                    if (DrawListItem(new Rect(0, i * itemHeight, contentWidth, itemHeight), scale, filteredTypes[i].Name))
                        OnTypeSelected(filteredTypes[i]);
                }
                GUI.EndScrollView();
                GUI.EndGroup();
                if (Event.current.type == EventType.MouseDown && !listRect.Contains(Event.current.mousePosition) && !new Rect(startX, startY + 50f * scale, contentWidth, 28f * scale).Contains(Event.current.mousePosition))
                    classDropdownOpen = false;
            }

            if (memberDropdownOpen)
            {
                Rect listRect = new Rect(startX, memberY + 80f * scale, contentWidth, DROPDOWN_HEIGHT * scale);
                DrawListBackground(listRect);
                GUI.BeginGroup(listRect);
                float itemHeight = 26f * scale;
                Rect viewRect = new Rect(0, 0, contentWidth - 20f * scale, Math.Max(filteredMembers.Count * itemHeight, listRect.height));
                memberScrollPos = GUI.BeginScrollView(new Rect(0, 0, contentWidth, listRect.height), memberScrollPos, viewRect);
                for (int i = 0; i < filteredMembers.Count; i++)
                {
                    if (DrawListItem(new Rect(0, i * itemHeight, contentWidth, itemHeight), scale, filteredMembers[i].Name))
                        OnMemberSelected(filteredMembers[i]);
                }
                GUI.EndScrollView();
                GUI.EndGroup();
                if (Event.current.type == EventType.MouseDown && !listRect.Contains(Event.current.mousePosition) && !new Rect(startX, memberY + 50f * scale, contentWidth, 28f * scale).Contains(Event.current.mousePosition))
                    memberDropdownOpen = false;
            }

            bool blockClose = classDropdownOpen || memberDropdownOpen;
            if (!blockClose && Event.current.type == EventType.MouseDown && !new Rect(dialogX, dialogY, dialogWidth, dialogHeight).Contains(Event.current.mousePosition))
                ToggleDialog(ref showValueDialog, null);
        }

        #region UI 辅助方法
        void DrawDialogBackground(float x, float y, float w, float h, float scale, string title)
        {
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(x, y, w, h), whiteTex);
            GUI.color = new Color(0.75f, 0.75f, 0.75f, 1f);
            GUI.DrawTexture(new Rect(x, y, w, 1f), whiteTex);
            GUI.DrawTexture(new Rect(x, y + h - 1f, w, 1f), whiteTex);
            GUI.DrawTexture(new Rect(x, y, 1f, h), whiteTex);
            GUI.DrawTexture(new Rect(x + w - 1f, y, 1f, h), whiteTex);
            GUI.color = Color.white;

            GUI.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            GUI.DrawTexture(new Rect(x, y, w, 36f * scale), whiteTex);
            GUI.color = Color.white;

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = (int)(20f * scale);
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = Color.white;
            titleStyle.alignment = TextAnchor.MiddleLeft;
            GUI.Label(new Rect(x + 12f * scale, y, w - 60f * scale, 36f * scale), title, titleStyle);
        }

        bool DrawDropdownBox(float x, float y, float w, float h, float scale, string text, ref bool isOpen)
        {
            GUI.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            GUI.DrawTexture(new Rect(x, y, w + 2f * scale, h + 2f * scale), whiteTex);
            GUI.color = new Color(0.95f, 0.95f, 0.95f, 1f);
            GUI.DrawTexture(new Rect(x + 1f * scale, y + 1f * scale, w, h - 2f * scale), whiteTex);
            GUI.color = Color.white;

            GUIStyle dropBtnStyle = new GUIStyle(GUI.skin.label);
            dropBtnStyle.fontSize = (int)(14f * scale);
            dropBtnStyle.normal.textColor = Color.black;
            dropBtnStyle.alignment = TextAnchor.MiddleLeft;
            GUI.Label(new Rect(x + 8f * scale, y, w - 30f * scale, h), text, dropBtnStyle);

            GUIStyle arrowStyle = new GUIStyle(GUI.skin.label);
            arrowStyle.fontSize = (int)(14f * scale);
            arrowStyle.normal.textColor = Color.black;
            arrowStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(x + w - 30f * scale, y, 30f * scale, h), "▼", arrowStyle);

            if (Event.current.type == EventType.MouseDown && new Rect(x, y, w, h).Contains(Event.current.mousePosition))
            {
                isOpen = !isOpen;
                Event.current.Use();
                return true;
            }
            return false;
        }

        bool DrawListItem(Rect rect, float scale, string text)
        {
            bool isHover = rect.Contains(Event.current.mousePosition);
            if (isHover)
            {
                GUI.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                GUI.DrawTexture(rect, whiteTex);
                GUI.color = Color.white;
            }

            GUIStyle itemStyle = new GUIStyle(GUI.skin.label);
            itemStyle.fontSize = (int)(13f * scale);
            itemStyle.alignment = TextAnchor.MiddleLeft;
            itemStyle.normal.textColor = Color.white;
            itemStyle.padding = new RectOffset((int)(10f * scale), 0, 0, 0);
            GUI.Label(rect, text, itemStyle);

            if (Event.current.type == EventType.MouseDown && isHover)
                return true;

            return false;
        }

        void DrawListBackground(Rect rect)
        {
            GUI.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            GUI.DrawTexture(rect, whiteTex);
            GUI.color = Color.white;
        }

        bool DrawButton(float x, float y, float w, float h, float scale, string text)
        {
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
            btnStyle.fontSize = (int)(14f * scale);
            btnStyle.alignment = TextAnchor.MiddleCenter;
            btnStyle.normal.textColor = Color.white;
            btnStyle.hover.textColor = Color.white;
            btnStyle.active.textColor = Color.white;

            GUI.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            GUI.DrawTexture(new Rect(x, y, w, h), whiteTex);
            GUI.color = Color.white;
            return GUI.Button(new Rect(x, y, w, h), text, btnStyle);
        }

        GUIStyle GetLabelStyle(float scale, int fontSize = 16)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Color.black;
            style.fontSize = (int)(fontSize * scale);
            style.alignment = TextAnchor.MiddleLeft;
            return style;
        }

        GUIStyle GetTextFieldStyle(float scale)
        {
            GUIStyle style = new GUIStyle(GUI.skin.textField);
            style.fontSize = (int)(12f * scale);
            style.normal.textColor = Color.black;
            return style;
        }
        #endregion
        #endregion
    }
}
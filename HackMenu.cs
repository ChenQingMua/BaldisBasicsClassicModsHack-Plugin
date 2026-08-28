using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UniversalHack
{
    public class HackMenu : MonoBehaviour
    {
        public static List<string> ActiveFeatures = new List<string>();
        public static readonly object Lock = new object();
        public static bool ShowMenu { get; private set; } = true;

        private readonly string[][] MENUS = new[]
        {
            new[] { "事件类", "无视校长互动" , "无视袜子互动", "禁用巴迪移动", "直接激活愤怒", "聋哑巴迪", "无巴迪", "无校长", "无袜子", "无欢乐时间", "无扫把", "无第一名", "无校霸" , "书黑客", "跳转全错场景", "直接胜利"},
            new[] { "移动类", "穿墙", "移速", "无视推动" },
            new[] { "玩家类", "无敌", "无限体力", "无限道具"},
            new[] { "视觉类", "绘制", "放大镜", "增大视野", "追踪器", "红温模式", "控件描边", "贴图旋转", "自转"},
            new[] { "主菜单", "功能列表", "水印", "隐藏菜单仅移除遮挡" }
        };

        private readonly string[][] MENUS_EN = new[]
        {
            new[] { "Events", "Ignore Principal" , "Ignore Crafters", "Disable Baldi Move", "Instant Rage", "Deaf Baldi", "No Baldi", "No Principal", "No Crafters", "No Playtime", "No Sweep", "No First Prize", "No Bully" , "Book Hacker", "Jump All Wrong", "Instant Win"},
            new[] { "Movement", "No Clip", "Speed", "Anti Push" },
            new[] { "Player", "God Mode", "Infinite Stamina", "Infinite Items"},
            new[] { "Visual", "ESP", "Magnifier", "FOV", "Tracker", "Red Light", "Outline", "Texture Rotate", "Auto Rotate"},
            new[] { "Main", "Feature List", "Watermark", "Hide Cover Only" }
        };

        private Dictionary<string, bool> featureStates = new Dictionary<string, bool>();
        private Dictionary<string, Rect> menuRects = new Dictionary<string, Rect>();
        private Dictionary<string, bool> menuExpanded = new Dictionary<string, bool>();
        private Dictionary<string, bool> isDragging = new Dictionary<string, bool>();
        private Dictionary<string, Vector2> dragOffset = new Dictionary<string, Vector2>();

        private float hue = 0f;
        private bool showMenu = true;
        private bool hideOverlayOnly = false;
        private bool englishMode = false;
        private GUIStyle titleStyle;
        private GUIStyle buttonStyle;
        private GUIStyle buttonActiveStyle;
        private GUIStyle listItemStyle;
        private GUIStyle controlStyle;
        private GUIStyle controlLabelStyle;

        private Texture2D whiteTex;
        private Texture2D bgTex;
        private Texture2D titleBgTex;
        private Texture2D blackTex;
        private Texture2D grayTex;
        private Texture2D checkTex;

        private const float SCALE = 0.75f;
        private const float MENU_WIDTH = 300f * SCALE;
        private const float TITLE_HEIGHT = 72f * SCALE;
        private const float BUTTON_HEIGHT = 64f * SCALE;
        private const float ALPHA_BG = 0.33f;
        private const float CHECKBOX_SIZE = 24f * SCALE;

        private float lastToggleTime = -1f;
        private const float TOGGLE_COOLDOWN = 0.15f;

        void Awake()
        {
            whiteTex = MakeTex(Color.white);
            bgTex = MakeTex(new Color(1f, 1f, 1f, ALPHA_BG));
            titleBgTex = MakeTex(Color.white);
            blackTex = MakeTex(new Color(0f, 0f, 0f, 0.54f));
            grayTex = MakeTex(new Color(0f, 0f, 0f, 0.6f));
            checkTex = MakeTex(new Color(0f, 0.8f, 0.2f, 1f));

            float startX = 40f * SCALE;
            float startY = 40f * SCALE;
            foreach (var menu in MENUS)
            {
                string title = menu[0];
                menuRects[title] = new Rect(startX, startY, MENU_WIDTH, TITLE_HEIGHT);
                menuExpanded[title] = true;
                isDragging[title] = false;
                startX += MENU_WIDTH + 20f * SCALE;
            }

            lock (Lock)
            {
                ActiveFeatures.Add("功能列表");
                ActiveFeatures.Add("水印");
            }

            featureStates["功能列表"] = true;
            featureStates["水印"] = true;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                TryToggle();
            }
        }

        void OnGUI()
        {
            Event e = Event.current;

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Tab)
            {
                if (TryToggle())
                {
                    e.Use();
                }
            }

            if (e.type == EventType.Repaint)
            {
                hue = (Time.realtimeSinceStartup * 90f) % 360f;

                InitStyles();

                if (showMenu)
                {
                    if (!hideOverlayOnly)
                    {
                        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), grayTex);
                        DrawBottomRightControls();
                    }
                    DrawMenusRepaint();
                }

                DrawWatermark();
                DrawFeatureList();
            }
            else if (showMenu && (e.type == EventType.MouseDown || e.type == EventType.MouseUp || e.type == EventType.MouseDrag))
            {
                HandleMenuInput(e);
                if (!hideOverlayOnly)
                    HandleBottomRightInput(e);
            }
        }

        bool TryToggle()
        {
            float now = Time.unscaledTime;
            if (now - lastToggleTime < TOGGLE_COOLDOWN)
                return false;

            lastToggleTime = now;
            ToggleMenu();
            return true;
        }

        void ToggleMenu()
        {
            bool hideOverlayOnlyEnabled = featureStates.ContainsKey("隐藏菜单仅移除遮挡")
                && featureStates["隐藏菜单仅移除遮挡"];

            if (showMenu)
            {
                if (hideOverlayOnlyEnabled)
                {
                    hideOverlayOnly = !hideOverlayOnly;
                }
                else
                {
                    showMenu = false;
                    ShowMenu = false;
                }
            }
            else
            {
                showMenu = true;
                ShowMenu = true;
                hideOverlayOnly = false;
            }

            if (showMenu)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        void InitStyles()
        {
            if (titleStyle != null) return;

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = (int)(34f * SCALE);
            titleStyle.fontStyle = (FontStyle)1;
            titleStyle.alignment = (TextAnchor)4;
            titleStyle.normal.textColor = new Color(0f, 0.204f, 1f, 1f);

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = (int)(30f * SCALE);
            buttonStyle.alignment = (TextAnchor)4;
            buttonStyle.normal.textColor = new Color(0f, 0.204f, 1f, 1f);
            buttonStyle.normal.background = null;
            buttonStyle.hover.textColor = new Color(0f, 0.204f, 1f, 1f);
            buttonStyle.hover.background = null;
            buttonStyle.active.textColor = new Color(0f, 0.204f, 1f, 1f);
            buttonStyle.active.background = null;
            buttonStyle.border = new RectOffset(0, 0, 0, 0);
            buttonStyle.padding = new RectOffset(0, 0, 0, 0);
            buttonStyle.margin = new RectOffset(0, 0, 0, 0);

            buttonActiveStyle = new GUIStyle(buttonStyle);
            buttonActiveStyle.normal.textColor = Color.white;
            buttonActiveStyle.hover.textColor = Color.white;
            buttonActiveStyle.active.textColor = Color.white;

            listItemStyle = new GUIStyle(GUI.skin.label);
            listItemStyle.fontSize = (int)(36f * SCALE);
            listItemStyle.alignment = (TextAnchor)5;

            controlStyle = new GUIStyle(GUI.skin.label);
            controlStyle.fontSize = (int)(22f * SCALE);
            controlStyle.alignment = (TextAnchor)4;
            controlStyle.normal.textColor = new Color(0.3f, 0.6f, 1f, 1f);

            controlLabelStyle = new GUIStyle(GUI.skin.label);
            controlLabelStyle.fontSize = (int)(22f * SCALE);
            controlLabelStyle.alignment = (TextAnchor)3;
            controlLabelStyle.normal.textColor = Color.white;
        }

        string GetDisplayText(string cnText)
        {
            if (!englishMode) return cnText;
            for (int m = 0; m < MENUS.Length; m++)
            {
                for (int i = 0; i < MENUS[m].Length; i++)
                {
                    if (MENUS[m][i] == cnText)
                        return MENUS_EN[m][i];
                }
            }
            return cnText;
        }

        void HandleMenuInput(Event e)
        {
            foreach (var menu in MENUS)
            {
                string title = menu[0];
                Rect rect = menuRects[title];
                bool expanded = menuExpanded[title];

                float totalHeight = TITLE_HEIGHT;
                if (expanded)
                    totalHeight += (menu.Length - 1) * BUTTON_HEIGHT;

                Rect titleRect = new Rect(rect.x, rect.y, MENU_WIDTH, TITLE_HEIGHT);

                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    if (titleRect.Contains(e.mousePosition))
                    {
                        isDragging[title] = true;
                        dragOffset[title] = new Vector2(e.mousePosition.x - rect.x, e.mousePosition.y - rect.y);
                        e.Use();
                    }
                }

                if (isDragging[title] && e.type == EventType.MouseDrag)
                {
                    rect.x = e.mousePosition.x - dragOffset[title].x;
                    rect.y = e.mousePosition.y - dragOffset[title].y;
                    menuRects[title] = rect;
                    e.Use();
                }

                if (e.type == EventType.MouseUp && e.button == 0)
                {
                    if (isDragging[title])
                    {
                        Vector2 dragDist = new Vector2(e.mousePosition.x - (rect.x + dragOffset[title].x), e.mousePosition.y - (rect.y + dragOffset[title].y));
                        if (dragDist.magnitude < 6f)
                        {
                            menuExpanded[title] = !expanded;
                        }
                        isDragging[title] = false;
                        e.Use();
                    }
                }

                if (expanded)
                {
                    for (int i = 1; i < menu.Length; i++)
                    {
                        string feature = menu[i];
                        bool isOn = featureStates.ContainsKey(feature) && featureStates[feature];
                        Rect btnRect = new Rect(rect.x, rect.y + TITLE_HEIGHT + (i - 1) * BUTTON_HEIGHT, MENU_WIDTH, BUTTON_HEIGHT);

                        if (e.type == EventType.MouseDown && e.button == 0 && btnRect.Contains(e.mousePosition))
                        {
                            featureStates[feature] = !isOn;
                            lock (Lock)
                            {
                                if (featureStates[feature])
                                {
                                    if (!ActiveFeatures.Contains(feature))
                                        ActiveFeatures.Add(feature);
                                }
                                else
                                {
                                    ActiveFeatures.Remove(feature);
                                }
                            }
                            e.Use();
                        }
                    }
                }
            }
        }

        void DrawMenusRepaint()
        {
            Color rainbow = GetRainbowColor(hue);

            foreach (var menu in MENUS)
            {
                string title = menu[0];
                Rect rect = menuRects[title];
                bool expanded = menuExpanded[title];

                float totalHeight = TITLE_HEIGHT;
                if (expanded)
                    totalHeight += (menu.Length - 1) * BUTTON_HEIGHT;

                Rect fullRect = new Rect(rect.x, rect.y, MENU_WIDTH, totalHeight);

                GUI.DrawTexture(fullRect, bgTex);

                Rect titleRect = new Rect(rect.x, rect.y, MENU_WIDTH, TITLE_HEIGHT);
                GUI.DrawTexture(titleRect, titleBgTex);
                GUI.Label(titleRect, GetDisplayText(title), titleStyle);

                if (expanded)
                {
                    for (int i = 1; i < menu.Length; i++)
                    {
                        string feature = menu[i];
                        bool isOn = featureStates.ContainsKey(feature) && featureStates[feature];
                        Rect btnRect = new Rect(rect.x, rect.y + TITLE_HEIGHT + (i - 1) * BUTTON_HEIGHT, MENU_WIDTH, BUTTON_HEIGHT);

                        if (isOn)
                        {
                            GUI.color = rainbow;
                            GUI.DrawTexture(btnRect, whiteTex);
                            GUI.color = Color.white;
                            GUI.Label(btnRect, GetDisplayText(feature), buttonActiveStyle);
                        }
                        else
                        {
                            GUI.color = Color.white;
                            GUI.Label(btnRect, GetDisplayText(feature), buttonStyle);
                        }
                    }
                }
            }

            GUI.color = Color.white;
        }

        void DrawBottomRightControls()
        {
            float pad = 20f * SCALE;
            float ctrlW = 200f * SCALE;
            float ctrlH = 40f * SCALE;
            float gap = 10f * SCALE;

            float x = Screen.width - pad - ctrlW;
            float yGitHub = Screen.height - pad - ctrlH - gap - ctrlH;
            float yCheck = Screen.height - pad - ctrlH;

            Rect gitHubRect = new Rect(x, yGitHub, ctrlW, ctrlH);
            GUI.DrawTexture(gitHubRect, bgTex);
            GUI.Label(gitHubRect, "GitHub", controlStyle);

            Rect checkRect = new Rect(x, yCheck, ctrlW, ctrlH);
            GUI.DrawTexture(checkRect, bgTex);

            float boxSize = CHECKBOX_SIZE;
            float cx = x + 12f * SCALE;
            float cy = yCheck + (ctrlH - boxSize) * 0.5f;
            Rect boxRect = new Rect(cx, cy, boxSize, boxSize);

            GUI.DrawTexture(boxRect, whiteTex);
            if (englishMode)
            {
                float inner = 4f * SCALE;
                GUI.DrawTexture(new Rect(boxRect.x + inner, boxRect.y + inner, boxSize - inner * 2f, boxSize - inner * 2f), checkTex);
            }

            float labelX = cx + boxSize + 10f * SCALE;
            float labelW = ctrlW - boxSize - 30f * SCALE;
            Rect labelRect = new Rect(labelX, yCheck, labelW, ctrlH);
            GUI.Label(labelRect, "English Mode", controlLabelStyle);
        }

        void HandleBottomRightInput(Event e)
        {
            if (e.type != EventType.MouseDown || e.button != 0) return;

            float pad = 20f * SCALE;
            float ctrlW = 200f * SCALE;
            float ctrlH = 40f * SCALE;
            float gap = 10f * SCALE;

            float x = Screen.width - pad - ctrlW;
            float yGitHub = Screen.height - pad - ctrlH - gap - ctrlH;
            float yCheck = Screen.height - pad - ctrlH;

            Rect gitHubRect = new Rect(x, yGitHub, ctrlW, ctrlH);
            if (gitHubRect.Contains(e.mousePosition))
            {
                Application.OpenURL("https://github.com/ChenQingMua/BaldisBasicsClassicModsHack-Plugin");
                e.Use();
                return;
            }

            Rect checkHitRect = new Rect(x, yCheck, ctrlW, ctrlH);
            if (checkHitRect.Contains(e.mousePosition))
            {
                englishMode = !englishMode;
                e.Use();
            }
        }

        void DrawWatermark()
        {
            bool show;
            lock (Lock)
            {
                show = ActiveFeatures.Contains("水印");
            }
            if (!show) return;

            float x = 20f * SCALE;
            float y = Screen.height - 20f * SCALE;

            string timeStr = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            string procStr = 
                "Baldis Basics Classic Mods Hack" +
                " v 1.3 " +
                "By JisGreen";
            string logoStr = "Press Tab To Open Or Close Menu";

            Color c1 = GetRainbowColor(hue);
            Color c2 = GetRainbowColor((hue + 40f) % 360f);
            Color c3 = GetRainbowColor((hue + 80f) % 360f);

            GUIStyle ws = new GUIStyle(GUI.skin.label);
            ws.fontSize = (int)(36f * SCALE);
            ws.alignment = (TextAnchor)3;
            ws.normal.background = blackTex;

            GUIStyle s1 = new GUIStyle(ws);
            s1.normal.textColor = c1;
            Vector2 sz1 = s1.CalcSize(new GUIContent(" " + timeStr + " "));

            GUIStyle s2 = new GUIStyle(ws);
            s2.normal.textColor = c2;
            Vector2 sz2 = s2.CalcSize(new GUIContent(" " + logoStr + " "));

            GUIStyle s3 = new GUIStyle(ws);
            s3.normal.textColor = c3;
            Vector2 sz3 = s3.CalcSize(new GUIContent(" " + procStr + " "));

            float totalHeight = sz1.y + sz2.y + sz3.y;
            float startY = y - totalHeight;

            GUI.Label(new Rect(x, startY, sz1.x, sz1.y), " " + timeStr + " ", s1);
            GUI.Label(new Rect(x, startY + sz1.y, sz2.x, sz2.y), " " + logoStr + " ", s2);
            GUI.Label(new Rect(x, startY + sz1.y + sz2.y, sz3.x, sz3.y), " " + procStr + " ", s3);
        }

        void DrawFeatureList()
        {
            bool show;
            lock (Lock)
            {
                show = ActiveFeatures.Contains("功能列表");
            }
            if (!show) return;

            List<string> features;
            lock (Lock)
            {
                features = new List<string>(ActiveFeatures);
            }
            if (features.Count == 0) return;

            features.Sort((a, b) => CalcTextWidth(GetDisplayText(b), (int)(36f * SCALE)).CompareTo(CalcTextWidth(GetDisplayText(a), (int)(36f * SCALE))));

            float x = Screen.width - 20f * SCALE;
            float y = 20f * SCALE;

            for (int i = 0; i < features.Count; i++)
            {
                string f = features[i];
                string display = GetDisplayText(f);
                Color col = GetItemColor(i, features.Count);
                float w = CalcTextWidth(display, (int)(36f * SCALE)) + 40f * SCALE;

                GUIStyle st = new GUIStyle(listItemStyle);
                st.normal.textColor = col;
                st.normal.background = blackTex;

                GUI.Label(new Rect(x - w, y, w, 52f * SCALE), " " + display + " ", st);
                y += 52f * SCALE;
            }
        }

        public static float CalcTextWidth(string text, int fontSize)
        {
            GUIStyle temp = new GUIStyle(GUI.skin.label);
            temp.fontSize = fontSize;
            return temp.CalcSize(new GUIContent(text)).x;
        }

        Color GetItemColor(int index, int total)
        {
            if (total <= 1) return GetRainbowColor(hue);
            float step = total <= 12 ? 30f : 360f / total;
            float itemHue = (hue - (index * step) + 720f) % 360f;
            return GetRainbowColor(itemHue);
        }

        Color GetRainbowColor(float h)
        {
            h = h % 360f;
            if (h < 0) h += 360f;

            float hd = h / 60f;
            int hi = (int)Mathf.Floor(hd) % 6;
            float f = hd - Mathf.Floor(hd);

            switch (hi)
            {
                case 0: return new Color(1f, f, 0f);
                case 1: return new Color(1f - f, 1f, 0f);
                case 2: return new Color(0f, 1f, f);
                case 3: return new Color(0f, 1f - f, 1f);
                case 4: return new Color(f, 0f, 1f);
                default: return new Color(1f, 0f, 1f - f);
            }
        }

        Texture2D MakeTex(Color col)
        {
            Texture2D t = new Texture2D(1, 1);
            t.SetPixel(0, 0, col);
            t.Apply();
            return t;
        }

        void OnDestroy()
        {
            if (whiteTex != null) Destroy(whiteTex);
            if (bgTex != null) Destroy(bgTex);
            if (titleBgTex != null) Destroy(titleBgTex);
            if (blackTex != null) Destroy(blackTex);
            if (grayTex != null) Destroy(grayTex);
            if (checkTex != null) Destroy(checkTex);
        }
    }
}
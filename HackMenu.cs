using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UniversalHack
{
    public class HackMenu : MonoBehaviour
    {
        public static List<string> ActiveFeatures = new List<string>();
        public static readonly object Lock = new object();

        private readonly string[][] MENUS = new[]
        {
            new[] { "事件类", "无视校长互动" , "无视袜子互动", "禁用巴迪移动", "无巴迪", "无校长", "无袜子", "无欢乐时间", "无扫把", "无第一名", "无校霸" },
            new[] { "移动类", "穿墙", "移速", "无视推动" },
            new[] { "玩家类", "无敌", "无限体力", "无限道具"},
            new[] { "视觉类", "绘制", "增大视野"},
            new[] { "主菜单", "功能列表", "水印" }
        };

        private Dictionary<string, bool> featureStates = new Dictionary<string, bool>();
        private Dictionary<string, Rect> menuRects = new Dictionary<string, Rect>();
        private Dictionary<string, bool> menuExpanded = new Dictionary<string, bool>();
        private Dictionary<string, bool> isDragging = new Dictionary<string, bool>();
        private Dictionary<string, Vector2> dragOffset = new Dictionary<string, Vector2>();

        private float hue = 0f;
        private bool showMenu = true;
        private GUIStyle titleStyle;
        private GUIStyle buttonStyle;
        private GUIStyle buttonActiveStyle;
        private GUIStyle listItemStyle;

        private Texture2D whiteTex;
        private Texture2D bgTex;
        private Texture2D titleBgTex;
        private Texture2D blackTex;

        private const float MENU_WIDTH = 280f;
        private const float TITLE_HEIGHT = 72f;
        private const float BUTTON_HEIGHT = 64f;
        private const float ALPHA_BG = 0.33f;

        void Awake()
        {
            whiteTex = MakeTex(Color.white);
            bgTex = MakeTex(new Color(1f, 1f, 1f, ALPHA_BG));
            titleBgTex = MakeTex(Color.white);
            blackTex = MakeTex(new Color(0f, 0f, 0f, 0.54f));

            float startX = 40f;
            float startY = 40f;
            foreach (var menu in MENUS)
            {
                string title = menu[0];
                menuRects[title] = new Rect(startX, startY, MENU_WIDTH, TITLE_HEIGHT);
                menuExpanded[title] = true;
                isDragging[title] = false;
                startX += MENU_WIDTH + 20f;
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
            hue = (hue + 0.5f) % 360f;

            if (Input.GetKeyDown(KeyCode.Insert))
                showMenu = !showMenu;

            if (Input.GetKeyDown(KeyCode.F1))
                Cursor.visible = !Cursor.visible;
        }

        void OnGUI()
        {
            InitStyles();

            if (showMenu)
            {
                Cursor.visible = true;
                DrawMenus();
            }

            DrawWatermark();
            DrawFeatureList();
        }

        void InitStyles()
        {
            if (titleStyle != null) return;

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 34;
            titleStyle.fontStyle = (FontStyle)1;
            titleStyle.alignment = (TextAnchor)4;
            titleStyle.normal.textColor = new Color(0f, 0.204f, 1f, 1f);

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 30;
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
            listItemStyle.fontSize = 36;
            listItemStyle.alignment = (TextAnchor)5;
        }

        void DrawMenus()
        {
            Event e = Event.current;
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
                GUI.Label(titleRect, title, titleStyle);

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

                        if (isOn)
                        {
                            GUI.color = rainbow;
                            GUI.DrawTexture(btnRect, whiteTex);
                            GUI.color = Color.white;
                            GUI.Label(btnRect, feature, buttonActiveStyle);
                        }
                        else
                        {
                            GUI.color = Color.white;
                            GUI.Label(btnRect, feature, buttonStyle);
                        }

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

            GUI.color = Color.white;
        }

        void DrawWatermark()
        {
            bool show;
            lock (Lock)
            {
                show = ActiveFeatures.Contains("水印");
            }
            if (!show) return;

            float x = 20f;
            float y = Screen.height - 20f;

            string timeStr = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            //string procStr = Application.productName + ":" + System.Diagnostics.Process.GetCurrentProcess().Id;
            string procStr = "Press F1 To Force Show The Mouse";
            string logoStr = "Baldis Basics Classic Mods Hack By JisGreen";

            Color c1 = GetRainbowColor(hue);
            Color c2 = GetRainbowColor((hue + 40f) % 360f);
            Color c3 = GetRainbowColor((hue + 80f) % 360f);

            GUIStyle ws = new GUIStyle(GUI.skin.label);
            ws.fontSize = 36;
            ws.alignment = (TextAnchor)3;
            ws.normal.background = blackTex;

            GUIStyle s1 = new GUIStyle(ws);
            s1.normal.textColor = c1;
            Vector2 sz1 = s1.CalcSize(new GUIContent(" " + timeStr + " "));

            GUIStyle s2 = new GUIStyle(ws);
            s2.normal.textColor = c2;
            Vector2 sz2 = s2.CalcSize(new GUIContent(" " + procStr + " "));

            GUIStyle s3 = new GUIStyle(ws);
            s3.normal.textColor = c3;
            Vector2 sz3 = s3.CalcSize(new GUIContent(" " + logoStr + " "));

            float totalHeight = sz1.y + sz2.y + sz3.y;
            float startY = y - totalHeight;

            GUI.Label(new Rect(x, startY, sz1.x, sz1.y), " " + timeStr + " ", s1);
            GUI.Label(new Rect(x, startY + sz1.y, sz2.x, sz2.y), " " + procStr + " ", s2);
            GUI.Label(new Rect(x, startY + sz1.y + sz2.y, sz3.x, sz3.y), " " + logoStr + " ", s3);
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

            features.Sort((a, b) => CalcTextWidth(b, 36).CompareTo(CalcTextWidth(a, 36)));

            float x = Screen.width - 20f;
            float y = 20f;

            for (int i = 0; i < features.Count; i++)
            {
                string f = features[i];
                Color col = GetItemColor(i, features.Count);
                float w = CalcTextWidth(f, 36) + 40f;

                GUIStyle st = new GUIStyle(listItemStyle);
                st.normal.textColor = col;
                st.normal.background = blackTex;

                GUI.Label(new Rect(x - w, y, w, 52f), " " + f + " ", st);
                y += 52f;
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
        }
    }
}
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
            new[] { "移动类", "穿墙", "移速", "无视推动", "飞行"},
            new[] { "玩家类", "无敌", "无限体力", "无限道具"},
            new[] { "视觉类", "绘制", "放大镜", "增大视野", "追踪器", "红温模式", "控件描边", "贴图旋转", "自转"},
            new[] { "主菜单", "功能列表", "水印" }
        };

        private readonly string[][] MENUS_EN = new[]
        {
            new[] { "Events", "Ignore Principal" , "Ignore Crafters", "Disable Baldi Move", "Instant Rage", "Deaf Baldi", "No Baldi", "No Principal", "No Crafters", "No Playtime", "No Sweep", "No First Prize", "No Bully" , "Book Hacker", "Jump All Wrong", "Instant Win"},
            new[] { "Movement", "No Clip", "Speed", "Anti Push", "Fly" },
            new[] { "Player", "God Mode", "Infinite Stamina", "Infinite Items"},
            new[] { "Visual", "ESP", "Magnifier", "FOV", "Tracker", "Red Light", "Outline", "Texture Rotate", "Auto Rotate"},
            new[] { "Main", "Feature List", "Watermark" }
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
        private bool gradientEffect = true;
        private bool hideCoverOnly = false;
        private float scalePercent = 50f;
        private bool showScaleDialog = false;
        private string scaleInput = "50";

        private Texture2D whiteTex;
        private Texture2D bgTex;
        private Texture2D titleBgTex;
        private Texture2D blackTex;
        private Texture2D grayTex;
        private Texture2D checkTex;
        private Texture2D dialogBgTex;

        private const float BASE_SCALE = 0.75f;
        private const float ALPHA_BG = 0.33f;
        private const float CHECKBOX_SIZE = 24f;

        private const float CTRL_WIDTH = 280f;
        private const float CTRL_HEIGHT = 32f;
        private const float CTRL_GAP = 4f;
        private const float CTRL_PAD = 20f;

        private float lastToggleTime = -1f;
        private const float TOGGLE_COOLDOWN = 0.15f;

        private Dictionary<string, Rect> initialMenuRects = new Dictionary<string, Rect>();
        private Dictionary<string, bool> initialMenuExpanded = new Dictionary<string, bool>();
        private bool initialEnglishMode = false;
        private bool initialGradientEffect = true;
        private bool initialHideCoverOnly = false;
        private float initialScalePercent = 50f;

        private GUIStyle cachedTitleStyle;
        private GUIStyle cachedButtonStyle;
        private GUIStyle cachedButtonActiveStyle;
        private GUIStyle cachedListItemStyle;
        private GUIStyle cachedControlStyle;
        private GUIStyle cachedControlLabelStyle;
        private GUIStyle cachedDialogStyle;
        private GUIStyle cachedDialogButtonStyle;
        private float lastStyleScale = -1f;

        private float GetScale()
        {
            return BASE_SCALE * (0.5f + scalePercent / 100f);
        }

        void Awake()
        {
            whiteTex = MakeTex(Color.white);
            bgTex = MakeTex(new Color(1f, 1f, 1f, ALPHA_BG));
            titleBgTex = MakeTex(Color.white);
            blackTex = MakeTex(new Color(0f, 0f, 0f, 0.54f));
            grayTex = MakeTex(new Color(0f, 0f, 0f, 0.6f));
            checkTex = MakeTex(new Color(0f, 0.8f, 0.2f, 1f));
            dialogBgTex = MakeTex(new Color(0.1f, 0.1f, 0.15f, 0.95f));

            RecalculateMenuPositions();

            lock (Lock)
            {
                ActiveFeatures.Add("功能列表");
                ActiveFeatures.Add("水印");
            }

            featureStates["功能列表"] = true;
            featureStates["水印"] = true;

            initialEnglishMode = englishMode;
            initialGradientEffect = gradientEffect;
            initialHideCoverOnly = hideCoverOnly;
            initialScalePercent = scalePercent;
        }

        void RecalculateMenuPositions()
        {
            float scale = GetScale();
            float startX = 40f * scale;
            float startY = 40f * scale;
            float menuWidth = 300f * scale;
            float titleHeight = 72f * scale;

            foreach (var menu in MENUS)
            {
                string title = menu[0];
                Rect rect = new Rect(startX, startY, menuWidth, titleHeight);
                menuRects[title] = rect;
                menuExpanded[title] = true;
                isDragging[title] = false;
                startX += menuWidth + 20f * scale;

                if (!initialMenuRects.ContainsKey(title))
                {
                    initialMenuRects[title] = rect;
                    initialMenuExpanded[title] = true;
                }
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                TryToggle();
            }

            if (showScaleDialog && Input.GetKeyDown(KeyCode.Escape))
            {
                showScaleDialog = false;
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
                if (gradientEffect)
                {
                    hue = (Time.realtimeSinceStartup * 90f) % 360f;
                }

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

            if (showScaleDialog)
            {
                DrawScaleDialog();
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
            if (showMenu)
            {
                if (hideCoverOnly)
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

        void ResetUI()
        {
            scalePercent = initialScalePercent;
            RecalculateMenuPositions();

            foreach (var title in initialMenuRects.Keys)
            {
                menuRects[title] = initialMenuRects[title];
                menuExpanded[title] = initialMenuExpanded[title];
            }

            englishMode = initialEnglishMode;
            gradientEffect = initialGradientEffect;
            hideCoverOnly = initialHideCoverOnly;
            hideOverlayOnly = false;
            showMenu = true;
            ShowMenu = true;

            lock (Lock)
            {
                ActiveFeatures.Clear();
                ActiveFeatures.Add("功能列表");
                ActiveFeatures.Add("水印");
            }

            featureStates.Clear();
            featureStates["功能列表"] = true;
            featureStates["水印"] = true;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            lastStyleScale = -1f;
        }

        void InitStyles()
        {
            float scale = GetScale();

            if (Mathf.Approximately(lastStyleScale, scale) && cachedTitleStyle != null)
                return;

            lastStyleScale = scale;

            cachedTitleStyle = new GUIStyle(GUI.skin.label);
            cachedTitleStyle.fontSize = (int)(34f * scale);
            cachedTitleStyle.fontStyle = (FontStyle)1;
            cachedTitleStyle.alignment = (TextAnchor)4;
            cachedTitleStyle.normal.textColor = new Color(0f, 0.204f, 1f, 1f);

            cachedButtonStyle = new GUIStyle(GUI.skin.button);
            cachedButtonStyle.fontSize = (int)(30f * scale);
            cachedButtonStyle.alignment = (TextAnchor)4;
            cachedButtonStyle.normal.textColor = new Color(0f, 0.204f, 1f, 1f);
            cachedButtonStyle.normal.background = null;
            cachedButtonStyle.hover.textColor = new Color(0f, 0.204f, 1f, 1f);
            cachedButtonStyle.hover.background = null;
            cachedButtonStyle.active.textColor = new Color(0f, 0.204f, 1f, 1f);
            cachedButtonStyle.active.background = null;
            cachedButtonStyle.border = new RectOffset(0, 0, 0, 0);
            cachedButtonStyle.padding = new RectOffset(0, 0, 0, 0);
            cachedButtonStyle.margin = new RectOffset(0, 0, 0, 0);

            cachedButtonActiveStyle = new GUIStyle(cachedButtonStyle);
            cachedButtonActiveStyle.normal.textColor = Color.white;
            cachedButtonActiveStyle.hover.textColor = Color.white;
            cachedButtonActiveStyle.active.textColor = Color.white;

            cachedListItemStyle = new GUIStyle(GUI.skin.label);
            cachedListItemStyle.fontSize = (int)(36f * scale);
            cachedListItemStyle.alignment = (TextAnchor)5;

            cachedControlStyle = new GUIStyle(GUI.skin.label);
            cachedControlStyle.fontSize = (int)(22f * scale);
            cachedControlStyle.alignment = (TextAnchor)4;
            cachedControlStyle.normal.textColor = Color.white;

            cachedControlLabelStyle = new GUIStyle(GUI.skin.label);
            cachedControlLabelStyle.fontSize = (int)(22f * scale);
            cachedControlLabelStyle.alignment = (TextAnchor)3;
            cachedControlLabelStyle.normal.textColor = Color.white;

            cachedDialogStyle = new GUIStyle(GUI.skin.label);
            cachedDialogStyle.fontSize = (int)(28f * scale);
            cachedDialogStyle.alignment = (TextAnchor)4;
            cachedDialogStyle.normal.textColor = Color.white;

            cachedDialogButtonStyle = new GUIStyle(GUI.skin.button);
            cachedDialogButtonStyle.fontSize = (int)(26f * scale);
            cachedDialogButtonStyle.alignment = (TextAnchor)4;
            cachedDialogButtonStyle.normal.textColor = Color.white;
            cachedDialogButtonStyle.hover.textColor = Color.white;
            cachedDialogButtonStyle.active.textColor = Color.white;
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
            float scale = GetScale();
            float menuWidth = 300f * scale;
            float titleHeight = 72f * scale;
            float buttonHeight = 64f * scale;

            foreach (var menu in MENUS)
            {
                string title = menu[0];
                Rect rect = menuRects[title];
                bool expanded = menuExpanded[title];

                Rect titleRect = new Rect(rect.x, rect.y, menuWidth, titleHeight);

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
                        Rect btnRect = new Rect(rect.x, rect.y + titleHeight + (i - 1) * buttonHeight, menuWidth, buttonHeight);

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
            InitStyles();

            float scale = GetScale();
            float menuWidth = 300f * scale;
            float titleHeight = 72f * scale;
            float buttonHeight = 64f * scale;

            int globalIndex = 0;
            int totalItems = 0;

            foreach (var menu in MENUS)
            {
                if (menuExpanded[menu[0]])
                {
                    totalItems += menu.Length - 1;
                }
            }

            foreach (var menu in MENUS)
            {
                string title = menu[0];
                Rect rect = menuRects[title];
                bool expanded = menuExpanded[title];

                float totalHeight = titleHeight;
                if (expanded)
                    totalHeight += (menu.Length - 1) * buttonHeight;

                Rect fullRect = new Rect(rect.x, rect.y, menuWidth, totalHeight);
                GUI.DrawTexture(fullRect, bgTex);

                Rect titleRect = new Rect(rect.x, rect.y, menuWidth, titleHeight);
                GUI.DrawTexture(titleRect, titleBgTex);
                GUI.Label(titleRect, GetDisplayText(title), cachedTitleStyle);

                if (expanded)
                {
                    for (int i = 1; i < menu.Length; i++)
                    {
                        string feature = menu[i];
                        bool isOn = featureStates.ContainsKey(feature) && featureStates[feature];
                        Rect btnRect = new Rect(rect.x, rect.y + titleHeight + (i - 1) * buttonHeight, menuWidth, buttonHeight);

                        if (isOn)
                        {
                            float progress = totalItems > 0 ? (float)globalIndex / totalItems : 0f;
                            float hueOffset = -progress * 222f;
                            Color bgColor = gradientEffect ? GetRainbowColor(hue + hueOffset) : Color.blue;
                            GUI.color = bgColor;
                            GUI.DrawTexture(btnRect, whiteTex);
                            GUI.color = Color.white;
                            GUI.Label(btnRect, GetDisplayText(feature), cachedButtonActiveStyle);
                        }
                        else
                        {
                            GUI.color = Color.white;
                            cachedButtonStyle.normal.textColor = new Color(0f, 0.204f, 1f, 1f);
                            cachedButtonStyle.hover.textColor = new Color(0f, 0.204f, 1f, 1f);
                            cachedButtonStyle.active.textColor = new Color(0f, 0.204f, 1f, 1f);
                            GUI.Label(btnRect, GetDisplayText(feature), cachedButtonStyle);
                        }

                        globalIndex++;
                    }
                }
            }

            GUI.color = Color.white;
        }

        void DrawBottomRightControls()
        {
            InitStyles();

            float scale = GetScale();
            float ctrlWidth = CTRL_WIDTH * scale;
            float ctrlHeight = CTRL_HEIGHT * scale;
            float ctrlGap = CTRL_GAP * scale;
            float ctrlPad = CTRL_PAD * scale;

            float x = Screen.width - ctrlPad - ctrlWidth;

            float yEnglish = Screen.height - ctrlPad - ctrlHeight;
            float yHideCover = yEnglish - ctrlHeight - ctrlGap;
            float yGradient = yHideCover - ctrlHeight - ctrlGap;
            float yScale = yGradient - ctrlHeight - ctrlGap;
            float yReset = yScale - ctrlHeight - ctrlGap;
            float yGitHub = yReset - ctrlHeight - ctrlGap;

            float boxSize = CHECKBOX_SIZE * scale;

            Rect englishRect = new Rect(x, yEnglish, ctrlWidth, ctrlHeight);
            GUI.DrawTexture(englishRect, bgTex);

            float cx = x + 12f * scale;
            float cy = yEnglish + (ctrlHeight - boxSize) * 0.5f;
            Rect boxRect = new Rect(cx, cy, boxSize, boxSize);
            GUI.DrawTexture(boxRect, whiteTex);
            if (englishMode)
            {
                float inner = 4f * scale;
                GUI.DrawTexture(new Rect(boxRect.x + inner, boxRect.y + inner, boxSize - inner * 2f, boxSize - inner * 2f), checkTex);
            }

            float labelX = cx + boxSize + 10f * scale;
            float labelW = ctrlWidth - boxSize - 30f * scale;
            GUI.Label(new Rect(labelX, yEnglish, labelW, ctrlHeight), "English Mode", cachedControlLabelStyle);

            Rect hideCoverRect = new Rect(x, yHideCover, ctrlWidth, ctrlHeight);
            GUI.DrawTexture(hideCoverRect, bgTex);

            cx = x + 12f * scale;
            cy = yHideCover + (ctrlHeight - boxSize) * 0.5f;
            boxRect = new Rect(cx, cy, boxSize, boxSize);
            GUI.DrawTexture(boxRect, whiteTex);
            if (hideCoverOnly)
            {
                float inner = 4f * scale;
                GUI.DrawTexture(new Rect(boxRect.x + inner, boxRect.y + inner, boxSize - inner * 2f, boxSize - inner * 2f), checkTex);
            }

            labelX = cx + boxSize + 10f * scale;
            GUI.Label(new Rect(labelX, yHideCover, labelW, ctrlHeight), "Hide Cover Only", cachedControlLabelStyle);

            Rect gradientRect = new Rect(x, yGradient, ctrlWidth, ctrlHeight);
            GUI.DrawTexture(gradientRect, bgTex);

            cx = x + 12f * scale;
            cy = yGradient + (ctrlHeight - boxSize) * 0.5f;
            boxRect = new Rect(cx, cy, boxSize, boxSize);
            GUI.DrawTexture(boxRect, whiteTex);
            if (gradientEffect)
            {
                float inner = 4f * scale;
                GUI.DrawTexture(new Rect(boxRect.x + inner, boxRect.y + inner, boxSize - inner * 2f, boxSize - inner * 2f), checkTex);
            }

            labelX = cx + boxSize + 10f * scale;
            GUI.Label(new Rect(labelX, yGradient, labelW, ctrlHeight), "Gradient Effect", cachedControlLabelStyle);

            Rect scaleRect = new Rect(x, yScale, ctrlWidth, ctrlHeight);
            GUI.DrawTexture(scaleRect, bgTex);
            GUI.Label(scaleRect, "Scale: " + Mathf.RoundToInt(scalePercent) + "%", cachedControlStyle);

            Rect resetRect = new Rect(x, yReset, ctrlWidth, ctrlHeight);
            GUI.DrawTexture(resetRect, bgTex);
            GUI.Label(resetRect, "Reset", cachedControlStyle);

            Rect gitHubRect = new Rect(x, yGitHub, ctrlWidth, ctrlHeight);
            GUI.DrawTexture(gitHubRect, bgTex);
            GUI.Label(gitHubRect, "GitHub", cachedControlStyle);
        }

        void HandleBottomRightInput(Event e)
        {
            if (e.type != EventType.MouseDown || e.button != 0) return;

            float scale = GetScale();
            float ctrlWidth = CTRL_WIDTH * scale;
            float ctrlHeight = CTRL_HEIGHT * scale;
            float ctrlGap = CTRL_GAP * scale;
            float ctrlPad = CTRL_PAD * scale;

            float x = Screen.width - ctrlPad - ctrlWidth;

            float yEnglish = Screen.height - ctrlPad - ctrlHeight;
            float yHideCover = yEnglish - ctrlHeight - ctrlGap;
            float yGradient = yHideCover - ctrlHeight - ctrlGap;
            float yScale = yGradient - ctrlHeight - ctrlGap;
            float yReset = yScale - ctrlHeight - ctrlGap;
            float yGitHub = yReset - ctrlHeight - ctrlGap;

            Rect gitHubRect = new Rect(x, yGitHub, ctrlWidth, ctrlHeight);
            if (gitHubRect.Contains(e.mousePosition))
            {
                Application.OpenURL("https://github.com/ChenQingMua/BaldisBasicsClassicModsHack-Plugin");
                e.Use();
                return;
            }

            Rect resetRect = new Rect(x, yReset, ctrlWidth, ctrlHeight);
            if (resetRect.Contains(e.mousePosition))
            {
                ResetUI();
                e.Use();
                return;
            }

            Rect scaleRect = new Rect(x, yScale, ctrlWidth, ctrlHeight);
            if (scaleRect.Contains(e.mousePosition))
            {
                showScaleDialog = true;
                scaleInput = Mathf.RoundToInt(scalePercent).ToString();
                e.Use();
                return;
            }

            Rect gradientRect = new Rect(x, yGradient, ctrlWidth, ctrlHeight);
            if (gradientRect.Contains(e.mousePosition))
            {
                gradientEffect = !gradientEffect;
                e.Use();
                return;
            }

            Rect hideCoverRect = new Rect(x, yHideCover, ctrlWidth, ctrlHeight);
            if (hideCoverRect.Contains(e.mousePosition))
            {
                hideCoverOnly = !hideCoverOnly;
                e.Use();
                return;
            }

            Rect englishRect = new Rect(x, yEnglish, ctrlWidth, ctrlHeight);
            if (englishRect.Contains(e.mousePosition))
            {
                englishMode = !englishMode;
                e.Use();
            }
        }

        void DrawScaleDialog()
        {
            InitStyles();

            float scale = GetScale();
            float dialogWidth = 380f * scale;
            float dialogHeight = 170f * scale;
            float dialogX = (Screen.width - dialogWidth) / 2f;
            float dialogY = (Screen.height - dialogHeight) / 2f;

            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), grayTex);

            Color oldColor = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(dialogX, dialogY, dialogWidth, dialogHeight), whiteTex);
            GUI.color = oldColor;

            GUI.color = new Color(0.75f, 0.75f, 0.75f, 1f);
            GUI.DrawTexture(new Rect(dialogX, dialogY, dialogWidth, 1f), whiteTex);
            GUI.DrawTexture(new Rect(dialogX, dialogY + dialogHeight - 1f, dialogWidth, 1f), whiteTex);
            GUI.DrawTexture(new Rect(dialogX, dialogY, 1f, dialogHeight), whiteTex);
            GUI.DrawTexture(new Rect(dialogX + dialogWidth - 1f, dialogY, 1f, dialogHeight), whiteTex);
            GUI.color = oldColor;

            GUI.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            GUI.DrawTexture(new Rect(dialogX, dialogY, dialogWidth, 36f * scale), whiteTex);
            GUI.color = oldColor;

            GUIStyle titleStyle2 = new GUIStyle(cachedDialogStyle);
            titleStyle2.normal.textColor = Color.white;
            titleStyle2.fontStyle = FontStyle.Bold;
            titleStyle2.fontSize = (int)(20f * scale);
            titleStyle2.alignment = TextAnchor.MiddleLeft;
            GUI.Label(new Rect(dialogX + 12f * scale, dialogY, dialogWidth - 60f * scale, 36f * scale), "Scale Setting", titleStyle2);



            float contentY = dialogY + 50f * scale;

            GUIStyle labelStyle2 = new GUIStyle(cachedDialogStyle);
            labelStyle2.normal.textColor = new Color(0.05f, 0.05f, 0.05f, 1f);
            labelStyle2.fontSize = (int)(16f * scale);
            labelStyle2.alignment = TextAnchor.MiddleLeft;
            GUI.Label(new Rect(dialogX + 10f * scale, contentY, 100f * scale, 32f * scale), "Scale:", labelStyle2);

            float inputWidth = 280f * scale;
            float inputX = dialogX + 60f * scale;
            float inputY = contentY;
            float inputHeight = 32f * scale;

            GUI.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            GUI.DrawTexture(new Rect(inputX, inputY, inputWidth + 2f * scale, inputHeight + 2f * scale), whiteTex);
            GUI.color = new Color(0.95f, 0.95f, 0.95f, 1f);
            GUI.DrawTexture(new Rect(inputX + 1f * scale, inputY + 1f * scale, inputWidth, inputHeight - 2f * scale), whiteTex);
            GUI.color = oldColor;

            GUI.SetNextControlName("ScaleInput");
            scaleInput = GUI.TextField(new Rect(inputX + 4f * scale, inputY + 2f * scale, inputWidth - 8f * scale, inputHeight - 4f * scale), scaleInput, 20);

            labelStyle2.fontSize = (int)(16f * scale);
            labelStyle2.alignment = TextAnchor.MiddleLeft;
            GUI.Label(new Rect(inputX + inputWidth + 8f * scale, inputY, 30f * scale, inputHeight), "%", labelStyle2);

            float btnWidth = 120f * scale;
            float btnHeight = 30f * scale;
            float btnY = dialogY + dialogHeight - btnHeight - 16f * scale;

            Rect confirmRect = new Rect(dialogX + dialogWidth - btnWidth * 2 - 12f * scale, btnY, btnWidth, btnHeight);
            GUI.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            GUI.DrawTexture(confirmRect, whiteTex);
            GUI.color = oldColor;
            if (GUI.Button(confirmRect, "OK", cachedDialogButtonStyle))
            {
                int newValue;
                if (int.TryParse(scaleInput, out newValue))
                {
                    scalePercent = Mathf.Clamp(newValue, 0f, 100f);
                    RecalculateMenuPositions();
                    showScaleDialog = false;
                    GUI.FocusControl(null);
                    lastStyleScale = -1f;
                }
                else
                {
                    scaleInput = Mathf.RoundToInt(scalePercent).ToString();
                }
            }

            Rect cancelRect = new Rect(dialogX + dialogWidth - btnWidth - 6f * scale, btnY, btnWidth, btnHeight);
            GUI.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            GUI.DrawTexture(cancelRect, whiteTex);
            GUI.color = oldColor;
            if (GUI.Button(cancelRect, "Cancel", cachedDialogButtonStyle))
            {
                showScaleDialog = false;
                GUI.FocusControl(null);
            }

            if (Event.current.type == EventType.MouseDown && !new Rect(dialogX, dialogY, dialogWidth, dialogHeight).Contains(Event.current.mousePosition))
            {
                showScaleDialog = false;
                GUI.FocusControl(null);
                Event.current.Use();
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                int newValue;
                if (int.TryParse(scaleInput, out newValue))
                {
                    scalePercent = Mathf.Clamp(newValue, 0f, 100f);
                    RecalculateMenuPositions();
                    showScaleDialog = false;
                    GUI.FocusControl(null);
                    lastStyleScale = -1f;
                    Event.current.Use();
                }
            }

            if (Event.current.type == EventType.Repaint && GUI.GetNameOfFocusedControl() != "ScaleInput")
            {
                GUI.FocusControl("ScaleInput");
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

            InitStyles();

            float scale = GetScale();
            float x = 20f * scale;
            float y = Screen.height - 20f * scale;

            string timeStr = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            string procStr = "Baldis Basics Classic Mods Hack 1.4 By JisGreen";
            string logoStr = "Press Tab To Open Or Close Menu";

            Color c1 = gradientEffect ? GetRainbowColor(hue) : Color.white;
            Color c2 = gradientEffect ? GetRainbowColor((hue + 40f) % 360f) : Color.white;
            Color c3 = gradientEffect ? GetRainbowColor((hue + 80f) % 360f) : Color.white;

            GUIStyle ws = new GUIStyle(GUI.skin.label);
            ws.fontSize = (int)(36f * scale);
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

            InitStyles();

            float scale = GetScale();

            List<string> features;
            lock (Lock)
            {
                features = new List<string>(ActiveFeatures);
            }
            if (features.Count == 0) return;

            features.Sort((a, b) => CalcTextWidth(GetDisplayText(b), (int)(36f * scale)).CompareTo(CalcTextWidth(GetDisplayText(a), (int)(36f * scale))));

            float x = Screen.width - 20f * scale;
            float y = 20f * scale;

            for (int i = 0; i < features.Count; i++)
            {
                string f = features[i];
                string display = GetDisplayText(f);
                Color col = gradientEffect ? GetItemColor(i, features.Count) : Color.white;
                float w = CalcTextWidth(display, (int)(36f * scale)) + 40f * scale;

                GUIStyle st = new GUIStyle(cachedListItemStyle);
                st.normal.textColor = col;
                st.normal.background = blackTex;

                GUI.Label(new Rect(x - w, y, w, 52f * scale), " " + display + " ", st);
                y += 52f * scale;
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
            if (dialogBgTex != null) Destroy(dialogBgTex);
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace UniversalHack
{
    public class ESPPlugin : MonoBehaviour
    {
        private Texture2D whiteTex;

        void Awake()
        {
            whiteTex = new Texture2D(1, 1);
            whiteTex.SetPixel(0, 0, Color.white);
            whiteTex.Apply();
        }

        void OnGUI()
        {
            if (!HackMenu.ActiveFeatures.Contains("绘制")
                && !HackMenu.ActiveFeatures.Contains("追踪器")
                && !HackMenu.ActiveFeatures.Contains("控件描边"))
                return;

            Camera cam = Camera.main;
            if (cam == null) return;

            bool drawBox = HackMenu.ActiveFeatures.Contains("绘制");
            bool drawTracer = HackMenu.ActiveFeatures.Contains("追踪器");
            bool drawMeshOutline = HackMenu.ActiveFeatures.Contains("控件描边");

            Vector2 screenTopCenter = new Vector2(Screen.width / 2f, 0f);

            if (drawBox || drawTracer)
            {
                foreach (SpriteRenderer sr in FindObjectsOfType<SpriteRenderer>())
                {
                    if (sr == null || !sr.gameObject.activeInHierarchy) continue;

                    Vector2 size = sr.sprite.bounds.size;
                    Vector2 pivot = sr.sprite.pivot / sr.sprite.pixelsPerUnit;

                    Vector3[] localCorners = new Vector3[4];
                    localCorners[0] = new Vector3(-pivot.x, -pivot.y, 0f);
                    localCorners[1] = new Vector3(size.x - pivot.x, -pivot.y, 0f);
                    localCorners[2] = new Vector3(size.x - pivot.x, size.y - pivot.y, 0f);
                    localCorners[3] = new Vector3(-pivot.x, size.y - pivot.y, 0f);

                    Vector3[] worldCorners = new Vector3[4];
                    for (int i = 0; i < 4; i++)
                        worldCorners[i] = sr.transform.TransformPoint(localCorners[i]);

                    float minX = float.MaxValue, minY = float.MaxValue;
                    float maxX = float.MinValue, maxY = float.MinValue;
                    bool visible = false;

                    foreach (Vector3 corner in worldCorners)
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

                    if (!visible) continue;

                    float w = maxX - minX;
                    float h = maxY - minY;
                    if (w < 5f || h < 5f) continue;

                    Vector2 boxTopCenter = new Vector2(minX + w / 2f, minY);

                    if (drawBox)
                    {
                        GUI.color = Color.red;
                        GUI.DrawTexture(new Rect(minX, minY, w, 3f), whiteTex);
                        GUI.DrawTexture(new Rect(minX, minY + h - 3f, w, 3f), whiteTex);
                        GUI.DrawTexture(new Rect(minX, minY, 3f, h), whiteTex);
                        GUI.DrawTexture(new Rect(minX + w - 3f, minY, 3f, h), whiteTex);
                        GUI.color = Color.white;

                        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
                        labelStyle.fontSize = 16;
                        labelStyle.normal.textColor = Color.white;
                        GUI.Label(new Rect(minX, minY - 24f, 300f, 24f), sr.name, labelStyle);
                    }

                    if (drawTracer)
                    {
                        DrawLine(boxTopCenter, screenTopCenter, Color.cyan, 2f);
                    }
                }
            }

            if (drawMeshOutline)
            {
                foreach (MeshRenderer mr in FindObjectsOfType<MeshRenderer>())
                {
                    if (mr == null || !mr.gameObject.activeInHierarchy) continue;

                    string lowerName = mr.name.ToLower();
                    if (lowerName.Contains("floor") || lowerName.Contains("ceiling")
                        || lowerName.Contains("ground") || lowerName.Contains("wall")) continue;

                    Bounds b = mr.bounds;
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

                    if (!visible) continue;

                    float w = maxX - minX;
                    float h = maxY - minY;
                    if (w < 5f || h < 5f) continue;

                    GUI.color = Color.gray;
                    GUI.DrawTexture(new Rect(minX, minY, w, 2f), whiteTex);
                    GUI.DrawTexture(new Rect(minX, minY + h - 2f, w, 2f), whiteTex);
                    GUI.DrawTexture(new Rect(minX, minY, 2f, h), whiteTex);
                    GUI.DrawTexture(new Rect(minX + w - 2f, minY, 2f, h), whiteTex);
                    GUI.color = Color.white;
                }
            }
        }

        void DrawLine(Vector2 start, Vector2 end, Color color, float thickness)
        {
            Vector2 delta = end - start;
            float length = delta.magnitude;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            GUI.color = color;
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(new Rect(start.x, start.y - thickness / 2f, length, thickness), whiteTex);
            GUIUtility.RotateAroundPivot(-angle, start);
            GUI.color = Color.white;
        }

        void OnDestroy()
        {
            if (whiteTex != null) Destroy(whiteTex);
        }
    }
}
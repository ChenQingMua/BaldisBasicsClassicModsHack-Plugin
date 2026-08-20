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
            if (!HackMenu.ActiveFeatures.Contains("绘制"))
                return;

            Camera cam = Camera.main;
            if (cam == null) return;

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
        }

        void OnDestroy()
        {
            if (whiteTex != null) Destroy(whiteTex);
        }
    }
}
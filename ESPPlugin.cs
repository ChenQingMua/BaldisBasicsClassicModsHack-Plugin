using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniversalHack
{
    public class ESPPlugin : MonoBehaviour
    {
        private Texture2D whiteTex;
        private Texture2D translucentWhiteTex;

        private string[] eventNames = new string[]
        {
            "baldi", "principal", "crafters", "playtime", "sweep", "prize",
            "bully", "boss", "null", "gotta", "first", "1st",
            "baldoon"
        };

        private string[] eventScriptNames = new string[]
        {
            "BaldiScript", "PrincipalScript", "CraftersScript", "PlaytimeScript",
            "GottaSweepScript", "FirstPrizeScript", "BullyScript", "BossScript",
            "NullScript", "Baldi", "Principal", "Crafters", "Playtime",
            "GottaSweep", "FirstPrize", "Bully", "Boss", "Null"
        };

        private Dictionary<int, Vector3> lastPositions = new Dictionary<int, Vector3>();
        private Dictionary<int, float> lastMoveTimes = new Dictionary<int, float>();
        private const float MOVE_THRESHOLD = 0.001f;
        private const float MOVE_MEMORY = 0.35f;

        void Awake()
        {
            whiteTex = new Texture2D(1, 1);
            whiteTex.SetPixel(0, 0, Color.white);
            whiteTex.Apply();

            translucentWhiteTex = new Texture2D(1, 1);
            translucentWhiteTex.SetPixel(0, 0, new Color(1f, 1f, 1f, 0.67f));
            translucentWhiteTex.Apply();
        }

        bool NameChainMatches(Transform t)
        {
            Transform current = t;
            int depth = 0;
            while (current != null && depth < 12)
            {
                string n = current.gameObject.name;
                if (!string.IsNullOrEmpty(n))
                {
                    string lower = n.ToLower();
                    foreach (string ev in eventNames)
                    {
                        if (lower.Contains(ev))
                            return true;
                    }
                }
                current = current.parent;
                depth++;
            }
            return false;
        }

        bool ScriptMatches(GameObject go)
        {
            if (go == null) return false;

            var behaviours = go.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var mb in behaviours)
            {
                if (mb == null) continue;
                string typeName = mb.GetType().Name;
                foreach (string s in eventScriptNames)
                {
                    if (typeName == s || typeName.Contains(s))
                        return true;
                }
            }
            return false;
        }

        bool IsMoving(GameObject go)
        {
            if (go == null) return false;

            int id = go.GetInstanceID();
            Vector3 pos = go.transform.position;
            float now = Time.unscaledTime;

            if (lastPositions.TryGetValue(id, out Vector3 prev))
            {
                if ((pos - prev).sqrMagnitude > MOVE_THRESHOLD * MOVE_THRESHOLD)
                {
                    lastMoveTimes[id] = now;
                    lastPositions[id] = pos;
                    return true;
                }
            }
            lastPositions[id] = pos;

            if (lastMoveTimes.TryGetValue(id, out float t))
            {
                if (now - t < MOVE_MEMORY)
                    return true;
            }

            return false;
        }

        bool IsEventObject(GameObject go)
        {
            if (go == null) return false;
            if (ScriptMatches(go)) return true;
            if (NameChainMatches(go.transform)) return true;
            if (IsMoving(go)) return true;
            return false;
        }

        string GetLabel(GameObject go)
        {
            if (go == null) return "";
            return go.name;
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
                List<SpriteRenderer> normalObjects = new List<SpriteRenderer>();
                List<SpriteRenderer> eventObjects = new List<SpriteRenderer>();

                foreach (SpriteRenderer sr in FindObjectsOfType<SpriteRenderer>())
                {
                    if (sr == null || !sr.gameObject.activeInHierarchy) continue;

                    if (IsEventObject(sr.gameObject))
                        eventObjects.Add(sr);
                    else
                        normalObjects.Add(sr);
                }

                foreach (SpriteRenderer sr in normalObjects)
                    DrawSpriteRenderer(sr, cam, drawBox, drawTracer, screenTopCenter, false);

                foreach (SpriteRenderer sr in eventObjects)
                    DrawSpriteRenderer(sr, cam, drawBox, drawTracer, screenTopCenter, true);
            }

            if (drawMeshOutline)
            {
                List<MeshRenderer> normalObjects = new List<MeshRenderer>();
                List<MeshRenderer> eventObjects = new List<MeshRenderer>();

                foreach (MeshRenderer mr in FindObjectsOfType<MeshRenderer>())
                {
                    if (mr == null || !mr.gameObject.activeInHierarchy) continue;

                    string name = mr.gameObject.name.ToLower();

                    if (IsEventObject(mr.gameObject))
                        eventObjects.Add(mr);
                    else
                        normalObjects.Add(mr);
                }

                foreach (MeshRenderer mr in normalObjects)
                    DrawMeshRenderer(mr, cam, false);

                foreach (MeshRenderer mr in eventObjects)
                    DrawMeshRenderer(mr, cam, true);
            }
        }

        void DrawSpriteRenderer(SpriteRenderer sr, Camera cam, bool drawBox, bool drawTracer, Vector2 screenTopCenter, bool isEventObject)
        {
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

            if (!visible) return;

            float w = maxX - minX;
            float h = maxY - minY;
            if (w < 5f || h < 5f) return;

            Vector2 boxTopCenter = new Vector2(minX + w / 2f, minY);
            string label = GetLabel(sr.gameObject);

            if (drawBox)
            {
                if (isEventObject)
                {
                    float thickness = 3f;
                    GUI.color = Color.red;
                    GUI.DrawTexture(new Rect(minX, minY, w, thickness), whiteTex);
                    GUI.DrawTexture(new Rect(minX, minY + h - thickness, w, thickness), whiteTex);
                    GUI.DrawTexture(new Rect(minX, minY, thickness, h), whiteTex);
                    GUI.DrawTexture(new Rect(minX + w - thickness, minY, thickness, h), whiteTex);
                }
                else
                {
                    float thickness = 2f;
                    GUI.color = Color.white;
                    GUI.DrawTexture(new Rect(minX, minY, w, thickness), translucentWhiteTex);
                    GUI.DrawTexture(new Rect(minX, minY + h - thickness, w, thickness), translucentWhiteTex);
                    GUI.DrawTexture(new Rect(minX, minY, thickness, h), translucentWhiteTex);
                    GUI.DrawTexture(new Rect(minX + w - thickness, minY, thickness, h), translucentWhiteTex);
                }
                GUI.color = Color.white;

                GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.fontSize = 16;
                if (isEventObject)
                    labelStyle.normal.textColor = Color.red;
                else
                    labelStyle.normal.textColor = new Color(1f, 1f, 1f, 0.67f);

                GUI.Label(new Rect(minX, minY - 24f, 300f, 24f), label, labelStyle);
            }

            if (drawTracer)
            {
                if (isEventObject)
                    DrawLine(boxTopCenter, screenTopCenter, Color.red, 3f, whiteTex);
                else
                    DrawLine(boxTopCenter, screenTopCenter, new Color(1f, 1f, 1f, 0.67f), 2f, translucentWhiteTex);
            }
        }

        void DrawMeshRenderer(MeshRenderer mr, Camera cam, bool isEventObject)
        {
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

            if (!visible) return;

            float w = maxX - minX;
            float h = maxY - minY;
            if (w < 5f || h < 5f) return;

            if (isEventObject)
            {
                float thickness = 3f;
                GUI.color = Color.red;
                GUI.DrawTexture(new Rect(minX, minY, w, thickness), whiteTex);
                GUI.DrawTexture(new Rect(minX, minY + h - thickness, w, thickness), whiteTex);
                GUI.DrawTexture(new Rect(minX, minY, thickness, h), whiteTex);
                GUI.DrawTexture(new Rect(minX + w - thickness, minY, thickness, h), whiteTex);
            }
            else
            {
                float thickness = 2f;
                GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.67f);
                GUI.DrawTexture(new Rect(minX, minY, w, thickness), translucentWhiteTex);
                GUI.DrawTexture(new Rect(minX, minY + h - thickness, w, thickness), translucentWhiteTex);
                GUI.DrawTexture(new Rect(minX, minY, thickness, h), translucentWhiteTex);
                GUI.DrawTexture(new Rect(minX + w - thickness, minY, thickness, h), translucentWhiteTex);
            }
            GUI.color = Color.white;
        }

        void DrawLine(Vector2 start, Vector2 end, Color color, float thickness, Texture2D tex)
        {
            Vector2 delta = end - start;
            float length = delta.magnitude;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            GUI.color = color;
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(new Rect(start.x, start.y - thickness / 2f, length, thickness), tex);
            GUIUtility.RotateAroundPivot(-angle, start);
            GUI.color = Color.white;
        }

        void OnDestroy()
        {
            if (whiteTex != null) Destroy(whiteTex);
            if (translucentWhiteTex != null) Destroy(translucentWhiteTex);
        }
    }
}
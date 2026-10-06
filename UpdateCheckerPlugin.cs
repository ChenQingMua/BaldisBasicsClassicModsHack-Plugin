using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEngine;

namespace UniversalHack
{
    public class UpdateCheckerPlugin : MonoBehaviour
    {
        private const string VERSION_URL = "https://baldi.muah.top/ModsHack/version.json";
        private const string CURRENT_VERSION = "1.5.0";
        private const int TIMEOUT_MS = 12345;

        private enum State
        {
            Checking,
            UpToDate,
            Failed,
            NewVersionFound,
            Closing
        }

        private State state = State.Checking;
        private string latestVersion = "";
        private string downloadLink = "";
        private bool closeRequested = false;

        private Texture2D whiteTex;

        private static MethodInfo getKeyDownMethod;

        static UpdateCheckerPlugin()
        {
            try
            {
                string[] assemblyNames = { "UnityEngine.InputLegacyModule", "UnityEngine", "UnityEngine.CoreModule" };
                Type inputType = null;

                foreach (string name in assemblyNames)
                {
                    try
                    {
                        var asm = Assembly.Load(name);
                        if (asm != null)
                        {
                            inputType = asm.GetType("UnityEngine.Input");
                            if (inputType != null) break;
                        }
                    }
                    catch { }
                }

                if (inputType == null)
                {
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            inputType = asm.GetType("UnityEngine.Input");
                            if (inputType != null) break;
                        }
                        catch { }
                    }
                }

                if (inputType != null)
                {
                    getKeyDownMethod = inputType.GetMethod("GetKeyDown", new Type[] { typeof(KeyCode) });
                }
            }
            catch { }
        }

        private static string LogPath
        {
            get
            {
                string gameRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                return Path.Combine(gameRoot, "plugin_version_check");
            }
        }

        void Awake()
        {
            whiteTex = MakeTex(Color.white);
            StartCoroutine(CheckVersion());
        }

        Texture2D MakeTex(Color col)
        {
            Texture2D t = new Texture2D(1, 1);
            t.SetPixel(0, 0, col);
            t.Apply();
            return t;
        }

        void WriteLog(string content)
        {
            try
            {
                File.WriteAllText(LogPath, content, Encoding.UTF8);
            }
            catch { }
        }

        bool IsKeyDown(KeyCode key)
        {
            if (getKeyDownMethod == null) return false;
            try { return (bool)getKeyDownMethod.Invoke(null, new object[] { key }); }
            catch { return false; }
        }

        static void EnableBestTls()
        {
            try
            {
                var t = typeof(SecurityProtocolType);
                int flags = 0;

                foreach (string name in new[] { "Tls12", "Tls11", "Tls", "Tls13" })
                {
                    var field = t.GetField(name);
                    if (field != null)
                    {
                        try { flags |= (int)field.GetValue(null); } catch { }
                    }
                }

                if (flags != 0)
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)flags;
            }
            catch { }
        }

        IEnumerator CheckVersion()
        {
            state = State.Checking;

            string result = null;
            string errorReason = null;
            bool threadDone = false;

            Thread t = new Thread(() =>
            {
                try
                {
                    EnableBestTls();

                    using (var client = new WebClient())
                    {
                        client.Encoding = Encoding.UTF8;
                        client.Headers.Add("User-Agent", "BaldisHackUpdater/1.0");
                        result = client.DownloadString(VERSION_URL);
                    }
                }
                catch (Exception ex)
                {
                    errorReason = ex.GetType().Name + ": " + ex.Message;
                }
                finally
                {
                    threadDone = true;
                }
            });

            t.IsBackground = true;
            t.Start();

            float elapsed = 0f;
            float timeoutSec = TIMEOUT_MS / 1000f;
            while (!threadDone && elapsed < timeoutSec)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!threadDone)
            {
                errorReason = "Timeout after " + timeoutSec + " seconds";
            }

            if (!string.IsNullOrEmpty(errorReason) || string.IsNullOrEmpty(result))
            {
                WriteLog("FAILED\n" + (errorReason ?? "Unknown error"));
                state = State.Failed;
                yield break;
            }

            WriteLog(result);

            try
            {
                latestVersion = ExtractJsonValue(result, "latest_version");
                downloadLink = ExtractJsonValue(result, "download_link");
            }
            catch (Exception ex)
            {
                WriteLog("FAILED\nJSON parse exception: " + ex.Message + "\nRaw:\n" + result);
                state = State.Failed;
                yield break;
            }

            if (string.IsNullOrEmpty(latestVersion))
            {
                WriteLog("FAILED\nlatest_version not found in JSON\nRaw:\n" + result);
                state = State.Failed;
                yield break;
            }

            if (CompareVersions(latestVersion, CURRENT_VERSION) > 0)
                state = State.NewVersionFound;
            else
                state = State.UpToDate;
        }

        string ExtractJsonValue(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return "";

            string pattern = "\"" + key + "\"";
            int keyIndex = json.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (keyIndex < 0) return "";

            int colonIndex = json.IndexOf(':', keyIndex + pattern.Length);
            if (colonIndex < 0) return "";

            int startQuote = json.IndexOf('"', colonIndex + 1);
            if (startQuote < 0) return "";

            int endQuote = json.IndexOf('"', startQuote + 1);
            if (endQuote < 0) return "";

            return json.Substring(startQuote + 1, endQuote - startQuote - 1);
        }

        int CompareVersions(string a, string b)
        {
            string[] pa = a.Split('.');
            string[] pb = b.Split('.');
            int len = Mathf.Max(pa.Length, pb.Length);

            for (int i = 0; i < len; i++)
            {
                int va = 0, vb = 0;
                if (i < pa.Length) int.TryParse(pa[i], out va);
                if (i < pb.Length) int.TryParse(pb[i], out vb);

                if (va > vb) return 1;
                if (va < vb) return -1;
            }
            return 0;
        }

        void ClosePrompt(bool triggerUpdate)
        {
            if (triggerUpdate && state == State.NewVersionFound)
            {
                try
                {
                    if (!string.IsNullOrEmpty(downloadLink))
                        Application.OpenURL(downloadLink);
                }
                catch { }
            }
            state = State.Closing;
            closeRequested = true;
        }

        void Update()
        {
            if (state == State.Closing || state == State.Checking) return;

            if (IsKeyDown(KeyCode.Y) || IsKeyDown(KeyCode.Return) || IsKeyDown(KeyCode.KeypadEnter))
            {
                ClosePrompt(true);
            }

            if (state == State.NewVersionFound && (IsKeyDown(KeyCode.N) || IsKeyDown(KeyCode.Escape)))
            {
                ClosePrompt(false);
            }
        }

        void OnGUI()
        {
            if (closeRequested) return;

            string text = null;

            switch (state)
            {
                case State.Checking:
                    text = "Checking for updates ...";
                    break;
                case State.UpToDate:
                    text = "You already have the latest version . \n(Press Y to close this prompt)";
                    break;
                case State.Failed:
                    text = "Failed to check for updates . \n(Press Y to close the prompt)";
                    break;
                case State.NewVersionFound:
                    text = "A new version is available. Do you want to update ? \n(Press Y to start the update, Press N to close this prompt.)";
                    break;
                default:
                    return;
            }

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 22;
            style.alignment = TextAnchor.MiddleCenter;
            style.wordWrap = true;
            style.normal.textColor = Color.white;

            float width = Mathf.Min(Screen.width - 80f, 900f);
            float height = 67f;
            float x = (Screen.width - width) * 0.5f;
            float y = Screen.height * 0.78f;

            Rect boxRect = new Rect(x, y, width, height);
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(boxRect, whiteTex);
            GUI.color = Color.white;

            GUI.Label(boxRect, text, style);

            if (state != State.Checking)
            {
                if (GUI.Button(boxRect, GUIContent.none, GUIStyle.none))
                {
                    ClosePrompt(true);
                }
            }
        }

        void OnDestroy()
        {
            if (whiteTex != null) Destroy(whiteTex);
        }
    }
}
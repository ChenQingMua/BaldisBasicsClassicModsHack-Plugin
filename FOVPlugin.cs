using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniversalHack
{
    public class FOVPlugin : MonoBehaviour
    {
        private Camera targetCamera;
        private float defaultFOV = 60f;
        private bool fovRecorded = false;
        private bool lastZoomIn = false;
        private bool lastZoomOut = false;

        void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            targetCamera = null;
            fovRecorded = false;
            defaultFOV = 60f;
            lastZoomIn = false;
            lastZoomOut = false;
        }

        void Update()
        {
            if (targetCamera == null)
            {
                targetCamera = FindPlayerCamera();
                if (targetCamera == null) return;
                fovRecorded = false;
            }

            if (!fovRecorded)
            {
                defaultFOV = targetCamera.fieldOfView;
                fovRecorded = true;
                ApplyFOV(targetCamera);
                return;
            }

            ApplyFOV(targetCamera);
        }

        Camera FindPlayerCamera()
        {
            Camera main = Camera.main;
            if (main != null)
            {
                if (main.CompareTag("MainCamera"))
                    return main;
            }

            foreach (var cam in FindObjectsOfType<Camera>())
            {
                if (cam == null || !cam.enabled || !cam.gameObject.activeInHierarchy) continue;
                if (cam.CompareTag("MainCamera")) return cam;
            }

            return null;
        }

        void ApplyFOV(Camera cam)
        {
            bool zoomIn = HackMenu.ActiveFeatures.Contains("放大镜");
            bool zoomOut = HackMenu.ActiveFeatures.Contains("增大视野");

            if (zoomIn == lastZoomIn && zoomOut == lastZoomOut)
                return;

            lastZoomIn = zoomIn;
            lastZoomOut = zoomOut;

            if ((zoomIn && zoomOut) || (!zoomIn && !zoomOut))
                cam.fieldOfView = defaultFOV;
            else if (zoomIn)
                cam.fieldOfView = defaultFOV - 30f;
            else if (zoomOut)
                cam.fieldOfView = defaultFOV + 50f;
        }
    }
}
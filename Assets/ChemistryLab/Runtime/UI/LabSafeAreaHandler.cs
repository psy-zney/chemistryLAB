using UnityEngine;

namespace ChemistryLab.Desktop
{
    /// <summary>
    /// Resilient safe-area handler that adjusts RectTransform anchors
    /// to screen notches, camera cutouts, and system bars.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public sealed class LabSafeAreaHandler : MonoBehaviour
    {
        private RectTransform panelRect;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private bool isApplying;

        private void Awake()
        {
            panelRect = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            var currentSafeArea = Screen.safeArea;
            var currentScreenSize = new Vector2Int(Screen.width, Screen.height);

            if (currentSafeArea != lastSafeArea || currentScreenSize != lastScreenSize)
            {
                ApplySafeArea();
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplySafeArea();
        }

        public void ApplySafeArea()
        {
            if (isApplying)
            {
                return;
            }

            if (panelRect == null)
            {
                panelRect = GetComponent<RectTransform>();
            }

            if (panelRect == null)
            {
                return;
            }

            var safeArea = Screen.safeArea;
            var screenWidth = Screen.width;
            var screenHeight = Screen.height;

            if (screenWidth <= 0 || screenHeight <= 0)
            {
                return;
            }

            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;

            anchorMin.x = Mathf.Clamp01(anchorMin.x / screenWidth);
            anchorMin.y = Mathf.Clamp01(anchorMin.y / screenHeight);
            anchorMax.x = Mathf.Clamp01(anchorMax.x / screenWidth);
            anchorMax.y = Mathf.Clamp01(anchorMax.y / screenHeight);

            if (panelRect.anchorMin == anchorMin &&
                panelRect.anchorMax == anchorMax &&
                panelRect.offsetMin == Vector2.zero &&
                panelRect.offsetMax == Vector2.zero &&
                lastSafeArea == safeArea &&
                lastScreenSize.x == screenWidth &&
                lastScreenSize.y == screenHeight)
            {
                return;
            }

            try
            {
                isApplying = true;
                lastSafeArea = safeArea;
                lastScreenSize = new Vector2Int(screenWidth, screenHeight);

                panelRect.anchorMin = anchorMin;
                panelRect.anchorMax = anchorMax;
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;
            }
            finally
            {
                isApplying = false;
            }
        }
    }
}

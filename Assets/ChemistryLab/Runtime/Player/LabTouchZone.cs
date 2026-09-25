using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ChemistryLab.Desktop
{
    public enum TouchZoneMode
    {
        Movement,
        Look
    }

    /// <summary>
    /// Virtual touch surface providing pointer-owned movement and look gestures.
    /// Dedicated pointer ID ownership prevents multitouch conflict with buttons.
    /// </summary>
    public sealed class LabTouchZone : MonoBehaviour,
        IPointerDownHandler,
        IDragHandler,
        IPointerUpHandler,
        ICancelHandler
    {
        [SerializeField]
        private TouchZoneMode mode = TouchZoneMode.Movement;

        [SerializeField]
        private float maxDragRadius = 70f;

        [SerializeField]
        private float lookSensitivityMultiplier = 0.22f;

        private RectTransform zoneRect;
        private RectTransform stickVisual;
        private const int NoPointer = int.MinValue;
        private int activePointerId = NoPointer;
        private Canvas canvas;
        private Vector2 touchOrigin;
        private Vector2 currentOffset;
        private Vector2 lookDeltaAccumulator;
        private Vector2 inputVector;

        public TouchZoneMode Mode
        {
            get { return mode; }
            set { mode = value; }
        }

        public bool HasActivePointer
        {
            get { return activePointerId != NoPointer; }
        }

        public int ActivePointerId
        {
            get { return activePointerId; }
        }

        public Vector2 InputVector
        {
            get { return inputVector; }
        }

        public void Initialise(TouchZoneMode zoneMode, RectTransform visualStick = null)
        {
            mode = zoneMode;
            stickVisual = visualStick;
            zoneRect = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            ResetPointer();
        }

        public Vector2 ConsumeLookDelta()
        {
            var delta = lookDeltaAccumulator;
            lookDeltaAccumulator = Vector2.zero;
            return delta;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (activePointerId != NoPointer)
            {
                // Already owned by another active touch pointer
                return;
            }

            activePointerId = eventData.pointerId;
            touchOrigin = eventData.position;
            currentOffset = Vector2.zero;
            lookDeltaAccumulator = Vector2.zero;
            inputVector = Vector2.zero;

            if (stickVisual != null)
            {
                stickVisual.anchoredPosition = Vector2.zero;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId)
            {
                return;
            }

            if (mode == TouchZoneMode.Movement)
            {
                var delta = (eventData.position - touchOrigin) / (canvas == null ? 1f : Mathf.Max(.01f, canvas.scaleFactor));
                var clamped = Vector2.ClampMagnitude(delta, maxDragRadius);
                currentOffset = clamped;
                inputVector = clamped / maxDragRadius;

                if (stickVisual != null)
                {
                    stickVisual.anchoredPosition = clamped;
                }
            }
            else if (mode == TouchZoneMode.Look)
            {
                lookDeltaAccumulator += eventData.delta * lookSensitivityMultiplier * (540f / Mathf.Max(1, Screen.height));
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == activePointerId)
            {
                ResetPointer();
            }
        }

        public void OnCancel(BaseEventData eventData)
        {
            ResetPointer();
        }

        private void OnDisable()
        {
            ResetPointer();
        }

        public void ResetPointer()
        {
            activePointerId = NoPointer;
            currentOffset = Vector2.zero;
            inputVector = Vector2.zero;
            lookDeltaAccumulator = Vector2.zero;

            if (stickVisual != null)
            {
                stickVisual.anchoredPosition = Vector2.zero;
            }
        }
    }
}

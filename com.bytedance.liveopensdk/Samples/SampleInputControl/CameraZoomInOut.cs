// Copyright (c) Bytedance. All rights reserved.
// Description:

using System.Collections.Generic;
using Douyin.LiveOpenSDK.Samples.SampleUI;
using UnityEngine;
// using UnityEngine.EventSystems;
// using Input = UnityEngine.Input;
using Input = Douyin.LiveOpenSDK.CloudGameInput;
using EventSystem = Douyin.LiveOpenSDK.CloudGameEventSystem;

namespace Douyin.LiveOpenSDK.Samples.SampleInputControl
{
    /// <summary>
    /// Sample演示镜头操作：缩放
    /// </summary>
    public class CameraZoomInOut : MonoBehaviour
    {
        public enum ZoomType
        {
            Forward,
            AxisY,
            AxisZ,
        }

        [SerializeField] public Transform CameraTransform;
        [SerializeField] public bool AutoCheckMainCamera = true;
        [SerializeField] public float ZoomSpeedFactor = 1f;
        [SerializeField] public ZoomType zoomType = ZoomType.Forward;
        [SerializeField] public float ZoomCameraMinY = 0.5f;
        [SerializeField] public bool IsAllowTouchDebugLog = true;
        [SerializeField] public bool IsAllowMouseDebugLog = true;
        [SerializeField] public bool ShowsToast = true;

        private float _scrollValueAccumulate;
        private readonly float _scrollStepMaxValue = 5f;
        private readonly Dictionary<int, string> _debugTouchStates = new Dictionary<int, string>();
        private readonly float _internalFactor = 0.5f;
        private readonly float _axisToScrollInternalFactor = 10f;

        void Start()
        {
            CheckMainCamera();
        }

        private void CheckMainCamera()
        {
            if (!AutoCheckMainCamera || CameraTransform != null)
                return;
            var mainCam = Camera.main;
            if (mainCam == null)
                return;
            CameraTransform = mainCam.transform;
        }

        public static bool IsMouseScrollAxisMode = false;

        public float GetMouseScrollDelta()
        {
            if (IsMouseScrollAxisMode)
                return Input.GetAxis("Mouse ScrollWheel") * _axisToScrollInternalFactor;
            else
                return Input.mouseScrollDelta.y;
        }

        void Update()
        {
            var scrollValueInput = GetMouseScrollDelta();
            if (Mathf.Abs(scrollValueInput) > _scrollStepMaxValue)
                scrollValueInput = Mathf.Clamp(scrollValueInput, -_scrollStepMaxValue, _scrollStepMaxValue);

            _scrollValueAccumulate += scrollValueInput; // avoid problem if the wheel input values are frequent but very small
            LogDebugTouchInfo();
            LogDebugScrollInfo();

            if (IsFloatEqual(_scrollValueAccumulate, 0))
                return;

            var scrollValue = _scrollValueAccumulate;
            // reset accumulated
            _scrollValueAccumulate = 0;

            // Skip if it is on GUI
            var eventSystem = EventSystem.current;
            if (eventSystem != null && eventSystem.IsPointerOverGameObject())
                return;

            var targetTransform = CameraTransform;
            if (targetTransform == null)
                return;

            // Zoom the camera based on mouse scroll input
            var speed = ZoomSpeedFactor * _internalFactor;
            Vector3 offsetPosition;
            switch (zoomType)
            {
                case ZoomType.AxisY:
                    offsetPosition = Vector3.down * scrollValue * speed;
                    break;
                case ZoomType.AxisZ:
                    offsetPosition = Vector3.forward * scrollValue * speed;
                    break;
                default:
                case ZoomType.Forward:
                    offsetPosition = targetTransform.forward * scrollValue * speed;
                    break;
            }

            var prevPosition = targetTransform.position;
            var position = prevPosition + offsetPosition;

            if (position.y < ZoomCameraMinY)
            {
                var y0 = prevPosition.y;
                var y1 = position.y;
                var yt = ZoomCameraMinY;
                var ratio = Mathf.Abs(yt - y0) / Mathf.Abs(y1 - y0);
                position = Vector3.Lerp(prevPosition, position, ratio);
                if (y0 < yt)
                {
                    // reset back
                    ratio = (yt - y0) / (y1 - y0);
                    position = Vector3.LerpUnclamped(prevPosition, position, ratio);
                }
            }

            targetTransform.position = position;
            ShowToast($"缩放滚动了: {scrollValue:F3}");
        }

        private void ShowToast(string message)
        {
            if (ShowsToast)
                SampleToastUI.Instance.ShowToast(message);
        }

        private bool IsVerboseTouchDebugLog()
        {
            return IsAllowTouchDebugLog && (CloudGameInput.IsDebugLog || Debug.isDebugBuild);
        }

        private bool IsVerboseMouseDebugLog()
        {
            return IsAllowMouseDebugLog && (CloudGameInput.IsDebugLog || Debug.isDebugBuild);
        }

        private void LogDebugScrollInfo()
        {
            if (!IsVerboseMouseDebugLog())
                return;
            var scrollValueInput = GetMouseScrollDelta();
            if (IsFloatEqual(scrollValueInput, 0) && IsFloatEqual(_scrollValueAccumulate, 0))
                return;
            var eventSystem = EventSystem.current;
            var isOverUI = eventSystem != null && eventSystem.IsPointerOverGameObject();
            Debug.Log($"wheel input value: {scrollValueInput}, accumulate: {_scrollValueAccumulate}, isOverUI: {isOverUI}");
        }

        private void LogDebugTouchInfo()
        {
            if (!IsVerboseTouchDebugLog())
                return;
            var touches = Input.touches;
            var count = touches.Length;
            for (int i = 0; i < count; i++)
            {
                LogDebugTouchInfoAt(i);
            }
        }

        private void LogDebugTouchInfoAt(int id)
        {
            if (!IsVerboseTouchDebugLog())
                return;
            var found = Input.GetFingerTouch(id, out var touch);
            var pos = touch.position;
            var delta = touch.deltaPosition;
            var msg = found
                ? $"touch #{id} - {touch.phase} pos: {pos.ToString("F3")} delta: {delta.ToString("F3")}"
                : "";
            var timeInfo = $" time: {touch.deltaTime:F3}";
            var nowInfo = found ? $"{id} - {touch.phase}" : "";
            var has = _debugTouchStates.TryGetValue(id, out var prevInfo);
            _debugTouchStates[id] = nowInfo;
            if (!has || nowInfo != prevInfo)
                Debug.Log(msg + timeInfo);
        }

        private static bool IsFloatEqual(float value1, float value2)
        {
            return Mathf.Abs(value1 - value2) < Mathf.Epsilon;
        }
    }
}
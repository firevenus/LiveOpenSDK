// Copyright (c) Bytedance. All rights reserved.
// Description:

using Douyin.LiveOpenSDK.Samples.SampleUI;
using UnityEngine;
// using UnityEngine.EventSystems;
// using Input = UnityEngine.Input;
using Input = Douyin.LiveOpenSDK.CloudGameInput;
using EventSystem = Douyin.LiveOpenSDK.CloudGameEventSystem;

namespace Douyin.LiveOpenSDK.Samples.SampleInputControl
{
    /// <summary>
    /// Sample演示镜头操作：拖拽移动
    /// </summary>
    public class CameraDragMove : MonoBehaviour
    {
        [SerializeField] public Transform CameraTransform;
        [SerializeField] public bool AutoCheckMainCamera = true;
        [SerializeField] public float CameraSpeedFactor = 1f;
        [SerializeField] public float MoveStartThreshold = 3f;
        [SerializeField] public float MoveDeltaThreshold = 0.1f;
        [SerializeField] public bool ShowsToast = true;

        private Vector3 _dragFromPosition;
        private bool _isDragging;
        private bool _isMoved;
        private readonly float _screenToCameraInternalFactor = 0.01f;

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

        void Update()
        {
            // Check for button down
            if (Input.GetMouseButtonDown(0))
            {
                // Skip if it is on GUI
                var eventSystem = EventSystem.current;
                if (eventSystem != null && eventSystem.IsPointerOverGameObject())
                {
                    return;
                }

                var mousePosition = Input.mousePosition;
                if (IsPositionZero(mousePosition)) // 部分设备会在点击开始或结束一刻、偶现坐标0,0异常信息，需要排除
                {
                    Debug.Log("拖拽 Down 排除: 坐标0,0异常信息");
                    return;
                }
                _isDragging = true;
                _dragFromPosition = mousePosition;
                Debug.Log("拖拽 Down 开始");
            }

            // Deactivate dragging when button up
            if (_isDragging && Input.GetMouseButtonUp(0))
            {
                _isDragging = false;
                if (_isMoved)
                {
                    _isMoved = false;
                    ShowToast("拖拽移动结束");
                }
                else
                {
                    ShowToast("拖拽结束: 移动距离不足");
                }
            }

            var targetTransform = CameraTransform;
            if (targetTransform == null)
                return;

            // Move the camera while the left mouse button is held down
            var held = Input.GetMouseButton(0);
            if (_isDragging && held)
            {
                var mousePosition = Input.mousePosition;
                if (IsPositionZero(mousePosition)) // 部分设备会在点击开始或结束一刻、偶现坐标0,0异常信息，需要排除
                {
                    Debug.Log("拖拽 held 排除: 坐标0,0异常信息");
                    return;
                }
                Vector3 screenDelta = mousePosition - _dragFromPosition;
                var delta = screenDelta.magnitude;
                var threshold = MoveDeltaThreshold;
                if (delta < threshold)
                    return;

                if (!_isMoved)
                {
                    threshold = MoveStartThreshold;
                    if (delta < threshold)
                        return;
                    _isMoved = true;
                    ShowToast($"拖拽开始: from {{ {_dragFromPosition.x:F1}, {_dragFromPosition.y:F1} }} (move: {delta:F2})");
                }

                // apply transform position
                var speed = CameraSpeedFactor * _screenToCameraInternalFactor;
                speed = Mathf.Clamp(speed, 0.0001f, 100f);
                var positionOffset = new Vector3(
                    -screenDelta.x * speed,
                    0,
                    -screenDelta.y * speed
                );
                targetTransform.position += positionOffset;
                _dragFromPosition = mousePosition;
                ShowToast($"拖拽移动了: {{ {screenDelta.x:F1}, {screenDelta.y:F1} }} (move: {delta:F2})");
            }
        }

        private static bool IsPositionZero(Vector3 pos)
        {
            return IsFloatEqual(pos.x, 0) && IsFloatEqual(pos.y, 0);
        }

        private static bool IsFloatEqual(float value1, float value2)
        {
            return Mathf.Abs(value1 - value2) < Mathf.Epsilon;
        }

        private void ShowToast(string message)
        {
            if (ShowsToast)
                SampleToastUI.Instance.ShowToast(message);
        }
    }
}
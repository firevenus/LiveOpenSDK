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
    public class Camera3DObjectClick : MonoBehaviour
    {
        private const string TAG = nameof(Camera3DObjectClick);

        [SerializeField] public Camera Camera;
        [SerializeField] public bool AutoCheckMainCamera = true;
        [SerializeField] public bool ShowsToast = true;

        private bool _isClicked;
        private GameObject _clickedObject;

        void Start()
        {
            CheckMainCamera();
        }

        private void CheckMainCamera()
        {
            if (!AutoCheckMainCamera || Camera != null)
                return;
            var mainCam = Camera.main;
            if (mainCam == null)
                return;
            Camera = mainCam;
        }

        void Update()
        {
            UpdateMouseButton();
        }

        private void UpdateMouseButton()
        {
            // Check for button down
            if (Input.GetMouseButtonDown(0))
            {
                // Skip if it is on GUI
                var eventSystem = EventSystem.current;
                if (eventSystem != null && eventSystem.IsPointerOverGameObject())
                {
                    ShowToast("点击了: UI");
                    return;
                }

                _isClicked = true;
                _clickedObject = GetClickedObject(Camera);
                var go = _clickedObject;
                if (go != null)
                    ShowToast($"点击到了: {go.name}");
                else
                    ShowToast("点击了: 场景");
            }

            // Deactivate dragging when button up
            if (_isClicked && Input.GetMouseButtonUp(0))
            {
                _isClicked = false;
                var go = _clickedObject;
                if (go != null)
                    ShowToast($"点击松开了: {go.name}");
                else
                    ShowToast("松开了: 场景");
            }
        }

        private static GameObject GetClickedObject(Camera camera)
        {
            var ray = camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hitInfo))
            {
                return hitInfo.collider.gameObject;
            }

            return null;
        }

        private void ShowToast(string message)
        {
            if (ShowsToast)
                SampleToastUI.Instance.ShowToast(message);
        }
    }
}
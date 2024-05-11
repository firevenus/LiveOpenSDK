// Copyright (c) Bytedance. All rights reserved.
// Description:

using System.Collections.Generic;
using UnityEngine;

namespace Douyin.LiveOpenSDK.Samples.MultiTouchDebugger
{
    /// <summary>
    /// 多指触摸调试器
    /// </summary>
    public class MultiTouchDebugger : MonoBehaviour
    {
        [SerializeField] public GameObject ShapePrefab;
        [SerializeField] public RectTransform TargetRectTransform;
        [SerializeField] public bool IsForceShapeSize;
        [SerializeField] public float ForceShapeSize = 64;
        [SerializeField] public bool IsAllowDebugLog = true;
        public System.Func<bool> CustomDebugLogEnabler;
        private const float FadeDuration = 1.5f;
        private const string MouseScrollAxisName = "Mouse ScrollWheel";

        private readonly List<BaseDebugShape> aliveList = new List<BaseDebugShape>();
        private readonly List<TouchShape> touchShapes = new List<TouchShape>();
        private readonly List<MouseShape> mouseShapes = new List<MouseShape>();
        private readonly List<WheelScrollShape> scrollShapes = new List<WheelScrollShape>();
        private readonly Dictionary<string, string> _lastLogs = new Dictionary<string, string>();

        void Start()
        {
            Debug.Log($"MultiTouchDebugger Input.touchSupported: {Input.touchSupported}");
            if (TargetRectTransform == null)
                Debug.LogError("TargetRectTransform is null!");
        }

        // Update is called once per frame
        void Update()
        {
            UpdateAlive();
            UpdateTouches();
            UpdateMouseButtons();
            UpdateScrollInfo();
        }

        private void UpdateAlive()
        {
            List<BaseDebugShape> destroyedList = null;
            foreach (var shape in aliveList)
            {
                shape.UpdateTime(Time.deltaTime);
                if (shape.isDestroyed)
                {
                    destroyedList = destroyedList ?? new List<BaseDebugShape>();
                    destroyedList.Add(shape);
                }
            }

            if (destroyedList != null)
            {
                foreach (var shape in destroyedList)
                {
                    aliveList.Remove(shape);
                }
            }
        }

        private void UpdateTouches()
        {
            int i = 0;
            while (i < Input.touchCount)
            {
                var t = Input.GetTouch(i);
                var id = t.fingerId;
                string stateName;
                var shape = touchShapes.Find(itShape => itShape.id == id);
                var touchName = $"touch:{id} [{i}]";
                switch (t.phase)
                {
                    case TouchPhase.Began:
                    {
                        stateName = "Began";
                        shape = new TouchShape(id, CreateTouchShape(t));
                        var isOverUI = shape.IsOverUI();
                        LogDebug(touchName, $"{stateName} {PositionText(t)} isOverUI: {isOverUI}");
                        DestroyShapeOfId(touchShapes, id);
                        shape.SetStateText(stateName);
                        shape.SetBeganColor();
                        touchShapes.Add(shape);
                        aliveList.Add(shape);
                        break;
                    }
                    case TouchPhase.Ended:
                    {
                        stateName = "Ended";
                        LogDebug(touchName, $"{stateName} {PositionText(t)}");
                        if (shape != null)
                        {
                            shape.SetEndedColor();
                            shape.SetStateText(stateName);
                            DestroyShape(touchShapes, shape);
                        }
                        else
                        {
                            Debug.LogWarning($"{touchName} {stateName} but not found!");
                        }

                        break;
                    }
                    case TouchPhase.Moved:
                    {
                        stateName = "Moved";
                        LogDebug(touchName, $"{stateName} {PositionText(t)}");
                        if (shape != null)
                        {
                            shape.SetMovedColor();
                            shape.SetStateText(stateName);
                            SetShapeScreenPosition(shape, t.position);
                        }
                        else
                        {
                            Debug.LogWarning($"{touchName} {stateName} but not found!");
                        }

                        break;
                    }
                    case TouchPhase.Stationary:
                    {
                        stateName = "Stay";
                        LogDebug(touchName, $"{stateName} {PositionText(t)}");
                        shape?.SetStateText(stateName);
                        shape?.SetStayColor();
                        break;
                    }
                    case TouchPhase.Canceled:
                    {
                        stateName = "Cancel";
                        LogDebug(touchName, $"{stateName} {PositionText(t)}");
                        shape?.SetStateText(stateName);
                        shape?.SetCanceledColor();
                        DestroyShape(touchShapes, shape);
                        break;
                    }
                }

                ++i;
            }
        }

        private void UpdateMouseButtons()
        {
            if (Input.touchSupported)
                return;

            foreach (var shape in mouseShapes)
            {
                shape.UpdateTime(Time.deltaTime);
            }

            var id = 0;
            while (id < 3)
            {
                string stateName;
                var down = Input.GetMouseButtonDown(id);
                var up = Input.GetMouseButtonUp(id);
                var held = Input.GetMouseButton(id);
                var position = Input.mousePosition;
                var shape = mouseShapes.Find(shape => shape.id == id);
                var touchName = $"button:{id}";
                if (down)
                {
                    stateName = "Down";
                    shape = new MouseShape(id, CreateMouseShape(id, position));
                    var isOverUI = shape.IsOverUI();
                    LogDebug(touchName, $"{stateName} {PositionText(position)} isOverUI: {isOverUI}");
                    DestroyShapeOfId(mouseShapes, id);
                    shape.SetStateText(stateName);
                    shape.SetDownColor();
                    mouseShapes.Add(shape);
                    aliveList.Add(shape);
                }
                else if (up)
                {
                    stateName = "Up";
                    LogDebug(touchName, $"{stateName} {PositionText(position)}");
                    if (shape != null)
                    {
                        shape.SetStateText(stateName);
                        shape.SetUpColor();
                        SetShapeScreenPosition(shape, position);
                        DestroyShape(mouseShapes, shape);
                    }
                    else
                    {
                        Debug.LogWarning($"{touchName} {stateName} but not found!");
                    }
                }
                else if (held)
                {
                    stateName = "Held";
                    LogDebug(touchName, $"{stateName} {PositionText(position)}");
                    if (shape != null)
                    {
                        shape.SetStateText(stateName);
                        shape.SetHeldColor();
                        SetShapeScreenPosition(shape, position);
                    }
                    else
                    {
                        Debug.LogWarning($"{touchName} {stateName} but not found!");
                    }
                }

                ++id;
            }
        }

        private bool GetIsDebugLog()
        {
            bool isDebugLog;
            if (CustomDebugLogEnabler != null)
                isDebugLog = CustomDebugLogEnabler.Invoke();
            else
                isDebugLog = IsAllowDebugLog && Debug.isDebugBuild;
            return isDebugLog;
        }

        private void LogDebug(string touchName, string msg)
        {
            bool isDebugLog = GetIsDebugLog();
            if (isDebugLog)
            {
                var msgInfo = $"{touchName} {msg}";
                var key = touchName;
                if (_lastLogs.TryGetValue(key, out var lastLog))
                    if (lastLog == msgInfo)
                        return;
                _lastLogs[key] = msgInfo;
#if LIVEOPENSDK_SAMPLES_ENABLE_STARKLOGS
                StarkLogs.StarkLog.LogDebug("[Touch]", msgInfo + $" #{Time.frameCount}f {System.DateTime.Now:mm:ss.fff}");
#else
                Debug.Log("[Touch] " + msgInfo + $" #{Time.frameCount}f {System.DateTime.Now:mm:ss.fff}");
#endif
            }
        }

        private static string PositionText(Touch touch)
        {
            var position = touch.position;
            var delta = touch.deltaPosition.magnitude;
            return $"({position.x:F1}, {position.y:F1}), move: {delta:F2}";
        }

        private static string PositionText(Vector2 position)
        {
            return $"({position.x:F1}, {position.y:F1})";
        }

        private void DestroyShapeOf<T>(List<T> list, System.Func<T, bool> match, float duration = FadeDuration) where T : BaseDebugShape
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var itShape = list[i];
                if (!match(itShape))
                    continue;
                itShape.Destroy(true, duration);
                list.RemoveAt(i);
            }
        }

        private void DestroyShapeOfId<T>(List<T> list, int id, float duration = FadeDuration) where T : BaseDebugShape
        {
            DestroyShapeOf(list, t => t.id == id, duration);
        }

        private void DestroyShape<T>(List<T> list, T shape, float duration = FadeDuration) where T : BaseDebugShape
        {
            if (shape == null)
                return;
            shape.Destroy(true, duration);
            var index = list.IndexOf(shape);
            if (index >= 0)
                list.RemoveAt(index);
        }

        // note: we are debugging with UI display, not camera world display.
        // private Vector2 GetWorldPosition(Vector2 touchPosition)
        // {
        //     var camera0 = GetCamera();
        //     if (camera0 == null)
        //         return Vector2.zero;
        //     return camera0.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, transform.position.z));
        // }

        private Vector2 GetUIPosition(Vector2 screenPosition)
        {
            var parentRect = GetTargetParent();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                screenPosition,
                null,
                out var localPoint
            );
            return localPoint;
        }

        private RectTransform GetTargetParent()
        {
            return TargetRectTransform;
        }

        private GameObject CreateTouchShape(Touch t)
        {
            return CreateShape(ShapePrefab, "Touch" + t.fingerId, t.position);
        }

        private GameObject CreateMouseShape(int button, Vector3 position)
        {
            return CreateShape(ShapePrefab, "Mouse" + button, position);
        }

        private GameObject CreateScrollShape(int button, Vector3 position)
        {
            return CreateShape(ShapePrefab, "Scroll" + button, position);
        }

        private GameObject CreateShape(GameObject prefab, string shapeName, Vector2 screenPosition)
        {
            var parent = TargetRectTransform;
            var shape = Instantiate(prefab, parent);
            shape.name = shapeName;
            var shapeRect = shape.transform as RectTransform;
            SetupShapeAnchor(shapeRect, parent);
            SetupShapeSize(shapeRect);
            SetShapeScreenPosition(shapeRect, screenPosition);
            shape.SetActive(true);
            return shape;
        }

        private void SetupShapeSize(RectTransform shapeRect)
        {
            if (!IsForceShapeSize)
                return;
            shapeRect.sizeDelta = new Vector2(ForceShapeSize, ForceShapeSize);
        }

        private void SetupShapeAnchor(RectTransform shapeRect, RectTransform parent)
        {
            var pivot = parent.pivot;
            shapeRect.anchorMin = pivot;
            shapeRect.anchorMax = pivot;
            shapeRect.anchoredPosition = Vector2.zero;
        }

        private void SetShapeScreenPosition(BaseDebugShape shape, Vector2 screenPosition)
        {
            var shapeRect = shape.shapeObject.transform as RectTransform;
            SetShapeScreenPosition(shapeRect, screenPosition);
        }

        private void SetShapeScreenPosition(RectTransform shapeRect, Vector2 screenPosition)
        {
            shapeRect.anchoredPosition = GetUIPosition(screenPosition);
        }

        public static int ConvertFingerToPointerId(int fingerId, bool isMouseButton)
        {
            if (!isMouseButton)
                return fingerId;

            return fingerId switch
            {
                0 => UnityEngine.EventSystems.PointerInputModule.kMouseLeftId,
                1 => UnityEngine.EventSystems.PointerInputModule.kMouseRightId,
                2 => UnityEngine.EventSystems.PointerInputModule.kMouseMiddleId,
                _ => fingerId
            };
        }

        private void UpdateScrollInfo()
        {
            if (!GetIsDebugLog())
                return;

            var scrollInput1 = Input.mouseScrollDelta.y;
            var scrollInput2 = Input.GetAxis(MouseScrollAxisName);
            var isZero = IsFloatEqual(scrollInput1, 0) && IsFloatEqual(scrollInput2, 0);
            var isValid = !isZero;
            if (isValid)
                Debug.Log($"mouseScrollDelta.y: {scrollInput1:F3}  Mouse ScrollWheel: {scrollInput2:F3}");

            var input = (Mathf.Abs(scrollInput1) > Mathf.Abs(scrollInput2)) ? scrollInput1 : scrollInput2;
            var isPositive = input > 0;
            var id = 0;
            var position = Input.mousePosition;
            var shape = scrollShapes.Find(shape => shape.id == id && shape.isPositive == isPositive);
            if (!isValid)
            {
                DestroyShapeOf(scrollShapes, (t) => true);
            }
            else
            {
                if (shape == null)
                {
                    shape = new WheelScrollShape(id, isPositive, CreateScrollShape(id, position));
                    scrollShapes.Add(shape);
                    aliveList.Add(shape);
                }

                shape.SetInputValue(input);
            }
        }

        private static bool IsFloatEqual(float value1, float value2)
        {
            return Mathf.Abs(value1 - value2) < Mathf.Epsilon;
        }
    }
}
// Copyright (c) Bytedance. All rights reserved.
// Description:

using System.Collections.Generic;
using Douyin.LiveOpenSDK.Modules;
using Douyin.LiveOpenSDK.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;
using EventSystem = UnityEngine.EventSystems.EventSystem;

// ReSharper disable MergeIntoPattern

// ReSharper disable once CheckNamespace
namespace Douyin.LiveOpenSDK
{
    /// <summary>
    /// 兼容适配了移动端云游戏的统一输入API。  包含了模仿对齐PC端UnityEngine.Input风格的API，便于开发者理解、适配。
    /// </summary>
    /// <remarks><para>- 接入方式1：在使用到Input的相关代码顶部，添加：`using Input = Douyin.LivePlay.SDK.CloudGameInput;`</para></remarks>
    /// <remarks><para>- 接入方式2：使用到UnityEngine.Input的相关代码的`Input.xxx`，替换为`CloudGameInput.xxx`</para></remarks>
    public static class CloudGameInput
    {
        /// <summary>
        /// 是否处于触摸模式。 通常如果是抖音移动端云启动、且云设备支持触摸、则返回true，否则返回false，如果是PC本地启动或PC云启动通常也是返回false。
        /// </summary>
        /// <remarks>若只使用普通的点击、拖拽、缩放的输入信息，使用`CloudGameInput`统一输入API时无需关心是否触摸模式为true。</remarks>
        public static bool IsTouchMode
        {
            get
            {
                if (IsForceMouseButtonMode)
                    return false;
                if (IsForceTouchModeOnPC)
                    return true;
                if (_isTouchModeCached)
                    return _isTouchMode;

                Debug.Log($"CloudGameInput - Input.touchSupported: {Input.touchSupported}");
                Debug.LogDebug($"CloudGameInput - IsStartFromMobile: {LiveOpenSDK.CloudGameAPI.IsStartFromMobile()}");
                Debug.LogDebug($"CloudGameInput - isMobilePlatform: {Application.isMobilePlatform}");
                var isMobile = LiveOpenSDK.CloudGameAPI.IsStartFromMobile() || Application.isMobilePlatform;
                _isTouchMode = isMobile && Input.touchSupported;
                _isTouchModeCached = true;
                return _isTouchMode;
            }
        }

        /// <summary>
        ///   <para>兼容保留了原`UnityEngine.Input`的手指信息API.</para>
        ///   <para>Returns whether the device on which application is currently running supports touch input.</para>
        /// </summary>
        public static bool touchSupported => Input.touchSupported;

        /// <summary>
        /// 缩放滚动变化系数（灵敏度）
        /// </summary>
        /// <remarks>同时适用与PC端和移动端。 可以运行时动态调整修改、即时生效。</remarks>
        public static float ZoomScrollFactor { get; set; } = 1f;

        /// <summary>
        /// 是否避免在UI上的触摸。 (默认开启) 如果时触摸模式，true可以避免在UI上的触摸输入被当作了场景上的输入
        /// </summary>
        public static bool IsPreventInputOverUI { get; set; } = true;

        /// <summary>
        /// 是否打开详细 debug log (可选功能，默认关闭)
        /// </summary>
        public static bool IsDebugLog { get; set; } = false;

        /// <summary>
        /// 是否强制总是用鼠标输入模式，使`IsTouchMode`总是返回false (可选功能，默认关闭)
        /// </summary>
        public static bool IsForceMouseButtonMode { get; set; }

        /// <summary>
        /// 是否强制PC也使用手指触摸模式，使`IsTouchMode`总是返回true (可选功能，默认关闭)
        /// </summary>
        public static bool IsForceTouchModeOnPC { get; set; }

        /// 用于 <exception cref="GetAxis"></exception>
        public const string Axis_Mouse_ScrollWheel = "Mouse ScrollWheel";

        internal static bool _isTouchMode;
        internal static bool _isTouchModeCached;
        private static readonly TouchInputTracker s_mouse0Tracker = new TouchInputTracker("button0", 0, true); // mocked touch
        private static readonly TouchInputTracker s_mouse1Tracker = new TouchInputTracker("button1", 1, true); // mocked touch
        private static readonly TouchInputTracker s_mouse2Tracker = new TouchInputTracker("button2", 2, true); // mocked touch
        private static readonly TouchInputTracker s_finger0Tracker = new TouchInputTracker("fingerId0", 0, false);
        private static readonly TouchInputTracker s_finger1Tracker = new TouchInputTracker("fingerId1", 1, false);
        private static readonly float s_pinchToScrollInternalFactor = 0.1f;
        private static readonly float s_zoomToAxisInternalFactor = 0.1f;
        private static SdkDebugLogger Debug => LiveOpenSDK.Debug;

        static CloudGameInput()
        {
            LogDebugInfo();
        }

        /// <summary>
        /// 是否当前帧点击按下，对应鼠标按钮或手指触摸。对齐PC端UnityEngine.Input风格的API. Returns true during the frame the user pressed down the virtual button
        /// </summary>
        public static bool GetMouseButtonDown(int index)
        {
            if (!IsTouchMode)
            {
                // Returns true during the frame the user pressed down the virtual button identified by buttonName.
                return Input.GetMouseButtonDown(index);
            }

            // 是否当前帧点击按下。自动兼容了移动端云游戏
            return GetIsDown(index);
        }

        /// <summary>
        /// 是否当前帧点击按下。自动兼容了移动端云游戏
        /// </summary>
        /// <param name="index">第几个按钮、手指。 0 表示左键、或第一个手指</param>
        public static bool GetIsDown(int index)
        {
            if (!IsTouchMode)
            {
                // Returns true during the frame the user pressed down the virtual button identified by buttonName.
                return Input.GetMouseButtonDown(index);
            }

            var tracker = GetMouseTracker(index);
            if (tracker == null)
                return false;
            tracker.UpdateFingerTouch(IsPreventInputOverUI);
            tracker.GetMouseButtonStates(out var down, out _, out _);
            return down;
        }

        /// <summary>
        /// 是否当前帧点击抬起，对应鼠标按钮或手指触摸。对齐PC端UnityEngine.Input风格的API. Returns true the first frame the user releases the virtual button
        /// </summary>
        /// <param name="index">第几个按钮、手指。 0 表示左键、或第一个手指</param>
        public static bool GetMouseButtonUp(int index)
        {
            if (!IsTouchMode)
            {
                // Returns true the first frame the user releases the virtual button identified by buttonName.
                return Input.GetMouseButtonUp(index);
            }

            // 是否当前帧点击抬起。自动兼容了移动端云游戏
            return GetIsUp(index);
        }

        /// <summary>
        /// 是否当前帧点击抬起。自动兼容了移动端云游戏
        /// </summary>
        /// <param name="index">第几个按钮、手指。 0 表示左键、或第一个手指</param>
        public static bool GetIsUp(int index)
        {
            if (!IsTouchMode)
            {
                // Returns true the first frame the user releases the virtual button identified by buttonName.
                return Input.GetMouseButtonUp(index);
            }

            var tracker = GetMouseTracker(index);
            if (tracker == null)
                return false;
            tracker.UpdateFingerTouch(IsPreventInputOverUI);
            tracker.GetMouseButtonStates(out _, out var up, out _);
            return up;
        }

        /// <summary>
        /// 是否当前帧保持按下状态，对应鼠标按钮或手指触摸。对齐PC端UnityEngine.Input风格的API. Returns whether the given mouse button is held down
        /// </summary>
        /// <param name="index">第几个按钮、手指。 0 表示左键、或第一个手指</param>
        public static bool GetMouseButton(int index)
        {
            if (!IsTouchMode)
            {
                // Returns whether the given mouse button is held down
                return Input.GetMouseButton(index);
            }

            // 是否当前帧保持按下状态。自动兼容了移动端云游戏
            return GetIsHeldDown(index);
        }

        /// <summary>
        /// 是否当前帧保持按下状态。自动兼容了移动端云游戏
        /// </summary>
        /// <param name="index">第几个按钮、手指。 0 表示左键、或第一个手指</param>
        public static bool GetIsHeldDown(int index)
        {
            if (!IsTouchMode)
            {
                // Returns whether the given mouse button is held down
                return Input.GetMouseButton(index);
            }

            var tracker = GetMouseTracker(index);
            if (tracker == null)
                return false;
            tracker.UpdateFingerTouch(IsPreventInputOverUI);
            tracker.GetMouseButtonStates(out _, out _, out var held);
            return held;
        }

        /// <summary>
        /// 鼠标或手指触摸位置。对齐PC端UnityEngine.Input风格的API. The current mouse position in pixel coordinates.
        /// </summary>
        /// <remarks>The z component of the Vector3 is always 0.</remarks>
        public static Vector3 mousePosition
        {
            get
            {
                if (!IsTouchMode)
                {
                    // The current mouse position in pixel coordinates. (Read Only).
                    // The z component of the Vector3 is always 0.
                    var nowPosition = Input.mousePosition;
                    TrackMousePosition(0, nowPosition);
                    return nowPosition;
                }
                else
                {
                    // 鼠标或手指触摸位置。自动兼容了移动端云游戏
                    return position;
                }
            }
        }

        /// <summary>
        /// 鼠标或手指触摸位置。自动兼容了移动端云游戏
        /// </summary>
        /// <remarks>The z component of the Vector3 is always 0.</remarks>
        public static Vector3 position
        {
            get
            {
                if (!IsTouchMode)
                {
                    return mousePosition;
                }

                var tracker = s_mouse0Tracker;
                tracker.UpdateFingerTouch(IsPreventInputOverUI);
                return tracker.GetPosition();
            }
        }

        /// <summary>
        /// 鼠标或手指触摸的位置变化量，跟上一帧比较。自动兼容了移动端云游戏
        /// </summary>
        public static Vector2 deltaPosition
        {
            get
            {
                var tracker = s_mouse0Tracker;
                if (!IsTouchMode)
                {
                    // The current mouse position in pixel coordinates.
                    var nowPosition = Input.mousePosition;
                    TrackMousePosition(0, nowPosition);
                    tracker.GetDeltaPosition(nowPosition, out var delta);
                    return delta;
                }
                else
                {
                    tracker.UpdateFingerTouch(IsPreventInputOverUI);
                    tracker.GetDeltaPosition(tracker.GetPosition(), out var delta);
                    return delta;
                }
            }
        }

        /// <summary>
        /// 缩放滚动变化量，对应鼠标滚轮滚动或双指缩放(捏合/张开)。自动兼容了移动端云游戏
        /// </summary>
        /// <remarks>返回`Vector2.y` 变化值 &gt; 0 为放大/滚轮上滚/双指张开，&lt; 0 为缩小/滚轮下滚/双指捏合</remarks>
        public static Vector2 mouseScrollDelta
        {
            get
            {
                if (!IsTouchMode)
                {
                    // The current mouse scroll delta.
                    return Input.mouseScrollDelta * ZoomScrollFactor;
                }

                // 缩放滚动变化量，对应鼠标滚轮滚动或双指缩放(捏合/张开)。自动兼容了移动端云游戏
                return zoomScrollDelta;
            }
        }

        /// <summary>
        /// 缩放滚动变化量，对应鼠标滚轮滚动或双指缩放(捏合/张开)。对齐PC端UnityEngine.Input风格的API. The current mouse scroll delta.
        /// </summary>
        /// <remarks>返回`Vector2.y` 变化值 &gt; 0 为放大/滚轮上滚/双指张开，&lt; 0 为缩小/滚轮下滚/双指捏合</remarks>
        public static Vector2 zoomScrollDelta
        {
            get
            {
                if (!IsTouchMode)
                {
                    // The current mouse scroll delta.
                    return Input.mouseScrollDelta * ZoomScrollFactor;
                }

                var track_0 = s_finger0Tracker;
                var track_1 = s_finger1Tracker;
                track_0.UpdateFingerTouch(IsPreventInputOverUI);
                track_1.UpdateFingerTouch(IsPreventInputOverUI);

                var touch_0 = track_0.Touch;
                var touch_1 = track_1.Touch;
                var pinching = IsTwoFingersPinching(touch_0, touch_1);
                if (!pinching)
                {
                    return Vector2.zero;
                }

                var hasPrevious = track_0.HasPreviousData() && track_1.HasPreviousData();
                if (!hasPrevious)
                {
                    return Vector2.zero;
                }

                var now_0 = track_0.GetPosition();
                var now_1 = track_1.GetPosition();
                var last_0 = track_0.GetPreviousPosition();
                var last_1 = track_1.GetPreviousPosition();
                var prevDistance = Vector2.Distance(last_0, last_1);
                var nowDistance = Vector2.Distance(now_0, now_1);
                var delta = Vector2.zero;
                var factor = ZoomScrollFactor * s_pinchToScrollInternalFactor;
                factor = Mathf.Clamp(factor, 0.001f, 1000f);
                delta.y = (nowDistance - prevDistance) * factor;
                return delta;
            }
        }

        /// <summary>
        ///   <para>兼容保留了原`UnityEngine.Input`的手指信息API.</para>
        ///   <para>Returns the value of the virtual axis identified by axisName.</para>
        /// </summary>
        public static float GetAxis(string axisName)
        {
            if (!IsTouchMode)
                return Input.GetAxis(axisName);

            switch (axisName)
            {
                case Axis_Mouse_ScrollWheel:
                    return zoomScrollDelta.y * ZoomScrollFactor * s_zoomToAxisInternalFactor;
                default:
                    return Input.GetAxis(Axis_Mouse_ScrollWheel);
            }
        }

        /// <summary>
        ///   <para>兼容保留了原`UnityEngine.Input`的手指信息API.</para>
        ///   <para>Returns list of objects representing status of all touches during last frame. (Read Only) (Allocates temporary variables).</para>
        /// </summary>
        /// <remarks>仅当<see cref="IsTouchMode"/>为true、表示是触摸模式时有有效返回，否则返回为无效信息</remarks>
        public static Touch[] touches => Input.touches;

        /// <summary>
        ///   <para>兼容保留了原`UnityEngine.Input`的手指信息API.</para>
        ///   <para>Call Input.GetTouch to obtain a Touch struct.</para>
        /// </summary>
        /// <remarks>注意，参数`Index`不是fingerId，而是`Input.touchCount`中依次的顺序</remarks>
        /// <param name="index">The touch input on the device screen.</param>
        public static Touch GetTouch(int index)
        {
            return Input.GetTouch(index);
        }

        /// <summary>
        ///   <para>兼容保留了原`UnityEngine.Input`的手指信息API.</para>
        ///   <para>Number of touches. Guaranteed not to change throughout the frame. (Read Only)</para>
        /// </summary>
        public static int touchCount => Input.touchCount;

        /// <summary>
        /// 通过手指id找到Touch信息。
        /// </summary>
        /// <param name="fingerId">手指id。 0 表示第一个手指。</param>
        /// <param name="touch">返回找到的touch</param>
        /// <returns>是否找到</returns>
        // note: do not use Input.GetTouch(index) for this case.
        // note: reason: if use Input.GetTouch(index), the index (i.e., order in the touches array) may change for a finger if another previous finger releases.
        public static bool GetFingerTouch(int fingerId, out Touch touch)
        {
            var count = Input.touchCount;
            for (int i = 0; i < count; i++)
            {
                var itTouch = Input.GetTouch(i);
                if (itTouch.fingerId == fingerId)
                {
                    touch = itTouch;
                    return true;
                }
            }

            touch = default;
            return false;
        }

        /// <summary>
        /// 兼容适配了移动端云游戏的`EventSystem`。
        /// </summary>
        public static CloudGameEventSystem CloudGameEventSystem => CloudGameEventSystem.Instance;

        /// <summary>
        /// 是否点击在UI上。 鼠标模式判断鼠标左键，触摸模式判断第一个手指（`fingerId`为0）。 如果是true，应视情况避免一些操作、例如常见的镜头操作。
        /// </summary>
        public static bool IsMouseButtonOverUI()
        {
            return CloudGameEventSystem.current.IsPointerOverGameObject();
        }

        /// <summary>
        /// 手指是否触摸在UI上。如果是true，应视情况避免一些操作、例如常见的镜头操作。
        /// </summary>
        /// <param name="fingerId">手指id。 0 表示第一个手指。 如果是鼠标，id特殊，请参考<see cref="PointerInputModule.kMouseLeftId"/></param>
        /// <remarks>注意：如果是鼠标，id特殊，请参考<see cref="PointerInputModule.kMouseLeftId"/></remarks>
        // ReSharper disable once UnusedMember.Local
        public static bool IsFingerTouchOverUI(int fingerId)
        {
            var pointerId = fingerId;
            if (!IsTouchMode)
                pointerId = ConvertFingerToPointerId(fingerId);
            var eventSystem = EventSystem.current;
            if (eventSystem != null && eventSystem.IsPointerOverGameObject(pointerId))
                return true;

            return false;
        }

        /// <summary>
        /// 兼容处理，如果是触摸模式，id无需转换，如果是鼠标模式，用了fingerId的`0`表示了左键，那么需要兼容转换，id参考<see cref="PointerInputModule.kMouseLeftId"/>
        /// </summary>
        public static int ConvertFingerToPointerId(int fingerId)
        {
            if (IsTouchMode)
                return fingerId;

            // 非触摸模式，finger要转为鼠标按钮
            return fingerId switch
            {
                0 => PointerInputModule.kMouseLeftId,
                1 => PointerInputModule.kMouseRightId,
                2 => PointerInputModule.kMouseMiddleId,
                _ => fingerId
            };
        }

        /// <summary>
        /// 是否处于多指输入状态，此时应避免将多指捏合或张开时产生的坐标变化、当作了单指的拖拽移动
        /// </summary>
        public static bool IsMultiFingersActive()
        {
            return Input.touchCount >= 2;
        }

        /// <summary>
        /// 是否触在双指在捏合或张开
        /// </summary>
        public static bool IsTwoFingersPinching(Touch touch0, Touch touch1)
        {
            var count = Input.touchCount;
            if (count < 2)
            {
                return false;
            }

            var id0 = touch0.fingerId;
            var id1 = touch1.fingerId;
            var isIdMatch = id0 == 0 && id1 == 1 || id0 == 1 && id1 == 0;
            if (!isIdMatch)
                return false;
            var has0 = IsAnyTouch(t => t.fingerId == id0);
            var has1 = IsAnyTouch(t => t.fingerId == id1);
            if (!has0 || !has1)
                return false;

            var phase0 = touch0.phase;
            var phase1 = touch1.phase;
            var pinching0 = phase0 == TouchPhase.Moved || phase0 == TouchPhase.Stationary;
            var pinching1 = phase1 == TouchPhase.Moved || phase1 == TouchPhase.Stationary;
            return pinching0 && pinching1;
        }

        internal static bool IsAnyTouch(System.Func<Touch, bool> match)
        {
            var count = Input.touchCount;
            for (int i = 0; i < count; i++)
            {
                var itTouch = Input.GetTouch(i);
                if (match(itTouch))
                    return true;
            }

            return false;
        }

        public static void LogDebugInfo()
        {
            var isWarning = LiveOpenSDK.CloudGameAPI.IsStartFromMobile() && Input.touchSupported == false;
            if (isWarning)
                Debug.LogWarning("Warning: `Input.touchSupported == false` ! Expecting `true` for StartFromMobile!");

            // ReSharper disable once UseObjectOrCollectionInitializer
            var logs = new List<string>();
            logs.Add($"isTouchMode: {IsTouchMode}");
            logs.Add($"Input.touchSupported: {Input.touchSupported}");
            logs.Add($"CloudGameAPI.IsStartFromMobile: {LiveOpenSDK.CloudGameAPI.IsStartFromMobile()}");
            logs.Add($"Application.isMobilePlatform: {Application.isMobilePlatform}");
            logs.Add($"CloudGameAPI.IsCloudGame: {LiveOpenSDK.CloudGameAPI.IsCloudGame()}");
            logs.Add($"IsForceMouseButtonMode: {IsForceMouseButtonMode}");
            logs.Add($"IsForceTouchModeOnPC: {IsForceTouchModeOnPC}");
            logs.Add($"ZoomScrollFactor: {ZoomScrollFactor}");
            logs.Add($"IsPreventInputOverUI: {IsPreventInputOverUI}");

            var modeInfo = IsTouchMode ? "触摸模式" : "鼠标模式";
            Debug.Log($"云适配输入模式: {modeInfo}\nDebugInfo: {string.Join("\n", logs)}");
        }

        // adapt for mouse
        private static void TrackMousePosition(int buttonIndex, Vector3 nowPosition)
        {
            var tracker = GetMouseTracker(buttonIndex);
            if (tracker != null)
                tracker.UpdateMouseButtonPosition(nowPosition, IsPreventInputOverUI);
        }

        private static TouchInputTracker GetMouseTracker(int index)
        {
            var tracker = index switch
            {
                0 => s_mouse0Tracker,
                1 => s_mouse1Tracker,
                2 => s_mouse2Tracker,
                _ => null
            };

            return tracker;
        }

        // ReSharper disable once UnusedMember.Local
        private static TouchInputTracker GetFingerTracker(int index)
        {
            var tracker = index switch
            {
                0 => s_finger0Tracker,
                1 => s_finger1Tracker,
                _ => null
            };

            return tracker;
        }
    }

    /// <summary>
    /// 兼容适配了移动端云游戏的`EventSystem`。
    /// </summary>
    public class CloudGameEventSystem
    {
        private static CloudGameEventSystem _instance;
        private EventSystem _current;

        internal CloudGameEventSystem()
        {
            _current = EventSystem.current;
        }

        internal static CloudGameEventSystem Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = new CloudGameEventSystem();
                return _instance;
            }
        }

        /// <summary>
        /// Return the current EventSystem.
        /// </summary>
        public static CloudGameEventSystem current => Instance;

        /// <summary>
        /// <para>是否点击在UI上。 鼠标模式判断左键，触摸模式判断第一个手指（`fingerId`为0）。 如果是true，应视情况避免一些操作、例如常见的镜头操作。</para>
        /// <para>Is the pointer with the given ID over an EventSystem object?</para>
        /// </summary>
        public bool IsPointerOverGameObject()
        {
            var eventSystem = EventSystem.current;
            if (!IsTouchMode)
            {
                var isOverUI = eventSystem != null && eventSystem.IsPointerOverGameObject();
                return isOverUI;
            }
            else
            {
                var pointerId = CloudGameInput.ConvertFingerToPointerId(0);
                var isOverUI = eventSystem != null && eventSystem.IsPointerOverGameObject(pointerId);
                return isOverUI;
            }
        }

        /// <summary>
        /// Is the pointer with the given ID over an EventSystem object?
        /// </summary>
        /// <remarks>
        /// If you use IsPointerOverGameObject() without a parameter, it points to the "left mouse button" (pointerId = -1); therefore when you use IsPointerOverGameObject for touch, you should consider passing a pointerId to it
        /// Note that for touch, IsPointerOverGameObject should be used with ''OnMouseDown()'' or ''Input.GetMouseButtonDown(0)'' or ''Input.GetTouch(0).phase == TouchPhase.Began''.
        /// </remarks>
        public bool IsPointerOverGameObject(int pointerId)
        {
            var eventSystem = EventSystem.current;
            return eventSystem.IsPointerOverGameObject(pointerId);
        }

        /// <summary>
        /// 是否处于触摸模式。
        /// </summary>
        public bool IsTouchMode => CloudGameInput.IsTouchMode;

        /// <summary>
        /// Should the EventSystem allow navigation events (move / submit / cancel).
        /// </summary>
        public bool sendNavigationEvents
        {
            get { return _current.sendNavigationEvents; }
            set { _current.sendNavigationEvents = value; }
        }

        /// <summary>
        /// The soft area for dragging in pixels.
        /// </summary>
        public int pixelDragThreshold
        {
            get { return _current.pixelDragThreshold; }
            set { _current.pixelDragThreshold = value; }
        }

        /// <summary>
        /// The currently active EventSystems.BaseInputModule.
        /// </summary>
        public BaseInputModule currentInputModule
        {
            get { return _current.currentInputModule; }
        }

        /// <summary>
        /// Only one object can be selected at a time. Think: controller-selected button.
        /// </summary>
        public GameObject firstSelectedGameObject
        {
            get { return _current.firstSelectedGameObject; }
            set { _current.firstSelectedGameObject = value; }
        }

        /// <summary>
        /// The GameObject currently considered active by the EventSystem.
        /// </summary>
        public GameObject currentSelectedGameObject
        {
            get { return _current.currentSelectedGameObject; }
        }

        [System.Obsolete("lastSelectedGameObject is no longer supported")]
        public GameObject lastSelectedGameObject
        {
            get { return null; }
        }

        /// <summary>
        /// Flag to say whether the EventSystem thinks it should be paused or not based upon focused state.
        /// </summary>
        /// <remarks>
        /// Used to determine inside the individual InputModules if the module should be ticked while the application doesnt have focus.
        /// </remarks>
        public bool isFocused
        {
            get { return _current.isFocused; }
        }

        /// <summary>
        /// Returns true if the EventSystem is already in a SetSelectedGameObject.
        /// </summary>
        public bool alreadySelecting
        {
            get { return _current.alreadySelecting; }
        }

        /// <summary>
        /// Set the object as selected. Will send an OnDeselect the the old selected object and OnSelect to the new selected object.
        /// </summary>
        /// <param name="selected">GameObject to select.</param>
        /// <param name="pointer">Associated EventData.</param>
        public void SetSelectedGameObject(GameObject selected, BaseEventData pointer)
        {
            _current.SetSelectedGameObject(selected, pointer);
        }

        /// <summary>
        /// Set the object as selected. Will send an OnDeselect the the old selected object and OnSelect to the new selected object.
        /// </summary>
        /// <param name="selected">GameObject to select.</param>
        public void SetSelectedGameObject(GameObject selected)
        {
            _current.SetSelectedGameObject(selected);
        }

        /// <summary>
        /// Raycast into the scene using all configured BaseRaycasters.
        /// </summary>
        /// <param name="eventData">Current pointer data.</param>
        /// <param name="raycastResults">List of 'hits' to populate.</param>
        public void RaycastAll(PointerEventData eventData, List<RaycastResult> raycastResults)
        {
            _current.RaycastAll(eventData, raycastResults);
        }

        public override string ToString()
        {
            return _current.ToString();
        }
    }
}
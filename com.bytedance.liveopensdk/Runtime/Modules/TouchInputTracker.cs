// Copyright (c) Bytedance. All rights reserved.
// Description:

using Douyin.LiveOpenSDK.Utilities;
using UnityEngine;

namespace Douyin.LiveOpenSDK.Modules
{
    public enum TouchInputState
    {
        None,
        Single,
        Multi
    }

    public enum ExtTouchPhase
    {
        None = -1,
        Began = 0,
        Moved,
        Stationary,
        Ended,
        Canceled,
    }

    public static class TouchPhaseExtension
    {
        public static ExtTouchPhase ToExtPhase(this TouchPhase phase, bool isValidTouch)
        {
            if (!isValidTouch)
                return ExtTouchPhase.None;
            return phase switch
            {
                TouchPhase.Began => ExtTouchPhase.Began,
                TouchPhase.Moved => ExtTouchPhase.Moved,
                TouchPhase.Stationary => ExtTouchPhase.Stationary,
                TouchPhase.Ended => ExtTouchPhase.Ended,
                TouchPhase.Canceled => ExtTouchPhase.Canceled,
                _ => ExtTouchPhase.None
            };
        }

        public static TouchPhase ToPhase(this ExtTouchPhase phase)
        {
            return phase switch
            {
                ExtTouchPhase.None => TouchPhase.Canceled,
                ExtTouchPhase.Began => TouchPhase.Began,
                ExtTouchPhase.Moved => TouchPhase.Moved,
                ExtTouchPhase.Stationary => TouchPhase.Stationary,
                ExtTouchPhase.Ended => TouchPhase.Ended,
                ExtTouchPhase.Canceled => TouchPhase.Canceled,
                _ => TouchPhase.Canceled
            };
        }
    }

    /// <summary>
    /// Touch输入跟踪器，便于在兼容适配多指输入时，跟踪信息变化
    /// </summary>
    public class TouchInputTracker
    {
        // ReSharper disable once UnusedMember.Local
        private const string TAG = nameof(TouchInputTracker);
        private readonly string _name;
        private readonly int _fingerId;
        private readonly bool _isMouseButtonMode;

        private int _previousFrame;
        private int _nowFrame;
        private Touch _previousTouch;
        private Touch _nowTouch;
        private ExtTouchPhase _previousTouchPhase = ExtTouchPhase.None;
        private ExtTouchPhase _nowProcessedPhase = ExtTouchPhase.None;
        private ExtTouchPhase _nowRawInputPhase = ExtTouchPhase.None;

        private bool _mouseButtonDown;
        private bool _mouseButtonUp;
        private bool _mouseButtonHeld;

        private static readonly SdkDebugLogger Debug = new SdkDebugLogger("InputTracker");
        private string _trackDebugInfo;

        public TouchInputTracker(string name, int fingerId, bool isMouseButtonMode)
        {
            _name = name;
            _fingerId = fingerId;
            _isMouseButtonMode = isMouseButtonMode;
            Debug.IsTimeEnabled = false;
        }

        public string Name => _name;

        /// <summary>
        /// 鼠标按钮模式，即Mock模拟了触摸信息，并且与多指触摸状态互斥
        /// </summary>
        public bool IsMouseButtonMode => _isMouseButtonMode;

        /// <summary>
        /// 手指触摸模式，允许多指触摸输入
        /// </summary>
        public bool IsFingerTouchMode => !_isMouseButtonMode;

        /// <summary>
        /// 是否有效状态，该按钮或触摸在活动中。 如果`false`，表示没有该触摸信息，`Phase`信息和`Touch`信息也无意义。
        /// </summary>
        public bool IsValidState => TouchInputState != TouchInputState.None;

        /// <summary>
        /// 触摸阶段
        /// </summary>
        public ExtTouchPhase Phase => _nowProcessedPhase;

        /// <summary>
        /// 输入状态，无输入、单指、多指
        /// </summary>
        public TouchInputState TouchInputState { get; private set; }

        /// <summary>
        /// 当前触摸数据
        /// </summary>
        public Touch Touch => _nowTouch;

        /// <summary>
        /// 是否阻止输入状态，例如单指操作模式的鼠标按钮，在遇到进入多指操作状态时
        /// </summary>
        public bool IsPreventInputState { get; private set; }

        public bool IsBeganOverUI { get; private set; }

        /// <summary>
        /// 是否多指都松开，意味着接下来可以回到单指输入状态
        /// </summary>
        public bool IsAllFingersReleased()
        {
            var touchCount = Input.touchCount;
            return touchCount == 0;
        }

        /// <summary>
        /// 是否其他多指都松开，意味着接下来可以回到单指输入状态
        /// </summary>
        public bool IsAllOtherFingersReleased()
        {
            var touchCount = Input.touchCount;
            if (touchCount == 0)
                return true;
            if (touchCount == 1)
            {
                var touch = Input.GetTouch(0);
                return touch.fingerId == _fingerId;
            }

            return false;
        }

        /// <summary>
        /// 是否处于多指输入状态
        /// </summary>
        public bool IsMultiFingersState()
        {
            return CloudGameInput.IsMultiFingersActive();
        }

        /// <summary>
        /// 是否处于多指输入、且存在任意一个的Phase状态满足参数
        /// </summary>
        public bool IsMultiFingersWithAny(TouchPhase phase)
        {
            var touchCount = Input.touchCount;
            if (touchCount >= 2)
            {
                return CloudGameInput.IsAnyTouch(itTouch => itTouch.phase == phase);
            }

            return false;
        }


        /// <summary>
        /// 经过模拟的鼠标按键信息。 经过`UpdateTouch`后有效。
        /// </summary>
        /// <param name="down"></param>
        /// <param name="up"></param>
        /// <param name="held"></param>
        public void GetMouseButtonStates(out bool down, out bool up, out bool held)
        {
            down = _mouseButtonDown;
            up = _mouseButtonUp;
            held = _mouseButtonHeld;
        }

        /// <summary>
        /// 更新当前信息，使用鼠标的坐标信息
        /// </summary>
        public void UpdateMouseButtonPosition(Vector3 nowPosition, bool preventOverUI)
        {
            if (!_isMouseButtonMode)
                Debug.LogWarning($"UpdateMouseButtonTouch - {_name} mode mismatch!");

            var id = _fingerId;
            var position = nowPosition;
            var down = Input.GetMouseButtonDown(id);
            var up = Input.GetMouseButtonUp(id);
            var held = Input.GetMouseButton(id);
            var newPhase = TouchPhase.Canceled;
            if (up) newPhase = TouchPhase.Ended;
            else if (down) newPhase = TouchPhase.Began;
            else if (held) newPhase = TouchPhase.Moved;

            Touch touch = new Touch
            {
                fingerId = _fingerId,
                position = position,
                rawPosition = position,
                deltaPosition = Vector2.zero,
                deltaTime = Time.deltaTime,
                tapCount = 1,
                phase = newPhase,
                pressure = 1,
                maximumPossiblePressure = 1,
                type = TouchType.Direct,
            };
            var isValid = down || up || held;
            ProcessTouchForMouseButton(touch, isValid, preventOverUI);
        }

        /// <summary>
        /// 更新当前信息，使用手指触摸信息
        /// </summary>
        public void UpdateFingerTouch(bool preventOverUI)
        {
            var isValidTouch = GetFingerTouch(out var touch);
            if (IsUpdateDuplicated(touch, isValidTouch))
                return;

            if (IsFingerTouchMode)
            {
                ProcessTouchData(touch, isValidTouch, preventOverUI);
            }
            else
            {
                // 用Touch数据适配到Mouse的数据
                ProcessTouchForMouseButton(touch, isValidTouch, preventOverUI);
            }
        }

        private bool GetFingerTouch(out Touch touch)
        {
            return CloudGameInput.GetFingerTouch(_fingerId, out touch);
        }

        // 为 鼠标按钮模式 处理touch信息
        private void ProcessTouchForMouseButton(Touch touch, bool isValid, bool preventOverUI)
        {
            if (!_isMouseButtonMode)
                Debug.LogWarning($"ProcessMouseButtonTouch - {_name} mode mismatch!");

            touch.fingerId = _fingerId;
            ProcessTouchData(touch, isValid, preventOverUI);
        }

        /// <summary>
        /// 处理新的触摸数据
        /// </summary>
        private void ProcessTouchData(Touch touch, bool isValidTouch, bool preventOverUI)
        {
            if (isValidTouch && touch.fingerId != _fingerId)
            {
                Debug.LogWarning($"UpdateTouch - {_name} fingerId mismatch! input {touch.fingerId} != this {_fingerId}");
                return;
            }

            var frame = Time.frameCount;
            var newPhase = touch.phase.ToExtPhase(isValidTouch);
            if (IsUpdateDuplicated(touch, isValidTouch))
                return;

            _nowRawInputPhase = newPhase;
            if (IsFingerTouchMode)
                ProcessFingerPrevent(newPhase, preventOverUI);
            else
                ProcessMousePrevent(newPhase, preventOverUI);

            if (!isValidTouch)
            {
                TouchInputState = TouchInputState.None;
                _nowFrame = frame;
                _nowProcessedPhase = newPhase;
                LogTrackInfo();
                return;
            }

            if (IsPreventInputState)
                StopFingerTouch(ref touch, out newPhase);

            if (IsMouseButtonMode)
            {
                SetMouseStatesData(newPhase);
            }

            touch.phase = newPhase.ToPhase();
            if (newPhase == ExtTouchPhase.None)
                TouchInputState = TouchInputState.None;
            else
                TouchInputState = IsFingerTouchMode && IsMultiFingersState() ? TouchInputState.Multi : TouchInputState.Single;

            // set data
            // move data to `previous`, push new frame data to `now`
            _previousFrame = _nowFrame;
            _previousTouch = _nowTouch;
            _previousTouchPhase = _nowProcessedPhase;
            _nowFrame = frame;
            _nowTouch = touch;
            _nowProcessedPhase = newPhase;

            LogTrackInfo();
        }

        private void ProcessFingerPrevent(ExtTouchPhase newPhase, bool preventOverUI)
        {
            if (newPhase == ExtTouchPhase.Began)
                IsBeganOverUI = CloudGameInput.IsFingerTouchOverUI(_fingerId);
            var blockByUI = preventOverUI && IsBeganOverUI;
            if (blockByUI)
            {
                // 阻止操作
                IsPreventInputState = true;
            }
            else
            {
                // 触摸模式，允许操作
                IsPreventInputState = false;
            }
        }

        private void ProcessMousePrevent(ExtTouchPhase newPhase, bool preventOverUI)
        {
            if (newPhase == ExtTouchPhase.Began)
                IsBeganOverUI = CloudGameInput.IsFingerTouchOverUI(_fingerId);
            var blockByUI = preventOverUI && IsBeganOverUI;
            if (!IsPreventInputState && IsMultiFingersWithAny(TouchPhase.Moved))
            {
                // 鼠标模式，变为多指状态时，结束单指操作的状态
                IsPreventInputState = true;
                if (isVerboseDebugLog())
                    Debug.LogDebug($"\"{Name}\" IsMultiFingersWithAny Moved #{Time.frameCount}f");
            }
            else if (!blockByUI && IsPreventInputState && IsAllOtherFingersReleased())
            {
                // 鼠标模式，多指都松开时，允许重新操作
                if (isVerboseDebugLog())
                    Debug.LogDebug($"\"{Name}\" IsAllOtherFingersReleased #{Time.frameCount}f");
                IsPreventInputState = false;
            }

            if (blockByUI)
            {
                // 阻止操作
                IsPreventInputState = true;
            }
        }

        private void SetMouseStatesData(ExtTouchPhase newPhase)
        {
            var up = false;
            var down = false;
            var held = false;
            switch (newPhase)
            {
                case ExtTouchPhase.Began:
                    down = true;
                    break;
                case ExtTouchPhase.Moved:
                case ExtTouchPhase.Stationary:
                    held = true;
                    break;
                case ExtTouchPhase.Ended:
                case ExtTouchPhase.Canceled:
                    up = true;
                    break;
                case ExtTouchPhase.None:
                default:
                    break;
            }

            _mouseButtonDown = down;
            _mouseButtonUp = up;
            _mouseButtonHeld = held;
        }

        private bool isVerboseDebugLog()
        {
            return CloudGameInput.IsDebugLog || Debug.isDebugBuild;
        }

        // returns true, if duplicated
        // ReSharper disable once UnusedParameter.Local
        private bool IsUpdateDuplicated(Touch inputTouch, bool isValidTouch)
        {
            var frame = Time.frameCount;
            var extPhase = inputTouch.phase.ToExtPhase(isValidTouch);
            // 兼容处理在同一frame多次track的情况
            if (_nowFrame == frame && extPhase == _nowRawInputPhase)
            {
                var isKeyEvent = extPhase == ExtTouchPhase.Began || extPhase == ExtTouchPhase.Ended;
                if (isKeyEvent)
                    LogTrackInfo();
                return true;
            }

            return false;
        }

        private void StopFingerTouch(ref Touch newTouch, out ExtTouchPhase extPhase)
        {
            var phase = _nowProcessedPhase;
            switch (phase)
            {
                case ExtTouchPhase.None:
                case ExtTouchPhase.Canceled:
                case ExtTouchPhase.Ended:
                    extPhase = ExtTouchPhase.None;
                    newTouch.phase = TouchPhase.Canceled;
                    break;
                case ExtTouchPhase.Began:
                case ExtTouchPhase.Moved:
                case ExtTouchPhase.Stationary:
                default:
                    extPhase = ExtTouchPhase.Ended;
                    newTouch.phase = TouchPhase.Ended;
                    break;
            }
        }

        private void LogTrackInfo()
        {
            if (!isVerboseDebugLog())
                return;

            var frame = Time.frameCount;
            var extPhase = _nowProcessedPhase;
            var touch = _nowTouch;
            var isKeyEvent = extPhase == ExtTouchPhase.Began || extPhase == ExtTouchPhase.Ended;
            var posInfo = extPhase != ExtTouchPhase.None ? touch.position.ToString() : "";
            var down = _mouseButtonDown;
            var up = _mouseButtonUp;
            var held = _mouseButtonHeld;
            if (_isMouseButtonMode)
                posInfo += $" up:{Debug.BoolTo01(up)} d:{Debug.BoolTo01(down)} h:{Debug.BoolTo01(held)}";
            var message = $"{Name} {extPhase} {posInfo} IsUI: {Debug.BoolTo01(IsBeganOverUI)}";
            var changed = message != _trackDebugInfo;
            _trackDebugInfo = message;
            if (changed || isKeyEvent)
                Debug.LogDebug($"{message}, #{frame}f");
        }

        /// <summary>
        /// 当前坐标
        /// </summary>
        public Vector3 GetPosition()
        {
            if (_nowFrame != 0)
                return _nowTouch.position;

            // no data
            return Vector3.zero;
        }

        /// <summary>
        /// 前一次坐标
        /// </summary>
        public Vector3 GetPreviousPosition()
        {
            // has previous tracked, get `previous`
            // 兼容处理，检查有上一帧数据
            if (HasPreviousData())
                return _previousTouch.position;

            // no `previous` data, try new tracked data then
            if (_nowFrame != 0)
                return _nowTouch.position;

            // no `previous` data
            return Vector3.zero;
        }

        public bool HasPreviousData()
        {
            var frame = Time.frameCount;
            if (_previousTouchPhase == ExtTouchPhase.None)
                return false;

            var deltaFrame = frame - _previousFrame;
            return Mathf.Abs(deltaFrame) <= 1;
        }

        /// <summary>
        /// 变化量。 如果没有上一帧数据，返回false，变化量数据无意义。
        /// </summary>
        public bool GetDeltaPosition(Vector3 now, out Vector3 delta)
        {
            var hasPrevious = HasPreviousData();
            if (hasPrevious)
            {
                var previousPosition = GetPreviousPosition();
                delta = now - previousPosition;
                return true;
            }

            delta = Vector3.zero;
            return false;
        }
    }
}
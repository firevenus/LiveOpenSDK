// Copyright (c) Bytedance. All rights reserved.
// Description:

using UnityEngine;
using UnityEngine.UI;

namespace Douyin.LiveOpenSDK.Samples.MultiTouchDebugger
{
    /// <summary>
    /// 基本调试形状
    /// </summary>
    public class BaseDebugShape
    {
        public string name { get; protected set; }
        public int id { get; protected set; }
        public GameObject shapeObject { get; protected set; }
        public int pointerId { get; protected set; }
        public bool isBeganOverUI { get; protected set; }

        public bool fadeOutToDestroy;
        public bool isDestroyed;
        public float fadeOutElapsed;
        public float fadeOutDuration;

        protected readonly Image image;
        protected readonly Text nameText;
        protected readonly Text idText;
        protected readonly Text stateText;

        private const string NameTextName = "NameText";
        private const string IdTextName = "IdText";
        private const string StateTextName = "StateText";

        protected BaseDebugShape(string typeName, int newTouchId, GameObject shape)
        {
            name = $"{typeName}{newTouchId.ToString()}";
            id = newTouchId;
            pointerId = id;
            shapeObject = shape;
            image = shapeObject.GetComponent<Image>();
            if (image == null)
                Debug.LogError("shape Image is null!");

            var nameTextTrans = shapeObject.transform.Find(NameTextName);
            if (nameTextTrans == null)
                Debug.LogError($"shape Text \"{NameTextName}\" is null!");
            nameText = nameTextTrans.GetComponent<Text>();
            nameText.text = name;

            var idTextTrans = shapeObject.transform.Find(IdTextName);
            if (idTextTrans == null)
                Debug.LogError($"shape Text \"{IdTextName}\" is null!");
            idText = idTextTrans.GetComponent<Text>();
            idText.text = id.ToString();

            var stateTextTrans = shapeObject.transform.Find(StateTextName);
            if (stateTextTrans == null)
                Debug.LogError($"shape Text \"{StateTextName}\" is null!");
            stateText = stateTextTrans.GetComponent<Text>();
            stateText.text = "";
        }

        public bool IsOverUI()
        {
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            var isOverUI = eventSystem != null && eventSystem.IsPointerOverGameObject(pointerId);
            return isOverUI;
        }

        // ReSharper disable once MemberCanBeProtected.Global
        public void SetColor(Color color)
        {
            image.color = color;
        }

        public void SetStateText(string text)
        {
            stateText.text = isBeganOverUI ? $"{text}\nOverUI" : text;
        }

        public void UpdateTime(float deltaTime)
        {
            UpdateToDestroy(deltaTime);
        }

        public void Destroy(bool toDestroy, float duration)
        {
            fadeOutToDestroy = toDestroy;
            fadeOutDuration = duration;
        }

        private void UpdateToDestroy(float deltaTime)
        {
            if (!fadeOutToDestroy)
                return;
            if (isDestroyed)
                return;
            fadeOutElapsed += deltaTime;
            var alpha = Mathf.Clamp01(1 - fadeOutElapsed / fadeOutDuration);
            UpdateAlpha(alpha);
            if (fadeOutElapsed > fadeOutDuration)
            {
                isDestroyed = true;
                Object.Destroy(shapeObject);
            }
        }

        protected void ResetElapseTime()
        {
            fadeOutElapsed = 0;
        }

        private void UpdateAlpha(float alpha)
        {
            var components = shapeObject.GetComponentsInChildren<Graphic>();
            foreach (var comp in components)
            {
                var color = comp.color;
                color.a = alpha;
                comp.color = color;
            }
        }
    }

    /// <summary>
    /// 触摸形状
    /// </summary>
    public class TouchShape : BaseDebugShape
    {
        public TouchShape(int touchId, GameObject shape) : base("touch", touchId, shape)
        {
            pointerId = touchId;
            isBeganOverUI = IsOverUI();
            SetDefaultColor();
        }

        // note: purple and blue
        public void SetDefaultColor()
        {
            SetColor(new Color(0.8f, 0f, 1f));
        }

        private readonly Color OverUIColorOffset = new Color(-0.2f, 0.2f, 0.0f, -0.4f);
        private Color OverUIColored(Color color) => isBeganOverUI ? (color + OverUIColorOffset) : color;

        public void SetBeganColor()
        {
            SetColor(OverUIColored(new Color(0.8f, 0f, 1f)));
        }

        public void SetMovedColor()
        {
            SetColor(OverUIColored(new Color(0.2f, 0.6f, 1f)));
        }

        public void SetStayColor()
        {
            SetColor(OverUIColored(new Color(0.0f, 0.4f, 0.8f)));
        }

        public void SetEndedColor()
        {
            SetColor(OverUIColored(new Color(0.8f, 0f, 0.4f)));
        }

        public void SetCanceledColor()
        {
            SetColor(OverUIColored(new Color(0.4f, 0f, 0.6f)));
        }
    }

    /// <summary>
    /// 鼠标形状
    /// </summary>
    public class MouseShape : BaseDebugShape
    {
        public MouseShape(int buttonId, GameObject shape) : base("button", buttonId, shape)
        {
            pointerId = MultiTouchDebugger.ConvertFingerToPointerId(buttonId, true);
            isBeganOverUI = IsOverUI();
            SetDefaultColor();
        }

        public MouseShape(string typeName, int buttonId, GameObject shape) : base(typeName, buttonId, shape)
        {
            pointerId = MultiTouchDebugger.ConvertFingerToPointerId(buttonId, true);
            isBeganOverUI = IsOverUI();
            SetDefaultColor();
        }

        // note: green and yellow
        public void SetDefaultColor()
        {
            SetColor(new Color(0.5f, 1f, 0f));
        }

        protected readonly Color OverUIColorOffset = new Color(-0.2f, -0.2f, 0.0f, -0.4f);
        protected Color OverUIColored(Color color) => isBeganOverUI ? (color + OverUIColorOffset) : color;

        public void SetDownColor()
        {
            SetColor(OverUIColored(new Color(0.5f, 1f, 0f)));
        }

        public void SetHeldColor()
        {
            SetColor(OverUIColored(new Color(1f, 1f, 0f)));
        }

        public void SetUpColor()
        {
            SetColor(OverUIColored(new Color(0.8f, 0.4f, 0f)));
        }
    }

    /// <summary>
    /// 滚轮滚动形状
    /// </summary>
    public class WheelScrollShape : MouseShape
    {
        public bool isPositive;

        public WheelScrollShape(int buttonId, bool isPositive, GameObject shape) : base("scroll", buttonId, shape)
        {
            name = $"scroll{buttonId.ToString()}";
            this.isPositive = isPositive;
            idText.text = "";
            idText.enabled = false;
            var stateTransform = stateText.transform;
            var imageTransform = image.transform;
            var stateScale = stateTransform.localScale.x + 0.2f;
            stateTransform.localScale = new Vector3(stateScale, stateScale, stateScale);
            if (this.isPositive)
            {
                var imageScale = imageTransform.localScale.x + 0.2f;
                imageTransform.localScale = new Vector3(imageScale, imageScale, imageScale);
            }
            else
            {
                var stateRect = stateText.GetComponent<RectTransform>();
                var pos = stateRect.anchoredPosition;
                pos.y -= 48;
                stateRect.anchoredPosition = pos;
            }
        }

        public void SetInputValue(float input)
        {
            ResetElapseTime();
            var state = isPositive ? $"^ +{input:F1}" : $"v {input:F1}";
            SetStateText(state);
            if (isPositive)
                SetPositiveColor();
            else
                SetNegativeColor();
        }

        public void SetPositiveColor()
        {
            SetColor(OverUIColored(new Color(0.4f, 1f, 1f)));
        }

        public void SetNegativeColor()
        {
            SetColor(OverUIColored(new Color(1f, 0.4f, 1f)));
        }
    }
}
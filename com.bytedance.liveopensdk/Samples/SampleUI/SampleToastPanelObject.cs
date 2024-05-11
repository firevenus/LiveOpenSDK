// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Douyin.LiveOpenSDK.Samples.SampleUI
{
    [RequireComponent(typeof(RectTransform))]
    public class SampleToastPanelObject : MonoBehaviour
    {
        private const float maxDuration = 10f;
        public float duration = 1.5f;
        public bool autoClose = true;

        private float _elapsedTime;
        private bool _isElapsedOver;
        private RectTransform _rectTransform;
        private static float s_height = 100;
        private static int s_activeListLimit = 16;
        private static List<SampleToastPanelObject> s_activeList = new List<SampleToastPanelObject>(16);

        private void Start()
        {
            _rectTransform = this.GetComponent<RectTransform>();
            s_height = _rectTransform.rect.height;
            AddToList();
            if (duration > maxDuration)
                duration = maxDuration;
        }

        private void Update()
        {
            if (!autoClose)
                return;
            if (_isElapsedOver)
                return;
            _elapsedTime += Time.deltaTime;
            if (_elapsedTime >= duration)
            {
                _isElapsedOver = true;
                RemoveFromList();
                Destroy(gameObject);
            }
        }

        private void AddToList()
        {
            s_activeList.Insert(0, this);
            while (s_activeList.Count > s_activeListLimit && s_activeListLimit > 0)
            {
                var index = s_activeList.Count - 1;
                s_activeList.RemoveAt(index);
            }

            for (int i = 0; i < s_activeList.Count; i++)
            {
                var toast = s_activeList[i];
                if (toast == null)
                {
                    s_activeList.RemoveAt(i);
                    i -= 1;
                    continue;
                }
                toast.UpdatePositionInList(i);
            }
        }

        private void RemoveFromList()
        {
            var index = s_activeList.IndexOf(this);
            if (index >= 0)
                s_activeList.RemoveAt(index);
        }

        protected void UpdatePositionInList(int index)
        {
            var pos = _rectTransform.anchoredPosition;
            var y = index * s_height;
            pos.y = y;
            _rectTransform.anchoredPosition = pos;
        }
    }
}
// Copyright (c) Bytedance. All rights reserved.
// Description:

using UnityEngine;
using UnityEngine.UI;

namespace Douyin.LiveOpenSDK.Samples.SampleUI
{
    public class SampleToastUI : MonoBehaviour
    {
        public static SampleToastUI Instance;

        [SerializeField] private GameObject toastPanelPrefab;
        [SerializeField] private RectTransform showAtUITransform;
        [SerializeField] private float duration = 1.5f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            if (showAtUITransform == null)
                Debug.LogError("showAtCanvas == null!");

            if (toastPanelPrefab == null)
                Debug.LogError("toastUIPrefab == null!");
        }

        public SampleToastPanelObject ShowToast(string message)
        {
            if (showAtUITransform == null) return null;
            if (toastPanelPrefab == null) return null;

            Debug.Log($"toast: {message} #{Time.frameCount}f");
            return CreateToastUIObject(message);
        }

        private SampleToastPanelObject CreateToastUIObject(string message)
        {
            var go = Instantiate(toastPanelPrefab, showAtUITransform);
            var toastPanel = go.GetComponent<SampleToastPanelObject>();
            toastPanel.duration = duration;
            toastPanel.autoClose = true;

            var uiText = go.GetComponentInChildren<Text>();
            uiText.text = message;
            uiText.gameObject.SetActive(true);
            return toastPanel;
        }
    }
}
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Douyin.LiveOpenSDK.Samples
{
    public class SampleGameStartup : MonoBehaviour
    {
        public RectTransform SelectModesPanel;
        public Button Button_ModeBack;
        public Button Button_Mode1;
        public Toggle Toggle_APIDebug;

        public RectTransform LogHintPanel;
        public Text LogText;


        /// <summary>
        /// Sample弹幕API接入
        /// </summary>
        private SampleLiveOpenSdkManager LiveOpenSdkManager { get; set; }

        /// <summary>
        /// Sample抖音云API接入
        /// </summary>
        private SampleDyCloudManager DyCloudManager { get; set; }

        private void Awake()
        {
            Debug.Log("SampleGameStartup.Awake");
            LiveOpenSdkManager = new SampleLiveOpenSdkManager();
            DyCloudManager = LiveOpenSdkManager.DyCloudManager;
        }

        // Start is called before the first frame update
        void Start()
        {
            Debug.Log("SampleGameStartup.Start");

            // 初始化直播开放 SDK
            LiveOpenSdkManager.OnCreate();
            InitEvents();
        }

        private void InitEvents()
        {
            ShowUI_ModeSelectPanel(true);
            Button_ModeBack.onClick.AddListener(() => { ShowUI_ModeSelectPanel(true); });
            Button_Mode1.onClick.AddListener(() =>
            {
                // 抖音云 一键开始全部
                Debug.Log($"{nameof(Button_Mode1)}.onClick");
                ShowUI_ModeSelectPanel(false);
                DyCloudManager.IsDebug = Toggle_APIDebug.isOn;
                DyCloudManager.IsDebug = Toggle_APIDebug.isOn;
                DyCloudManager.StartAllInOne();
            });

            Application.logMessageReceived += OnApplicationLogMessageReceived;
        }

        private void OnDestroy()
        {
            Debug.Log("SampleGameStartup.OnDestroy");
            Application.logMessageReceived -= OnApplicationLogMessageReceived;

            // 销毁直播开放 SDK
            LiveOpenSdkManager.OnDestroy();
        }

        private void OnApplicationLogMessageReceived(string condition, string stacktrace, LogType type)
        {
            LogText.text =
                $"last log {DateTime.Now.ToString("HH:mm:ss.fff")} (#{Time.frameCount}f):\n{condition}\n{stacktrace}";
        }

        private void ShowUI_ModeSelectPanel(bool show)
        {
            SelectModesPanel.gameObject.SetActive(show);
            LogHintPanel.gameObject.SetActive(!show);
            Button_ModeBack.gameObject.SetActive(!show);
        }

        // Update is called once per frame
        void Update()
        {
        }
    }
}
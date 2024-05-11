using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Douyin.LiveOpenSDK.Plugins.LitJson;
using Debug = UnityEngine.Debug;

namespace Douyin.LiveOpenSDK.Utilities
{
    internal static class SdkTeaConsts
    {
        #region 事件名称

        internal const string INIT_START = "lo_init_start";
        internal const string INIT_RESULT = "lo_init_result";

        internal const string CALL_CONTAINER_START = "lo_call_container_start";
        internal const string CALL_CONTAINER_RESULT = "lo_call_container_result";

        internal const string CONNECT_CONTAINER_START = "lo_connect_container_start";
        internal const string CONNECT_CONTAINER_RESULT = "lo_connect_container_result";

        internal const string CLOSE_WEBSOCKET = "lo_close_websocket";

        internal const string REQUEST_WEBCAST_INFO_START = "lo_request_webcast_info_start";
        internal const string REQUEST_WEBCAST_INFO_RESULT = "lo_request_webcast_info_result";

        #endregion


        #region 参数名称

        internal const string APP_ID = "app_id";
        internal const string ENV_ID = "env_id";
        internal const string SERVICE_ID = "service_id";
        internal const string IS_CLOUD = "is_cloud";
        internal const string UNITY_VERSION = "unity_version";
        internal const string SDK_VERSION = "sdk_version";
        internal const string SCRIPTING_BACKEND = "scripting_backend";

        internal const string PATH = "path";
        internal const string METHOD = "method";
        internal const string DURATION = "duration";
        internal const string ERROR_CODE = "error_code";
        internal const string ERROR_MSG = "error_msg";

        #endregion
    }

    internal interface IReportItem: IDisposable
    {
        Dictionary<string, JsonData> SpecialParams { get; }
        long Duration { get; }
    }

    class CommonReportItem: IReportItem
    {
        private static uint _idGenerator = 0;

        private readonly uint _id;   // for debug one day.
        private Stopwatch _sw;
        private Dictionary<string, JsonData> _specialParams;

        internal CommonReportItem(Dictionary<string, JsonData> specialParams)
        {
            _id = _idGenerator++;
            _sw = new Stopwatch();
            _sw.Start();
            _specialParams = specialParams;
            if (Debug.isDebugBuild)
            {
                Debug.Log($"ReportItem [{_id}] online.");
            }
        }

        public Dictionary<string, JsonData> SpecialParams => _specialParams;

        public long Duration
        {
            get
            {
                _sw.Stop();
                return _sw.ElapsedMilliseconds;
            }
        }

        public void Dispose()
        {
            if (Debug.isDebugBuild)
            {
                Debug.Log($"ReportItem [{_id}] dispose.");
            }
            _sw.Stop();
            _sw = null;
            _specialParams?.Clear();
            _specialParams = null;
        }
    }

    internal class SdkTeaReporter
    {
        private static SdkTeaReporter _instance;

        internal static SdkTeaReporter Instance {
            get
            {
                if (_instance == null)
                {
                    _instance = new SdkTeaReporter();
                    _instance._teaSdk.Init(580801, SdkUtils.GetHashDeviceId(), "cn", Application.version);
                }
                return _instance;
            }
        }
        private static SdkDebugLogger Debug => LiveOpenSDK.Debug;

        private readonly TeaSdk _teaSdk = new TeaSdk();

        private bool _didStartupInfo;

        private string _defaultServiceId;

        private readonly Dictionary<string, JsonData> _commonParams = new Dictionary<string, JsonData>();

        private IReportItem _lastConnectContainerItem;
        private IReportItem _lastWebcastInfoItem;

        private void SetCommonParams(string appId, string envId, string serviceId)
        {
            _commonParams.Clear();
            _commonParams.Add(SdkTeaConsts.APP_ID, appId);
            _commonParams.Add(SdkTeaConsts.ENV_ID, envId);
            _commonParams.Add(SdkTeaConsts.SERVICE_ID, serviceId);
            _commonParams.Add(SdkTeaConsts.IS_CLOUD, IsCloud() ? 1 : 0);
            _commonParams.Add(SdkTeaConsts.UNITY_VERSION, Application.unityVersion);
            _commonParams.Add(SdkTeaConsts.SDK_VERSION, SdkVersion.Version);
#if ENABLE_MONO
            string scriptingBackend = "mono";
#elif ENABLE_IL2CPP
            string scriptingBackend = "il2cpp";
#else
            string scriptingBackend = "unknown";
#endif
            _commonParams.Add(SdkTeaConsts.SCRIPTING_BACKEND, scriptingBackend);
        }

        private bool IsCloud()
        {
            return LiveOpenSDK.CloudGameAPI.IsCloudGame();
        }

        private JsonData CreateParamsData(Dictionary<string, JsonData> specialParams = null)
        {
            var json = new JsonData();
            InsertParamsData(json, _commonParams);
            if (specialParams != null)
            {
                InsertParamsData(json, specialParams);
            }
            return json;
        }

        private void InsertParamsData(JsonData json, Dictionary<string, JsonData> paramsDic)
        {
            foreach (var kvp in paramsDic)
            {
                json[kvp.Key] = kvp.Value;
            }
        }

        // 开始初始化
        public IReportItem InitStart(string appId, string envId, string serviceId)
        {
            _defaultServiceId = serviceId;
            SetCommonParams(appId, envId, serviceId);
            Debug.LogDebug(
                $"Report [{nameof(InitStart)}] appId: {appId}, envId: {envId}, serviceId: {serviceId}");
            var tmpDic = CreateParamsData();
            _teaSdk.Collect(SdkTeaConsts.INIT_START, tmpDic.ToJson(), null);
            return new CommonReportItem(null);
        }

        // 初始化结果
        public void InitResult(IReportItem item, int errCode, string errMsg)
        {
            var duration = item.Duration;
            Debug.LogDebug($"Report [{nameof(InitResult)}] duration: {duration}, errCode: {errCode}, errMsg: {errMsg}");
            var tmpDic = CreateParamsData();
            tmpDic[SdkTeaConsts.DURATION] = duration;
            tmpDic[SdkTeaConsts.ERROR_CODE] = errCode;
            tmpDic[SdkTeaConsts.ERROR_MSG] = errMsg;
            _teaSdk.Collect(SdkTeaConsts.INIT_RESULT, tmpDic.ToJson(), null);
            item.Dispose();
        }

        // 开始执行 CallContainer
        public IReportItem CallContainerStart(string path, string method, string serviceId)
        {
            if (string.IsNullOrEmpty(serviceId))
            {
                serviceId = _defaultServiceId;
            }
            Debug.LogDebug($"Report [{nameof(CallContainerStart)}] path: {path}, method: {method}");
            var specialParams = new Dictionary<string, JsonData>()
            {
                { SdkTeaConsts.PATH, path },
                { SdkTeaConsts.METHOD, method },
                { SdkTeaConsts.SERVICE_ID, serviceId },
            };
            var tmpDic = CreateParamsData(specialParams);
            _teaSdk.Collect(SdkTeaConsts.CALL_CONTAINER_START, tmpDic.ToJson(), null);
            return new CommonReportItem(specialParams);
        }

        // CallContainer 的执行结果
        public void CallContainerResult(IReportItem item, int errCode, string errMsg)
        {
            var duration = item.Duration;
            Debug.LogDebug($"Report [{nameof(CallContainerResult)}] duration: {duration}, errCode: {errCode}, errMsg: {errMsg}");
            var tmpDic = CreateParamsData(item.SpecialParams);
            tmpDic[SdkTeaConsts.DURATION] = duration;
            tmpDic[SdkTeaConsts.ERROR_CODE] = errCode;
            tmpDic[SdkTeaConsts.ERROR_MSG] = errMsg;
            _teaSdk.Collect(SdkTeaConsts.CALL_CONTAINER_RESULT, tmpDic.ToJson(), null);
            item.Dispose();
        }

        // 关闭 WebSocket 连接
        public void CloseWebSocket()
        {
            Debug.LogDebug($"Report [{nameof(CloseWebSocket)}]");
            var tmpDic = CreateParamsData();
            _teaSdk.Collect(SdkTeaConsts.CLOSE_WEBSOCKET, tmpDic.ToJson(), null);
        }

        // 开始执行 ConnectContainer
        public void ConnectContainerStart(string path, string serviceId)
        {
            Debug.LogDebug($"Report [{nameof(ConnectContainerStart)}] path: {path}, method: {serviceId}");
            var specialParams = new Dictionary<string, JsonData>()
            {
                { SdkTeaConsts.PATH, path },
                { SdkTeaConsts.SERVICE_ID, serviceId },
            };
            var tmpDic = CreateParamsData(specialParams);
            _teaSdk.Collect(SdkTeaConsts.CONNECT_CONTAINER_START, tmpDic.ToJson(), null);
            var item = new CommonReportItem(specialParams);
            if (_lastConnectContainerItem != null)
            {
                // Dispose prev item for safety.
                _lastConnectContainerItem.Dispose();
            }
            _lastConnectContainerItem = item;
        }

        // ConnectContainer 的执行结果
        public void ConnectContainerResult(int errCode,string errMsg)
        {
            if (_lastConnectContainerItem == null)
            {
                // prev report event not set, illegal report here.
                return;
            }
            var duration = _lastConnectContainerItem.Duration;
            Debug.LogDebug(
                $"Report [{nameof(ConnectContainerResult)}] duration: {duration}, errCode: {errCode}, errMsg: {errMsg}");
            var tmpDic = CreateParamsData(_lastConnectContainerItem.SpecialParams);
            tmpDic[SdkTeaConsts.DURATION] = duration;
            tmpDic[SdkTeaConsts.ERROR_CODE] = errCode;
            tmpDic[SdkTeaConsts.ERROR_MSG] = errMsg;
            _teaSdk.Collect(SdkTeaConsts.CONNECT_CONTAINER_RESULT, tmpDic.ToJson(), null);
            _lastConnectContainerItem.Dispose();
            _lastConnectContainerItem = null;
        }

        // 内部开始执行 RequestWebcastInfo
        public void RequestWebcastInfoStart()
        {
            Debug.LogDebug($"Report [{nameof(RequestWebcastInfoStart)}]");
            var tmpDic = CreateParamsData();
            _teaSdk.Collect(SdkTeaConsts.REQUEST_WEBCAST_INFO_START, tmpDic.ToJson(), null);
            var item = new CommonReportItem(null);
            if (_lastWebcastInfoItem != null)
            {
                // Dispose prev item for safety.
                _lastWebcastInfoItem.Dispose();
            }
            _lastWebcastInfoItem = item;
        }

        // RequestWebcastInfo 的执行结果
        public void RequestWebcastInfoResult(int errCode, string errMsg)
        {
            if (_lastWebcastInfoItem == null)
            {
                // prev report event not set, illegal report here.
                return;
            }
            var duration = _lastWebcastInfoItem.Duration;
            Debug.LogDebug($"Report [{nameof(RequestWebcastInfoResult)}] duration: {duration}, errCode: {errCode}, errMsg: {errMsg}");
            var tmpDic = CreateParamsData();
            tmpDic[SdkTeaConsts.DURATION] = duration;
            tmpDic[SdkTeaConsts.ERROR_CODE] = errCode;
            tmpDic[SdkTeaConsts.ERROR_MSG] = errMsg;
            _teaSdk.Collect(SdkTeaConsts.REQUEST_WEBCAST_INFO_RESULT, tmpDic.ToJson(), null);
            _lastWebcastInfoItem.Dispose();
            _lastWebcastInfoItem = null;
        }

        // Copy from StarkLive, not used.
        // private class StarkTeaDebugProvider : ITeaDataProvider
        // {
        //     public string TestDeviceId => "StarkTeaDebugProvider";
        //     private Dictionary<string, object> m_CustomValues = new Dictionary<string, object>();
        //     public Dictionary<string, object> CustomValues => m_CustomValues;
        // }
    }
}
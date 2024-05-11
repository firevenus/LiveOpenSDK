// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ByteDance.LiveOpenSdk;
using dyCloudUnitySDK;
using Newtonsoft.Json;
using UnityEngine;
using utils = dyCloudUnitySDK.utils;

namespace Douyin.LiveOpenSDK.Utilities
{
    internal static class SdkUtils
    {
        internal static SdkDebugLogger Debug => LiveOpenSDK.Debug;

        internal static string GetUnityDeviceId()
        {
            return SystemInfo.deviceUniqueIdentifier;
        }

        private static string s_hashDeviceId;

        internal static string GetHashDeviceId()
        {
            if (!string.IsNullOrEmpty(s_hashDeviceId))
                return s_hashDeviceId;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            // note: 在一些主机、例如tb云主机上，可能主板和bios的序列号为空、且windows序列号为相同，导致 unity api 取到的 did 相同
            // note: 因此我们这里hash增加一些计算因子
            // 1. unity api did
            var unity_did = GetUnityDeviceId();
            // 2. windows 设备名
            var name = SystemInfo.deviceName;
            // 3. 设备型号
            var model = SystemInfo.deviceModel;
            // 4. windows 设备ID。 即Windows系统信息(about)里的"设备ID"
            var ad_id = "";
#if UNITY_WSA
            // note: `ad_id`: 需要设备上打开隐私设置的允许广告id（PC Settings -> Privacy -> Let apps use my advertising ID）, 否则为空
            ad_id = UnityEngine.WSA.Application.advertisingIdentifier;
#endif
            var hash = Hash128.Compute($"{unity_did}_{name}_{model}_{ad_id}");
            var did = hash.ToString();
#else
            var did = GetUnityDeviceId();
#endif
            s_hashDeviceId = did;
            return did;
        }

        [DebuggerStepThrough]
        public static string ToJsonString(Dictionary<string, string> stringMap)
        {
            return JsonConvert.SerializeObject(stringMap);
        }

        [DebuggerStepThrough]
        public static string ToJsonString(DYCloudHttpResponse res)
        {
            // var headersJson = ToString(res.headers);
            // return $"statusCode: {res.statusCode}\nbody: {res.body}\nheaders: {headersJson}";
            return JsonConvert.SerializeObject(res);
        }

        [DebuggerStepThrough]
        public static string ToJsonString(object obj, bool indent = false)
        {
            return JsonConvert.SerializeObject(obj, indent ? Formatting.Indented : Formatting.None);
        }

        [DebuggerStepThrough]
        public static string ToJsonString(object obj, bool indent, JsonSerializerSettings settings)
        {
            return JsonConvert.SerializeObject(obj, indent ? Formatting.Indented : Formatting.None, settings);
        }

        public static JsonSerializerSettings JsonIgnoreNullSettings => new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        public static T FromJsonString<T>(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return default;
            }
        }

        public static int ToIntStatusCode(long longRespCode)
        {
            try
            {
                var intValue = Convert.ToInt32(longRespCode);
                return intValue;
            }
            catch (OverflowException e)
            {
                Debug.LogWarning(e.Message);
                return APICode.ExceptionError;
            }
        }

        private static bool _isPpe;

        internal static bool IsPPE
        {
            get => _isPpe;
            set
            {
                if (_isPpe == value) return;
                Debug.Log($"IsPPE = {value}");
                _isPpe = value;
                SetDYCloudHeadersEnv();
                SetHeadersEnv(LiveOpenSdk.Env.HttpHeaders);
            }
        }

        internal static void SetHeadersEnv(Dictionary<string, string> headers)
        {
            if (!IsPPE) return;
            if (headers == null) return;
            headers["x-tt-env"] = "ppe_liveplays_sdk";
            headers["x-use-ppe"] = "1";
        }

        private static void SetDYCloudHeadersEnv()
        {
            if (IsPPE)
            {
                var headers = new Dictionary<string, string>();
                SetHeadersEnv(headers);
                utils.SetRequestBaseHeaders(headers);
            }
            else
            {
                utils.SetRequestBaseHeaders(null);
            }
        }

        public static void OpenLocalFile(string filePath)
        {
            Debug.LogDebug($"{nameof(OpenLocalFile)} {filePath}");
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogError("arg `filePath` is empty");
                return;
            }

            try
            {
                System.Diagnostics.Process.Start(filePath);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public static void RevealLocalFile(string filePath)
        {
            Debug.LogDebug($"{nameof(RevealLocalFile)} {filePath}");
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogError("arg `filePath` is empty");
                return;
            }

            try
            {
                if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
                {
                    string argument = "/select," + System.IO.Path.GetFullPath(filePath);
                    System.Diagnostics.Process.Start("explorer.exe", argument);
                }
                else
                {
                    Debug.LogWarning("RevealLocalFile only works on Windows.");
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
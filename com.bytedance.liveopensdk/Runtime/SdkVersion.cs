// Copyright (c) Bytedance. All rights reserved.
// Description:

using UnityEngine;

namespace Douyin.LiveOpenSDK
{
    /// <summary>
    /// 提供Sdk Version信息
    /// </summary>
    [UnityEngine.Scripting.Preserve]
    public static class SdkVersion
    {
        /// <summary>
        /// 版本号
        /// </summary>
        [UnityEngine.Scripting.Preserve]
        public static string Version => _version;

        [UnityEngine.Scripting.Preserve]
        internal static void LogPluginVersion()
        {
            Debug.Log("LiveOpenSDK Version: " + Version);
        }

        private const string _version = "1.3.0";
    }
}
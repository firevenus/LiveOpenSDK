// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Douyin.LiveOpenSDK.Utilities
{
    public class SdkDebugInfo
    {
        internal static SdkDebugLogger Debug => LiveOpenSDK.Debug;

        public void LogDebugVer()
        {
            try
            {
                var sdkVer = LiveOpenSDK.Version;
                var sdkVerInfo = $"LiveOpenSDK ver: {sdkVer}";
                var gameVer = Application.version;
                var gameVerInfo = $"GameApp ver: {gameVer} name: {Application.productName}" +
                                  $"    {Application.platform} - {Application.unityVersion}";
                Debug.Log(sdkVerInfo);
                Debug.Log(gameVerInfo);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void LogDebugEnvs()
        {
            try
            {
                var lines = _GetPathLines();

                lines.Add("---- env vars ----");
                var envLines = _GetEnvLines();
                lines.AddRange(envLines);
                var text = string.Join("\n", lines);
                Debug.LogDebug(text);
                if (IsPC() && !Application.isEditor)
                {
                    var path = "envs.log";
                    File.WriteAllText(path, text);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static List<string> _GetPathLines()
        {
            var lines = new List<string>();

            lines.Add("---- current path ----");
            lines.Add($"System.Environment.CurrentDirectory: {TryGet(() => Environment.CurrentDirectory)}");
            lines.Add($"System.IO.Directory.GetCurrentDirectory: {TryGet(Directory.GetCurrentDirectory)}");
            lines.Add("---- unity app path ----");
            lines.Add($"unity Application.dataPath: {Application.dataPath}");
            lines.Add($"unity Application.persistentDataPath: {Application.persistentDataPath}");
            lines.Add($"unity Application.consoleLogPath: {Application.consoleLogPath}");
            lines.Add($"unity Application.streamingAssetsPath: {Application.streamingAssetsPath}");
            lines.Add("---- assembly path ----");
            lines.Add($"CurrentDomain.BaseDirectory: {TryGet(() => AppDomain.CurrentDomain?.BaseDirectory)}");
            lines.Add($"ExecutingAssembly().Location: {TryGet(() => Assembly.GetExecutingAssembly()?.Location)}");
            lines.Add($"EntryAssembly().Location: {TryGet(() => Assembly.GetEntryAssembly()?.Location)}");
            return lines;
        }

        private static string TryGet(Func<string> getter)
        {
            try
            {
                return getter.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.ToString());
            }

            return string.Empty;
        }

        private static List<string> _GetEnvLines()
        {
            var envLines = new List<string>();
            var envVars = Environment.GetEnvironmentVariables();

            foreach (DictionaryEntry de in envVars)
            {
                var key = $"{de.Key}";
                var value = $"{de.Value}";
                var compare = string.Compare(key, "path", StringComparison.OrdinalIgnoreCase);
                if (compare == 0)
                {
                    value = value.Replace(";", ";\n");
                }

                envLines.Add($"env {key} = {value}");
            }

            envLines.Sort(); // sort by alphabet
            for (int i = 0; i < envLines.Count; i++)
            {
                envLines[i] = $" #{i} " + envLines[i];
            }

            envLines.Insert(0, $"env vars: {envLines.Count}");
            return envLines;
        }

        public void LogDebugCmdArgs()
        {
            try
            {
                var commandline = Environment.CommandLine;
                Debug.LogDebug($"GetLaunchToken CommandLine: {commandline}");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        internal void LogDebugDid()
        {
            try
            {
                var hash_did = SdkUtils.GetHashDeviceId();
                var isPC = IsPC();
                if (isPC)
                {
                    var unity_did = SdkUtils.GetUnityDeviceId();
                    var name = SystemInfo.deviceName;
                    var model = SystemInfo.deviceModel;
                    var ad_id = "";
#if UNITY_WSA
                    // note: `ad_id`: 需要设备上打开隐私设置的允许广告id（PC Settings -> Privacy -> Let apps use my advertising ID）, 否则为空
                    ad_id = UnityEngine.WSA.Application.advertisingIdentifier;
#endif
                    Debug.LogDebug($"hash_did: {hash_did}, unity_did: {unity_did}, name: {name}, model: {model}, ad_id: {ad_id}");
                }
                else
                {
                    Debug.LogDebug($"did: {hash_did}");
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        internal bool IsPC()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var isPC = true;
#else
            var isPC = false;
#endif
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            return isPC;
        }
    }
}
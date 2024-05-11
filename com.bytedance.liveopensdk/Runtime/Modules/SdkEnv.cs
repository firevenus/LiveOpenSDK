// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using System.Collections.Generic;
using Douyin.LiveOpenSDK.Data;
using Douyin.LiveOpenSDK.Utilities;
using UnityEngine;

namespace Douyin.LiveOpenSDK.Modules
{
    public class SdkEnv
    {
        internal const string TAG = nameof(LiveOpenSDK);

        internal const string ArgMobile = "-mobile";
        internal const string ArgCloudGame = "-cloud-game";
        internal const string ArgScreenFullscreen = "-screen-fullscreen";
        internal const string ArgScreenHeight = "-screen-height";
        internal const string ArgScreenWidth = "-screen-width";

        private static SdkDebugLogger Debug => LiveOpenSDK.Debug;
        private static string s_startTokenCached;
        private static Dictionary<string, int> s_cloudGameArgs;
        private static bool s_hasSetFullscreen;

        internal string GetLaunchToken(bool cache = true)
        {
            if (cache && !string.IsNullOrEmpty(s_startTokenCached))
                return s_startTokenCached;

            var commandline = Environment.CommandLine;
            Debug.LogDebug($"GetLaunchToken CommandLine: {commandline}");

            var args = Environment.GetCommandLineArgs();
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i].Trim();
                if (arg.StartsWith(ConstsInternal.LaunchCmdArg_Token))
                {
                    var startToken = arg.Substring(ConstsInternal.LaunchCmdArg_Token.Length);
                    s_startTokenCached = startToken;
                    return startToken;
                }
            }

            if (Application.isEditor)
                Debug.LogDebug($"launch token not found. args len = {args.Length}");
            else
                Debug.LogWarning($"launch token not found. args len = {args.Length}");

            return "";
        }

        private static bool HasCmdArgToken()
        {
            string[] args = Environment.GetCommandLineArgs();
            foreach (var t in args)
            {
                if (t.StartsWith("-token="))
                {
                    return true;
                }
            }

            return false;
        }

        internal int GetCloudGameArgValue(string key)
        {
            ReadCloudGameArgs();
            if (s_cloudGameArgs.TryGetValue(key, out int value))
                return value;
            return 0;
        }

        // for self test or mock
        internal void SetCloudGameArgValue(string key, int value)
        {
            ReadCloudGameArgs();
            s_cloudGameArgs[key] = value;
        }

        internal void ReadCloudGameArgs(bool cache = true)
        {
            if (cache && s_cloudGameArgs != null) return;

            var logs = new List<string>();
            var keyValues = new Dictionary<string, int>();
            var argKeys = new HashSet<string>
            {
                ArgScreenFullscreen,
                ArgScreenHeight,
                ArgScreenWidth,
                ArgCloudGame,
                ArgMobile
            };

            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                var key = args[i];
                if (argKeys.Contains(key))
                {
                    if (int.TryParse(args[i + 1], out var intVar))
                    {
                        keyValues.Add(key, intVar);
                        logs.Add($"{key} {intVar}");
                    }
                }
            }

            s_cloudGameArgs = keyValues;
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (logs.Count > 0)
                Debug.LogDebug($"launch cloud game args: {string.Join(", ", logs)}");
            else
                Debug.LogDebug("launch cloud game args, not found");
        }

        internal bool IsStartFromCloud()
        {
            var cloudGame = GetCloudGameArgValue(ArgCloudGame);
            return cloudGame == 1;
        }

        internal bool IsStartFromMobile()
        {
            var mobile = GetCloudGameArgValue(ArgMobile);
            return mobile == 1;
        }

        internal bool IsStartFromMobileBanLv()
        {
            var hasToken = HasCmdArgToken();
            var mobile = GetCloudGameArgValue(ArgMobile);
            return hasToken && mobile == 1;
        }

        internal bool IsStartFromPCBanLv()
        {
            var hasToken = HasCmdArgToken();
            var mobile = GetCloudGameArgValue(ArgMobile);
            return hasToken && mobile == 0;
        }

        internal bool TryInitCloudGameScreen()
        {
            ReadCloudGameArgs();
            if (Application.isEditor)
                return false;
            var cloudGame = GetCloudGameArgValue(ArgCloudGame);
            var fullscreen = GetCloudGameArgValue(ArgScreenFullscreen);
            if (cloudGame == 1 && fullscreen == 1)
            {
                try
                {
                    // ReSharper disable once RedundantNameQualifier
                    var prev = UnityEngine.Screen.fullScreen;
                    // ReSharper disable once RedundantNameQualifier
                    UnityEngine.Screen.fullScreen = true;

                    if (!s_hasSetFullscreen)
                        Debug.Log(!prev ? "cloud game set fullScreen = true" : "cloud game set fullScreen = true, already true");
                    if (!Screen.fullScreen)
                        Debug.LogError("Error unexpected: `fullScreen` is still `false` after set true!");
                }
                catch (Exception e)
                {
                    // ReSharper disable once RedundantToStringCall
                    Debug.LogError("Error unexpected: " + e.ToString());
                }

                s_hasSetFullscreen = true;
                return true;
            }

            return false;
        }
    }
}
// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using ByteDance.LiveOpenSdk;
using ByteDance.LiveOpenSdk.Logging;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Douyin.LiveOpenSDK.Integration
{
    public static class UnityLogger
    {
        private const string ColorTag = "#80a0ff";

        public static event Action<Severity, string>? OnRichLog;

        public static void WriteLog(LogItem item)
        {
            var (severity, tag, message) = item;
            var logType = severity switch
            {
                Severity.Verbose => LogType.Log,
                Severity.Debug => LogType.Log,
                Severity.Info => LogType.Log,
                Severity.Warning => LogType.Warning,
                Severity.Error => LogType.Error,
                _ => LogType.Error
            };
            var logStr =
                $"{MakeColor($"[{GetInitial(severity)}]", severity)} {MakeColor($"[{tag}]", ColorTag)} {MakeColor(message, severity)}";

            var context = LiveOpenSdk.DefaultSynchronizationContext;
            if (context != null)
            {
                context.Post(PerformLog, null);
            }
            else
            {
                PerformLog(null);
            }

            return;

            void PerformLog(object? _)
            {
                Debug.unityLogger.Log(logType, logStr);
                OnRichLog?.Invoke(severity, logStr);
            }
        }

        private static string GetInitial(Severity severity)
        {
            return severity switch
            {
                Severity.Verbose => "V",
                Severity.Debug => "D",
                Severity.Info => "I",
                Severity.Warning => "W",
                Severity.Error => "E",
                _ => severity.ToString()
            };
        }

        private static string GetColor(Severity severity)
        {
            return severity switch
            {
                Severity.Verbose => "#A6A6A6",
                Severity.Debug => "#BFBFBF",
                Severity.Info => "#99BF99",
                Severity.Warning => "#BfB42F",
                Severity.Error => "#BF2D2D",
                _ => "#BF2D2D"
            };
        }

        private static string MakeColor(string text, string hexColor)
        {
            return $"<color={hexColor}>{text}</color>";
        }

        private static string MakeColor(string text, Severity severity)
        {
            return MakeColor(text, GetColor(severity));
        }
    }
}
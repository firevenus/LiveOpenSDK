// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;

namespace Douyin.LiveOpenSDK.Utilities
{
    public static class APIStateExtension
    {
        public static bool IsNone(this APIState state)
        {
            return state switch
            {
                APIState.None => true,
                _ => false
            };
        }

        public static bool IsInProgress(this APIState state)
        {
            return state switch
            {
                APIState.InProgress => true,
                _ => false
            };
        }

        public static bool IsInProgressOrSuccess(this APIState state)
        {
            return state switch
            {
                APIState.InProgress => true,
                APIState.Success => true,
                _ => false
            };
        }

        public static bool IsDone(this APIState state)
        {
            return state switch
            {
                APIState.Success => true,
                APIState.Failed => true,
                _ => false
            };
        }
    }

    public static class RespSourceTypeExtension
    {
        public static string ToName(this RespSourceType type)
        {
            return Enum.GetName(typeof(RespSourceType), type);
        }

        public static int ToInt(this RespSourceType type)
        {
            return (int) type;
        }
    }

    public static class LogLevelExtension
    {
        public static UnityEngine.LogType ToLogType(this LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => UnityEngine.LogType.Log,
                LogLevel.Info => UnityEngine.LogType.Log,
                LogLevel.Warning => UnityEngine.LogType.Warning,
                LogLevel.Error => UnityEngine.LogType.Error,
                LogLevel.Exception => UnityEngine.LogType.Exception,
                _ => UnityEngine.LogType.Log
            };
        }
    }

    public static class StringExtension
    {
        public static string SafeSubstring(this string input, int startIndex, int length)
        {
            if (input == null)
                return string.Empty;
            if (length < 0)
                return string.Empty;

            // safe is to: avoid ArgumentOutOfRangeException: startIndex is less than zero or greater than the length of this instance.
            if (startIndex < 0)
                startIndex = 0;
            else if (startIndex >= input.Length)
                return string.Empty;

            if (startIndex + length > input.Length)
            {
                length = input.Length - startIndex;
            }

            return input.Substring(startIndex, length);
        }
    }
}
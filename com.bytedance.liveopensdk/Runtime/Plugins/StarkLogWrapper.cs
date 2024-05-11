// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using System.Runtime.CompilerServices;
using Douyin.LiveOpenSDK.Utilities;

namespace Douyin.LiveOpenSDK.Plugins
{
    public static class StarkLog
    {
        // info
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Log(string tag, string msg) => UnityEngine.Debug.Log(TimeUtil.NowTime + " [" + tag + "] " + msg);

        // debug
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogDebug(string tag, string msg) => UnityEngine.Debug.Log(TimeUtil.NowTime + " [" + tag + "] " + msg);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogWarning(string tag, string msg) => UnityEngine.Debug.LogWarning(TimeUtil.NowTime + " [" + tag + "] " + msg);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogError(string tag, string msg) => UnityEngine.Debug.LogError(TimeUtil.NowTime + " [" + tag + "] " + msg);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogException(Exception exception) => UnityEngine.Debug.LogException(exception);
    }
}
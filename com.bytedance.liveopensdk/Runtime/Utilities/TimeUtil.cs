// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;

namespace Douyin.LiveOpenSDK.Utilities
{
    public static class TimeUtil
    {
        /// <summary>
        /// 当前时间字符串。通常用于日志、调试信息。
        /// </summary>
        /// <remarks>注意：外部使用时，不要依赖此具体格式结构、或解析其成分。</remarks>
        public static string NowTime => DateTime.Now.ToString("HH:mm:ss.fff");

        /// <summary>
        /// 当前日期时间字符串。通常用于日志、调试信息。
        /// </summary>
        /// <remarks>注意：外部使用时，不要依赖此具体格式结构、或解析其成分。</remarks>
        public static string NowDateTime => DateTime.Now.ToString("MM-dd HH:mm:ss.fff");

        /// <summary>
        /// 当前Unix时间戳 (UTC)
        /// </summary>
        public static long NowTimestamp => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>
        /// 当前Unix毫秒级时间戳 (UTC)
        /// </summary>
        public static long NowTimestampMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
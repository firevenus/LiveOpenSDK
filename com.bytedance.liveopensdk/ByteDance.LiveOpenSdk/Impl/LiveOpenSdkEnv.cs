// Copyright (c) Bytedance. All rights reserved.
// Description:

using System.Collections.Generic;

namespace ByteDance.LiveOpenSdk
{
    /// <summary>
    /// 直播开放 SDK 的环境公共参数。
    /// </summary>
    public sealed class LiveOpenSdkEnv
    {
        /// <summary>
        /// 小玩法的 AppId。
        /// </summary>
        public string AppId { get; set; } = "";

        /// <summary>
        /// 直播伴侣启动或云启动时提供的访问令牌。
        /// </summary>
        public string Token { get; set; } = "";

        /// <summary>
        /// 额外的 HTTP 请求标头。
        /// </summary>
        /// <remarks>仅供 SDK 内部使用的字段。</remarks>
        public Dictionary<string, string> HttpHeaders { get; set; } = new Dictionary<string, string>();
    }
}
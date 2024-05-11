// Copyright (c) Bytedance. All rights reserved.
// Description:

using Douyin.LiveOpenSDK.Data;
using Douyin.LiveOpenSDK.Utilities;
using UnityEngine;

namespace Douyin.LiveOpenSDK.Modules
{
    internal class SdkCore
    {
        internal SdkDebugInfo SdkDebugInfo;
        internal SdkDebugLogger Debug;
        internal SdkEnv Env;

        internal SdkCore()
        {
            SdkDebugInfo = new SdkDebugInfo();
            Debug = new SdkDebugLogger(nameof(LiveOpenSDK));
            Env = new SdkEnv();
        }
    }
}
// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;

namespace ByteDance.LiveOpenSdk
{
    /// <summary>
    /// 用于标注直播开放 SDK 对外提供的服务接口。
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public class ServiceApiAttribute : Attribute
    {
    }
}
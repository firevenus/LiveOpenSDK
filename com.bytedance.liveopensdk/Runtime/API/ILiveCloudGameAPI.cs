// Copyright (c) Bytedance. All rights reserved.
// Description:

// ReSharper disable once CheckNamespace
namespace Douyin.LiveOpenSDK
{
    /// <summary>
    /// 云启动云游戏API
    /// </summary>
    /// <remarks>会自动读取解析云启动参数。参考 https://developer.open-douyin.com/docs/resource/zh-CN/interaction/jierushuoming/pingtaijichu/cloud#0bf5266e</remarks>
    public interface ILiveCloudGameAPI
    {
        /// <summary>
        /// 是否是云启动。
        /// </summary>
        /// <remarks>会读取云启动参数，参考 https://developer.open-douyin.com/docs/resource/zh-CN/interaction/jierushuoming/pingtaijichu/cloud#0bf5266e</remarks>
        bool IsCloudGame();

        /// <summary>
        /// 是否是从手机端启动（手机抖音开播小玩法）。如果是PC伴侣开播，返回`false`
        /// </summary>
        /// <remarks>会读取云启动参数，参考 https://developer.open-douyin.com/docs/resource/zh-CN/interaction/jierushuoming/pingtaijichu/cloud#0bf5266e</remarks>
        bool IsStartFromMobile();

        /// <summary>
        /// 尝试初始化全屏，自动适配云游戏设备的分辨率。 如果不是云启动云开播，不会产生影响。
        /// </summary>
        void TryInitFullScreen();
    }
}
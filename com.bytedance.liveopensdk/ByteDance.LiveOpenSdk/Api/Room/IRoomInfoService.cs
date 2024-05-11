// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using System.Threading.Tasks;

namespace ByteDance.LiveOpenSdk.Room
{
    /// <summary>
    /// 直播间信息服务的接口。
    /// </summary>
    [ServiceApi]
    public interface IRoomInfoService
    {
        /// <summary>
        /// 当前的房间信息。<br/>
        /// 若获取房间信息失败，所有属性的值是默认值。
        /// </summary>
        IRoomInfo RoomInfo { get; }

        /// <summary>
        /// 房间消息有变化时触发的事件。
        /// </summary>
        event Action<IRoomInfo>? OnRoomInfoChanged;

        /// <summary>
        /// 手动触发房间信息更新。
        /// </summary>
        /// <returns>最新的房间信息</returns>
        Task<IRoomInfo> UpdateRoomInfoAsync();
    }

    /// <summary>
    /// 表示房间信息的接口。
    /// </summary>
    public interface IRoomInfo
    {
        /// <summary>
        /// 房间ID。
        /// </summary>
        string RoomId { get; }

        /// <summary>
        /// 主播信息。
        /// </summary>
        IUserInfo Anchor { get; }
    }

    /// <summary>
    /// 表示用户信息的接口。
    /// </summary>
    public interface IUserInfo
    {
        /// <summary>
        /// 用户的 OpenID。
        /// </summary>
        string OpenId { get; }

        /// <summary>
        /// 用户的头像 URL。
        /// </summary>
        string AvatarUrl { get; }

        /// <summary>
        /// 用户的昵称。
        /// </summary>
        string NickName { get; }
    }
}
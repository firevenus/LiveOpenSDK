// Copyright (c) Bytedance. All rights reserved.
// Description:

namespace ByteDance.LiveOpenSdk.Report
{
    /// <summary>
    /// 消息履约上报服务的接口。
    /// </summary>
    [ServiceApi]
    public interface IMessageAckService
    {
        /// <summary>
        /// 上报互动消息在小玩法内的展现。
        /// </summary>
        /// <param name="msgId">要上报的消息 ID</param>
        /// <param name="msgType">消息的类型，可选取值为 live_like/live_comment/live_gift</param>
        void ReportAck(string msgId, string msgType);
    }
}
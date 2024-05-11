// Copyright (c) Bytedance. All rights reserved.
// Description:

using System.Collections.Generic;
using System.Runtime.Serialization;
using Douyin.LiveOpenSDK.Data;
using Douyin.LiveOpenSDK.Utilities;
using dyCloudUnitySDK;
using UnityEngine.Networking;

// ReSharper disable once CheckNamespace
namespace Douyin.LiveOpenSDK
{
    /// <summary>
    /// 一些常量，用于一些API请求
    /// </summary>
    public static class APIConsts
    {
        public static class DYCloud
        {
            internal const string ConnectPath = "/web_socket/on_connect/v2";
        }
    }

    /// <summary>
    /// API返回码
    /// </summary>
    public struct APICode : System.IComparable<APICode>, System.IEquatable<APICode>
    {
        /// 0 成功
        public static APICode Success = new APICode(0);

        /// 1 通用的API返回错误, 需要看具体返回的 APIResponse, statusCode, errorMsg 内容。
        public static APICode APIResponseError = new APICode(1);

        /// 2 运行时`Exception`的错误
        public static APICode ExceptionError = new APICode(2);

        /// 3 输入参数错误。请修正接入代码、传入参数
        public static APICode ArgsError = new APICode(3);

        /// 10 已初始化。请忽略，不需要重新初始化
        public static APICode AlreadyInitializedWarning = new APICode(10);

        /// 11 初始化进行中。请等待回调，不需要重新初始化
        public static APICode InitializeInProgressWarning = new APICode(11);

        /// 12 初始化状态错误。请初始化
        public static APICode NotInitializedError = new APICode(12);

        // @internal
        // ReSharper disable once UnusedMember.Local
        private static APICode _Internal_LastCode = new APICode(LastValue);

        public bool IsSuccess() => Value == 0;

        internal int Value { get; private set; }
        private const int LastValue = 99999;
        private static APIState s_ctorState;
        private static Dictionary<int, APICode> s_codesMap;

        public APICode(int value)
        {
            Value = value;
#if UNITY_EDITOR
            if (s_ctorState == APIState.None)
                s_ctorState = APIState.InProgress;
            if (value == LastValue)
                s_ctorState = APIState.Success;
            if (s_ctorState != APIState.InProgress)
                return;
            if (null == s_codesMap)
                s_codesMap = new Dictionary<int, APICode>();
            if (s_codesMap.ContainsKey(value))
            {
                s_ctorState = APIState.Failed;
                throw new System.Exception($"APICode duplicate! value: {value}");
            }

            s_codesMap[value] = this;
#endif
        }

        // converter
        public static implicit operator int(APICode myEnum) => myEnum.Value;

        // converter
        public static explicit operator APICode(int value) => new APICode(value);

        public int CompareTo(APICode other) => Value.CompareTo(other.Value);

        public bool Equals(APICode other) => Value.Equals(other.Value);

        public override bool Equals(object obj) =>
            obj is APICode other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();
    }

    /// <summary>
    /// API状态。
    /// </summary>
    /// <remarks>通常只在SDK内部维护。 开发者接入时也可以仿照这种枚举，来维护自己功能的状态。</remarks>
    public enum APIState
    {
        None,
        InProgress,
        Success,
        Failed,
    }

    /// <summary>
    /// 通用的API返回结果。通常在API回调方法的参数中携带。
    /// </summary>
    /// <remarks>也用作特定类型API返回的基类</remarks>
    [System.Serializable]
    public class APIResponse
    {
        /// 是否结果为成功。
        /// <remarks>注：特定API可能调用成功、返回码=0，但有具体返回数据body中存在部分的错误，此时要以具体API协议内容为准</remarks>
        public virtual bool IsResultSuccess => code == APICode.Success && string.IsNullOrEmpty(errorMsg);

        /// 返回码， 0 表示成功。
        /// <remarks>注：特定API可能调用成功、返回码=0，但有具体返回数据body中存在部分的错误，此时要以具体API协议内容为准</remarks>
        public int code;

        /// 错误信息
        public string errorMsg;

        /// 返回的数据
        public string body;

        /// 信息来源类型标识，用于辅助调试
        public RespSourceType respSource;

        /// 转Json字符串
        public virtual string ToJsonString() => SdkUtils.ToJsonString(this);

        /// 转Json字符串
        public override string ToString() => SdkUtils.ToJsonString(this);

        public virtual string ToRespInfo() => $"code: {code} source: {respSource} {ErrorMsgInfo()}resp: {this}";
        public virtual string ToErrorLog() => $"code: {code} source: {respSource} errorMsg: {errorMsg}";
        public virtual string ErrorMsgInfo() => string.IsNullOrEmpty(errorMsg) ? string.Empty : $"errorMsg: {errorMsg} ";
        protected static SdkDebugLogger Debug => LiveOpenSDK.Debug;

        internal APIResponse()
        {
            code = APICode.Success;
            errorMsg = string.Empty;
            body = string.Empty;
        }

        internal APIResponse(APICode code, string errorMsg)
        {
            this.code = code;
            this.errorMsg = errorMsg ?? string.Empty;
        }

        internal APIResponse(int code, string errorMsg)
        {
            this.code = code;
            this.errorMsg = errorMsg ?? string.Empty;
        }

        internal APIResponse(int code, string errorMsg, string body)
        {
            this.code = code;
            this.errorMsg = errorMsg ?? string.Empty;
            this.body = body ?? string.Empty;
        }

        internal virtual void AcceptException(System.Exception e)
        {
            code = APICode.ExceptionError;
            errorMsg = e.Message;
        }

        internal virtual void AcceptException(DYCloudSdkException ex)
        {
            code = APICode.ExceptionError;
            errorMsg = ex.Message;
            respSource = RespSourceType.DYCloudSDK;
        }
    }

    /// 信息来源类型标识，用于辅助调试
    public enum RespSourceType
    {
        Unknown = 0,
        SDKClient = 1,
        DYCloud = 11,
        DYCloudSDK = 12,
        DYCloudService = 13,
        WebcastService = 21,
    }

    /// <summary>
    /// 通用的HttpAPI返回结果
    /// </summary>
    [System.Serializable]
    public class HttpAPIResponse : APIResponse
    {
        /// 是否结果为成功。
        /// <remarks>注：特定API可能调用成功、返回码=0、statusCode=200，但有具体返回数据body中存在部分的错误，此时要以具体API协议内容为准</remarks>
        public override bool IsResultSuccess => code == APICode.Success && IsStatusCodeSuccess() && string.IsNullOrEmpty(errorMsg);

        public virtual bool IsConnectionError => reqResultType == UnityWebRequest.Result.ConnectionError;

        /// HTTP 状态码。 通常 200 表示成功
        public int statusCode;

        /// HTTP 调用path，不包含host域名
        public string path;

        /// HTTP 调用类型，例如"GET"
        public string httpType;

        /// 返回的 HTTP Response Header
        public Dictionary<string, string> headers = new Dictionary<string, string>();

        /// 日志id
        public string logid;

        /// 结果类型，进行中、成功、或错误的类型（参考<see cref="UnityWebRequest.Result"/>）
        public UnityWebRequest.Result reqResultType;

        protected virtual bool IsStatusCodeSuccess()
        {
            return statusCode == 200;
        }

        public HttpAPIResponse()
        {
            reqResultType = UnityWebRequest.Result.InProgress;
        }

        internal HttpAPIResponse(string path, string httpType)
        {
            statusCode = 0;
            this.path = path;
            this.httpType = httpType;
            logid = string.Empty;
            reqResultType = UnityWebRequest.Result.InProgress;
        }

        internal HttpAPIResponse(APIResponse resp, int statusCode, string path, string httpType) : base(resp.code, resp.errorMsg)
        {
            this.statusCode = statusCode;
            this.path = path;
            this.httpType = httpType;
            code = resp.code;
            errorMsg = resp.errorMsg;
            if (resp.code == APICode.Success && statusCode == 200 && string.IsNullOrEmpty(errorMsg))
                reqResultType = UnityWebRequest.Result.Success;
            else
                reqResultType = UnityWebRequest.Result.DataProcessingError;
        }

        internal virtual void AcceptWebRequest(UnityWebRequest req)
        {
            var respHeaders = req.GetResponseHeaders();
            AcceptHeaders(respHeaders);

            statusCode = SdkUtils.ToIntStatusCode(req.responseCode);
            body = req.downloadHandler.text;
            reqResultType = req.result;
            if (string.IsNullOrEmpty(errorMsg))
                errorMsg = req.error ?? "";

            code = IsResultSuccess ? APICode.Success : statusCode;

            var log_id = string.Empty;
            if (respHeaders != null)
                respHeaders.TryGetValue(HeaderDefined.TtLogId, out log_id);
            if (!string.IsNullOrEmpty(log_id))
                logid = log_id;
        }

        internal virtual void AcceptHeaders(Dictionary<string, string> _headers)
        {
            if (_headers == null)
                return;
            if (this.headers == null)
                this.headers = new Dictionary<string, string>();
            foreach (var pair in _headers)
            {
                this.headers[pair.Key] = pair.Value;
            }
        }

        internal override void AcceptException(System.Exception e)
        {
            code = APICode.ExceptionError;
            errorMsg = e.Message;
            reqResultType = UnityWebRequest.Result.DataProcessingError;
        }

        public override string ToRespInfo() => $"code: {code} source: {respSource} {httpType} statusCode: {statusCode} {ErrorMsgInfo()}resp: {this}";
    }

    /// <summary>
    /// 初始化直播信息
    /// </summary>
    [System.Serializable]
    public class InitWebcastInfoResponse : HttpAPIResponse
    {
        // ReSharper disable once RedundantOverriddenMember
        public override bool IsResultSuccess => base.IsResultSuccess;

        internal InitWebcastInfoResponse()
        {
        }

        internal InitWebcastInfoResponse(APIResponse resp, int statusCode, string path, string httpType) : base(resp, statusCode, path, httpType)
        {
        }

        /// 房间ID
        public string room_id;

        /// 主播openID
        public string anchor_open_id;

        /// 主播头像地址
        public string avatar_url;

        /// 主播昵称
        public string nick_name;
    }

    /// <summary>
    /// `CallContainer`API返回结果
    /// </summary>
    /// <remarks>参考 @see: https://developer.open-douyin.com/docs/resource/zh-CN/developer/tools/cloud/develop-guide/danmu-unity-sdk</remarks>
    [System.Serializable]
    public class DYCloudCallResponse : HttpAPIResponse
    {
        internal DYCloudCallResponse(string path, string httpType) : base(path, httpType)
        {
        }

        internal DYCloudCallResponse(APIResponse resp, int statusCode, string path, string httpType) : base(resp, statusCode, path, httpType)
        {
        }

        internal void AcceptResponse(DYCloudHttpResponse resp)
        {
            if (null == resp)
                return;
            statusCode = resp.statusCode;
            code = IsStatusCodeSuccess() ? APICode.Success : statusCode;
            body = resp.body;
            AcceptHeaders(resp.headers);
            reqResultType = IsStatusCodeSuccess() ? UnityWebRequest.Result.Success : UnityWebRequest.Result.DataProcessingError;
        }

        internal override void AcceptException(DYCloudSdkException ex)
        {
            code = ex.code;
            logid = ex.logid;
            errorMsg = ex.Message;
            reqResultType = UnityWebRequest.Result.DataProcessingError;
        }
    }

    /// <summary>
    /// 直播互动消息数据。 一个数据对象，对应一条互动消息：评论、礼物、或点赞等。
    /// </summary>
    [System.Serializable]
    [DataContract]
    public class LiveMsgItem
    {
        /// 缓存透传的原始数据。 只用作本地缓存和读取，不用作API请求参数字段。
        public string data_cached { get; set; }

        /// 唯一标识，平台推送payload数据里的msg_id。会被用于履约校验
        [DataMember(Name = "msg_id")] public string msg_id;

        /// 消息类型，
        /// 1. 评论：live_comment
        /// 2. 礼物：live_gift
        /// 3. 点赞：live_like
        /// <see cref="LiveMsgType"/>
        [DataMember(Name = "msg_type")] public string msg_type;

        /// 毫秒级时间戳，例如 1705989099973。当ack_type为1时即为指令收到后的时间，当ack_type为2时即为渲染成功后的时间
        [DataMember(Name = "client_time")] public long client_time;

        /// 一条消息数据，使用当前时间戳
        /// <param name="rawData">透传原始数据</param>
        /// <param name="msg_id">消息id，见成员：<see cref="MsgAckItem.msg_id"/></param>
        /// <param name="msg_type">消息类型，见成员：<see cref="MsgAckItem.msg_type"/></param>
        public LiveMsgItem(string rawData, string msg_id, string msg_type)
        {
            data_cached = rawData ?? "";
            this.msg_id = msg_id ?? "";
            this.msg_type = msg_type ?? "";
            UseNowTimestamp();
        }

        /// 一条消息数据，使用指定时间戳
        /// <param name="rawData">透传原始数据</param>
        /// <param name="msg_id">消息id，见成员：<see cref="MsgAckItem.msg_id"/></param>
        /// <param name="msg_type">消息类型，见成员：<see cref="MsgAckItem.msg_type"/></param>
        /// <param name="client_time">毫秒级时间戳，见成员：<see cref="MsgAckItem.client_time"/></param>
        public LiveMsgItem(string rawData, string msg_id, string msg_type, long client_time)
        {
            data_cached = rawData ?? "";
            this.msg_id = msg_id ?? "";
            this.msg_type = msg_type ?? "";
            this.client_time = client_time;
        }

        /// 令`client_time`使用当前时间戳
        public void UseNowTimestamp()
        {
            client_time = TimeUtil.NowTimestampMs;
        }

        /// 当前Unix毫秒级时间戳 (UTC)
        public static long NowTimestamp() => TimeUtil.NowTimestampMs;
    }

    /// <summary>
    /// 履约Ack请求的互动消息数据，用于履约上报API的请求参数/>
    /// </summary>
    [System.Serializable]
    [DataContract]
    public class MsgAckItem : LiveMsgItem
    {
        /// 一条消息数据，使用当前时间戳
        public MsgAckItem(string msg_id, string msg_type)
            : base("", msg_id, msg_type)
        {
        }

        /// 一条消息数据，使用指定时间戳
        public MsgAckItem(string msg_id, string msg_type, long _client_time)
            : base("", msg_id, msg_type, _client_time)
        {
        }

        /// 单条消息数据转为List
        public List<MsgAckItem> ToList()
        {
            var list = new List<MsgAckItem> {this};
            return list;
        }

        internal static string ValidateErrorField(MsgAckItem item)
        {
            // 检查item为空、msg_id为空、msg_type为空
            var errorField = string.Empty;
            if (item == null)
                errorField = "`item` is null!";
            else if (string.IsNullOrEmpty(item.msg_id))
                errorField = "`msg_id` is empty!";
            else if (string.IsNullOrEmpty(item.msg_type))
                errorField = "`msg_type` is empty!";
            else if (item.client_time == 0)
                errorField = "`client_time` is 0!";
            return errorField;
        }
    }

    /// <summary>
    /// Connect OnError连接错误时的详细错误数据结构
    /// </summary>
    [System.Serializable]
    [DataContract]
    public struct ConnectErrorData
    {
        /// 连接错误时，抖音云返回的`code`字段.
        /// <remarks>抖音云目前抛过来的可能是int也可能是string，目前这里适配成统一转为string</remarks>
        [DataMember] public string code;

        /// 连接错误时，抖音云返回的`message`字段
        [DataMember] public string message;

        /// 是否系统会自动重连。 如果`true`，表示本次错误系统会自动重连，开发者上层不需要重新发起连接。 如果`false`，表示连接失败，开发者上层需要自己选择处理，可以稍后重试建连、或先弹框提示再点击后重试建连。
        [DataMember] public bool? willReconnect;

        public override string ToString() => SdkUtils.ToJsonString(this, false, SdkUtils.JsonIgnoreNullSettings);


        internal static ConnectErrorData CreateFromConnectErrorMsg(string errorMsg)
        {
            try
            {
                var errorData = SdkUtils.FromJsonString<ConnectErrorData>(errorMsg);
                if (errorData.code == null)
                    errorData.code = string.Empty;

                return errorData;
            }
            catch (System.Exception e)
            {
                LiveOpenSDK.Debug.LogWarning(e.ToString());
                return new ConnectErrorData
                {
                    message = errorMsg,
                    code = APICode.ExceptionError.ToString(),
                    willReconnect = null
                };
            }
        }
    }
}
// Copyright (c) Bytedance. All rights reserved.
// Description:

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ByteDance.LiveOpenSdk.DyCloud
{
    /// <summary>
    /// 抖音云功能的接口。<br/>
    /// 详细信息可参阅抖音云 SDK 的文档。
    /// </summary>
    [ServiceApi]
    public interface IDyCloudApi
    {
        /// <summary>
        /// 初始化抖音云实例。
        /// </summary>
        /// <param name="initParams">抖音云的初始化参数</param>
        /// <returns>对应的可等待 Task</returns>
        /// <remarks>
        /// 必须等待初始化成功后才能调用其他方法。<br/>
        /// 调用该方法时，若 <paramref name="initParams">initParams</paramref> 中的某些属性为空，则会用从 SDK 的公共参数当中读取到的值填充。
        /// </remarks>
        Task InitializeAsync(DyCloudInitParams initParams);

        /// <summary>
        /// 发起 HTTP 请求。
        /// </summary>
        /// <param name="path">请求路径</param>
        /// <param name="serviceId">服务 ID，若为空则使用抖音云初始化时提供的默认服务 ID。</param>
        /// <param name="method">HTTP 方法，支持 GET/POST/OPTIONS/PUT/DELETE/TRACE/PATCH</param>
        /// <param name="body">HTTP 请求体，大小上限为 1MB</param>
        /// <param name="headers">HTTP 请求标头</param>
        /// <returns>HTTP 请求的结果</returns>
        Task<IDyCloudHttpResponse> CallContainerAsync(
            string path,
            string serviceId,
            string method,
            string body,
            IDictionary<string, string> headers
        );

        /// <summary>
        /// 获取抖音云长连接 WebSocket 的单例。
        /// </summary>
        /// <remarks>
        /// 抖音云内部只支持一个 WebSocket 连接，因此暂不提供创建新的 <see cref="IDyCloudWebSocket"/> 实例的方法。
        /// </remarks>
        IDyCloudWebSocket WebSocket { get; }
    }

    /// <summary>
    /// 抖音云初始化参数。
    /// </summary>
    public sealed class DyCloudInitParams
    {
        /// <summary>
        /// 直播伴侣启动或云启动时提供的访问令牌。
        /// </summary>
        public string Token { get; set; } = "";

        /// <summary>
        /// 小玩法的 AppId。
        /// </summary>
        public string AppId { get; set; } = "";

        /// <summary>
        /// 云环境 ID。
        /// </summary>
        public string EnvId { get; set; } = "";

        /// <summary>
        /// 发起请求时使用的默认云服务 ID。
        /// </summary>
        public string DefaultServiceId { get; set; } = "";

        /// <summary>
        /// 是否设置为调试模式。<br/>
        /// 调试模式时可以使用空的 <see cref="Token"/>。
        /// </summary>
        public bool IsDebug { get; set; } = false;

        /// <summary>
        /// 本地调试时使用的IP地址。
        /// </summary>
        public string DebugIpAddress { get; set; } = "";

        [CompilerGenerated]
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("DyCloudInitParams");
            builder.Append(" { ");
            if (this.PrintMembers(builder))
                builder.Append(' ');
            builder.Append('}');
            return builder.ToString();
        }

        [CompilerGenerated]
        private bool PrintMembers(StringBuilder builder)
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();
            builder.Append("Token = ");
            builder.Append((object)this.Token);
            builder.Append(", AppId = ");
            builder.Append((object)this.AppId);
            builder.Append(", EnvId = ");
            builder.Append((object)this.EnvId);
            builder.Append(", DefaultServiceId = ");
            builder.Append((object)this.DefaultServiceId);
            builder.Append(", IsDebug = ");
            builder.Append(this.IsDebug.ToString());
            builder.Append(", DebugIpAddress = ");
            builder.Append((object)this.DebugIpAddress);
            return true;
        }
    }

    /// <summary>
    /// 抖音云 WebSocket 的接口。
    /// </summary>
    public interface IDyCloudWebSocket
    {
        /// <summary>
        /// WebSocket 的连接选项。
        /// </summary>
        /// <remarks>
        /// 若在发起连接后修改这个属性，可能不生效。
        /// </remarks>
        DyCloudWebSocketOptions Options { get; }

        /// <summary>
        /// 对指定的路径发起长连接，使用默认的服务 ID。
        /// </summary>
        /// <param name="path">接口地址</param>
        /// <returns>对应的可等待 Task</returns>
        Task ConnectContainerAsync(string path);

        /// <summary>
        /// 对指定的路径发起长连接。
        /// </summary>
        /// <param name="path">接口地址</param>
        /// <param name="serviceId">云服务 ID</param>
        /// <returns>对应的可等待 Task</returns>
        Task ConnectContainerAsync(string path, string serviceId);

        /// <summary>
        /// 向远端发送数据。
        /// </summary>
        /// <param name="message">文本形式的消息内容</param>
        void SendMessage(string message);

        /// <summary>
        /// 主动关闭连接。
        /// </summary>
        void Close();

        /// <summary>
        /// 连接建立时的事件。
        /// </summary>
        public event OnOpenCallback? OnOpen;

        /// <summary>
        /// 连接关闭时的事件。
        /// </summary>
        /// <remarks>无论主动还是被动关闭连接，都会触发这个事件。</remarks>
        public event OnCloseCallback? OnClose;

        /// <summary>
        /// 收到远端消息时的事件。
        /// </summary>
        public event OnMessageCallback? OnMessage;

        /// <summary>
        /// 发生错误时的事件。
        /// </summary>
        /// <remarks>发生错误时，连接可能会被关闭。若 <see cref="IDyCloudWebSocketError.WillReconnect"/> 为 true，会自动重新连接。</remarks>
        public event OnErrorCallback? OnError;
    }

    /// <summary>
    /// HTTP 请求的结果。
    /// </summary>
    public interface IDyCloudHttpResponse
    {
        /// <summary>
        /// HTTP 状态码。
        /// </summary>
        int StatusCode { get; }

        /// <summary>
        /// HTTP 响应体。
        /// </summary>
        string Body { get; }

        /// <summary>
        /// HTTP 响应标头。
        /// </summary>
        IReadOnlyDictionary<string, string> Headers { get; }
    }

    /// <summary>
    /// 抖音云 WebSocket 连接选项。
    /// </summary>
    public sealed class DyCloudWebSocketOptions
    {
        /// <summary>
        /// 遇到错误时，是否按照特定的规则重新连接。默认为 true。
        /// </summary>
        public bool AutoReconnect { get; set; } = true;
    }

    /// <summary>
    /// 抖音云 WebSocket 错误信息。
    /// </summary>
    public interface IDyCloudWebSocketError
    {
        /// <summary>
        /// 原始的字符串形式的错误信息。
        /// </summary>
        string RawMessage { get; }

        /// <summary>
        /// 错误代码。
        /// </summary>
        /// <remarks>该属性从 <see cref="RawMessage"/> 解析而来，若不存在对应字段则为 null。</remarks>
        string? Code { get; }

        /// <summary>
        /// 错误消息。
        /// </summary>
        /// <remarks>该属性从 <see cref="RawMessage"/> 解析而来，若不存在对应字段则为 null。</remarks>
        string? Message { get; }

        /// <summary>
        /// 是否会尝试自动重新连接。
        /// <remarks>该属性从 <see cref="RawMessage"/> 解析而来，若不存在对应字段则为 null。</remarks>
        /// </summary>
        bool? WillReconnect { get; }
    }

    /// <seealso cref="IDyCloudWebSocket.OnOpen"/>
    public delegate void OnOpenCallback();

    /// <seealso cref="IDyCloudWebSocket.OnClose"/>
    public delegate void OnCloseCallback();

    /// <seealso cref="IDyCloudWebSocket.OnMessage"/>
    public delegate void OnMessageCallback(string message);

    /// <seealso cref="IDyCloudWebSocket.OnError"/>
    public delegate void OnErrorCallback(IDyCloudWebSocketError error);
}
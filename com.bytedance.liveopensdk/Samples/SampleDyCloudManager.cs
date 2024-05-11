// Copyright (c) Bytedance. All rights reserved.
// Description:

#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ByteDance.LiveOpenSdk;
using ByteDance.LiveOpenSdk.DyCloud;
using Douyin.LiveOpenSDK.Data;
using Douyin.LiveOpenSDK.Utilities;

namespace Douyin.LiveOpenSDK.Samples
{
    /// <summary>
    /// 直播开放 SDK 中抖音云相关功能的接入示例代码。
    /// 实现了抖音云的短连接、长连接、互动消息消费与履约。
    /// </summary>
    public class SampleDyCloudManager
    {
        private const string Tag = nameof(SampleDyCloudManager);

        public string EnvId { get; set; } = "<your-dycloud-env-id>";
        public string ServiceId { get; set; } = "<your-dycloud-service-id>";

        /// sdk调用isDebug模式，参考 https://developer.open-douyin.com/docs/resource/zh-CN/developer/tools/cloud/develop-guide/danmu-unity-sdk#9ac14cc5  - 章节：常见问题 isDebug=true
        public bool IsDebug = false;

        /// sdk支持本地调试、流量转发，参考 https://developer.open-douyin.com/docs/resource/zh-CN/developer/tools/cloud/develop-guide/danmu-unity-sdk#9ac14cc5  - 章节：常见问题 IP地址
        public string DebugIpAddress = ""; //

        public event Action<DyCloudSocketMessage>? OnDyCloudMessage;

        public const string kHelloPath = "/hello?name=xxx";
        public const string kStartTaskPath = "/live_data/task/start";
        public const string kTopGiftPath = "/api/gift/top_gift";
        public const string kTopGiftArg = "sec_gift_id_list";
        public const string kConnectPath = "/web_socket/on_connect/v2";

        public string connectPath = kConnectPath;

        public GiftConfigs GiftConfigs = new GiftConfigs()
        {
            Gifts = new List<GiftConfigs.Gift>()
            {
                new GiftConfigs.Gift("仙女棒", 1, "n1/Dg1905sj1FyoBlQBvmbaDZFBNaKuKZH6zxHkv8Lg5x2cRfrKUTb8gzMs="),
                new GiftConfigs.Gift("甜甜圈", 52, "PJ0FFeaDzXUreuUBZH6Hs+b56Jh0tQjrq0bIrrlZmv13GSAL9Q1hf59fjGk="),
                new GiftConfigs.Gift("恶魔炸弹", 199, "gx7pmjQfhBaDOG2XkWI2peZ66YFWkCWRjZXpTqb23O/epru+sxWyTV/3Ufs="),
                new GiftConfigs.Gift("能力药丸", 10, "28rYzVFNyXEXFC8HI+f/WG+I7a6lfl3OyZZjUS+CVuwCgYZrPrUdytGHu0c="),
                new GiftConfigs.Gift("能量电池", 99, "IkkadLfz7O/a5UR45p/OOCCG6ewAWVbsuzR/Z+v1v76CBU+mTG/wPjqdpfg="),
                new GiftConfigs.Gift("神秘空投", 520, "pGLo7HKNk1i4djkicmJXf6iWEyd+pfPBjbsHmd3WcX0Ierm2UdnRR7UINvI="),
                // new GiftConfigs.Gift("超级空投", 1, "lsEGaeC++k/yZbzTU2ST64EukfpPENQmqEZxaK9v1+7etK+qnCRKOnDyjsE="),
                // new GiftConfigs.Gift("超能喷射", 1, "P7zDZzpeO215SpUptB+aURb1+zC14UC9MY1+MHszKoF0p5gzYk8CNEbey60="),
                // new GiftConfigs.Gift("魔法镜", 1, "fJs8HKQ0xlPRixn8JAUiL2gFRiLD9S6IFCFdvZODSnhyo9YN8q7xUuVVyZI"),
            }
        };

        /// 持有websocket。
        private IDyCloudWebSocket _webSocket;

        public SdkDebugLogger Debug = new SdkDebugLogger(Tag);

        /// <summary>
        /// 一键开始全部
        /// </summary>
        public async Task StartAllInOne()
        {
            Debug.Log("开始全部 - 初始化");
            await Init();
            Debug.Log("开始全部 - 开启推送任务");
            await Call_StartTasks();
            Debug.Log("开始全部 - 礼物置顶");
            await Call_StartGifts();
            Debug.Log("开始全部 - 连接抖音云");
            await ConnectWebSocket();
            Debug.Log("开始全部 - 结束");
        }

        /// <summary>
        /// 初始化抖音云。
        /// </summary>
        public async Task Init()
        {
            Debug.Log("正在初始化抖音云 SDK");

            // 初始化抖音云
            var initParams = new DyCloudInitParams()
            {
                EnvId = EnvId,
                DefaultServiceId = ServiceId,
                IsDebug = IsDebug,
                DebugIpAddress = DebugIpAddress
            };

            try
            {
                if (LiveOpenSdk.DyCloudApi == null)
                {
                    throw new InvalidOperationException("抖音云 API 不可用，请检查 LiveOpenSdk 是否已初始化");
                }
                await LiveOpenSdk.DyCloudApi.InitializeAsync(initParams);

                Debug.Log("初始化抖音云 SDK：成功");
            }
            catch (Exception e)
            {
                Debug.LogError($"初始化抖音云 SDK：失败 {e.Message}");
            }
        }


        [DataContract]
        [Serializable]
        private class StartTasksResponse
        {
            [DataMember(Name = "result")] public Dictionary<string, string> Result;
        }

        [DataContract]
        [Serializable]
        private class StartTaskResponse
        {
            [DataMember(Name = "err_no")] public int ErrNo;
            [DataMember(Name = "err_msg")] public string ErrMsg;
            [DataMember(Name = "data")] public Payload Data;

            [DataContract]
            [Serializable]
            public class Payload
            {
                [DataMember(Name = "task_id")] public string TaskId;
            }
        }

        public async Task Call_StartTasks()
        {
            Debug.Log("开始任务");
            try
            {
                var resp = await CallContainer(kStartTaskPath, HttpType.POST);
                var respData = SampleUtil.FromJsonString<StartTasksResponse>(resp);
                foreach (var entry in respData.Result)
                {
                    var resultStr = entry.Value;
                    var result = SampleUtil.FromJsonString<StartTaskResponse>(resultStr);
                    Debug.LogDebug($"开始任务 - 任务名称：{entry.Key} 结果：{result.ErrNo == 0} 详情：{result.ErrMsg}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"开始任务 - 失败 {e}");
            }
        }

        public async Task Call_StartGifts()
        {
            Debug.Log("置顶礼物");
            try
            {
                var reqData = new TopGiftRequestData()
                {
                    GiftIDs = GiftConfigs.GiftIDs
                };
                var body = SampleUtil.ToJsonString(reqData);
                var resp = await CallContainer(kTopGiftPath, HttpType.POST, null, body);
                Debug.Log($"置顶礼物 - 结束");
            }
            catch (Exception e)
            {
                Debug.Log("置顶礼物 - 失败");
            }
        }

        public async Task<string> CallContainer(string path, string method = HttpType.GET,
            Dictionary<string, string> headers = null, string body = "")
        {
            Debug.LogDebug($"调用抖音云{nameof(CallContainer)}, path: {path}, serviceId: {ServiceId}, method: {method}" +
                           $", headers: {SampleUtil.ToJsonString(headers)}, body: {body}");

            try
            {
                var resp = await LiveOpenSdk.DyCloudApi.CallContainerAsync(path, ServiceId, method, body,
                    headers ?? new Dictionary<string, string>());
                Debug.LogDebug($"{nameof(CallContainer)}, statusCode: {resp.StatusCode} {resp.Body}");
                return resp.Body;
            }
            catch (Exception e)
            {
                Debug.LogError($"{nameof(CallContainer)}, exception: {e.Message}");
                throw;
            }
        }

        public async Task ConnectWebSocket()
        {
            Debug.LogDebug($"{nameof(ConnectWebSocket)} serviceId: {ServiceId}");
            var path = connectPath;
            Debug.Log($"连接抖音云WebSocket ConnectContainer path: {path}");

            if (_webSocket == null)
            {
                _webSocket = LiveOpenSdk.DyCloudApi.WebSocket;
                _webSocket.OnOpen += OnOpen;
                _webSocket.OnMessage += OnMessage;
                _webSocket.OnError += OnError;
                _webSocket.OnClose += OnClose;
            }

            try
            {
                await _webSocket.ConnectContainerAsync(path, ServiceId);
            }
            catch (Exception e)
            {
                Debug.LogError($"抖音云WebSocket 建立连接失败 - {e.Message}");
            }
        }

        public void Socket_Send(string data)
        {
            if (_webSocket == null)
            {
                Debug.LogError("WebSocket is null!");
                return;
            }

            data = data ?? "";
            Debug.LogDebug($"WebSocket SendMessage data: {data}");
            _webSocket.SendMessage(data);
        }

        private void OnOpen()
        {
            Debug.Log("抖音云WebSocket 建立连接成功 OnOpen");
        }

        private void OnClose()
        {
            Debug.LogWarning("抖音云WebSocket 连接被关闭 OnClose");
        }

        private void OnError(IDyCloudWebSocketError error)
        {
            Debug.LogError($"抖音云WebSocket 错误 OnError - {error}");
            if (error.WillReconnect == true)
            {
                // 本次错误系统会自动重连，开发者上层不需要重新发起连接
                Debug.Log($"willReconnect true - 本次错误系统会自动重连，开发者上层不需要重新发起连接");
                return;
            }

            // 连接失败，开发者上层需要自己选择处理，可以稍后重试建连、或先弹框提示再点击后重试建连。
            Debug.LogWarning($"willReconnect false - 连接失败，开发者上层需要自己选择处理，可以稍后重试建连、或先弹框提示再点击后重试建连。");
        }

        private void OnMessage(string data)
        {
            if (string.IsNullOrEmpty(data)) return;
            var message = SampleUtil.FromJsonString<DyCloudSocketMessage>(data);
            var msgId = message.MsgId;
            var msgType = message.MsgType;
            if (!string.IsNullOrEmpty(msgType))
            {
                var typeText = LiveMsgType.ToChineseText(msgType);
                Debug.Log($"抖音云WebSocket 互动消息：{typeText} - msg_type: \"{msgType}\", msg_id: \"{msgId}\", data: \"{data}\"");
                OnDyCloudMessage?.Invoke(message);
                return;
            }

            Debug.Log($"抖音云WebSocket 消息 - msg_type: \"{msgType}\", msg_id: \"{msgId}\", data: {data}");
        }

        public void ReportAck(MsgAckItem msgItem)
        {
            if (msgItem == null)
            {
                Debug.LogError($"履约 {nameof(ReportAck)} arg `{nameof(msgItem)}` is null!");
                return;
            }

            Debug.Log($"履约 {nameof(ReportAck)} msg_type: {msgItem.msg_type} msg_id: {msgItem.msg_id}");
            LiveOpenSdk.MessageAckService.ReportAck(msgItem.msg_id, msgItem.msg_type);
        }

        /// <summary>
        /// 主动关闭 WebSocket 连接。
        /// </summary>
        public void CloseWebSocket()
        {
            if (_webSocket == null)
                return;
            Debug.LogDebug("主动关闭抖音云 WebSocket");
            _webSocket?.Close();
        }
    }

    [DataContract]
    [Serializable]
    public class GiftConfigs
    {
        [DataContract]
        [Serializable]
        public class Gift
        {
            [DataMember(Name = "Name")] public string Name;
            [DataMember(Name = "Price")] public int Price;
            [DataMember(Name = "ID")] public string ID;

            public Gift(string name, int price, string id)
            {
                Name = name;
                Price = price;
                ID = id;
            }
        }

        [DataMember(Name = "Gifts")] public List<Gift> Gifts;

        public List<string> GiftIDs
        {
            get
            {
                var giftList = new List<string>();
                foreach (var gift in Gifts)
                {
                    giftList.Add(gift.ID);
                }

                return giftList;
            }
        }
    }

    [DataContract]
    public class TopGiftRequestData
    {
        [DataMember(Name = SampleDyCloudManager.kTopGiftArg)]
        public List<string> GiftIDs;
    }
}
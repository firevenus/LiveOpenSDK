// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ByteDance.LiveOpenSdk.Contracts;
using ByteDance.LiveOpenSdk.Legacy;
using ByteDance.LiveOpenSdk.Logging;
using ByteDance.LiveOpenSdk.Utilities;

namespace ByteDance.LiveOpenSdk.Room
{
    internal class RoomInfoServiceImpl : IRoomInfoService, IDisposable
    {
        private const string Tag = "RoomInfoService";
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        public IRoomInfo RoomInfo { get; private set; } = new RoomInfo();
        public List<AckConfig> AckConfigs { get; private set; } = FallbackAckConfig;
        public event Action<IRoomInfo>? OnRoomInfoChanged;
        public event Action<List<AckConfig>>? OnAckConfigsChanged;

        public RoomInfoServiceImpl()
        {
            Task.Run(FetchRoomInfoOnStart);
        }

        private async Task<IRoomInfo> FetchRoomInfoOnStart()
        {
            var retryIntervals = Enumerable.Range(1, 11)
                .Select(sec => TimeSpan.FromSeconds(sec)).AsEnumerable();
            return await TaskUtils.Retry(() => UpdateWebcastInfo(_cts.Token),
                retryIntervals, _cts.Token, Tag, nameof(FetchRoomInfoOnStart));
        }

        public async Task<IRoomInfo> UpdateRoomInfoAsync()
        {
            try
            {
                return await UpdateWebcastInfo(_cts.Token);
            }
            catch (Exception e)
            {
                Log.Warning(Tag, $"{nameof(UpdateRoomInfoAsync)} fail, exception = {e.Message}");
                throw;
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            OnRoomInfoChanged = null;
            OnAckConfigsChanged = null;
            Log.Verbose(Tag, "Disposed");
        }

        private async Task<IRoomInfo> UpdateWebcastInfo(CancellationToken cancellationToken)
        {
            var webcastInfoResponse = await RequestWebcastInfo(cancellationToken);
            var payload = webcastInfoResponse.data;
            if (payload?.info == null)
            {
                throw new InvalidDataException("Missing webcast info payload");
            }

            var roomInfo = ParseRoomInfo(payload.info);
            RoomInfo = roomInfo;
            if (payload.ackConfig != null)
            {
                AckConfigs = payload.ackConfig;
            }
            else
            {
                if (ReferenceEquals(AckConfigs, FallbackAckConfig))
                {
                    Log.Warning(Tag, "No Ack config found, using fallback config");
                }
                else
                {
                    Log.Warning(Tag, "No Ack config found, using previous config");
                }
            }
            Log.Debug(Tag, $"UpdateWebcastInfo complete, roomInfo = {roomInfo}");
            LiveOpenSdk.Post(() =>
            {
                OnRoomInfoChanged?.Invoke(roomInfo);
                OnAckConfigsChanged?.Invoke(AckConfigs);
            });
            return roomInfo;
        }

        private async Task<WebcastMateInfoResponse> RequestWebcastInfo(CancellationToken cancellationToken)
        {
            var token = LiveOpenSdk.Env.Token;
            var appId = LiveOpenSdk.Env.AppId;

            var uriBuilder = new UriBuilder
            {
                Scheme = "https",
                Host = ConstsInternal.ApiHost_Webcast,
                Path = ConstsInternal.ApiPath_WebcastInfo,
                Query = $"?appid={appId}"
            };
            var requestUri = uriBuilder.ToString();

            // 复刻了一期的请求数据，与OpenAPI文档不太一样
            var jsonDict = new Dictionary<string, string>
            {
                ["token"] = token,
                ["appid"] = appId
            };

            var headers = new Dictionary<string, string>
            {
                ["token"] = token,
                ["appid"] = appId
            };

            // 固定超时10秒
            var timeoutCts = new CancellationTokenSource();
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var response = await HttpUtils.Post<WebcastMateInfoResponse>(
                requestUri,
                headers,
                jsonDict,
                linkedCts.Token
            );
            response.EnsureSuccessResponse();
            return response;
        }

        private RoomInfo ParseRoomInfo(WebcastInfo data)
        {
            var roomInfo = new RoomInfo()
            {
                RoomId = data.room_id.ToString(),
                Anchor = new UserInfo()
                {
                    OpenId = data.anchor_open_id ?? "",
                    AvatarUrl = data.avatar_url ?? "",
                    NickName = data.nick_name ?? ""
                }
            };
            return roomInfo;
        }

        // 兜底的履约配置
        private static readonly List<AckConfig> FallbackAckConfig = new List<AckConfig>
        {
            new AckConfig()
            {
                msg_type = LiveMsgType.live_gift,
                ack_type = 1,
                batch_interval = 10,
                batch_max_num = 3
            },
            new AckConfig()
            {
                msg_type = LiveMsgType.live_gift,
                ack_type = 2,
                batch_interval = 10,
                batch_max_num = 3
            }
        };
    }

    internal class RoomInfo : IRoomInfo
    {
        public string RoomId { get; internal set; } = "";
        public IUserInfo Anchor { get; internal set; } = new UserInfo();

        [CompilerGenerated]
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("RoomInfo");
            builder.Append(" { ");
            if (this.PrintMembers(builder))
                builder.Append(' ');
            builder.Append('}');
            return builder.ToString();
        }

        [CompilerGenerated]
        protected virtual bool PrintMembers(StringBuilder builder)
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();
            builder.Append("RoomId = ");
            builder.Append((object)this.RoomId);
            builder.Append(", Anchor = ");
            builder.Append((object)this.Anchor);
            return true;
        }
    }

    internal class UserInfo : IUserInfo
    {
        public string OpenId { get; internal set; } = "";
        public string AvatarUrl { get; internal set; } = "";
        public string NickName { get; internal set; } = "";

        [CompilerGenerated]
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("UserInfo");
            builder.Append(" { ");
            if (this.PrintMembers(builder))
                builder.Append(' ');
            builder.Append('}');
            return builder.ToString();
        }

        [CompilerGenerated]
        protected virtual bool PrintMembers(StringBuilder builder)
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();
            builder.Append("OpenId = ");
            builder.Append((object)this.OpenId);
            builder.Append(", AvatarUrl = ");
            builder.Append((object)this.AvatarUrl);
            builder.Append(", NickName = ");
            builder.Append((object)this.NickName);
            return true;
        }
    }
}
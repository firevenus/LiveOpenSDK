// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Douyin.LiveOpenSDK.Utilities;

namespace Douyin.LiveOpenSDK.Modules
{
    /// API调用信息，包含计数器、可配置重试机制。失败后自动退避重试、成功后清空退避状态
    internal class CallInfo
    {
        public string Name { get; }
        public string CallID => $"{InstanceName}#{CallCount}";

        /// 状态
        public APIState State
        {
            get => _state;
            set
            {
                _prevState = _state;
                _state = value;
            }
        }

        /// 前一个状态。 受最近一次设置`State`影响。
        public APIState PrevState => _prevState;

        /// 是否状态进行中或已成功 ( `InProgress` | `Success` )
        public bool IsInProgressOrSuccess() => _state.IsInProgressOrSuccess();

        /// 状态：已调用次数，正常只累积、不归零
        /// <remarks>在<see cref="Set12_CallAddCount"/>时变更</remarks>
        public int CallCount { get; private set; } = 0;

        /// 状态：已重试次数，并以此退避，并在成功后重置归零
        public int RetryCount { get; private set; } = 0;

        /// 状态：重试等待时间（秒），按已重试次数退避增加
        public int RetryWaitSec
        {
            get
            {
                var count = RetryCount;
                count = count <= 0 ? 0 : count;
                var wait = Conf_RetryWaitSec + count * Conf_RetryWaitSecPerCount;
                wait = wait <= 0 ? 0 : wait;
                return wait;
            }
        }

        /// 配置：重试开启
        /// <remarks>特殊情况例如app退出，应阻止重试</remarks>
        public bool Conf_RetryEnabled { get; set; }

        /// 配置：重试等待时长（秒）
        public int Conf_RetryWaitSec { get; set; } = 1;

        /// 配置：按重试次数的等待时长（秒），形成退避作用
        public int Conf_RetryWaitSecPerCount { get; set; } = 1;

        /// 配置：重试次数上限，达到后不再重试
        public int Conf_RetryCountLimit { get; set; } = 20;

        public string CallStateMsg => $"api state {CallID}: {State}";
        public string CallStateChangeMsg => $"api state {CallID}: {PrevState} -> {State}";
        internal string NextCallStateChangeMsg => $"api state {InstanceName}#{CallCount + 1}: {PrevState} -> {State}";

        internal long InstanceIndex { get; }
        internal string InstanceName { get; }

        private static SdkDebugLogger Debug => LiveOpenSDK.Debug;
        private static readonly Dictionary<string, long> s_namedCounts = new Dictionary<string, long>();
        private APIState _state;
        private APIState _prevState;
        protected static AppQuitHelper QuitHelper;

        public CallInfo(string name, bool confRetryEnabled)
        {
            Name = name;
            var counts = s_namedCounts;
            if (counts.TryGetValue(name, out long index))
                index = counts[name] + 1;
            else
                index = 1;
            counts[name] = index;
            InstanceIndex = index;
            InstanceName = index > 1 ? $"{Name}_{index}" : Name;
            Conf_RetryEnabled = confRetryEnabled;
            if (QuitHelper == null)
                QuitHelper = new AppQuitHelper(nameof(LiveOpenSDK));
        }

        /// <summary>
        /// 0. 清空状态，到None
        /// </summary>
        public void ResetAll()
        {
            var prev = State;
            State = APIState.None;
            RetryCount = 0;
            if (prev != State)
                Debug.Log(CallStateChangeMsg);
        }

        /// <summary>
        /// 0. 清空状态，到None
        /// </summary>
        public void Set0_Idle()
        {
            var prev = State;
            State = APIState.None;
            RetryCount = 0;
            if (prev != State)
                Debug.Log(CallStateChangeMsg);
        }

        /// <summary>
        /// 过程 1. 在即将调用API前，标志`InProgress`状态。也使其他调用能判断到已进行、或即将准备进行
        /// </summary>
        public void Process1_CallInProgress()
        {
            // 1.1
            Set11_CallInProgress();
            // 1.2
            Set12_CallAddCount();
        }

        /// <summary>
        /// 子过程 1.1 在即将调用API前，标志`InProgress`状态。也使其他调用能判断到已进行、或即将准备进行
        /// </summary>
        protected void Set11_CallInProgress()
        {
            State = APIState.InProgress;
            Debug.Log(CallStateChangeMsg);
        }

        /// <summary>
        /// 子过程 1.2 在即将调用API前，已经在`InProgress`状态，递增调用次数`CallCount`
        /// </summary>
        protected void Set12_CallAddCount()
        {
            ++CallCount;
            CheckAssertState(State, APIState.InProgress);
        }

        private void CheckAssertState(APIState curState, APIState expectState)
        {
            if (curState != expectState)
                Debug.LogError($"Unexpected state for {CallID}: {curState}, expect: {expectState}");
        }

        /// <summary>
        /// 过程 2. 调用API成功，设置`Success`状态
        /// </summary>
        public void Process2_Success()
        {
            RetryCount = 0;
            State = APIState.Success;
            Debug.Log($"{CallStateChangeMsg} 成功");
        }

        /// <summary>
        /// 过程 3. 调用API失败，设置`Failed`状态、决定是否要重试、等待重试
        /// </summary>
        /// <param name="extraGiveUpReasoner">放弃原因回调，做额外的条件判断后，如果返回空，表示条件通过；如果返回有内容字符串，表示不通过、放弃重试的的原因</param>
        /// <returns>isRetry 是否继续重试</returns>
        public async Task<bool> Process3_FailedAndWaitRetry(Func<string> extraGiveUpReasoner)
        {
            var callInfo = this;
            // 3.x
            var retry = callInfo.Set3x_FailedAndIsRetry(extraGiveUpReasoner);
            if (!retry)
                return false;

            // 4.1
            retry = await callInfo.Set41_AwaitNextRetryState();
            if (!retry)
                return false;

            // 4.2
            callInfo.Set42_AddRetry();
            return true;
        }

        /// <summary>
        /// 子过程 3.x 调用API失败，设置`Failed`状态，并且决定是否要重试
        /// </summary>
        /// <param name="extraGiveUpReasoner">放弃原因回调，做额外的条件判断后，如果返回空，表示条件通过；如果返回有内容字符串，表示不通过、放弃重试的的原因</param>
        /// <returns>是否继续重试</returns>
        protected bool Set3x_FailedAndIsRetry(Func<string> extraGiveUpReasoner)
        {
            // 3.1 调用API失败后，设置状态`Failed`
            State = APIState.Failed;
            // 3.2 检查是否还要重试
            var isRetry = Is3_FailedAndKeepRetry(extraGiveUpReasoner, out var reason);
            if (!isRetry)
            {
                // 3.3 如果放弃重试，`RetryCount`归零
                RetryCount = 0;
                Debug.LogWarning($"{CallStateChangeMsg} 放弃重试 reason: {reason}");
            }

            return isRetry;
        }

        /// <summary>
        /// 子过程 3.0 仅作判断，是否API失败、并判断是否要重试
        /// </summary>
        /// <param name="extraGiveUpReasoner">放弃原因回调，做额外的条件判断后，如果返回空，表示条件通过；如果返回有内容字符串，表示不通过、放弃重试的的原因</param>
        /// <param name="giveUpReason">决定放弃的原因</param>
        /// <returns>是否继续重试</returns>
        public bool Is3_FailedAndKeepRetry(Func<string> extraGiveUpReasoner, out string giveUpReason)
        {
            giveUpReason = string.Empty;
            var keepRetry = true;
            if (State == APIState.InProgress)
            {
                keepRetry = false;
                giveUpReason = "进行中，不重试";
            }

            if (State == APIState.Success)
            {
                keepRetry = false;
                giveUpReason = "已成功，不重试";
            }

            if (keepRetry && QuitHelper.IsAppQuitting())
            {
                keepRetry = false;
                giveUpReason = "app退出";
            }

            if (keepRetry && !Conf_RetryEnabled)
            {
                keepRetry = false;
                giveUpReason = "配置为不重试";
            }

            // 检查放弃原因：达到重试次数上限
            if (keepRetry && RetryCount >= Conf_RetryCountLimit)
            {
                keepRetry = false;
                giveUpReason = $"重试次数达到上限 (retry count: {RetryCount} / {Conf_RetryCountLimit})";
            }

            // 检查其他放弃原因：例如请求为协议错误（不是网络错误）
            if (keepRetry && extraGiveUpReasoner != null)
            {
                try
                {
                    giveUpReason = extraGiveUpReasoner();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            keepRetry = string.IsNullOrEmpty(giveUpReason);
            if (!keepRetry)
            {
                return false;
            }

            Debug.LogDebug($"{CallStateMsg} 可重试 (retry count: {RetryCount} / {Conf_RetryCountLimit})");
            return true;
        }

        /// <summary>
        /// 子过程 4.1 确定并等待下次重试，标志`InProgress`状态，并进行等待。
        /// </summary>
        protected async Task<bool> Set41_AwaitNextRetryState()
        {
            State = APIState.InProgress;
            var waitSec = RetryWaitSec;
            Debug.Log($"{CallStateChangeMsg} 等待重试 wait {waitSec}s");

            await Task.Delay(new TimeSpan(0, 0, waitSec));

            // check app after time elapsed
            if (!ValidateAppState(Name))
            {
                Conf_RetryEnabled = false;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 子过程 4.2 执行重试前，递增`RetryCount`表示第n次重试
        /// </summary>
        protected void Set42_AddRetry()
        {
            RetryCount++;
            CheckAssertState(State, APIState.InProgress);
            Debug.LogDebug($"{CallStateMsg} 进行重试 retry #{RetryCount}");
        }

        /// <summary>
        /// 检查app退出
        /// </summary>
        protected bool ValidateAppState(string caller)
        {
            if (QuitHelper.IsAppQuitting())
            {
                Debug.LogDebug($"\"{caller}\" stop. app is quiting.");
                return false;
            }

            return true;
        }
    }
}
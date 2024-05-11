// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using System.Runtime.CompilerServices;
using System.Text;
using ByteDance.LiveOpenSdk.Legacy;
using Newtonsoft.Json;

namespace ByteDance.LiveOpenSdk.DyCloud
{
    internal class DyCloudWebSocketError : IDyCloudWebSocketError
    {
        private readonly ConnectErrorData? _internalData;

        public DyCloudWebSocketError(string rawMessage)
        {
            RawMessage = rawMessage ?? string.Empty;
            try
            {
                _internalData = JsonConvert.DeserializeObject<ConnectErrorData>(RawMessage);
            }
            catch (Exception e)
            {
                _internalData = null;
            }
        }

        public string RawMessage { get; }

        public string? Code => _internalData?.code;

        public string? Message => _internalData?.message;

        public bool? WillReconnect => _internalData?.willReconnect;

        [CompilerGenerated]
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("DyCloudWebSocketError");
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
            builder.Append("RawMessage = ");
            builder.Append((object?)this.RawMessage);
            builder.Append(", Code = ");
            builder.Append((object?)this.Code);
            builder.Append(", Message = ");
            builder.Append((object?)this.Message);
            builder.Append(", WillReconnect = ");
            builder.Append(this.WillReconnect.ToString());
            return true;
        }
    }
}
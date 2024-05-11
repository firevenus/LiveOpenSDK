// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using ByteDance.LiveOpenSdk.Contracts;

namespace ByteDance.LiveOpenSdk.Utilities
{
    internal class OpenApiException : Exception
    {
        public OpenApiException(int errorCode, string errorMessage) : base(
            $"OpenAPI error {errorCode} ({errorMessage})")
        {
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }

        public int ErrorCode { get; }
        public string ErrorMessage { get; }
    }

    internal static class OpenApiResponseUtils
    {
        public static void EnsureSuccessResponse(this OpenApiBaseResponse response)
        {
            if (response.errCode != 0)
            {
                throw new OpenApiException(response.errCode, response.errMsg);
            }
        }
    }
}
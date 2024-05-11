// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ByteDance.LiveOpenSdk.Logging;

namespace ByteDance.LiveOpenSdk.Utilities
{
    internal static class TaskUtils
    {
        public static async Task<T> Retry<T>(
            Func<Task<T>> taskProvider,
            IEnumerable<TimeSpan> intervals,
            CancellationToken cancellationToken,
            RetryCallback? callback = null
        )
        {
            using var intervalEnumerator = intervals.GetEnumerator();
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    return await taskProvider();
                }
                catch (Exception e)
                {
                    if (intervalEnumerator.MoveNext())
                    {
                        var nextInterval = intervalEnumerator.Current;
                        callback?.Invoke(e, nextInterval);
                        await Task.Delay(nextInterval, cancellationToken);
                    }
                    else
                    {
                        callback?.Invoke(e, null);
                        throw new RetryLimitExceededException(e);
                    }
                }
            }

            throw new OperationCanceledException();
        }

        public static async Task<T> Retry<T>(
            Func<Task<T>> taskProvider,
            IEnumerable<TimeSpan> intervals,
            CancellationToken cancellationToken,
            string tagName,
            string operationName
        )
        {
            return await Retry(taskProvider, intervals, cancellationToken, ((exception, retry) =>
                    {
                        if (retry != null)
                        {
                            Log.Warning(tagName,
                                $"{operationName} fail, exception = {exception.Message}, nextRetry = {retry}");
                        }
                        else
                        {
                            Log.Error(tagName,
                                $"{operationName} fail, exception = {exception.Message}, retry limit exceeded");
                        }
                    }
                ));
        }

        public delegate void RetryCallback(Exception e, TimeSpan? nextRetry);
    }

    public class RetryLimitExceededException : Exception
    {
        public RetryLimitExceededException(Exception lastException)
            : base($"Retry limit exceeded, last exception was {lastException.Message}", lastException)
        {
        }

        public RetryLimitExceededException(string message, Exception lastException)
            : base(message, lastException)
        {
        }
    }
}
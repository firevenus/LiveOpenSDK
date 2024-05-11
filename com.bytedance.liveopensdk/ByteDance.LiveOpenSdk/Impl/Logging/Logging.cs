// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;

namespace ByteDance.LiveOpenSdk.Logging
{
    public enum Severity
    {
        Verbose,
        Debug,
        Info,
        Warning,
        Error
    }

    public struct LogItem
    {
        public Severity Severity { get; set; }
        public string Tag { get; set; }
        public string Message { get; set; }

        public void Deconstruct(out Severity severity, out string tag, out string message)
        {
            severity = Severity;
            tag = Tag;
            message = Message;
        }
    }

    public interface ILogger
    {
        event Action<LogItem>? OnLog;
        void WriteLog(LogItem item);
    }

    internal static class Log
    {
        private static ILogger Logger => LiveOpenSdk.Logger;

        public static void Write(Severity severity, string tag, string message)
        {
            var item = new LogItem()
            {
                Severity = severity,
                Tag = tag,
                Message = message
            };
            Logger.WriteLog(item);
        }

        public static void Verbose(string tag, string message)
        {
            Write(Severity.Verbose, tag, message);
        }

        public static void Debug(string tag, string message)
        {
            Write(Severity.Debug, tag, message);
        }

        public static void Info(string tag, string message)
        {
            Write(Severity.Info, tag, message);
        }

        public static void Warning(string tag, string message)
        {
            Write(Severity.Warning, tag, message);
        }

        public static void Error(string tag, string message)
        {
            Write(Severity.Error, tag, message);
        }
    }
}
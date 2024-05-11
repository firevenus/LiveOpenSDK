// Copyright (c) Bytedance. All rights reserved.
// Description:

using System;
using ByteDance.LiveOpenSdk.Logging;

namespace ByteDance.LiveOpenSdk.Utilities
{
    public static class CommandLineUtils
    {
        private const string Tag = "CommandLineUtils";
        private const string ArgPrefixToken = "-token=";

        private static bool _isParsed;
        private static string? _launchToken;

        public static string LaunchToken
        {
            get
            {
                ParseCommandLineOnce();
                return _launchToken ?? "";
            }
        }

        private static void ParseCommandLineOnce()
        {
            if (_isParsed) return;

            var cmdLine = Environment.CommandLine;
            Log.Debug(Tag, $"Parsing command line: {cmdLine}");
            var argv = Environment.GetCommandLineArgs();

            foreach (var arg in argv)
            {
                var argTrimmed = arg.Trim();
                if (argTrimmed.StartsWith(ArgPrefixToken))
                {
                    _launchToken = argTrimmed.Substring(ArgPrefixToken.Length);
                    Log.Debug(Tag, $"Launch token is {_launchToken}");
                }
            }

            if (string.IsNullOrEmpty(_launchToken))
            {
                Log.Warning(Tag, "Launch token not found");
            }

            _isParsed = true;
        }
    }
}
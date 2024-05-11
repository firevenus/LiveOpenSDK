// Copyright (c) Bytedance. All rights reserved.
// Description:

using System.IO;
using UnityEditor;
using UnityEngine;

namespace Douyin.LiveOpenSDK.Editor
{
    public static class SdkEditorTool
    {
        public const string SdkPackagePath = "Packages/com.bytedance.liveopensdk";

        public static void CopyFile(string sourceFilePath, string targetFolder)
        {
            Debug.Log($"CopyFile start, {sourceFilePath}, to: {targetFolder}");
            if (!Directory.Exists(targetFolder))
                Directory.CreateDirectory(targetFolder);

            var srcFullPath = Path.GetFullPath(sourceFilePath);
            var destFullPath = Path.Combine(targetFolder, Path.GetFileName(sourceFilePath));
            File.Copy(srcFullPath, destFullPath, overwrite: true);
            Debug.Log($" - CopyFile finish, {sourceFilePath} to {destFullPath}");
        }

        public static bool IsBuildForMobile()
        {
            var activeTarget = EditorUserBuildSettings.activeBuildTarget;
            return activeTarget switch
            {
                BuildTarget.iOS => true,
                BuildTarget.Android => true,
                _ => false
            };
        }

        public static BuildTarget ActiveBuildTarget => EditorUserBuildSettings.activeBuildTarget;
    }
}
// Copyright (c) Bytedance. All rights reserved.
// Description:

using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.UnityLinker;
using UnityEngine;

namespace Douyin.LiveOpenSDK.Editor
{
    /// <summary>
    /// 自动添加linker信息
    /// </summary>
    /// <remarks>参考：https://forum.unity.com/threads/the-current-state-of-link-xml-in-packages.995848/#post-7223887</remarks>
    /// <remarks>参考：https://docs.unity3d.com/2019.4/Documentation/ScriptReference/Build.IUnityLinkerProcessor.GenerateAdditionalLinkXmlFile.html</remarks>
    // note: 修正兼容性：il2cpp 异常 #3011504827 https://meego.feishu.cn/sc_game/issue/detail/3011504827
    //  - 重现时：dyCloudUnitySDK.dll 里面 tokenToSid 的 json deserialize 会 exception。
    //  - 如果只做简单修复、让`Sid`的构造函数被preserve的话，重现情况会变为：`Sid obj` 会拿到是空的、sid会取到null。
    //  - 因此最后决定，添加linker信息、把dycloud unity sdk dll的全部类型、全部字段保留，避免被il2cpp strip
    public class SdkLinkerProcess : IUnityLinkerProcessor
    {
        private const string _linkXML = "Packages/com.bytedance.liveopensdk/Runtime/link.xml";

        public int callbackOrder { get; }

        public string GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data)
        {
            Debug.Log($"SdkLinkerProcess, add LinkXmlFile: {_linkXML}");
            var path = Path.GetFullPath(_linkXML);
            Debug.Log($"SdkLinkerProcess, full path: {path}");
            return path;
        }

        public void OnBeforeRun(BuildReport report, UnityLinkerBuildPipelineData data)
        {
        }

        public void OnAfterRun(BuildReport report, UnityLinkerBuildPipelineData data)
        {
        }
    }
}
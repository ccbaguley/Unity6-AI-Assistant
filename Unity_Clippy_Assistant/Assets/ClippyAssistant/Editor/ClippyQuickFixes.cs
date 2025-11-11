#if UNITY_EDITOR
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ClippyAssistant
{
    public static class ClippyQuickFixes
    {
        public static bool FixHandlesCaps(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath)) return false;
            string src = File.ReadAllText(assetPath);
            string bak = assetPath + ".bak";
            File.WriteAllText(bak, src);

            var rxCall = new Regex(@"Handles\.(\w+?)Cap\s*\(", RegexOptions.Multiline);
            src = rxCall.Replace(src, m => $"Handles.{m.Groups[1].Value}HandleCap(");

            var rxFourArgs = new Regex(@"Handles\.(\w+?)HandleCap\s*\(\s*([^\)]*?)\)\s*;", RegexOptions.Multiline);
            src = rxFourArgs.Replace(src, (Match m) =>
            {
                string args = m.Groups[2].Value;
                int commas = 0; foreach (var ch in args) if (ch == ',') commas++;
                if (commas == 3 && !args.Contains("EventType")) // 4 args → add 5th
                    return $"Handles.{m.Groups[1].Value}HandleCap({args}, EventType.Repaint);";
                return m.Value;
            });

            File.WriteAllText(assetPath, src);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Clippy: Applied Handles cap fix. Backup: {bak}");
            return true;
        }

        public static bool FixHdrpInclude(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath)) return false;
            string src = File.ReadAllText(assetPath);
            string bak = assetPath + ".bak";
            File.WriteAllText(bak, src);

            src = src.Replace(
                "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl",
                "Packages/com.unity.render-pipelines.core/ShaderLibrary/ShaderVariables.hlsl"
            );

            File.WriteAllText(assetPath, src);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Clippy: Updated HDRP include path. Backup: {bak}");
            return true;
        }

        public static bool FixUrpTexelRedef(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath)) return false;
            string src = File.ReadAllText(assetPath);
            string bak = assetPath + ".bak";
            File.WriteAllText(bak, src);

            src = Regex.Replace(src, @"\bfloat4\s+_CameraDepthTexture_TexelSize\s*;\s*", "", RegexOptions.Multiline);
            src = Regex.Replace(src, @"\bsampler2D\s+_CameraDepthTexture\s*;\s*", "", RegexOptions.Multiline);

            if (!src.Contains("DeclareDepthTexture.hlsl"))
            {
                var coreInc = "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl";
                int idx = src.IndexOf(coreInc, StringComparison.Ordinal);
                if (idx >= 0)
                {
                    int lineEnd = src.IndexOf('\n', idx);
                    if (lineEnd < 0) lineEnd = idx;
                    src = src.Insert(lineEnd + 1, "#include \"Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl\"\n");
                }
            }

            File.WriteAllText(assetPath, src);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Clippy: Removed duplicate _CameraDepthTexture_TexelSize; ensured URP depth include. Backup: {bak}");
            return true;
        }
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ClippyAssistant
{
    [Serializable]
    public class ClippySuggestion
    {
        public string id;
        public string title;
        public string details;
        public string[] quickFixIds;
        public string filePath;
        public int line;
    }

    public static class ClippyRuleEngine
    {
        static readonly Regex RX_CS0246 = new Regex(@"CS0246.*(?<type>[A-Za-z0-9_\.]+).*", RegexOptions.Compiled);
        static readonly Regex RX_CS0266 = new Regex(@"CS0266.*IXRHoverInteractable.*IXRSelectInteractable", RegexOptions.Compiled);
        static readonly Regex RX_HANDLES_CAP = new Regex(@"'Handles' does not contain a definition for '(?<cap>\w+Cap)'", RegexOptions.Compiled);
        static readonly Regex RX_ONSCENE_SERIALIZED = new Regex(@"serializedObject should not be used inside On(Scene|Preview)GUI", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex RX_HDRP_INCLUDE = new Regex(@"Couldn't open include file 'Packages/com\.unity\.render-pipelines\.high-definition/Runtime/ShaderLibrary/ShaderVariables\.hlsl'", RegexOptions.Compiled);
        static readonly Regex RX_URP_TEXEL_REDEF = new Regex(@"redefinition of '_CameraDepthTexture_TexelSize'", RegexOptions.Compiled);
        static readonly Regex RX_TRANSIENT = new Regex(@"TransientArtifactProvider::IsTransientArtifact", RegexOptions.Compiled);
        static readonly Regex RX_SERVER_RPC = new Regex(@"ServerRpc can only be used.*NetworkBehaviour", RegexOptions.Compiled);
        static readonly Regex RX_NETWORKTRANSFORM = new Regex(@"'NetworkTransform' could not be found", RegexOptions.Compiled);

        public static List<ClippySuggestion> GetSuggestions(string message, string stack, string pathHint)
        {
            var list = new List<ClippySuggestion>();

            if (RX_CS0266.IsMatch(message))
            {
                list.Add(new ClippySuggestion{
                    id="xri_ixr_hover_select",
                    title="XRI 3: hovered is IXRHoverInteractable; need IXRSelectInteractable",
                    details="Filter hovered list to IXRSelectInteractable and cast. Example: var best = interactor.interactablesHovered.FirstOrDefault(i => i is IXRSelectInteractable) as IXRSelectInteractable;",
                });
            }

            var mCap = RX_HANDLES_CAP.Match(message);
            if (mCap.Success)
            {
                string oldCap = mCap.Groups["cap"].Value;
                string newCap = oldCap.Replace("Cap", "HandleCap");
                list.Add(new ClippySuggestion{
                    id="handles_cap_rename",
                    title=$"Replace Handles.{oldCap} → Handles.{newCap}",
                    details="Unity 6 removed old Cap APIs. Use *HandleCap variants and include EventType argument.",
                    quickFixIds=new[]{ "fix_handles_caps" },
                    filePath=pathHint
                });
            }

            if (RX_ONSCENE_SERIALIZED.IsMatch(message))
            {
                list.Add(new ClippySuggestion{
                    id="editor_onScene_serialized",
                    title="Don't use SerializedObject in OnSceneGUI/OnPreviewGUI",
                    details="Cast `(MyComp)target` and use direct field access with Undo.RecordObject; keep SerializedObject inside OnInspectorGUI only.",
                    filePath=pathHint
                });
            }

            if (RX_HDRP_INCLUDE.IsMatch(message))
            {
                list.Add(new ClippySuggestion{
                    id="hdrp_include_core",
                    title="Update HDRP include path → SRP Core",
                    details="Replace the HDRP include with the SRP Core path for ShaderVariables.hlsl.",
                    quickFixIds=new[]{ "fix_hdrp_include" },
                    filePath=pathHint
                });
            }

            if (RX_URP_TEXEL_REDEF.IsMatch(message))
            {
                list.Add(new ClippySuggestion{
                    id="urp_depth_texel_redef",
                    title="Remove manual _CameraDepthTexture_TexelSize declaration",
                    details="URP already declares it via DeclareDepthTexture.hlsl; delete manual declarations and include the header.",
                    quickFixIds=new[]{ "fix_urp_texel_redef" },
                    filePath=pathHint
                });
            }

            if (RX_TRANSIENT.IsMatch(message))
            {
                list.Add(new ClippySuggestion{
                    id="editor_transient_artifacts",
                    title="AssetDatabase called during import—defer work",
                    details="Guard with EditorApplication.isUpdating/isCompiling and wrap AssetDatabase ops with EditorApplication.delayCall."
                });
            }

            if (RX_SERVER_RPC.IsMatch(message))
            {
                list.Add(new ClippySuggestion{
                    id="ngo_serverrpc",
                    title="ServerRpc requires NetworkBehaviour",
                    details="Change class base to NetworkBehaviour and ensure a NetworkObject exists on the GameObject."
                });
            }

            if (RX_NETWORKTRANSFORM.IsMatch(message))
            {
                list.Add(new ClippySuggestion{
                    id="ngo_networktransform",
                    title="Missing NetworkTransform (optional)",
                    details="Install package that provides Unity.Netcode.Components.NetworkTransform or remove the dependency.",
                });
            }

            var m = RX_CS0246.Match(message);
            if (m.Success)
            {
                var type = m.Groups["type"].Value;
                list.Add(new ClippySuggestion{
                    id="cs0246_missing_type",
                    title=$"Type or namespace not found: {type}",
                    details="Add the correct using directive or install/enable the package that defines this type; check asmdef references."
                });
            }
            return list;
        }
    }
}
#endif

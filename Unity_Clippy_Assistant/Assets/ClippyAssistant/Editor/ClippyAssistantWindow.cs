#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ClippyAssistant
{
    public class ClippyAssistantWindow : EditorWindow
    {
        [MenuItem("Window/Clippy Assistant")]
        public static void Open() => GetWindow<ClippyAssistantWindow>("Clippy Assistant");

        class LogEntry
        {
            public string message, stack, path;
            public int line;
            public LogType type;
        }

        Vector2 _scroll;
        readonly List<LogEntry> _entries = new();
        readonly List<ClippySuggestion> _current = new();
        bool _auto;
        string _filter = "Error";

        void OnEnable()
        {
            Application.logMessageReceived += OnLog;
            ScanConsoleNow();
        }
        void OnDisable() { Application.logMessageReceived -= OnLog; }

        void OnLog(string condition, string stacktrace, LogType type)
        {
            if (type == LogType.Log) return;
            _entries.Add(Parse(condition, stacktrace, type));
            if (_auto) AnalyzeLast();
            Repaint();
        }

        void OnGUI()
        {
            using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Scan Console", EditorStyles.toolbarButton)) ScanConsoleNow();
                _auto = GUILayout.Toggle(_auto, "Auto", EditorStyles.toolbarButton);
                GUILayout.FlexibleSpace();
                _filter = GUILayout.TextField(_filter, EditorStyles.toolbarTextField, GUILayout.Width(160));
            }

            _scroll = GUILayout.BeginScrollView(_scroll);
            foreach (var e in _entries)
            {
                if (!string.IsNullOrEmpty(_filter) &&
                    !e.type.ToString().Contains(_filter, StringComparison.OrdinalIgnoreCase) &&
                    !e.message.Contains(_filter, StringComparison.OrdinalIgnoreCase))
                    continue;

                DrawEntry(e);
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("Analyze Last", GUILayout.Height(24)))
                AnalyzeLast();

            foreach (var s in _current) DrawSuggestion(s);
        }

        void DrawEntry(LogEntry e)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label($"[{e.type}] {e.message}", EditorStyles.boldLabel);
            if (!string.IsNullOrEmpty(e.path)) GUILayout.Label($"{e.path}:{e.line}", EditorStyles.miniLabel);
            using (new GUILayout.HorizontalScope())
            {
                if (!string.IsNullOrEmpty(e.path) && GUILayout.Button("Open File")) OpenAt(e.path, e.line);
                if (GUILayout.Button("Analyze"))
                {
                    _current.Clear();
                    _current.AddRange(ClippySettings.backend.Analyze(e.message, e.stack, e.path));
                }
            }
            GUILayout.EndVertical();
            GUILayout.Space(4);
        }

        void DrawSuggestion(ClippySuggestion s)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(s.title, EditorStyles.boldLabel);
            GUILayout.Label(s.details, EditorStyles.wordWrappedLabel);
            if (s.quickFixIds != null && s.quickFixIds.Length > 0 && !string.IsNullOrEmpty(s.filePath))
            {
                using (new GUILayout.HorizontalScope())
                {
                    foreach (var id in s.quickFixIds)
                        if (GUILayout.Button($"Quick Fix: {id}")) ApplyQuickFix(id, s.filePath);
                }
            }
            GUILayout.EndVertical();
            GUILayout.Space(6);
        }

        void ApplyQuickFix(string id, string path)
        {
            bool ok = false;
            switch (id)
            {
                case "fix_handles_caps": ok = ClippyQuickFixes.FixHandlesCaps(path); break;
                case "fix_hdrp_include": ok = ClippyQuickFixes.FixHdrpInclude(path); break;
                case "fix_urp_texel_redef": ok = ClippyQuickFixes.FixUrpTexelRedef(path); break;
            }
            if (!ok) EditorUtility.DisplayDialog("Clippy", "Quick Fix failed or not applicable.", "OK");
        }

        void AnalyzeLast()
        {
            if (_entries.Count == 0) return;
            var e = _entries[_entries.Count - 1];
            _current.Clear();
            _current.AddRange(ClippySettings.backend.Analyze(e.message, e.stack, e.path));
        }

        void ScanConsoleNow()
        {
            _entries.Clear();
            // Best-effort reflection read of Console; it's internal.
            try
            {
                var logEntriesType = Type.GetType("UnityEditor.LogEntries, UnityEditor");
                var getCount = logEntriesType.GetMethod("GetCount", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                var getEntry = logEntriesType.GetMethod("GetEntryInternal", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                var startGetting = logEntriesType.GetMethod("StartGettingEntries", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                var endGetting = logEntriesType.GetMethod("EndGettingEntries", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                var logEntryType = Type.GetType("UnityEditor.LogEntry, UnityEditor");
                var entry = Activator.CreateInstance(logEntryType);

                startGetting.Invoke(null, null);
                int count = (int)getCount.Invoke(null, null);
                for (int i = 0; i < count; i++)
                {
                    object[] args = new object[] { i, entry };
                    getEntry.Invoke(null, args);
                    var msg = (string)logEntryType.GetField("message").GetValue(entry);
                    var mode = (int)logEntryType.GetField("mode").GetValue(entry);
                    var file = (string)logEntryType.GetField("file").GetValue(entry);
                    var line = (int)logEntryType.GetField("line").GetValue(entry);

                    var type = (mode & 1) != 0 ? LogType.Error : ((mode & 2) != 0 ? LogType.Warning : LogType.Log);
                    _entries.Add(new LogEntry{ message=msg, stack="", path=file, line=line, type=type});
                }
                endGetting.Invoke(null, null);
            }
            catch { /* reflection may fail on future versions */ }
        }

        LogEntry Parse(string condition, string stacktrace, LogType type)
        {
            string path = ""; int line = 0;
            var m = Regex.Match(condition, @"(Assets/[^:\n\r]+)\((\d+)\)");
            if (m.Success) { path = m.Groups[1].Value; int.TryParse(m.Groups[2].Value, out line); }
            return new LogEntry{ message=condition, stack=stacktrace, path=path, line=line, type=type };
        }

        static void OpenAt(string path, int line)
        {
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (obj) AssetDatabase.OpenAsset(obj, line > 0 ? line : 1);
            else EditorUtility.RevealInFinder(path);
        }
    }
}
#endif

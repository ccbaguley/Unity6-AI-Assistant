# Clippy Assistant (Unity 6 / Editor Tool)
A friendly Editor window that watches Console logs and suggests probable fixes for common Unity 6 issues.
No internet required. Includes optional quick-fix patches for safe patterns.

## Install
1. Unzip into `Assets/` so you get `Assets/ClippyAssistant/Editor/`.
2. Open **Window → Clippy Assistant**.

## Features
- Live console watcher (errors & warnings)
- Open-at-file/line
- Rule-based fix hints (XRI, URP/HDRP shader issues, Handles Cap renames, NGO basics)
- Optional Quick Fixes with file backups

## Extend
Implement `IAssistantBackend` for your own AI or service; default backend is local rules.

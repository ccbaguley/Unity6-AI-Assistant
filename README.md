# Clippy Assistant (Unity 6 / Editor Tool)

A friendly Unity Editor window that watches Console logs and suggests probable fixes for common **Unity 6** issues
(URP/HDRP shader includes, XR Interaction Toolkit API changes, Handles Cap rename, NGO basics, etc.).  
Think of it as a lightweight, local, “paperclip” helper—**no internet required**.

> Tested with **Unity 6 (6000.0.40f1)** on Windows (DX11), OpenXR + URP projects.

---

## ✨ Features

- Live **Console watcher** (errors & warnings)
- **Open file at line** (one click)
- Targeted **Fix Suggestions** (rule-based)
  - XRI 3.x: `IXRHoverInteractable` vs `IXRSelectInteractable`
  - Handles `*Cap` → `*HandleCap` API changes (adds `EventType` arg if missing)
  - `serializedObject` used in `OnSceneGUI/OnPreviewGUI`
  - HDRP include path → **SRP Core** replacement
  - URP `_CameraDepthTexture_TexelSize` redefinition
  - `TransientArtifactProvider` / AssetDatabase during import
  - NGO: `ServerRpc` needs `NetworkBehaviour`, missing `NetworkTransform`
- **Quick Fix** buttons with automatic `.bak` backups

---

## 📦 Install

1. Copy this folder into your Unity project so paths look like:
2. In Unity: **Window → Clippy Assistant**.

No packages needed; this is Editor-only.

---

## 🧪 Usage

1. Open the window → press **Scan Console** (or toggle **Auto**).
2. Click any log entry → **Analyze** to see suggestions.
3. If a suggestion offers a **Quick Fix**, click it.  
A `.bak` file is created next to the original in case you want to revert.

> Tip: Use **Auto** to re-scan every second while you work.

---

## 🧰 Extend it

The suggestion engine is pluggable.  
Implement `IAssistantBackend` and return `List<ClippySuggestion>` — then set your backend in `ClippySettings`.

```csharp
public class MyCloudBackend : IAssistantBackend {
public List<ClippySuggestion> Analyze(string msg, string stack, string path) {
 // call your service, map results → ClippySuggestion
}
}

# Cognitive3D Unity SDK Technical Reference

This file provides guidance to AI coding tools when working with this repository. It combines a fast docs-routing layer with SDK-specific architecture knowledge not easily found in the docs.

**Primary docs root:** https://docs.cognitive3d.com/

## CRITICAL RULES

1. **DISCOVERY BEFORE IMPLEMENTATION.** When a user asks to integrate, set up, or add Cognitive3D analytics to their project, you MUST ask the Pre-Implementation Discovery questions below and wait for the user's answers BEFORE writing any code, making any file changes, or suggesting specific implementation steps. Do NOT skip this step.

2. **NEVER read, log, store, or output API keys, developer keys, or any credentials** found in `Cognitive3D_Preferences` assets, environment variables, config files, or user code. Do not open or read files that are likely to contain credentials (e.g., `Cognitive3D_Preferences.asset`). When setting up the SDK, instruct the user to enter their API key themselves through the Cognitive3D Project Setup wizard.

## Operating Rules

1. **This file is advisory, not authoritative.** Treat the linked docs pages as the source of truth.
2. **Use the deepest relevant page first.** Do not answer from the docs root when a feature page exists.
3. **Escalate to live docs when freshness matters.** If the question involves latest versions, compatibility, release notes, hardware support, or API/MCP auth — verify from the live page.
4. **If browsing is unavailable, answer with clear caveats.** Give the best likely page(s) to confirm.
5. **Stay at the user's level.** Do not dump low-level implementation detail unless asked.

### Trust hierarchy

1. **Exact live feature page** — e.g., Unity Dynamic Objects, Unity Custom Events
2. **Engine landing page** — e.g., Unity minimal setup guide
3. **Release pages / repositories** — for latest versions, release notes, compatibility
4. **Docs portal root** — when the question is broad or needs routing
5. **API/Data and MCP docs** — for programmatic access, not instrumentation

---

## Pre-Implementation Discovery

**Present ALL of these questions to the user and wait for their answers before doing anything else.** You may briefly explore the project structure to understand what exists, but do NOT suggest implementations, write code, or make changes until the user has responded.

### Project Context

1. **What is the experience about?** — e.g., training simulation, VR game, therapy app, virtual showroom, architectural walkthrough
2. **Who are the target end users?** — e.g., patients, students, employees, consumers, research participants
3. **What insights are you trying to gain?** — e.g., user engagement, task completion rates, error tracking, comfort/safety metrics, training effectiveness
4. **What are the key interactions in the experience?** — e.g., picking up objects, navigating menus, following instructions, completing tasks
5. **Are there specific KPIs or success metrics?** — e.g., time to complete, accuracy, drop-off points, areas of interest

### Technical Setup

6. **Which SDK features do you need?** — Custom events, sensors, dynamic objects, gaze tracking, exit polls, participants, audio recording, etc.

After receiving answers, use the project context to recommend *what* to track and *why*. Use the technical setup to determine *how* to implement it.

---

## Common Implementation Mental Model

1. **Create/choose a project** in the dashboard
2. **Get the right keys** (Developer Key retrieves Application Key)
3. **Install the SDK** via Unity Package Manager
4. **Associate the app** with the project and configure scene handling
5. **Start and end sessions** correctly
6. **Attach session metadata** — participant info, tags, session properties
7. **Record telemetry** — gaze/fixations, custom events, sensors, dynamic objects, exit polls, remote controls, media, local cache
8. **Upload scenes/meshes/object geometry**
9. **Validate in dashboard** — replay, scene/object views, analysis, performance
10. **Troubleshoot** if data is missing

### Canonical Nouns

Organization, Project, Scene, Scene Version, Session, Participant, Dynamic Object, Custom Event, Sensor, ExitPoll, Remote Controls

Dashboard Concepts: https://docs.cognitive3d.com/dashboard/concepts/

---

## Fast Route by Question Type

### "How do I install the SDK?"
- Unity minimal setup: https://docs.cognitive3d.com/unity/minimal-setup-guide/
- UPM git URL: `https://github.com/CognitiveVR/cvr-sdk-unity.git`
- Latest release: https://github.com/CognitiveVR/cvr-sdk-unity/releases/latest

### "How do I configure keys/auth?"
- Unity setup page: https://docs.cognitive3d.com/unity/minimal-setup-guide/
- Uses Developer Key to retrieve Application Key from dashboard

### "How do I upload scenes?"
- Unity Scenes: https://docs.cognitive3d.com/unity/scenes/
- **This is an Editor workflow.** No code involved — use Scene Manager (SDK 2.3+) or Project Setup.

### "How do I track dynamic objects?"
- Unity Dynamic Objects: https://docs.cognitive3d.com/unity/dynamic-objects/
- **Primarily an Editor workflow.** Use **Feature Builder > Dynamic Objects** for component setup, mesh export, and upload.
- **Only use code (ID Pools) when** objects are spawned at runtime (e.g., projectiles, procedural items, networked avatars).

### "How do I record custom events?"
- Unity Custom Events: https://docs.cognitive3d.com/unity/customevents/

### "How do I track gaze and fixations?"
- Unity Gaze/Fixations: https://docs.cognitive3d.com/unity/gaze-fixations/
- Fixations explainer: https://docs.cognitive3d.com/fixations/
- Supported hardware: https://docs.cognitive3d.com/hardware/

### "How do I record sensors/performance?"
- Unity Sensors: https://docs.cognitive3d.com/unity/sensors/
- Unity Performance: https://docs.cognitive3d.com/unity/performance/

### "How do I add session/participant metadata?"
- Comprehensive setup: https://docs.cognitive3d.com/unity/comprehensive-setup-guide/
- Participants: https://docs.cognitive3d.com/unity/participants/

### "How do I ask users questions in-app?"
- Unity ExitPoll: https://docs.cognitive3d.com/unity/exitpoll/

### "How do I control runtime behavior remotely?"
- Unity Remote Controls: https://docs.cognitive3d.com/unity/remote-controls/
- Dashboard Remote Controls: https://docs.cognitive3d.com/dashboard/remote-controls/

### "How do I access data programmatically?"
- API get started: https://docs.cognitive3d.com/api/get-started/
- Postman docs: https://docs.api.cognitive3d.com/

### "How do I expose Cognitive3D to an AI client or MCP?"
- MCP getting started: https://docs.cognitive3d.com/mcp-server/getting-started/
- Note: MCP is a data/tool access layer, not the SDK instrumentation layer.

### Best-answer routing summary

- **installation/setup** → minimal setup, then comprehensive setup
- **consent/session lifecycle** → comprehensive setup → Begin and End Sessions
- **metadata/tags/properties** → comprehensive setup → Session Name / Session Property / Session Tags
- **events** → Custom Events
- **interactables/controllers/hands** → Dynamic Objects
- **dashboard views** → Session Details, Analysis Tool, Objectives, Scene/Object views
- **missing data** → Project Validation, Troubleshooting, Data Uploader, Performance

---

## Project Overview

Cognitive3D Unity SDK (`com.cognitive3d.c3d-sdk`) — an analytics platform for VR/AR/MR experiences. Distributed as a Unity Package Manager (UPM) package. Requires Unity 2021.3 LTS or newer.

This is a UPM package, not a standalone Unity project. There is no build command or test suite. No CI/CD pipelines, linters, or automated tests configured. To develop: import into a Unity project via Package Manager and iterate using the Unity Editor.

## Architecture

### Assembly Structure

Two assemblies with strict dependency direction:

- **Runtime (`Cognitive3D.asmdef`)** — Core SDK. Auto-referenced, ships in builds. Exposes internals to the editor assembly via `InternalsVisibleTo`.
- **Editor (`Cognitive3DEditor.asmdef`)** — Editor-only tools, inspectors, and setup wizards. Never included in builds.

### Runtime Core (`Runtime/Scripts/` and `Runtime/Internal/`)

- **`Cognitive3D_Manager`** — Singleton entry point (`DefaultExecutionOrder(-50)`). Manages session lifecycle, scene transitions, and data collection orchestration.
- **`Cognitive3D_Preferences`** — ScriptableObject loaded from `Resources/`. Stores API keys, scene settings, and SDK configuration.
- **`NetworkManager`** — Handles all HTTP communication with Cognitive3D backend.
- **`DualFileCache`** — Persistent local cache for offline data batching.
- **Serialization** (`Runtime/Internal/Serialization/`) — Custom JSON serialization via `JsonUtil`, `CoreInterface`, `SharedCore`, and `StringBuilderExtensions`.

### Component System (`Runtime/Components/`)

31 sensor/feature components inherit from **`AnalyticsComponentBase`**. Each component self-describes via `GetDescription()`, `GetWarning()`, and `GetError()` methods (used by editor reflection for UI generation). Components auto-register on session begin.

### Subsystems

- **Gaze Tracking** — `GazeBase`, `PhysicsGaze`, `GazeHelper`, `GazeCore` (internal), `FixationRecorder`, `EyeCapture`
- **Dynamic Objects** — `DynamicObject` component + `DynamicManager` (internal). Three ID modes: CustomID, GeneratedID, PoolID (via `DynamicObjectIdPool`)
- **Custom Events** — `CustomEvent` class for app-specific analytics
- **Sensors** — `SensorRecorder` for time-series data
- **ExitPoll** (`Runtime/ExitPoll/`) — In-VR survey system with pointer handling and microphone input
- **ActiveSessionView** (`Runtime/ActiveSessionView/`) — Real-time debug visualization overlay

### Editor Tools (`Editor/`)

- **`EditorCore`** (~3300 lines) — Central editor orchestration
- **Project Setup / Scene Manager** — Wizard windows for SDK configuration
- **GLTF Export** (`Editor/GLTF/`) — Scene export with shader property mappings for Built-in, HDRP, and URP pipelines
- **Feature Builder** (`Editor/Features/`) — `FeatureLibrary` registry + per-feature GUI panels
- **Project Validation** (`Editor/ProjectValidation/`) — Automated setup checks

### Conditional Compilation

The SDK supports many XR platforms via preprocessor defines auto-set by version defines in the `.asmdef` files. Key symbols:

- `C3D_STEAMVR2`, `C3D_OCULUS`, `C3D_PICOXR`, `C3D_VIVEWAVE` — Platform SDKs
- `C3D_HDRP`, `C3D_URP` — Render pipelines
- `C3D_TMPRO` — TextMeshPro support
- `COGNITIVE3D_INCLUDE_COREUTILITIES`, `COGNITIVE3D_INCLUDE_META_CORE_*` — Feature-level toggles

All 31 assembly references in `Cognitive3D.asmdef` are optional — the SDK compiles without any of them installed.

---

## SDK-Specific Knowledge

### Objectives

Objectives are a configurable sequence of required steps used to evaluate processes you want Participants to perform — e.g., training protocols, task workflows, sequential behaviour validation.

**When to recommend:** If a user asks about tracking success/failure, task completion, step-by-step workflows, process compliance, or training evaluation.

**How they work:**
- **Dashboard side:** Objectives are created and configured on the dashboard. Each defines ordered Steps matched by Custom Event names.
- **Unity side:** Send Custom Events that correspond to Steps. No special Objective API — use `CustomEvent` (e.g., `new CustomEvent("StepName").Send()`).
- **Review:** [Objectives Summary](https://docs.cognitive3d.com/dashboard/objectives-summary/) and [Objective Details](https://docs.cognitive3d.com/dashboard/objective-details/)
- **Create:** [Creating Objectives](https://docs.cognitive3d.com/dashboard/creating-objectives/)

### Custom Shaders

When a project uses custom shaders, materials appear white on the dashboard because the GLTF exporter can't map custom shader properties to standard PBR. Check for custom shaders during setup. If present, refer to the [Custom Shaders troubleshooting guide](https://docs.cognitive3d.com/unity/troubleshooting/#custom-shaders) and create an `ExportShaderProperties` script **before** uploading the scene.

---

## Docs Directory

### Unity SDK

| Category | Pages |
|---|---|
| **Setup** | [Minimal Setup](https://docs.cognitive3d.com/unity/minimal-setup-guide/), [Comprehensive Setup](https://docs.cognitive3d.com/unity/comprehensive-setup-guide/), [Feature Builder](https://docs.cognitive3d.com/unity/feature-builder/), [Pre-launch Checklist](https://docs.cognitive3d.com/unity/prelaunch-checklist/), [Project Validation](https://docs.cognitive3d.com/unity/project-validation/) |
| **Core Features** | [Scenes](https://docs.cognitive3d.com/unity/scenes/), [Custom Events](https://docs.cognitive3d.com/unity/customevents/), [Dynamic Objects](https://docs.cognitive3d.com/unity/dynamic-objects/), [Gaze/Fixations](https://docs.cognitive3d.com/unity/gaze-fixations/), [ExitPoll](https://docs.cognitive3d.com/unity/exitpoll/), [Sensors](https://docs.cognitive3d.com/unity/sensors/), [Participants](https://docs.cognitive3d.com/unity/participants/), [Remote Controls](https://docs.cognitive3d.com/unity/remote-controls/), [Audio Recording](https://docs.cognitive3d.com/unity/audio-recording/) |
| **Extra** | [Ready Room](https://docs.cognitive3d.com/unity/ready-room/), [Active Session View](https://docs.cognitive3d.com/unity/active-session-view/), [Built-In Components](https://docs.cognitive3d.com/unity/components/), [Media & 360](https://docs.cognitive3d.com/unity/media/), [Multiplayer](https://docs.cognitive3d.com/unity/multiplayer/), [Local Cache](https://docs.cognitive3d.com/unity/local-cache/) |
| **Advanced** | [Preferences](https://docs.cognitive3d.com/unity/preferences/), [Data Uploader](https://docs.cognitive3d.com/unity/data-uploader/), [HMD Info](https://docs.cognitive3d.com/unity/hmd-specific-info/), [Troubleshooting](https://docs.cognitive3d.com/unity/troubleshooting/), [Performance](https://docs.cognitive3d.com/unity/performance/) |

### Dashboard

[Concepts](https://docs.cognitive3d.com/dashboard/concepts/) · [Session Replay](https://docs.cognitive3d.com/dashboard/session-replay/) · [Project Overview](https://docs.cognitive3d.com/dashboard/project-overview/) · [App Performance](https://docs.cognitive3d.com/dashboard/app-performance/) · [Scene Viewer](https://docs.cognitive3d.com/dashboard/scene-viewer/) · [Session Details](https://docs.cognitive3d.com/dashboard/session-details/) · [Object Explorer](https://docs.cognitive3d.com/dashboard/object-explorer/) · [Objectives](https://docs.cognitive3d.com/dashboard/objectives-summary/) · [Simple Analysis](https://docs.cognitive3d.com/dashboard/simple-analysis/) · [Advanced Analysis](https://docs.cognitive3d.com/dashboard/advanced-analysis/) · [Data Export](https://docs.cognitive3d.com/dashboard/data-export/) · [ExitPoll Results](https://docs.cognitive3d.com/dashboard/exitpoll-results/)

### API & MCP

[API Get Started](https://docs.cognitive3d.com/api/get-started/) · [Postman Docs](https://docs.api.cognitive3d.com/) · [MCP Getting Started](https://docs.cognitive3d.com/mcp-server/getting-started/) · [MCP Sessions](https://docs.cognitive3d.com/mcp-server/sessions/) · [MCP Objectives](https://docs.cognitive3d.com/mcp-server/objectives/)

### General Reference

[Docs Root](https://docs.cognitive3d.com/) · [SDK Downloads](https://docs.cognitive3d.com/download/) · [Supported Hardware](https://docs.cognitive3d.com/hardware/) · [Fixations](https://docs.cognitive3d.com/fixations/) · [Metrics Glossary](https://docs.cognitive3d.com/metrics-glossary/) · [Privacy Language](https://docs.cognitive3d.com/legal/) · [Firewall](https://docs.cognitive3d.com/firewall/)

---

## Git Workflow

- **`master`** — main/release branch
- **`develop`** — active development branch; PRs typically target `develop`
- PR template requires: Linear issue ID, type-of-change classification, self-review, and Copilot review
- Code owners: @calderarchinuk @parisacognitive3d @matt-manuel @alfred316

## High-Staleness Surfaces (always verify live)

Latest SDK versions, release notes, supported engine versions, supported hardware/eye tracking, package install coordinates, dashboard navigation paths, API key formats/auth examples, MCP server config, device/platform feature support.

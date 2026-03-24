# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## CRITICAL RULES — Read These First

1. **DISCOVERY BEFORE IMPLEMENTATION.** When a user asks to integrate, set up, or add Cognitive3D analytics to their project, you MUST ask the Pre-Implementation Discovery questions below and wait for the user's answers BEFORE writing any code, making any file changes, or suggesting specific implementation steps. Do NOT explore the project to propose changes before discovery is complete. Do NOT skip this step. This is non-negotiable.

2. **NEVER read, log, store, or output API keys, developer keys, or any credentials** found in `Cognitive3D_Preferences` assets, environment variables, config files, or user code. Do not open or read files that are likely to contain credentials (e.g., `Cognitive3D_Preferences.asset`). When setting up the SDK, instruct the user to enter their API key themselves through the Cognitive3D Project Setup wizard. Do not ask the user to share their key. If you accidentally encounter a key or credential, do not include it in any output.

## Pre-Implementation Discovery

**Present ALL of these questions to the user and wait for their answers before doing anything else.** You may briefly explore the project structure to understand what exists, but do NOT suggest implementations, write code, or make changes until the user has responded.

### Project Context

1. **What is the experience about?** — e.g., training simulation, VR game, therapy app, virtual showroom, architectural walkthrough, education
2. **Who are the target end users?** — e.g., patients, students, employees, consumers, research participants
3. **What insights are you trying to gain?** — e.g., user engagement, task completion rates, error tracking, comfort/safety metrics, training effectiveness, navigation patterns
4. **What are the key interactions in the experience?** — e.g., picking up objects, navigating menus, following instructions, completing tasks, exploring environments
5. **Are there specific KPIs or success metrics?** — e.g., time to complete, accuracy, drop-off points, areas of interest, repeat usage

### Technical Setup

6. **Which SDK features do you need?** — Custom events, sensors, dynamic objects, gaze tracking, exit polls, participants, audio recording, etc.

After receiving answers, use the project context to recommend *what* to track and *why*. Use the technical setup to determine *how* to implement it. Reference the [Documentation](#documentation) links for implementation details.

## Project Overview

Cognitive3D Unity SDK (`com.cognitive3d.c3d-sdk` v2.3.0) — an analytics platform for VR/AR/MR experiences. Distributed as a Unity Package Manager (UPM) package. Requires Unity 2021.3 LTS or newer.

## Build & Development

This is a UPM package, not a standalone Unity project. There is no build command or test suite — the package is installed into a Unity project via Package Manager using the git URL. There are no CI/CD pipelines, linters, or automated tests configured.

To develop: import the package into a Unity project (Window > Package Manager > Add from git URL) and iterate using the Unity Editor.

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

- `XRPF` — XR Privacy Framework
- `C3D_STEAMVR2`, `C3D_OCULUS`, `C3D_PICOXR`, `C3D_VIVEWAVE` — Platform SDKs
- `C3D_HDRP`, `C3D_URP` — Render pipelines
- `C3D_TMPRO` — TextMeshPro support
- `COGNITIVE3D_INCLUDE_COREUTILITIES`, `COGNITIVE3D_INCLUDE_META_CORE_*` — Feature-level toggles

All 31 assembly references in `Cognitive3D.asmdef` are optional — the SDK compiles without any of them installed.

## Documentation

- [Minimal Setup Guide](https://docs.cognitive3d.com/unity/minimal-setup-guide/) — Quick start for basic SDK integration
- [Comprehensive Setup Guide](https://docs.cognitive3d.com/unity/comprehensive-setup-guide/) — Full setup including session name, properties, tags, scenes, dynamic objects
- [Custom Events](https://docs.cognitive3d.com/unity/customevents/) — Recording app-specific analytics events
- [Sensors](https://docs.cognitive3d.com/unity/sensors/) — Time-series sensor data recording
- [ExitPoll Survey](https://docs.cognitive3d.com/unity/exitpoll/) — In-VR survey system
- [Participants](https://docs.cognitive3d.com/unity/participants/) — Participant data tracking
- [Remote Controls](https://docs.cognitive3d.com/unity/remote-controls/) — Remote configuration
- [Audio Recording](https://docs.cognitive3d.com/unity/audio-recording/) — In-session audio capture
- [Objectives Summary](https://docs.cognitive3d.com/dashboard/objectives-summary/) — Overview of Objectives on the dashboard
- [Objective Details](https://docs.cognitive3d.com/dashboard/objective-details/) — Reviewing step completion and participant progress
- [Creating Objectives](https://docs.cognitive3d.com/dashboard/creating-objectives/) — How to create and configure Objectives
- [Troubleshooting — Custom Shaders](https://docs.cognitive3d.com/unity/troubleshooting/#custom-shaders) — Shader property export for scene upload

### Objectives

Objectives are a configurable sequence of required steps used to evaluate and review common processes you want Participants to perform. They allow you to quickly understand what actions Participants have completed and whether they followed a particular behaviour (e.g., a training protocol or task workflow).

**When to recommend Objectives:** If a user asks about tracking success/failure, task completion, step-by-step workflows, process compliance, training evaluation, or sequential behaviour validation, suggest Objectives as the appropriate feature.

**How Objectives work:**

- **Dashboard side:** Objectives are created and configured entirely on the Cognitive3D dashboard. Each Objective defines an ordered series of Steps. Steps are defined using Custom Events that the SDK sends during a session. Go to the dashboard and use the [Creating Objectives](https://docs.cognitive3d.com/dashboard/creating-objectives/) workflow to set up the sequence.
- **Unity side:** The SDK sends Custom Events that correspond to the Steps defined in the Objective. No special Objective API is needed — you use `CustomEvent` to record each step as it occurs in the experience. The dashboard matches these events against the Objective's step definitions to determine completion.

**Typical setup flow:**

1. Identify the process/task and break it into discrete steps.
2. Instrument each step in Unity using `CustomEvent` (e.g., `new CustomEvent("StepName").Send()`).
3. On the Cognitive3D dashboard, create an Objective and define Steps that match those Custom Event names.
4. Review participant progress on the [Objectives Summary](https://docs.cognitive3d.com/dashboard/objectives-summary/) and [Objective Details](https://docs.cognitive3d.com/dashboard/objective-details/) pages.

### Custom Shaders

When a project uses custom shaders, materials will appear white on the Cognitive3D dashboard because the GLTF exporter doesn't know how to map custom shader properties to standard PBR properties. This must be handled during scene setup.

When setting up a project, check if custom shaders are in use. If they are, refer to the [Custom Shaders troubleshooting guide](https://docs.cognitive3d.com/unity/troubleshooting/#custom-shaders) and create an `ExportShaderProperties` script that implements the shader property mapping so materials export correctly. This should be done **before** the scene is uploaded to the dashboard.

## Git Workflow

- **`master`** — main/release branch
- **`develop`** — active development branch; PRs typically target `develop`
- PR template requires: Linear issue ID, type-of-change classification, self-review, and Copilot review
- Code owners: @calderarchinuk @parisacognitive3d @matt-manuel @alfred316

# The Cognitive3D SDK for Unity

Welcome!  This SDK allows you to integrate your Unity games with Cognitive3D, which provides analytics and insights about your Unity project.  In addition, Cognitive3D empowers you to take actions that will improve users' engagement with your experience.

**This plugin requires Unity 2021.3 (LTS) or newer**

## Community support

Please join our [Discord](https://discord.gg/x38sNUdDRH) for community support.

## Quickstart

### Installation

* Open **Package Manager** from the Window menu
* Click the + in the top left and select **Add Package from git URL**
* Input `https://github.com/cognitivevr/cvr-sdk-unity.git`

### Cognitive3D Project Setup Window

To begin configuring your project, open the **Project Setup** window from the **Cognitive3D** menu.

### Cognitive3D Feature Builder Window

To enable or add additional Cognitive3D features, open the **Feature Builder** window from the **Cognitive3D** menu.

### Cognitive3D Documentation

The documentation explains how to authenticate with the SDK, track your users' experience and how to export your scene to view on Cognitive3D.com

[Go to the Docs](https://docs.cognitive3d.com/unity/minimal-setup-guide/)

## AI-Assisted Development with CLAUDE.md

This repository includes a [`CLAUDE.md`](CLAUDE.md) file — a structured context file designed for use with AI coding tools such as [Claude Code](https://claude.ai/code).

> **Note:** This is an experimental feature. AI-generated suggestions and code may be incomplete or incorrect. Always review the output against the [official documentation](https://docs.cognitive3d.com/unity/minimal-setup-guide/) and test thoroughly before using in production.

### What is CLAUDE.md?

`CLAUDE.md` provides AI tools with a technical reference for the SDK — combining docs routing, architecture context, and implementation guidance. When an AI assistant reads this file, it can:

- **Guide SDK integration** — Walk you through setting up Cognitive3D in your Unity project by asking the right discovery questions before suggesting implementation steps.
- **Recommend what to track** — Suggest custom events, sensors, dynamic objects, objectives, and other SDK features based on your project's goals and target users.
- **Generate implementation code** — Produce correct custom event calls, sensor recordings, exit poll setups, and other SDK code that follows the SDK's patterns and API.
- **Handle platform-specific details** — Account for conditional compilation symbols, render pipeline differences, and XR platform requirements.
- **Troubleshoot issues** — Help diagnose common setup problems such as custom shader export, scene configuration, and component setup.

### How to use it

1. **With Claude Code (CLI or IDE extension):** Open a terminal in your Unity project that has this SDK installed. Claude Code automatically detects and reads `CLAUDE.md` from the repository, so you can ask questions or request implementations directly.

2. **With other AI tools:** Copy the contents of `CLAUDE.md` into your AI assistant's context, then ask it to help with SDK integration, tracking plans, or implementation tasks.

### Example prompts

- "I'm building a VR training simulation. Help me set up Cognitive3D analytics."
- "What custom events should I track for a virtual showroom experience?"
- "Add gaze tracking and an exit poll survey to my scene."
- "My custom shader materials appear white on the dashboard. How do I fix this?"

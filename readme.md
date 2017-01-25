The cognitiveVR SDK for Unity
=========
Welcome!  This SDK allows you to integrate your Unity games with cognitiveVR, which provides analytics and insights about your Unity project.  In addition, cognitiveVR empowers you with the ability to take actions that will improve users' engagement with your experience.

**This plugin now requires Unity 5.4.1p4 or newer**

Last Updated: Jan 24, 2017


Quickstart
=========
## Sign up
If you have not already done so, please register at
[https://dashboard.cognitivevr.co](https://dashboard.cognitivevr.io).

## Download the SDK
You can clone this git repo to stay up to date with fixes and changes. You can also download the unity package directly from the Releases page : [Releases](https://github.com/CognitiveVR/cvr-sdk-unity/releases)

## Import SDK
Follow the standard unity package import process. You can import a custom package from the Assets>Import Package>Custom Package... menu option.

![Importing the Custom Package for the cognitiveVR SDK](doc/25_import_custom_package_bubblepop.png)

When updating to a new version of the SDK, it is recommended to delete these folders:

```
Assets/CognitiveVR/Editor/
Assets/CognitiveVR/Scripts/
Plugins/CognitiveVR/
```

It is very important to **NOT** delete your CognitiveVR_Preferences asset!

## CognitiveVR Settings window
![cognitiveVR Settings Popup](doc/settings_window.png)

cognitiveVR Documentation
=========
The documentation explains how to login to the SDK, track your users' experience and how to export your scene to view on SceneExplorer.com

[Go to the Docs](http://docs.cognitivevr.io/unity/get-started/)
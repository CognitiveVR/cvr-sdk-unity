The cognitiveVR SDK for Unity
=========
Welcome!  This SDK allows you to integrate your Unity games with cognitiveVR, which provides analytics and insights about your Unity project.  In addition, cognitiveVR empowers you with the ability to take actions that will improve users' engagement with your experience.

Last Updated: July 4, 2016

Quickstart
=========
## Sign up
If you have not already done so, please register at
[https://dashboard.cognitivevr.co](https://dashboard.cognitivevr.io).

## Download the SDK
It is recommended that you clone this git repo to stay up to date with fixes and changes. You can also download the unity package directly : [cognitiveVR.unitypackage](https://github.com/CognitiveVR/cvr-sdk-unity/raw/master/dist/CognitiveVR.unitypackage)

## Import SDK
Follow the standard unity package import process. You can import a custom package from the Assets>Import Package>Custom Package... menu option.

![Importing the Custom Package for the cognitiveVR SDK](doc/25_import_custom_package_bubblepop.png)

If you are updating to the latest version of the cognitiveVR Unity SDK, it is recommended that you delete the existing cognitiveVR folders :
```
Assets/CognitiveVR
Assets/Plugins/CognitiveVR
Assets/Plugins/CognitiveVR.dll
```

## CognitiveVR Settings window
![cognitiveVR Settings Popup](doc/init_window.png)

### CustomerID
This is required to send telemetry to your application. The format is : ```yourcompanyname1234-productname-test```. This is taken from the dashboard on your product page.

![cognitiveVR Product Page](doc/13_cognitivevr_choose_product.png)


![cognitiveVR Customer ID](doc/customer_id.png)

### Init Prefab
This button will create a prefab that will automatically initialize the cognitiveVR analytics. We recommend only using this as a reference - you should move this code elsewhere into your project to fit with your existing startup flow.

### VR SDK
Select the SDK you are using for implementing VR into your unity project. At this time, we are only supporting the SteamVR plugin. Support for other SDKs including Oculus Utilities for Unity 5 will be coming soon.

## Done!
That's it! You are now tracking your user's basic data including GPU,CPU,OS and RAM. If you are using the SteamVR plugin, you will also recieve the current room size. Unity 5.4 beta users will also receive the model of Head Mounted Display.


cognitiveVR Wiki
=========
This is only a quick reference to set up the cognitiveVR SDK. The wiki explains the concepts behind the SDK and provides more code samples.

[Go to the Wiki](https://github.com/CognitiveVR/cvr-sdk-unity/wiki)
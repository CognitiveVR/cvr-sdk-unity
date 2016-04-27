# Introduction

Customers who deploy Unity games over the web may be interested in tracking how many web visitors have the Unity plugin installed, or choose to install it.  The code in this folder provides a starting point for customers to track this, using the Splyt Javascript SDK.

You may argue that this therefore belongs under the Javascript SDK folder, but since it is only of interest in Unity scenarios, I have placed it here.

# What This Sample Code Does 

With this sample code in place, the following will be reported to Splyt:

* Whether the Unity plugin was already installed when the user visited the web page.
* Whether the user clicked the Unity plugin download link (if not already installed).
* Whether the Unity plugin install completed.
* Whether the customer's Unity application ever started.

# How to Use

`unityinstalltracker.html` includes some Javascript to track the Unity plugin installation status.  Customers can use this as a starting template and integrate it into the page hosting their own Unity apps as needed.

Note the "TODOs" in the page source.  If the customer wants to try running the HTML through its paces as-is, then these are the minimal set of things that the user must change; namely:

1. Setting the appropriate Splyt customer ID
2. Send real user and device IDs.  The user and device IDs should match the ones you report from inside the Unity app, so that the same user/device doesn't get counted twice with two different IDs
3. A correct URL for the Unity app (`.unity3d`) to run.

# Browser Support

This has been tested the following browsers, both with and without the Unity plugin installed:

* Chrome v29.0.1547.65 running on Mac OS X 10.8
* Safari v6.0.5 (8536.30.1) running on Mac OS X 10.8
* Firefox v23.0.1 running on Mac OS X 10.8

# Dependencies

The script depends on Unity's `UnityObject2.js` (referenced by the HTML, and a copy is included in this folder).  It also depends on the Splyt Javascript SDK, which in turn depends on jQuery.  This sample page was tested with jQuery 1.10.2 (again, referenced by the HTML, and a copy is included in this folder).

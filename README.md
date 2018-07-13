# XiaoMi/Oculus VR Hybrid App

This guide will help you to reproduce some controller issues in XiaoMi/Oculus hybrid app

## Envrionment ##
* Mac OSX 10.13.4
* Unity 2017.4.0f1
* Oculus Mobile SDK v1.7
* Xiaomi Unity SDK v1.4.5 or Oculus Unity SDK v1.26.0

## Description ##
We are making a hybrid VR app using both Xiaomi(or Oculus) Unity VR SDK and Oculus native SDK.
We are using the Oculus sample VrController as the native code and made some minimal changes so it can be launched within the Unity app.

The native sample VrController app works well. However, if it is launched from the Unity-built app. The back/home keys are not working. Other keys are working fine.
Dive into the code of `vrcontroller.cpp` line 982
`vrapi_GetCurrentInputState()` gets value 0 for `remoteInputState` when back/home is clicked

## Quick Setup ##
1. For Xiaomi headset, use the demo scene `360ViewController` from Xiaomi Unity SDK
   For Oculus Go/Gear VR, use the demo scene `GearVrControllerTest` from Oculus Unity SDK
2. Clone this repo
3. Copy the following aar libraries into the Unity project
 	* `UnitySDK/Assets/Plugins/VrSound.aar`
	* `UnitySDK/Assets/Plugins/VrLocale.aar`
	* `UnitySDK/Assets/Plugins/VrGUI.aar`
	* `UnitySDK/Assets/Plugins/VrAppFramework.aar`
	* `UnitySDK/Assets/Plugins/vrcontroller-release.aar`
4. For Xiaomi headset, Copy the `UnitySDK/Assets/MIVR/Scripts/ButtonClick.cs` to replace the scipt in Unity
   For Oculus Go/Gear VR, Copy the `UnitySDK/Assets/OVR/Scripts/LaunchNative.cs` and attach to any gameobject in the scene
5. Run demo scene 
6. For Xiaomi headset, after clicking the button, the app will be transited to the natvie code (Oculus sample VrController). 
   For Oculus Go/Gear VR, the app will be transited to the natvie code (Oculus sample VrController) after launch.
7. We can see that both back/home button are not responding.


## Step-by-step Instruction ##
This section descibe the detailed change we made on the original sample code.

**IMPORTANT**: It is important to use Oculus Mobile SDK v1.7. Other version may conflict with the Oculus libraries embedded in Xiaomi/Oculus Unity SDK.

### Download [Oculus Mobile SDK v1.7](https://developer.oculus.com/downloads/package/oculus-mobile-sdk/1.7.0/) 

### Build the native VrController App
This step is optional. Just to verify that the controller does work.

##### 1. Copy the Xiaomi device signature file to `VrSample/Native/VrController/Project/Android/assets`
##### 2. under `VrSample/Native/VrController` folder, run `Project/Android/build.py`
##### 3. Verify that the controller works fine in native app.

### Build the Oculus support library
VrApp.java needs to be modified to avoid duplicated intialization on ovrapi

##### 1. Comment out `nativeOnCreate()` and `nativeOnDestroy` in `VrAppFramework/java/com/oculus/vrappframework/VrApp.java` 
```java
    public static void onCreate(Activity activity) {
      //nativeOnCreate( activity );
    }

	...
	
    public void onDestroy() {
      if ( mSurfaceHolder != null ) {
          nativeSurfaceDestroyed( mAppPtr );
      }

      //nativeOnDestroy( mAppPtr );
      mAppPtr = 0L;
    }	
```

##### 2. under `VrSample/Native/VrController` folder, run `Project/Android/build.py`
##### 3. Collect following aar files and copy them into Unity project under `Assets/Plugins/Android/`
* `VrAppSupport/VrSound/Libs/Android/aar/Release/VrSound.aar`
* `VrAppSupport/VrLocale/Libs/Android/aar/Release/VrLocale.aar`
* `VrAppSupport/VrGUI/Libs/Android/aar/Release/VrGUI.aar`
* `VrAppFramework/Libs/Android/aar/Release/VrAppFramework.aar`

### Rebuild VrController	into a library instead of an app ###
##### 1. Change the `VrSamples/Native/VrController/Projects/Android/build.gradle`, making it a library type 
``` 
apply plugin: 'com.android.library'
apply from: "${rootProject.projectDir}/VrApp.gradle"

dependencies {
	...
}

android {
  project.archivesBaseName = "vrcontroller"

  defaultConfig {
    // applicationId "com.oculus.vrcontroller"

    // override app plugin abiFilters to test experimental 64-bit support
    externalNativeBuild {
        ndk {
                abiFilters 'armeabi-v7a'//,'arm64-v8a'
        }
        ndkBuild {
                abiFilters 'armeabi-v7a'//,'arm64-v8a'
        }
    }
    
    packagingOptions {
        exclude '**/libvrapi.so'
    }     
  }

  sourceSets {
	...
  }
}
``` 
##### 2. Change the `VrSamples/Native/VrController/Projects/Android/AndroidManifest.xml `, removing extra application related settings and LAUNCHER
```
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android"
		package="com.oculus.vrcontroller"
		android:versionCode="1" 
		android:versionName="1.0" 
android:installLocation="auto" >
	...
	<application>
		<meta-data android:name="com.samsung.android.vr.application.mode" android:value="vr_only"/>
		<!-- launchMode is set to singleTask because there should never be multiple copies of the app running -->
		<!-- Theme.Black.NoTitleBar.Fullscreen gives solid black instead of a (bad stereoscopic) gradient on app transition -->
		<activity
				android:name="com.oculus.vrcontroller.MainActivity"
				android:theme="@android:style/Theme.Black.NoTitleBar.Fullscreen"
				android:label="@string/app_name"
				android:launchMode="singleTask"
				android:screenOrientation="landscape"
				android:configChanges="orientation|screenSize|keyboard|keyboardHidden"> 
			<!-- This filter lets the apk show up as a launchable icon -->
			<intent-filter>
				<action android:name="android.intent.action.MAIN" />
				<category android:name="android.intent.category.INFO" />
			</intent-filter>
		</activity>
	</application>
</manifest>

```
##### 3. Change the `VrApp.gradle`, commenting out `project.android.applicationVariants.all{}` section
##### 4. Add a convenient method in `VrSamples/Native/VrController/java/com/oculus/vrcontroller/MainActivity.java` so it can be launched from Unity
```java
package com.oculus.vrcontroller;

import android.app.Activity;
import android.os.Bundle;
import android.util.Log;
import android.content.Intent;
import com.oculus.vrappframework.VrActivity;

public class MainActivity extends VrActivity {
	
	...
	    
    public static void start(Activity activity) {
        Intent intent = new Intent(activity, MainActivity.class);
        activity.startActivity(intent);
        activity.overridePendingTransition(0, 0);
    }         
}

```
##### 5. Clean the build by running `Project/Android/build.py clean`
##### 5. Build the libray by running `Project/Android/build.py`
##### 6. Copy the `VrSamples/Native/VrController/Projects/Android/build/outputs/aar/vrcontroller-release.aar` into the Unity project under `Assets/Plugins/Android/`


### Integrate into Unity (Oculus) ###
##### 1. Use the demo scene `GearVrControllerTest `
##### 2. Add a script and attach to any gameobject
```c#
    public void OnEnable()
    {
		  var vrControllerClass = new AndroidJavaClass ("com.oculus.vrcontroller.MainActivity");

		  AndroidJavaClass unityPlayerClass = new AndroidJavaClass ("com.unity3d.player.UnityPlayer");
		  AndroidJavaObject unityPlayer = unityPlayerClass.GetStatic<AndroidJavaObject> ("currentActivity");

		  vrControllerClass.CallStatic ("start", unityPlayer);
    }

```

### Integrate into Unity (Xiaomi) ###
##### 1. Use the demo scene `360ViewController`
##### 2. Modify the `ButtonClick.cs`, so when clicking the button the native VrController will be launched
```c#
    public void OnClick()
    {
        Debug.Log("**** OnClick.");
        this.transform.GetComponentInChildren<Text>().text = (Random.value * 100).ToString();

		  var vrControllerClass = new AndroidJavaClass ("com.oculus.vrcontroller.MainActivity");

		  AndroidJavaClass unityPlayerClass = new AndroidJavaClass ("com.unity3d.player.UnityPlayer");
		  AndroidJavaObject unityPlayer = unityPlayerClass.GetStatic<AndroidJavaObject> ("currentActivity");

		  vrControllerClass.CallStatic ("start", unityPlayer);
    }

```


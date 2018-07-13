//-----------------------------------------------------------------------
// <copyright file="ButtonClick.cs" company="XiaoMi Corporation">
//     All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// the sample button click script
/// </summary>
public class LaunchNative : MonoBehaviour
{
    /// <summary>
    /// Called when [click].
    /// </summary>
    public void OnEnable()
    {
        var vrControllerClass = new AndroidJavaClass ("com.oculus.vrcontroller.MainActivity");

        AndroidJavaClass unityPlayerClass = new AndroidJavaClass ("com.unity3d.player.UnityPlayer");
        AndroidJavaObject unityPlayer = unityPlayerClass.GetStatic<AndroidJavaObject> ("currentActivity");

        vrControllerClass.CallStatic ("start", unityPlayer);
    }

}
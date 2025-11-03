using UnityEngine;
using System.Collections;

public class ForceOrientation : MonoBehaviour
{
    public enum LockTo { Portrait, PortraitUpsideDown, LandscapeLeft, LandscapeRight, Auto }
    public LockTo lockTo = LockTo.Portrait;

    private ScreenOrientation previousOrientation;
    private bool previousAutoRotate;
    private bool prevAutorotatePortrait;
    private bool prevAutorotateLandscapeLeft;
    private bool prevAutorotateLandscapeRight;
    private bool prevAutorotatePortraitUpsideDown;

    void OnEnable()
    {
        // Save previous settings
        previousOrientation = Screen.orientation;
        previousAutoRotate = Screen.autorotateToPortrait || Screen.autorotateToLandscapeLeft || Screen.autorotateToLandscapeRight || Screen.autorotateToPortraitUpsideDown;
        prevAutorotatePortrait = Screen.autorotateToPortrait;
        prevAutorotateLandscapeLeft = Screen.autorotateToLandscapeLeft;
        prevAutorotateLandscapeRight = Screen.autorotateToLandscapeRight;
        prevAutorotatePortraitUpsideDown = Screen.autorotateToPortraitUpsideDown;

        // Lock orientation (do it on next frame to avoid race conditions)
        StartCoroutine(SetOrientationNextFrame());
    }

    IEnumerator SetOrientationNextFrame()
    {
        yield return null;

        // disable autorotate first
        Screen.autorotateToPortrait = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
        Screen.autorotateToPortraitUpsideDown = false;

        switch (lockTo)
        {
            case LockTo.Portrait:
                Screen.orientation = ScreenOrientation.Portrait;
                break;
            case LockTo.PortraitUpsideDown:
                Screen.orientation = ScreenOrientation.PortraitUpsideDown;
                break;
            case LockTo.LandscapeLeft:
                Screen.orientation = ScreenOrientation.LandscapeLeft;
                break;
            case LockTo.LandscapeRight:
                Screen.orientation = ScreenOrientation.LandscapeRight;
                break;
            case LockTo.Auto:
                Screen.orientation = ScreenOrientation.AutoRotation;
                // enable autorotation back to stored flags (done below)
                break;
        }
    }

    void OnDisable()
    {
        // restore previous autorotation prefs and orientation
        if (lockTo == LockTo.Auto) return;

        Screen.orientation = previousOrientation;

        // restore autorotate flags
        Screen.autorotateToPortrait = prevAutorotatePortrait;
        Screen.autorotateToLandscapeLeft = prevAutorotateLandscapeLeft;
        Screen.autorotateToLandscapeRight = prevAutorotateLandscapeRight;
        Screen.autorotateToPortraitUpsideDown = prevAutorotatePortraitUpsideDown;
    }
}

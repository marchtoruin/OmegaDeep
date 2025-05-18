using UnityEngine;
using System.IO;

public class ScreenshotCapture : MonoBehaviour
{
    public string fileName = "screenshot";

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F12))
        {
            string path = Path.Combine(Application.dataPath, fileName + "_" + Screen.width + "x" + Screen.height + ".png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("Screenshot saved to: " + path);
        }
        if (Input.GetKeyDown(KeyCode.F12))
    {
        Debug.Log("F12 pressed — trying to take screenshot.");
        // rest of the code...
    }
    }
}

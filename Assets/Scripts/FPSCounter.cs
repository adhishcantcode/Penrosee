using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    public enum FPSLimit
    {
        Uncapped = 0,
        FPS30 = 30,
        FPS60 = 60,
        FPS90 = 90,
        FPS120 = 120
    }

    [Header("FPS Settings")]
    public FPSLimit fpsLimit = FPSLimit.FPS60;
    public float updateInterval = 0.5f;

    private float elapsedTime;
    private int frameCount;
    private float fps;

    private GUIStyle textStyle;
    private GUIStyle backgroundStyle;

    private void Awake()
    {
        Debug.Log("Device Name: " + SystemInfo.deviceName);
        Debug.Log("Machine Name: " + System.Environment.MachineName);
        QualitySettings.vSyncCount = 0;

        Application.targetFrameRate = fpsLimit == FPSLimit.Uncapped
            ? -1
            : (int)fpsLimit;
    }

    private void Update()
    {
        elapsedTime += Time.unscaledDeltaTime;
        frameCount++;

        if (elapsedTime >= updateInterval)
        {
            fps = frameCount / elapsedTime;

            elapsedTime = 0f;
            frameCount = 0;
        }
    }

    private void OnGUI()
    {
        if (textStyle == null)
        {
            textStyle = new GUIStyle(GUI.skin.label);
            textStyle.fontSize = Mathf.Max(16, Screen.height / 40);
            textStyle.fontStyle = FontStyle.Bold;
            textStyle.normal.textColor = Color.white;

            backgroundStyle = new GUIStyle(GUI.skin.box);
        }

        float boxWidth = 150f;
        float boxHeight = 45f;

        GUI.Box(new Rect(15, 15, boxWidth, boxHeight), "");

        string fpsText = $"FPS: {fps:0}";

        textStyle.normal.textColor =
            fps >= 50 ? Color.green :
            fps >= 30 ? Color.yellow :
            Color.red;

        GUI.Label(
            new Rect(25, 20, boxWidth - 20, boxHeight - 5),
            fpsText,
            textStyle
        );
    }
}
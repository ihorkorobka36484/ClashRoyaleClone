using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    public TextMeshProUGUI fpsText; // Assign a UI Text in the Inspector

    private float deltaTime = 0.0f;
    private int frameCount = 0;
    private int calculationsCount = 0;
    private float lowestFps = float.MaxValue;

    void Update()
    {
        float frameTime = Time.unscaledDeltaTime;
        deltaTime += (frameTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;

        frameCount  += Mathf.CeilToInt(fps);
        calculationsCount++;
        float avgFps = frameCount / calculationsCount;

        if (fps < lowestFps && fps >= 30f)
            lowestFps = fps;

        if (fpsText != null)
            fpsText.text = $"FPS: {Mathf.CeilToInt(fps)}\nAvg: {Mathf.CeilToInt(avgFps)}\nLowest: {Mathf.CeilToInt(lowestFps)}";
    }
}
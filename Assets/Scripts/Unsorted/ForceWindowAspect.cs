using UnityEngine;
using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}

public class ForceWindowAspect : MonoBehaviour
{
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    [DllImport("user32.dll")]
    static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    float targetAspect = 9f / 16f; // Portrait (change as needed)
    private int lastWidth = 0;
    private bool isAdjusting = false;

    void Awake () {
        int width = Screen.width;
        lastWidth = width;
    }

    void Update()
    {
        int width = Screen.width;
        int height = Screen.height;

        // Prevent feedback loop
        if (isAdjusting)
        {
            isAdjusting = false;
            lastWidth = width;
            return;
        }

        // Only react if width changed by user
        if (width != lastWidth)
        {
            int newHeight = Mathf.RoundToInt(width / targetAspect);
            var handle = GetActiveWindow();
            
            // Get current window position
            RECT rect;
            GetWindowRect(handle, out rect);
            int x = rect.Left;
            int y = rect.Top;

            MoveWindow(handle, x, y, width, newHeight, true);
            lastWidth = width;
            isAdjusting = true;
        }
    }
#endif
}
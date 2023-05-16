using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
   
    private int fps;
    private int lastFrameCount;
    private float nextUpdateTime;
    [Range(0.01f, 1f)]
    public float updateRate = 0.1f;
    private void Awake()
    {
        lastFrameCount = Time.frameCount;
        nextUpdateTime = Time.time + updateRate;
    }
    private void Update()
    {
        if (nextUpdateTime < Time.time)
        {
            nextUpdateTime = Time.time + updateRate;
            fps = (int)((Time.frameCount - lastFrameCount) / updateRate);
            lastFrameCount = Time.frameCount;
        }
        
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 150, 100), $"FPS: {fps}");
    }
}

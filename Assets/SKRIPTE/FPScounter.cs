using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FPScounter : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI fpsPrikaz, averageFpsPrikaz, minFpsPrikaz, maxFpsPrikaz;
    float fps, fpsTotal, minFps, maxFps;
    [SerializeField] int frameLimit = 9999;
    int framePassed;

    private void Start()
    {
        Application.targetFrameRate = frameLimit;
        minFps = Mathf.Infinity;
    }

    private void Update()
    {
        fps = 1 / Time.unscaledDeltaTime;
        fpsPrikaz.text = "FPS - " + fps.ToString("F0");

        fpsTotal += fps;
        framePassed++;
        averageFpsPrikaz.text = "Average FPS - " + (fpsTotal / framePassed).ToString("F0");

        if(framePassed > 10)
        {
            if(fps > maxFps)
            {
                maxFps = fps;
                maxFpsPrikaz.text = "Max FPS - " + maxFps.ToString("F0");
            }
            if(fps < minFps)
            {
                minFps = fps;
                minFpsPrikaz.text = "Min FPS - " + minFps.ToString("F0");
            }
        }
    }
}

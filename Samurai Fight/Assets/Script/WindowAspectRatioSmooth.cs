using UnityEngine;
using System.Collections;

public class WindowAspectRatioSmooth : MonoBehaviour
{
    private float targetAspect = 16.0f / 9.0f;
    
    public float resizeDelay = 0.2f;

    private int lastWidth;
    private int lastHeight;
    private Coroutine resizeCoroutine;

    void Start()
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;
    }

    void Update()
    {
        if (Screen.fullScreen) return;

        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;

            if (resizeCoroutine != null) StopCoroutine(resizeCoroutine);

            resizeCoroutine = StartCoroutine(FixRatioAfterDelay());
        }
    }

    IEnumerator FixRatioAfterDelay()
    {
        yield return new WaitForSeconds(resizeDelay);

        int newHeight = Mathf.RoundToInt(Screen.width / targetAspect);

        if (Mathf.Abs(Screen.height - newHeight) > 2) 
        {
            Screen.SetResolution(Screen.width, newHeight, false);
            

            lastWidth = Screen.width;
            lastHeight = newHeight;
        }
    }
}
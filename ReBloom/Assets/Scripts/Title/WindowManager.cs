using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class WindowManager : MonoBehaviour
{
    public List<Window> windows = new List<Window> ();

    public Window startWindow;
    public Window currentWindow;

    private void Start()
    {
        if (windows != null)
        {
            currentWindow = windows[0];
        }

        currentWindow.gameObject.SetActive(true);
    }

    public void ChangeWindow(Window nextWindow)
    {
        if (!windows.Contains(nextWindow))
        {
            currentWindow.gameObject.SetActive(false);
            currentWindow = nextWindow;
            currentWindow.gameObject.SetActive(true);
        }
    }
}

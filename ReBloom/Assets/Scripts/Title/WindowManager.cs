using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public enum Windows
{ 

}

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

        for (int i = 1; i < windows.Count; i++)
        {
            windows[i].gameObject.SetActive(false);  
        }

        currentWindow.gameObject.SetActive(true);
    }

    public void ChangeWindow(Window nextWindow)
    {
        if (windows.Contains(nextWindow))
        {
            currentWindow.gameObject.SetActive(false);
            currentWindow = nextWindow;
            currentWindow.gameObject.SetActive(true);
        }
    }
}

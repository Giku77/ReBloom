using UnityEngine;

public class EmotionChanger : MonoBehaviour
{

    public int totalEmotions = 10;
    public int currentEmotionEyesIndex = 0;
    public int currentEmotionMouthIndex = 0;
    public Renderer objectRendererEyes;
    public Renderer objectRendererMouth;

    void Start()
    {
        UpdateEmotion();
    }

    public void SetEmotionEyes(int emotionIndex)
    {
        if (emotionIndex >= 0 && emotionIndex < totalEmotions)
        {
            currentEmotionEyesIndex = emotionIndex;
            UpdateEmotion();
        }
        else
        {
            Debug.LogWarning("Emotion index out of range!");
        }
    }

    public void SetEmotionMouth(int emotionIndex)
    {
        if (emotionIndex >= 0 && emotionIndex < totalEmotions)
        {
            currentEmotionMouthIndex = emotionIndex;
            UpdateEmotion();
        }
        else
        {
            Debug.LogWarning("Emotion index out of range!");
        }
    }

    private void UpdateEmotion()
    {
        if ((objectRendererEyes != null)&& (objectRendererMouth != null))
        {
            float offsetXEyes = (float)currentEmotionEyesIndex / totalEmotions;
            objectRendererEyes.material.SetTextureOffset("_MainTex", new Vector2(offsetXEyes, 0));
            float offsetXMouth = (float)currentEmotionMouthIndex / totalEmotions;
            objectRendererMouth.material.SetTextureOffset("_MainTex", new Vector2(offsetXMouth, 0));
        }
        else
        {
            Debug.LogError("Object Renderer is not assigned!");
        }
    }
}

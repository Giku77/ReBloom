using UnityEngine;

public class TextureScroller : MonoBehaviour
{
    public Renderer targetRenderer; 
    public float horizontalSpeed = 0.2f; 
    public int trackCount = 3; 
    public float verticalStep = 1.0f / 3.0f; 

    private Vector2 uvOffset = Vector2.zero; 
    public int currentTrack = 2; 

    void Start()
    {
        if (targetRenderer == null)
        {
            Debug.LogError("Назначьте объект с текстурой в поле Target Renderer!");
        }
        else
        {
            verticalStep = 1.0f / trackCount; 
        }
    }

    void Update()
    {
        if (targetRenderer != null)
        {
            uvOffset.x += horizontalSpeed * Time.deltaTime;

         //     currentTrack = (currentTrack + 1) % trackCount; 
                uvOffset.y = currentTrack * verticalStep; 
        }

            targetRenderer.material.SetTextureOffset("_MainTex", uvOffset);
        
    }
}

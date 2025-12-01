using UnityEngine;

public class PushDetection : MonoBehaviour
{
    public bool isPushing = false; 
    private GameObject pushableObject;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Pushable")) 
        {
            isPushing = true;
            pushableObject = other.gameObject; 
            
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Pushable"))
        {
            isPushing = false;
            pushableObject = null; 

        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

public class SpawnItemInputTest : MonoBehaviour
{
    public void Spawn(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) SpawnItemInWorld(1, transform.position + Vector3.forward * 2);
    }

    void SpawnItemInWorld(int id, Vector3 pos) { Debug.Log($"Spawn {id} at {pos}"); }
}
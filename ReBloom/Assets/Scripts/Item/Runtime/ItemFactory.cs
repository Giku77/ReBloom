using UnityEngine;

public abstract class ItemFactory : MonoBehaviour
{

    public abstract IProduct GetProduct(Vector3 position);

    public string GetLog(IProduct product)
    {
        string logMessage = "Factory: created product " + product.productName;
        return logMessage;
    }
}
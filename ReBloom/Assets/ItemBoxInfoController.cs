using TMPro;
using UnityEngine;

public class ItemBoxInfoController : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private TextMeshProUGUI ItemInfo;
    [SerializeField] private DeathBoxData ItemBoxData;
    [SerializeField] private Canvas worldCanvas;
    private void Start()
    {
        ItemInfo.text = ItemBoxData.BoxID;
        if (worldCanvas != null)
        {
            worldCanvas.transform.SetParent(transform);

            worldCanvas.transform.localPosition = new Vector3(0, 2f, 0);
            worldCanvas.transform.localRotation = Quaternion.identity;
            worldCanvas.transform.localScale = Vector3.one * 0.007f; // 월드 캔버스 크기 조정
        }
    }
    private void LateUpdate()
    {
        if (worldCanvas != null)
        {
            worldCanvas.transform.position = transform.position + Vector3.up * 2f;

            // 카메라를 향하게 (빌보드)
            if (Camera.main != null)
            {
                worldCanvas.transform.rotation = Quaternion.LookRotation(
                    worldCanvas.transform.position - Camera.main.transform.position
                );
            }
        }
    }
}

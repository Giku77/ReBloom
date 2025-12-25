using UnityEngine;
using UnityEngine.UI;

public class UICloseButton : MonoBehaviour
{
    [SerializeField] private Button button;

    private UIType targetType;

    public void Bind(UIType type)
    {
        targetType = type;

        if (button == null)
            button = GetComponent<Button>();

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            if (UIManager.Instance != null)
                UIManager.Instance.HideUI(targetType);
        });
    }
}

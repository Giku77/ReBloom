using UnityEngine;

public class StatusPanelInteractable : BuildingInteractableBase
{
    [Header("World UI Prefab (Canvas WorldSpace)")]
    [SerializeField] private StatusPanelUI worldPanelPrefab;

    [Header("Attach Point (없으면 자기 transform)")]
    [SerializeField] private Transform attachPoint;

    [Header("Offset / LookAt")]
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 2.0f, 0f);
    [SerializeField] private bool faceCamera = true;

    private StatusPanelUI spawned;
    private Camera mainCam;

    protected override void Awake()
    {
        if (attachPoint == null) attachPoint = transform;
        mainCam = Camera.main;
        base.Awake();
    }

    public override float HoldTime => 0.2f;

    public override void Interact(PlayerController player)
    {
        if (worldPanelPrefab == null)
        {
            Debug.LogWarning("[StatusPanelInteractable] worldPanelPrefab이 비어있음");
            return;
        }

        if (spawned == null)
        {
            spawned = Instantiate(worldPanelPrefab, attachPoint);
            spawned.transform.localPosition = localOffset;
            spawned.transform.localRotation = Quaternion.identity;
            spawned.gameObject.SetActive(true);

            SoundManager.I?.PlayTvOn();
        }
        else
        {
            bool next = !spawned.gameObject.activeSelf;
            spawned.gameObject.SetActive(next);
            if (next)
            {
                spawned.RefreshAll();
                SoundManager.I?.PlayTvOn();
            }
            else
            {
                SoundManager.I?.PlayTvOff();
            }
        }
    }

    private void LateUpdate()
    {
        if (!faceCamera || spawned == null || !spawned.gameObject.activeSelf) return;

        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        var t = spawned.transform;
        Vector3 dir = t.position - mainCam.transform.position;
        t.rotation = Quaternion.LookRotation(dir);
    }
}

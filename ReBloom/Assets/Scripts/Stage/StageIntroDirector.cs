using System.Collections.Generic;
using UnityEngine;

public class StageIntroDirector : MonoBehaviour
{
    [System.Serializable]
    public class StageIntro
    {
        public int stageId;
        public Transform lookAt;
        public Transform cameraPos;
        public float blendIn = 0.35f;
        public float hold = 0.8f;
        public float blendOut = 0.35f;
    }

    [SerializeField] private ThirdPersonCamera tpsCam;
    [SerializeField] private List<StageIntro> intros = new();

    private readonly HashSet<int> visited = new();
    private readonly Dictionary<int, StageIntro> introByStageId = new();

    private void Awake()
    {
        introByStageId.Clear();
        foreach (var i in intros)
        {
            if (i == null) continue;
            if (i.lookAt == null || i.cameraPos == null) continue;
            introByStageId[i.stageId] = i;
        }
    }

    private void OnEnable()
    {
        StageDetector.OnStageChanged += HandleStageChanged;
    }

    private void OnDisable()
    {
        StageDetector.OnStageChanged -= HandleStageChanged;
    }

    private void HandleStageChanged(int stageId)
    {
        if (tpsCam == null) return;
        if (!introByStageId.TryGetValue(stageId, out var intro)) return;

        if (visited.Contains(stageId)) return;
        visited.Add(stageId);

        AutoSaveService.I?.RequestSave($"VisitedStage:{stageId}");

        tpsCam.PlayFocusSequenceUniTask(
            focusLookAtWorld: intro.lookAt.position,
            cameraPosWorld: intro.cameraPos.position,
            blendIn: intro.blendIn,
            hold: intro.hold,
            blendOut: intro.blendOut
        );
    }

    // Save/Load 연결용
    public HashSet<int> CaptureVisited() => new HashSet<int>(visited);

    public void ApplyVisited(IEnumerable<int> stageIds)
    {
        visited.Clear();
        if (stageIds == null) return;
        foreach (var id in stageIds) visited.Add(id);
    }
}

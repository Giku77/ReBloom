using BansheeGz.BGDatabase;
using System.Collections.Generic;

public class VoiceData
{
    public string Speaker;
    public int VarcoID;
    public string Line;
    public int Situation;
    public string VarcoVoiceFile;
}

public class VoiceDB
{
    private readonly Dictionary<int, VoiceData> _voices = new();
    private readonly Dictionary<int, List<int>> _voicesBySituation = new();

    public void LoadFromBG()
    {
        var meta = BGRepo.I.GetMeta("Voice");
        if (meta == null)
        {
            UnityEngine.Debug.LogError("[VoiceDB] Voice 테이블을 찾을 수 없습니다.");
            return;
        }

        _voices.Clear();
        _voicesBySituation.Clear();

        foreach (var e in meta.EntitiesToList())
        {
            var d = new VoiceData
            {
                Speaker = e.Get<string>("Speaker"),
                VarcoID = e.Get<int>("VarcoID"),
                Line = e.Get<string>("Line"),
                Situation = e.Get<int>("Situation"),
                VarcoVoiceFile = e.Get<string>("VarcoVoiceFile")
            };
            _voices[d.VarcoID] = d;

            if (!_voicesBySituation.ContainsKey(d.Situation))
                _voicesBySituation[d.Situation] = new List<int>();

            _voicesBySituation[d.Situation].Add(d.VarcoID);
        }

        UnityEngine.Debug.Log($"[VoiceDB] {_voices.Count}개 음성 데이터 로드됨");
    }

    public bool TryGet(int varcoId, out VoiceData data)
        => _voices.TryGetValue(varcoId, out data);

    public IReadOnlyDictionary<int, VoiceData> GetAll()
        => _voices;

    public int GetRandomVarcoIdBySituation(int situation)
    {
        if (!_voicesBySituation.TryGetValue(situation, out var list) || list.Count == 0)
        {
            UnityEngine.Debug.LogWarning($"[VoiceDB] Situation {situation}에 해당하는 음성이 없습니다.");
            return 0;
        }

        int randomIndex = UnityEngine.Random.Range(0, list.Count);
        return list[randomIndex];
    }
}

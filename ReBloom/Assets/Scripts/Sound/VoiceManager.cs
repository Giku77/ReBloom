using UnityEngine;
using System.Collections.Generic;

public class VoiceManager : MonoBehaviour
{
    private static VoiceManager instance;
    public static VoiceManager I => instance;

    [Header("Audio")]
    [SerializeField] private AudioSource voiceSource;

    private VoiceDB voiceDb;
    private Dictionary<int, AudioClip> audioClipReferences = new Dictionary<int, AudioClip>();

    [Header("Audio Clips")]
    [SerializeField] private List<AudioClipEntry> audioClips = new List<AudioClipEntry>();

    [System.Serializable]
    public class AudioClipEntry
    {
        public int varcoId;
        public AudioClip clip;
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        voiceDb = new VoiceDB();
        voiceDb.LoadFromBG();

        BuildReferences();
    }

    private void BuildReferences()
    {
        audioClipReferences.Clear();

        foreach (var entry in audioClips)
        {
            if (entry.clip != null)
            {
                audioClipReferences[entry.varcoId] = entry.clip;
            }
        }

        Debug.Log($"[DialogueVoice] {audioClipReferences.Count}개 음성 참조 등록");
    }

    public void PlayVoice(int varcoId)
    {
        Debug.Log($"[VoiceManager] PlayVoice 호출됨 - VarcoID: {varcoId}");

        if (voiceSource == null || varcoId <= 0)
        {
            Debug.LogWarning($"[VoiceManager] voiceSource null 또는 varcoId 이상: {varcoId}");
            return;
        }

        if (!audioClipReferences.TryGetValue(varcoId, out AudioClip clip))
        {
            Debug.LogWarning($"[VoiceManager] VarcoID {varcoId}에 해당하는 AudioClip 없음");
            Debug.Log($"[VoiceManager] 현재 등록된 VarcoID 목록: {string.Join(", ", audioClipReferences.Keys)}");
            return;
        }

        voiceSource.clip = clip;
        voiceSource.Play();
    }

    public void Stop()
    {
        if (voiceSource != null && voiceSource.isPlaying)
        {
            voiceSource.Stop();
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Load Audio Clips")]
    public void AutoLoadAudioClips()
    {
        audioClips.Clear();

        var tempVoiceDb = new VoiceDB();
        tempVoiceDb.LoadFromBG();

        string folderPath = "Assets/Sound/Voice";

        var allVoices = tempVoiceDb.GetAll();
        int foundCount = 0;
        int missingCount = 0;

        foreach (var voice in allVoices.Values)
        {
            if (string.IsNullOrEmpty(voice.VarcoVoiceFile))
            {
                missingCount++;
                continue;
            }

            string[] guids = UnityEditor.AssetDatabase.FindAssets(
                voice.VarcoVoiceFile,
                new[] { folderPath }
            );

            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                AudioClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);

                if (clip != null)
                {
                    audioClips.Add(new AudioClipEntry
                    {
                        varcoId = voice.VarcoID,
                        clip = clip
                    });
                    foundCount++;
                }
            }
            else
            {
                Debug.LogWarning($"[DialogueVoice] 파일 없음: {voice.VarcoVoiceFile} (VarcoID: {voice.VarcoID})");
                missingCount++;
            }
        }

        audioClips.Sort((a, b) => a.varcoId.CompareTo(b.varcoId));

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[DialogueVoice] 자동 로드 완료: {foundCount}개 발견, {missingCount}개 누락");
    }

    public AudioClip GetAudioClip(int varcoId)
    {
        if (audioClipReferences.TryGetValue(varcoId, out AudioClip clip))
            return clip;

        return null;
    }
#endif
}
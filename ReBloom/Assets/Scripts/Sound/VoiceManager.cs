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

    [Header("Poppy Voice Settings")]
    [SerializeField] private int currentPoppyVoiceType = 1;

    [System.Serializable]
    public class AudioClipEntry
    {
        public int varcoId;
        public AudioClip clip;
        public string fileName;
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

        LoadPoppyVoiceTypeFromSettings();

        BuildReferences();
    }

    private void BuildReferences()
    {
        audioClipReferences.Clear();

        foreach (var entry in audioClips)
        {
            if (entry.fileName.StartsWith("Poppy_"))
            {
                string expectedSuffix = $"_{currentPoppyVoiceType}";
                if (entry.fileName.Contains(expectedSuffix))
                {
                    audioClipReferences[entry.varcoId] = entry.clip;
                }
            }
            else
            {
 
                audioClipReferences[entry.varcoId] = entry.clip;
            }
        }

        Debug.Log($"[DialogueVoice] {audioClipReferences.Count}개 음성 참조 등록");
    }

    public void PlayVoice(int varcoId)
    {
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

    public AudioClip GetAudioClip(int varcoId)
    {
        if (audioClipReferences.TryGetValue(varcoId, out AudioClip clip))
            return clip;

        return null;
    }

    private void LoadPoppyVoiceTypeFromSettings()
    {
        if (SettingManager.I != null)
        {
            currentPoppyVoiceType = SettingManager.I.GetPoppyVoiceType();
        }
    }

    public void SetPoppyVoiceType(int voiceType)
    {
        if (currentPoppyVoiceType != voiceType)
        {
            currentPoppyVoiceType = voiceType;
            BuildReferences();
            Debug.Log($"[VoiceManager] 뽀삐 음성 타입 변경: {voiceType}");
        }
    }

    public int GetRandomVarcoIdBySituation(int situation)
    {
        if (voiceDb == null) return 0;
        return voiceDb.GetRandomVarcoIdBySituation(situation);
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

            // Poppy 파일인 경우 모든 버전을 찾음 (_1, _2 등)
            if (voice.VarcoVoiceFile.StartsWith("Poppy_"))
            {
                // _1, _2, _3 등 모든 버전 찾기
                for (int i = 1; i <= 3; i++) // 최대 3개 버전까지 지원
                {
                    string fileNameWithVersion = $"{voice.VarcoVoiceFile}_{i}";

                    string[] guids = UnityEditor.AssetDatabase.FindAssets(
                        fileNameWithVersion,
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
                                clip = clip,
                                fileName = fileNameWithVersion
                            });
                            foundCount++;
                        }
                    }
                }
            }
            else
            {
                // 일반 파일은 그대로 찾기
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
                            clip = clip,
                            fileName = voice.VarcoVoiceFile
                        });
                        foundCount++;
                    }
                }
                else
                {
                    Debug.LogWarning($"[VoiceManager] 파일 없음: {voice.VarcoVoiceFile} (VarcoID: {voice.VarcoID})");
                    missingCount++;
                }
            }
        }

        audioClips.Sort((a, b) => a.varcoId.CompareTo(b.varcoId));

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[VoiceManager] 자동 로드 완료: {foundCount}개 발견, {missingCount}개 누락");
    }
#endif
}
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Region Definition")]
public class RegionDefinition : ScriptableObject
{
    public EntranceType regionId;         
    public string displayName;       
    public string subtitle;         
    public Color mainColor = Color.white;
    public Sprite backgroundSprite;  // 배너용 이미지
    public AudioClip enterSfx;       // 진입 사운드
    public float showDuration = 2f;  // 화면에 머무르는 시간
}

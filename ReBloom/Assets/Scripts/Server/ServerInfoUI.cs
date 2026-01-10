using TMPro;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public class ServerInfoUI : MonoBehaviour
{
    [SerializeField] private TMP_Text joinCodeText;   // "참여코드 : XXXX"
    [SerializeField] private TMP_Text countText;      // "서버인원 : N"
    [SerializeField] private TMP_Text listText;       // "서버원 목록 :\n- ..."

    public void ToggleUI()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }

    private void OnEnable()
    {
        TryHook();
    }

    private void OnDisable()
    {
        Unhook();
    }

    private void TryHook()
    {
        if (PlayerRegistry.I == null)
        {
            Invoke(nameof(TryHook), 0.1f);
            return;
        }

        PlayerRegistry.I.Players.OnListChanged += OnPlayersChanged;
        PlayerRegistry.I.JoinCode.OnValueChanged += OnJoinCodeChanged;

        RefreshAll();
    }

    private void Unhook()
    {
        if (PlayerRegistry.I == null) return;

        PlayerRegistry.I.Players.OnListChanged -= OnPlayersChanged;
        PlayerRegistry.I.JoinCode.OnValueChanged -= OnJoinCodeChanged;
    }

    private void OnPlayersChanged(NetworkListEvent<PlayerEntry> e) => RefreshPlayers();
    private void OnJoinCodeChanged(FixedString32Bytes prev, FixedString32Bytes next) => RefreshJoinCode();

    private void RefreshAll()
    {
        RefreshJoinCode();
        RefreshPlayers();
    }

    private void RefreshJoinCode()
    {
        if (joinCodeText == null || PlayerRegistry.I == null) return;

        string code = PlayerRegistry.I.JoinCode.Value.ToString();
        if (string.IsNullOrEmpty(code)) code = "(Host만 표시)";

        joinCodeText.text = $"참여코드 : {code}";
    }

    private void RefreshPlayers()
    {
        if (PlayerRegistry.I == null) return;

        int n = PlayerRegistry.I.Players.Count;
        if (countText) countText.text = $"서버인원 : {n}";

        if (listText)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("서버원 목록 :");

            for (int i = 0; i < PlayerRegistry.I.Players.Count; i++)
            {
                var p = PlayerRegistry.I.Players[i];
                sb.Append("- ");
                sb.Append(p.Name.ToString());
                sb.AppendLine();
            }

            listText.text = sb.ToString();
        }
    }
}

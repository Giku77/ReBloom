using Unity.Netcode;
using Unity.Collections;
using UnityEngine;

public class PlayerName : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> Name =
        new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            var myName = NicknameStore.CurrentName;
            SubmitNameRpc(myName);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void SubmitNameRpc(string name)
    {
        Name.Value = Sanitize(name);
    }

    private FixedString32Bytes Sanitize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) s = "Player";
        s = s.Trim();
        if (s.Length > 16) s = s.Substring(0, 16);
        return new FixedString32Bytes(s);
    }
}

public static class NicknameStore
{
    public static string CurrentName
    {
        get => PlayerPrefs.GetString("nickname", "Player");
        set => PlayerPrefs.SetString("nickname", value);
    }
}

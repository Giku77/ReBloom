using Unity.Netcode;
using UnityEngine;

public class NetworkBuildingBootstrap : NetworkBehaviour
{
    private BuildingInstance _inst;

    private void Awake()
    {
        _inst = GetComponent<BuildingInstance>();
    }

    public override void OnNetworkSpawn()
    {
        // 모든 클라(서버 포함)에서 로컬 시스템에 등록
        if (BuildManager.I != null && _inst != null)
        {
            BuildManager.I.RegisterBuilding(_inst);
            BuildManager.I.OccupyForNetworkSpawn(_inst); // 아래 BuildManager에 추가할 함수
        }
    }

    public override void OnNetworkDespawn()
    {
        // 모든 클라에서 로컬 시스템에서 해제
        if (BuildManager.I != null && _inst != null)
        {
            BuildManager.I.ReleaseForNetworkDespawn(_inst);
            BuildManager.I.UnregisterBuilding(_inst);
        }
    }
}
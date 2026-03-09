using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BuildManager : MonoBehaviour
{
    public static BuildManager I;
    public bool IsInitialized { get; private set; } = false;
    private void Awake()
    {
        I = this;
        
        var inventory = FindFirstObjectByType<GameInventory>();
        var arcDB = new ArcDB();
        arcDB.LoadFromBG();
        var arcRecipeDB = new ArcRecipeDB();
        arcRecipeDB.LoadFromBG();

        Init(arcDB, arcRecipeDB, inventory);
        IsInitialized = true;
    }

    private void OnEnable()
    {
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned += BindLocalPlayer;
        NetworkPlayerOwnerGate.OnLocalPlayerDespawned += UnbindLocalPlayer;
    }

    private void OnDisable()
    {
        NetworkPlayerOwnerGate.OnLocalPlayerSpawned -= BindLocalPlayer;
        NetworkPlayerOwnerGate.OnLocalPlayerDespawned -= UnbindLocalPlayer;
    }

    private void BindLocalPlayer(GameObject go)
    {
        player = go;
        Debug.Log($"[BuildManager] Bound Local Player: {go.name}");
    }

    private void UnbindLocalPlayer()
    {
        if (player != null) Debug.Log("[BuildManager] Unbound Local Player");
        player = null;
    }

    private BuildingFootprintProvider footprintProvider;

    private ArcDB arcDB;
    public ArcDB ArcDB => arcDB;
    private ArcRecipeDB recipeDB;
    public ArcRecipeDB RecipeDB => recipeDB;
    private GameInventory inventory;

    public GameObject prefab;

    [Header("Build Rules")]
    [SerializeField] private LayerMask buildableLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float maxHeightDiff = 0.1f;
    [SerializeField] private float maxSlopeAngle = 5f;
    [SerializeField] private StageDetector stageDetector;

    private List<IBuildRule> buildRules = new List<IBuildRule>();

    private readonly Dictionary<int, HashSet<BuildingInstance>> instancesByArcId 
    = new Dictionary<int, HashSet<BuildingInstance>>(); 

    private Dictionary <int, int> buildingCounts = new Dictionary<int, int>();

    private GameObject player;

    private bool debugBuildingMode = false;

    public bool debugMode => debugBuildingMode;

    
    public void RegisterBuilding(BuildingInstance inst)
    {
        if (inst == null) return;

        if (!instancesByArcId.TryGetValue(inst.ArcId, out var set))
        {
            set = new HashSet<BuildingInstance>();
            instancesByArcId[inst.ArcId] = set;
        }

        if (set.Add(inst))
        {
            if (buildingCounts.ContainsKey(inst.ArcId))
                buildingCounts[inst.ArcId]++;
            else
                buildingCounts[inst.ArcId] = 1;
        }

        var sp = inst.GetComponent<CorridorSocketProvider>();
        if (sp != null)
        {
            int rotIndex = CorridorGrid.GetRotIndex(inst.transform.rotation);

            // baseCell: 건물 pivot 위치를 기준 셀로 쓰는 방식(가장 단순)
            Vector2Int baseCell = CorridorGrid.WorldToCell(inst.transform.position);
            CorridorSocketManager.I?.RegisterSockets(sp, baseCell, rotIndex);
        }
    }

    public void UnregisterBuilding(BuildingInstance inst)
    {
        if (inst == null) return;

        var sp = inst.GetComponent<CorridorSocketProvider>();
        if (sp != null)
            CorridorSocketManager.I?.UnregisterSockets(sp);

        if (instancesByArcId.TryGetValue(inst.ArcId, out var set))
        {
            if (set.Remove(inst))
            {
                if (buildingCounts.ContainsKey(inst.ArcId))
                {
                    buildingCounts[inst.ArcId]--;
                    if (buildingCounts[inst.ArcId] <= 0)
                        buildingCounts.Remove(inst.ArcId);
                }
            }
        }
    }

    public int GetCount(int arcId)
    {
        if (buildingCounts.TryGetValue(arcId, out var count))
            return count;
        return 0;
    }

    public IReadOnlyCollection<BuildingInstance> GetInstances(int arcId)
    {
        if (instancesByArcId.TryGetValue(arcId, out var set))
            return set;
        return Array.Empty<BuildingInstance>();
    }

    private IEnumerable<Vector2Int> GetCellsFromFootprint(BuildingFootprint fp, Vector3 pos, Quaternion rot)
    {
        int sx = Mathf.Max(1, Mathf.CeilToInt(fp.sizeX / CorridorGrid.CellSize));
        int sz = Mathf.Max(1, Mathf.CeilToInt(fp.sizeZ / CorridorGrid.CellSize));

        int rotIndex = CorridorGrid.GetRotIndex(rot);
        if (rotIndex % 2 == 1) (sx, sz) = (sz, sx);

        Vector2Int baseCell = CorridorGrid.WorldToCell(pos);

        int hx = sx / 2;
        int hz = sz / 2;

        for (int x = -hx; x < -hx + sx; x++)
            for (int z = -hz; z < -hz + sz; z++)
                yield return new Vector2Int(baseCell.x + x, baseCell.y + z);
    }


    private bool TryAdjustToGround(ArcContext ctx, out Vector3 adjustedPos, out string errorCode)
    {
        var fp = ctx.FootPrint;
        Vector3 center = ctx.Position;
        Quaternion rot = ctx.Rotation;

        float halfX = fp.sizeX * 0.5f;
        float halfZ = fp.sizeZ * 0.5f;

        Vector3[] localOffsets = new[]
        {
            new Vector3(-halfX, 0, -halfZ),
            new Vector3(-halfX, 0,  halfZ),
            new Vector3( halfX, 0, -halfZ),
            new Vector3( halfX, 0,  halfZ),
            Vector3.zero
        };

        float minY = float.MaxValue;
        float maxY = float.MinValue;

        Vector3[] hitPoints = new Vector3[localOffsets.Length];
        int hitCount = 0;

        for (int i = 0; i < localOffsets.Length; i++)
        {
            Vector3 worldPos = center + rot * localOffsets[i] + Vector3.up * 5f;

            if (Physics.Raycast(worldPos, Vector3.down, out var hit, 20f, buildableLayer))
            {
                hitPoints[i] = hit.point;
                hitCount++;

                float y = hit.point.y;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        if (hitCount == 0)
        {
            errorCode = "NO_GROUND";
            adjustedPos = center;
            return false;
        }

        float heightDiff = maxY - minY;
        if (heightDiff > maxHeightDiff)
        {
            errorCode = "SLOPE_TOO_HIGH";
            adjustedPos = center;
            return false;
        }

        float finalY = maxY - ctx.DepthOffset;

        adjustedPos = new Vector3(center.x, finalY, center.z);
        errorCode = null;
        return true;
    }

    private void InitRules()
    {
        buildRules.Add(new FlatSurfaceRule(buildableLayer, maxHeightDiff, maxSlopeAngle));
        buildRules.Add(new CollisionRule(obstacleLayer));
        buildRules.Add(new LimitRule(this));
        buildRules.Add(new CorridorAttachRule());
        buildRules.Add(new CorridorCellRule());
        buildRules.Add(new OccupancyRule());
    }

    public void ToggleDebugBuildingMode()
    {
        debugBuildingMode = !debugBuildingMode;
    }

    public void Init(ArcDB arcDB, ArcRecipeDB recipeDB, GameInventory inventory)
    {
        InitRules();
        this.arcDB = arcDB;
        this.recipeDB = recipeDB;
        this.inventory = inventory;
        footprintProvider = GetComponent<BuildingFootprintProvider>();
    }

    public bool Validate(ArcContext ctx, out string errorCode)
    {
        foreach (var rule in buildRules)
        {
            if (!rule.Validate(ctx, out errorCode))
                return false;
        }
        errorCode = null;
        return true;
    }

    public bool CanBuildAt(ArcData arc, Vector3 pos, Quaternion rot, out string errorCode, bool isMove = false)
    {
        float depthOffset = 0.1f; 

        if (arc.buildPrefab != null &&
            arc.buildPrefab.TryGetComponent<BuildingInstance>(out var biOnPrefab))
        {
            if (biOnPrefab.depthOffset != 0f)
              depthOffset = biOnPrefab.depthOffset;
        }

        var ctx = new ArcContext
        {
            Data = arc,
            Position = pos,
            Rotation = rot,
            ArcPrefab = arc.buildPrefab,
            FootPrint = footprintProvider.GetFootprint(arc),
            PlayerTransform = player.transform,
            DepthOffset = depthOffset,
        };

        if (!Validate(ctx, out errorCode))
            return false;

        if (!TryAdjustToGround(ctx, out var adjustedPos, out errorCode))
            return false;

        ctx.Position = adjustedPos;
        return true;
    }

    public bool TryMoveBuilding(BuildingInstance inst, Vector3 desiredPos, Quaternion desiredRot, out string errorCode)
    {
        if (inst == null)
        {
            errorCode = "NULL_INSTANCE";
            return false;
        }

        if (!arcDB.TryGet(inst.ArcId, out var arc))
        {
            errorCode = "ARC_NOT_FOUND";
            return false;
        }

        float depthOffset = 0.1f;

        if (arc.buildPrefab != null &&
            arc.buildPrefab.TryGetComponent<BuildingInstance>(out var biOnPrefab) &&
            biOnPrefab.depthOffset != 0f)
        {
            depthOffset = biOnPrefab.depthOffset;
        }

        var ctx = new ArcContext
        {
            Data = arc,
            Position = desiredPos,
            Rotation = desiredRot,
            ArcPrefab = arc.buildPrefab,
            FootPrint = footprintProvider.GetFootprint(arc),
            PlayerTransform = player.transform,
            DepthOffset = depthOffset,
            IgnoreOccupancyInstance = inst
        };

        if (!Validate(ctx, out errorCode))
            return false;

        if (!TryAdjustToGround(ctx, out var adjustedPos, out errorCode))
            return false;

        inst.transform.SetPositionAndRotation(adjustedPos, desiredRot);

        if (inst.TryGetComponent<CorridorNode>(out var node))
        {
            var cell = CorridorGrid.WorldToCell(adjustedPos);
            node.Cell = cell;
            CorridorConnectionManager.I.Register(node); 
        }

        SetupTemporaryPassThrough(inst.gameObject);

        errorCode = null;
        AutoSaveService.I?.RequestSave("MoveBuilding");
        GridOccupancyManager.I?.Release(inst);
        var fp = footprintProvider.GetFootprint(arc);
        var cells = GetCellsFromFootprint(fp, adjustedPos, desiredRot);
        GridOccupancyManager.I?.Occupy(inst, cells);
        return true;
    }

    public bool CanMoveAt(BuildingInstance inst, Vector3 pos, Quaternion rot, out string errorCode)
    {
        errorCode = null;
        if (inst == null) { errorCode = "NULL"; return false; }
        if (!arcDB.TryGet(inst.ArcId, out var arc)) { errorCode = "ARC_NOT_FOUND"; return false; }

        float depthOffset = 0.1f;
        if (arc.buildPrefab != null && arc.buildPrefab.TryGetComponent<BuildingInstance>(out var bi) && bi.depthOffset != 0f)
            depthOffset = bi.depthOffset;

        var ctx = new ArcContext
        {
            Data = arc,
            Position = pos,
            Rotation = rot,
            ArcPrefab = arc.buildPrefab,
            FootPrint = footprintProvider.GetFootprint(arc),
            PlayerTransform = player.transform,
            DepthOffset = depthOffset,
            IgnoreOccupancyInstance = inst
        };

        if (!Validate(ctx, out errorCode))
            return false;

        if (!TryAdjustToGround(ctx, out _, out errorCode))
            return false;

        return true;
    }


    // 기존 로직 그대로 검사/재료처리까지는 "클라 UX용"으로 둘 수 있는데,
    // 멀티에서는 실제 차감은 서버에서만 해야 안전함.
    // 그래서 여기서는 "클라에서는 토스트/검사만", 재료 차감은 서버에서 처리하는 걸 추천.

    public bool TryBuild(int arcId, Vector3 pos, Quaternion rot)
    {
        if (!arcDB.TryGet(arcId, out var arc))
        {
            Debug.LogWarning($"없는 건물: {arcId}");
            return false;
        }

        if (!IsInBuildableZone())
        {
            ToastMessageUI.Instance.Show("이 지역에서는 건물을 지을 수 없습니다.");
            return false;
        }

        if (!CanBuildAt(arc, pos, rot, out var errorCode))
        {
            ToastMessageUI.Instance.Show($"건물 설치 불가: {errorCode}");
            return false;
        }

        // 멀티면 서버에 요청만
        if (NetworkBuildManager.I != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkBuildManager.I.RequestBuild(arcId, pos, rot);
            return true;
        }

        // 오프라인/싱글이면 기존 방식 유지
        return Spawn(arc, pos, rot);
    }

    public void OccupyForNetworkSpawn(BuildingInstance inst)
    {
        if (inst == null) return;
        if (!arcDB.TryGet(inst.ArcId, out var arc)) return;

        var fp = footprintProvider.GetFootprint(arc);
        var cells = GetCellsFromFootprint(fp, inst.transform.position, inst.transform.rotation);

        GridOccupancyManager.I?.Occupy(inst, cells);

        if (inst.TryGetComponent<CorridorNode>(out var corridorNode))
        {
            var cell = CorridorGrid.WorldToCell(inst.transform.position);
            corridorNode.Cell = cell;
            CorridorConnectionManager.I?.Register(corridorNode);
        }
    }

    public void ReleaseForNetworkDespawn(BuildingInstance inst)
    {
        if (inst == null) return;

        if (inst.TryGetComponent<CorridorNode>(out var node))
            CorridorConnectionManager.I?.Unregister(node);

        GridOccupancyManager.I?.Release(inst);

        var sp = inst.GetComponent<CorridorSocketProvider>();
        if (sp != null)
            CorridorSocketManager.I?.UnregisterSockets(sp);
    }

    public bool CanBuildAt_Server(ArcData arc, Vector3 pos, Quaternion rot, Transform builder, out string errorCode, out Vector3 adjustedPos)
    {
        errorCode = null;
        adjustedPos = pos;

        if (arc == null)
        {
            errorCode = "ARC_NULL";
            return false;
        }

        float depthOffset = 0.1f;
        if (arc.buildPrefab != null &&
            arc.buildPrefab.TryGetComponent<BuildingInstance>(out var biOnPrefab) &&
            biOnPrefab.depthOffset != 0f)
        {
            depthOffset = biOnPrefab.depthOffset;
        }

        var ctx = new ArcContext
        {
            Data = arc,
            Position = pos,
            Rotation = rot,
            ArcPrefab = arc.buildPrefab,
            FootPrint = footprintProvider.GetFootprint(arc),
            PlayerTransform = builder,     // ? 서버에서도 정확한 플레이어 기준
            DepthOffset = depthOffset,
        };

        if (!Validate(ctx, out errorCode))
            return false;

        if (!TryAdjustToGround(ctx, out adjustedPos, out errorCode))
            return false;

        return true;
    }

    // 서버에서 구역 제한 검증도 하려면 (StageDetector가 서버에도 존재한다는 전제)
    public bool IsInBuildableZone_Server(Transform builder)
    {
        // 서버에서 stageDetector가 의미 있게 동작하도록 구성되어 있으면 그대로 사용 가능
        return IsInBuildableZone();
    }

    public bool TryGetRecipe(int arcId, out ArcRecipe recipe)
    {
        return recipeDB.TryGetRecipe(arcId, out recipe);
    }

    public bool HasMaterials(PlayerInventoryRuntime inv, ArcRecipe recipe)
    {
        if (inv == null) return false;

        foreach (var (itemId, amount) in recipe.materials)
        {
            if (!inv.HasItem(itemId, amount))
                return false;
        }

        return true;
    }

    public void Remove(PlayerInventoryRuntime inv, ArcRecipe recipe)
    {
        if (inv == null) return;

        foreach (var (itemId, amount) in recipe.materials)
        {
            inv.TryRemoveItem(itemId, amount);
        }
    }

    public bool HasMaterials(ArcRecipe recipe)
    {
        foreach (var (itemId, amount) in recipe.materials)
        {
            if (!inventory.HasItem(itemId, amount))
                return false;
        }
        return true;
    }

    private void Remove(ArcRecipe recipe)
    {
        foreach (var (itemId, amount) in recipe.materials)
            inventory.RemoveItem(itemId, amount);
    }

    private bool Spawn(ArcData arc, Vector3 pos, Quaternion rot)
    {
        //var prefab = Resources.Load<GameObject>($"Arc/{arc.arcId}");
        float depthOffset = 0.1f;

        if (arc.buildPrefab != null &&
            arc.buildPrefab.TryGetComponent<BuildingInstance>(out var biOnPrefab))
        {
            if (biOnPrefab.depthOffset != 0f)
                depthOffset = biOnPrefab.depthOffset;
        }
        var ctx = new ArcContext
        {
            Data = arc,
            Position = pos,
            Rotation = rot,
            ArcPrefab = arc.buildPrefab,
            FootPrint = footprintProvider.GetFootprint(arc),
            PlayerTransform = player.transform,
            DepthOffset = depthOffset
        };
        if (!TryAdjustToGround(ctx, out var adjustedPos, out _))
           adjustedPos = pos;   
        var buildprefab = arc.buildPrefab != null ? arc.buildPrefab : prefab;
        if (buildprefab == null)
        {
            Debug.LogError($"프리팹 없음: {arc.arcId}");
            return false;
        }
        var p = Instantiate(buildprefab, adjustedPos, rot);
        p.SetActive(false);

        var bInstance = p.GetComponent<BuildingInstance>();
        if (bInstance != null) bInstance.arcId = arc.arcId;


        var id = p.GetComponent<SaveableEntity>();
        if (id != null) id.AssignNewId();

        var ws = p.GetComponent<WorldStorage>();
        if (ws != null && id != null)
            ws.SetContainerGuid($"container:{id.PersistentId}");

        p.SetActive(true);
        SoundManager.I?.PlayBuild();
        //RegisterBuilding(bInstance);
        var fp = footprintProvider.GetFootprint(arc);
        var cells = GetCellsFromFootprint(fp, p.transform.position, p.transform.rotation);
        GridOccupancyManager.I?.Occupy(bInstance, cells);
        if (p.TryGetComponent<CorridorNode>(out var corridorNode))
        {
            var cell = CorridorGrid.WorldToCell(adjustedPos);
            corridorNode.Cell = cell;
            CorridorConnectionManager.I.Register(corridorNode);
        }
        if (p.TryGetComponent<InteractionHighlight>(out var highlight))
          highlight.promptFormat = $"상호작용 [E] : {arc.name}";
        SetupTemporaryPassThrough(p);
        NetworkQuestManager.I?.ReportCraft(arc.arcId);
        AutoSaveService.I?.RequestSave("Build");
        return true;
    }

    private void SetupTemporaryPassThrough(GameObject buildingInstance)
    {
        if (player == null) return;

        var playerColliders   = player.GetComponentsInChildren<Collider>();
        var buildingColliders = buildingInstance.GetComponentsInChildren<Collider>();

        if (playerColliders.Length == 0 || buildingColliders.Length == 0)
            return;

        var passThrough = buildingInstance.AddComponent<TemporaryPlayerPassThrough>();
        passThrough.Init(playerColliders, buildingColliders);
    }

    public bool IsInBuildableZone()
    {
       if (stageDetector == null)
            return true;
       if (stageDetector.CurrentStage.StageID != (int)EntranceType.Shelter || !stageDetector.CanBuild)
            return false;
       return true;
    }

    public bool TryRemoveBuilding(BuildingInstance inst)
    {
        if (inst == null) return false;

        // 멀티면 서버 요청
        var netObj = inst.GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned && NetworkBuildManager.I != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkBuildManager.I.RequestRemove(netObj.NetworkObjectId);
            return true;
        }

        // 싱글 fallback
        if (inst.TryGetComponent<CorridorNode>(out var node))
            CorridorConnectionManager.I.Unregister(node);

        GridOccupancyManager.I?.Release(inst);

        UnregisterBuilding(inst);
        Destroy(inst.gameObject);

        AutoSaveService.I?.RequestSave("RemoveBuilding");
        return true;
    }

    public void RemoveAllBuildingsOfArc(int arcId)
    {
        var instances = new List<BuildingInstance>(GetInstances(arcId));
        foreach (var inst in instances)
            TryRemoveBuilding(inst);
    }

    public void MoveBuilding(BuildingInstance inst, Vector3 newPos, Quaternion newRot)
    {
        // 필요하면 다시 바닥 맞추기 or 규칙 체크
        inst.transform.SetPositionAndRotation(newPos, newRot);
    }

    public IEnumerable<BuildingInstance> EnumerateAllInstances()
    {
        foreach (var kv in instancesByArcId)
        {
            foreach (var inst in kv.Value)
            {
                if (inst != null) yield return inst;
            }
        }
    }

    public void ClearAllBuildingsForLoad()
    {
        var list = new List<BuildingInstance>(EnumerateAllInstances());

        foreach (var inst in list)
        {
            if (inst == null) continue;

            if (inst.TryGetComponent<CorridorNode>(out var node))
                CorridorConnectionManager.I.Unregister(node);

            UnregisterBuilding(inst);
            Destroy(inst.gameObject);
        }
    }


    public BuildingInstance SpawnForLoad(int arcId, Vector3 pos, Quaternion rot, string guid, string containerGuid)
    {
        if (!arcDB.TryGet(arcId, out var arc) || arc.buildPrefab == null)
            return null;

        // 기존 Spawn과 동일하게 바닥 맞추기까지 하고 싶으면 TryAdjustToGround 로직 재사용
        var p = Instantiate(arc.buildPrefab, pos, rot);

        var bi = p.GetComponent<BuildingInstance>();
        if (bi != null) bi.arcId = arcId;

        var idComp = p.GetComponent<SaveableEntity>();
        if (idComp != null) idComp.ForceSetId(guid);

        //RegisterBuilding(bi);

        // corridor/패스스루 등도 Spawn과 동일 처리
        if (p.TryGetComponent<CorridorNode>(out var corridorNode))
        {
            var cell = CorridorGrid.WorldToCell(p.transform.position);
            corridorNode.Cell = cell;
            CorridorConnectionManager.I.Register(corridorNode);
        }
        SetupTemporaryPassThrough(p);

        if (p.TryGetComponent<WorldStorage>(out var ws))
        {
            ws.SetContainerGuid(containerGuid);
        }

        var nm = NetworkManager.Singleton;
        var netObj = p.GetComponent<NetworkObject>();
        if (netObj != null && nm != null && nm.IsListening && nm.IsServer && !netObj.IsSpawned)
            netObj.Spawn(true);

        return bi;
    }

}

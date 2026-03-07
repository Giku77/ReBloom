using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HostRoomPopupUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RelayBootstrap relayBootstrap;
    [SerializeField] private GameObject popupRoot;

    [Header("Account Input")]
    [SerializeField] private TMP_InputField hostNicknameInput;
    [SerializeField] private TMP_InputField hostPasswordInput;
    [SerializeField] private TextMeshProUGUI hostNicknameGuideText;
    [SerializeField] private TextMeshProUGUI hostPasswordGuideText;
    [SerializeField] private string defaultNicknameGuideText = "멀티플레이에서 사용할 닉네임을 입력하세요.";
    [SerializeField] private string defaultPasswordGuideText = "세이브 계정 비밀번호를 입력하세요.";
    [SerializeField] private string invalidNicknameGuideText = "닉네임은 2자 이상 입력해주세요.";
    [SerializeField] private string invalidPasswordGuideText = "비밀번호를 입력해주세요.";
    [SerializeField] private string loginFailedGuideText = "계정 정보를 확인한 뒤 다시 시도해주세요.";
    [SerializeField] private string defaultNickname = "Player";

    [Header("Slots")]
    [SerializeField] private Transform slotContentRoot;
    [SerializeField] private HostRoomSlotCardUI slotCardPrefab;
    [SerializeField] private TextMeshProUGUI slotListLabel;
    [SerializeField] private TextMeshProUGUI selectedSlotInfoText;
    [SerializeField] private TextMeshProUGUI emptyStateText;
    [SerializeField] private int maxSlotCount = 8;
    [SerializeField] private bool enableSlotDelete = true;

    [Header("Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button cancelButton;

    private readonly List<HostRoomSlotCardUI> spawnedCards = new();
    private IReadOnlyList<WorldSlotMetaDTO> slots = Array.Empty<WorldSlotMetaDTO>();
    private WorldSlotMetaDTO selectedSlot;
    private bool isBusy;

    private GameObject RootObject => popupRoot != null ? popupRoot : gameObject;

    private void Awake()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(() => OnContinueClicked().Forget());

        if (newGameButton != null)
            newGameButton.onClick.AddListener(() => OnNewGameClicked().Forget());

        if (cancelButton != null)
            cancelButton.onClick.AddListener(Close);

        if (hostNicknameInput != null)
            hostNicknameInput.onEndEdit.AddListener(_ => OnCredentialInputCommitted().Forget());

        if (hostPasswordInput != null)
            hostPasswordInput.onEndEdit.AddListener(_ => OnCredentialInputCommitted().Forget());

        if (RootObject.activeSelf)
            RootObject.SetActive(false);
    }

    public void Open()
    {
        RootObject.SetActive(true);
        PrepareOpenState();
        RefreshSlotsForCurrentCredentialAsync().Forget();
    }

    public void Close()
    {
        if (RootObject.activeSelf)
            RootObject.SetActive(false);
    }

    public void CloseImmediate()
    {
        RootObject.SetActive(false);
    }

    private void PrepareOpenState()
    {
        string initialNickname = !string.IsNullOrWhiteSpace(PlayFabAuth.CurrentDisplayName)
            ? PlayFabAuth.CurrentDisplayName
            : NicknameStore.CurrentName;

        if (hostNicknameInput != null)
            hostNicknameInput.SetTextWithoutNotify(initialNickname);

        if (hostPasswordInput != null)
            hostPasswordInput.SetTextWithoutNotify(string.Empty);

        SetGuideTexts(defaultNicknameGuideText, defaultPasswordGuideText);
        selectedSlot = null;
        ClearCards();
        SetEmptyState(true);
        UpdateSelectedSlotInfo();
        UpdateButtons();
    }

    private async UniTaskVoid RefreshSlotsForCurrentCredentialAsync()
    {
        if (!TryGetCredential(out var displayName, out var password))
        {
            slots = Array.Empty<WorldSlotMetaDTO>();
            SetEmptyState(true);
            UpdateSelectedSlotInfo();
            UpdateButtons();
            return;
        }

        await SwitchAccountAndRefreshSlotsAsync(displayName, password);
    }

    private async UniTaskVoid OnCredentialInputCommitted()
    {
        if (!RootObject.activeSelf || isBusy)
            return;

        if (!TryGetCredential(out var displayName, out var password))
        {
            ClearCards();
            slots = Array.Empty<WorldSlotMetaDTO>();
            SetSelectedSlot(null);
            SetEmptyState(true);
            return;
        }

        await SwitchAccountAndRefreshSlotsAsync(displayName, password);
    }

    private async UniTask<bool> SwitchAccountAndRefreshSlotsAsync(string displayName, string password)
    {
        isBusy = true;
        UpdateButtons();
        SetGuideTexts(defaultNicknameGuideText, defaultPasswordGuideText);

        try
        {
            if (!await EnsureCredentialReadyAsync(displayName, password))
            {
                ClearCards();
                slots = Array.Empty<WorldSlotMetaDTO>();
                SetSelectedSlot(null);
                SetEmptyState(true);
                SetGuideTexts(defaultNicknameGuideText, loginFailedGuideText);
                return false;
            }

            ClearCards();
            slots = await LoadSlotsAsync();

            if (slotListLabel != null)
                slotListLabel.text = slots.Count > 0 ? "월드 선택" : "새 월드 시작";

            if (slots.Count > 0)
            {
                foreach (var slot in slots)
                {
                    if (slotCardPrefab == null || slotContentRoot == null)
                        break;

                    var card = Instantiate(slotCardPrefab, slotContentRoot);
                    card.Bind(slot, OnSlotSelected, enableSlotDelete ? OnSlotDeleteRequested : null);
                    card.SetSelected(false);
                    spawnedCards.Add(card);
                }

                SetSelectedSlot(slots[0]);
                SetEmptyState(false);
            }
            else
            {
                SetSelectedSlot(null);
                SetEmptyState(true);
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[HostRoomPopupUI] Failed to refresh slots for credential input. {e.Message}");
            ClearCards();
            slots = Array.Empty<WorldSlotMetaDTO>();
            SetSelectedSlot(null);
            SetEmptyState(true);
            SetGuideTexts(defaultNicknameGuideText, loginFailedGuideText);
            return false;
        }
        finally
        {
            isBusy = false;
            UpdateButtons();
        }
    }

    private async UniTask<IReadOnlyList<WorldSlotMetaDTO>> LoadSlotsAsync()
    {
        if (SaveManager.I == null)
            return Array.Empty<WorldSlotMetaDTO>();

        return await SaveManager.I.ListWorldSlotsAsync();
    }

    private void OnSlotSelected(WorldSlotMetaDTO slot)
    {
        SetSelectedSlot(slot);
    }

    private void SetSelectedSlot(WorldSlotMetaDTO slot)
    {
        selectedSlot = slot;

        if (selectedSlot != null)
            ApplySelectedSlot(selectedSlot.slotId, selectedSlot.displayName);

        foreach (var card in spawnedCards)
        {
            if (card == null)
                continue;

            bool isSelected = selectedSlot != null && string.Equals(card.SlotId, selectedSlot.slotId, StringComparison.OrdinalIgnoreCase);
            card.SetSelected(isSelected);
        }

        UpdateSelectedSlotInfo();
        UpdateButtons();
    }

    private void ApplySelectedSlot(string slotId, string displayName)
    {
        if (SaveManager.I != null)
        {
            SaveManager.I.SetActiveSlot(slotId, displayName);
            return;
        }

        GameStartContext.SlotId = string.IsNullOrWhiteSpace(slotId) ? GameStartContext.SlotId : slotId.Trim();
        GameStartContext.SlotDisplayName = string.IsNullOrWhiteSpace(displayName)
            ? GameStartContext.SlotId
            : displayName.Trim();
    }

    private async void OnSlotDeleteRequested(WorldSlotMetaDTO slot)
    {
        if (slot == null || isBusy)
            return;

        if (SaveManager.I == null)
        {
            ShowToast("SaveManager가 준비되지 않았습니다.");
            return;
        }

        isBusy = true;
        UpdateButtons();

        try
        {
            await SaveManager.I.ResetSlotAsync(slot.slotId, saveDefaultImmediately: false);

            if (selectedSlot != null && string.Equals(selectedSlot.slotId, slot.slotId, StringComparison.OrdinalIgnoreCase))
                selectedSlot = null;

            ShowToast($"슬롯 삭제 완료: {(string.IsNullOrWhiteSpace(slot.displayName) ? slot.slotId : slot.displayName)}");
            if (TryGetCredential(out var displayName, out var password))
                await SwitchAccountAndRefreshSlotsAsync(displayName, password);
        }
        catch (Exception e)
        {
            isBusy = false;
            UpdateButtons();
            ShowToast($"슬롯 삭제 실패: {e.Message}");
        }
    }

    private async UniTaskVoid OnContinueClicked()
    {
        if (isBusy)
            return;

        if (!TryGetCredential(out var displayName, out var password) || !await EnsureCredentialReadyAsync(displayName, password))
        {
            FocusInvalidField();
            return;
        }

        if (selectedSlot == null)
        {
            ShowToast("이어할 월드를 선택해주세요.");
            return;
        }

        GameStartContext.StartMode = GameStartContext.Mode.Continue;
        ApplySelectedSlot(selectedSlot.slotId, selectedSlot.displayName);
        Close();

        relayBootstrap?.StartHostRelayWithCredential(displayName, password);
    }

    private async UniTaskVoid OnNewGameClicked()
    {
        if (isBusy)
            return;

        if (!TryGetCredential(out var displayName, out var password) || !await EnsureCredentialReadyAsync(displayName, password))
        {
            FocusInvalidField();
            return;
        }

        isBusy = true;
        UpdateButtons();

        string newSlotId = await GetSuggestedNewSlotIdAsync();
        if (string.IsNullOrWhiteSpace(newSlotId))
            newSlotId = "slot1";

        GameStartContext.StartMode = GameStartContext.Mode.NewGame;
        ApplySelectedSlot(newSlotId, newSlotId);

        if (SaveManager.I != null && await SaveManager.I.HasSaveAsync(newSlotId))
            await SaveManager.I.ResetSlotAsync(newSlotId, saveDefaultImmediately: false);

        Close();
        relayBootstrap?.StartHostRelayWithCredential(displayName, password);

        isBusy = false;
        UpdateButtons();
    }

    private async UniTask<string> GetSuggestedNewSlotIdAsync()
    {
        if (SaveManager.I == null)
            return GameStartContext.SlotId;

        return await SaveManager.I.SuggestNextSlotIdAsync(maxSlotCount);
    }

    private async UniTask<bool> EnsureCredentialReadyAsync(string displayName, string password)
    {
        if (!PlayFabAuth.TryValidateCredentials(displayName, password, out var normalizedDisplayName, out var normalizedPassword))
            return false;

        bool remoteReady;
        if (SaveManager.I != null)
            remoteReady = await SaveManager.I.EnsureRemoteReadyAsync(normalizedDisplayName, normalizedPassword);
        else
        {
            await PlayFabAuth.LoginAsync(normalizedDisplayName, normalizedPassword);
            remoteReady = true;
        }

        if (!remoteReady)
            return false;

        NicknameStore.CurrentName = normalizedDisplayName;
        SetGuideTexts(defaultNicknameGuideText, defaultPasswordGuideText);
        return true;
    }

    private bool TryGetCredential(out string displayName, out string password)
    {
        string rawDisplayName = hostNicknameInput != null ? hostNicknameInput.text : string.Empty;
        string rawPassword = hostPasswordInput != null ? hostPasswordInput.text : string.Empty;

        bool valid = PlayFabAuth.TryValidateCredentials(rawDisplayName, rawPassword, out displayName, out password);
        if (valid)
        {
            SetGuideTexts(defaultNicknameGuideText, defaultPasswordGuideText);
            return true;
        }

        string normalizedDisplayName = PlayFabAuth.NormalizeDisplayName(rawDisplayName);
        if (normalizedDisplayName.Length < 2)
            SetGuideTexts(invalidNicknameGuideText, defaultPasswordGuideText);
        else
            SetGuideTexts(defaultNicknameGuideText, invalidPasswordGuideText);

        return false;
    }

    private void FocusInvalidField()
    {
        string normalizedDisplayName = PlayFabAuth.NormalizeDisplayName(hostNicknameInput != null ? hostNicknameInput.text : string.Empty);
        if (normalizedDisplayName.Length < 2)
            hostNicknameInput?.ActivateInputField();
        else
            hostPasswordInput?.ActivateInputField();
    }

    private void UpdateSelectedSlotInfo()
    {
        if (selectedSlotInfoText == null)
            return;

        if (selectedSlot == null)
        {
            selectedSlotInfoText.text = slots.Count > 0
                ? "월드를 선택해서 이어하기를 누르세요."
                : "저장된 월드가 없습니다. 새 게임을 시작하세요.";
            return;
        }

        string title = string.IsNullOrWhiteSpace(selectedSlot.displayName) ? selectedSlot.slotId : selectedSlot.displayName;
        string scene = string.IsNullOrWhiteSpace(selectedSlot.sceneName) ? "Unknown Scene" : selectedSlot.sceneName;
        string savedAt = FormatTimestamp(selectedSlot.lastSavedAtUtcTicks);
        selectedSlotInfoText.text = $"선택된 월드: {title}\n마지막 저장: {savedAt}";
    }

    private void UpdateButtons()
    {
        bool hasValidCredential = PlayFabAuth.TryValidateCredentials(
            hostNicknameInput != null ? hostNicknameInput.text : string.Empty,
            hostPasswordInput != null ? hostPasswordInput.text : string.Empty,
            out _,
            out _);

        if (continueButton != null)
            continueButton.interactable = !isBusy && hasValidCredential && selectedSlot != null;

        if (newGameButton != null)
            newGameButton.interactable = !isBusy && hasValidCredential;

        if (cancelButton != null)
            cancelButton.interactable = true;
    }

    private void SetGuideTexts(string nicknameMessage, string passwordMessage)
    {
        if (hostNicknameGuideText != null)
            hostNicknameGuideText.text = nicknameMessage;

        if (hostPasswordGuideText != null)
            hostPasswordGuideText.text = passwordMessage;
    }

    private void SetEmptyState(bool visible)
    {
        if (emptyStateText != null)
            emptyStateText.gameObject.SetActive(visible);
    }

    private void ClearCards()
    {
        foreach (var card in spawnedCards)
        {
            if (card != null)
                Destroy(card.gameObject);
        }

        spawnedCards.Clear();
    }

    private void ShowToast(string message)
    {
        if (ToastService.I != null)
            ToastService.I.Show(message);
        else
            Debug.LogWarning(message);
    }

    private static string FormatTimestamp(long utcTicks)
    {
        if (utcTicks <= 0)
            return "저장 기록 없음";

        try
        {
            var utcTime = new DateTime(utcTicks, DateTimeKind.Utc);
            return utcTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        }
        catch
        {
            return "저장 기록 없음";
        }
    }
}

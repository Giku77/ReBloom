using System;
using System.Security.Cryptography;
using System.Text;
using Cysharp.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public static class PlayFabAuth
{
    public static string CurrentPlayFabId { get; private set; }
    public static string CurrentCredentialInput { get; private set; }
    public static string CurrentDisplayName { get; private set; }
    public static bool HasCredentialInput => !string.IsNullOrWhiteSpace(CurrentCredentialInput);
    public static string CurrentStorageNamespace => ResolveStorageNamespace();

    public static bool TryParseCredentialInput(string rawInput, out string displayName, out string password)
    {
        displayName = string.Empty;
        password = string.Empty;

        if (string.IsNullOrWhiteSpace(rawInput))
            return false;

        string input = rawInput.Trim();
        int separatorIndex = input.IndexOf('#');
        if (separatorIndex <= 0 || separatorIndex >= input.Length - 1)
            return false;

        return TryValidateCredentials(input.Substring(0, separatorIndex), input.Substring(separatorIndex + 1), out displayName, out password);
    }

    public static bool TryValidateCredentials(string rawDisplayName, string rawPassword, out string displayName, out string password)
    {
        displayName = NormalizeDisplayName(rawDisplayName);
        password = rawPassword?.Trim() ?? string.Empty;

        return displayName.Length >= 2 && !string.IsNullOrWhiteSpace(password);
    }

    public static string BuildCredentialInput(string displayName, string password)
    {
        return $"{NormalizeDisplayName(displayName)}#{password?.Trim()}";
    }

    public static string NormalizeDisplayName(string rawInputOrName)
    {
        if (TryParseCredentialInputFallback(rawInputOrName, out var displayName))
            return displayName;

        string display = rawInputOrName;
        if (string.IsNullOrWhiteSpace(display))
            display = "Player";

        display = display.Trim();
        if (display.Length > 16)
            display = display.Substring(0, 16);

        return display;
    }

    public static UniTask LoginAsync(string credentialInput = null)
    {
        if (!TryParseCredentialInput(ResolveCredentialInput(credentialInput), out var displayName, out var password))
            throw new InvalidOperationException("닉네임#비밀번호 형식으로 입력해주세요.");

        return LoginAsync(displayName, password);
    }

    public static async UniTask LoginAsync(string rawDisplayName, string rawPassword)
    {
        if (!TryValidateCredentials(rawDisplayName, rawPassword, out var displayName, out var password))
            throw new InvalidOperationException("닉네임과 비밀번호를 모두 입력해주세요.");

        string credentialInput = BuildCredentialInput(displayName, password);
        string customId = BuildCustomId(displayName, password);
        var tcs = new UniTaskCompletionSource();

        var req = new LoginWithCustomIDRequest
        {
            CustomId = customId,
            CreateAccount = true
        };

        PlayFabClientAPI.LoginWithCustomID(req,
            result =>
            {
                CurrentPlayFabId = result.PlayFabId;
                CurrentCredentialInput = credentialInput;
                CurrentDisplayName = displayName;
                tcs.TrySetResult();
            },
            err => tcs.TrySetException(new Exception(err.GenerateErrorReport())));

        await tcs.Task;
    }

    private static string ResolveCredentialInput(string credentialInput)
    {
        if (!string.IsNullOrWhiteSpace(credentialInput))
            return credentialInput;

        if (!string.IsNullOrWhiteSpace(CurrentCredentialInput))
            return CurrentCredentialInput;

        return string.Empty;
    }

    private static string ResolveStorageNamespace()
    {
        string input = ResolveCredentialInput(null);
        if (!TryParseCredentialInput(input, out var displayName, out var password))
            return "anonymous";

        return BuildCustomId(displayName, password);
    }

    private static bool TryParseCredentialInputFallback(string rawInput, out string displayName)
    {
        displayName = string.Empty;
        if (string.IsNullOrWhiteSpace(rawInput))
            return false;

        string input = rawInput.Trim();
        int separatorIndex = input.IndexOf('#');
        if (separatorIndex <= 0)
            return false;

        displayName = input.Substring(0, separatorIndex).Trim();
        if (displayName.Length > 16)
            displayName = displayName.Substring(0, 16);
        return !string.IsNullOrWhiteSpace(displayName);
    }

    private static string BuildCustomId(string displayName, string password)
    {
        string source = $"rebloom:{displayName.ToLowerInvariant()}#{password}";
        using var sha = SHA256.Create();
        byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(source));
        var builder = new StringBuilder("acct_");
        foreach (byte b in bytes)
            builder.Append(b.ToString("x2"));
        return builder.ToString();
    }
}

using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public static class SaveSerializerNewtonsoft
{
    // Newtonsoft 설정: 세이브는 "안전/호환" 쪽으로
    // - TypeNameHandling은 보안/호환 이슈 많아서 None 권장
    // - ReferenceLoop는 에러로 (DTO에 Unity 객체 섞이면 바로 터지게)
    private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        Formatting = Formatting.None,
        TypeNameHandling = TypeNameHandling.None,
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Include,
        ReferenceLoopHandling = ReferenceLoopHandling.Error,
        MissingMemberHandling = MissingMemberHandling.Ignore, // 버전업 시 구버전 로드 유연
        Converters = { new StringEnumConverter() }
    };

    /// DTO -> JSON 문자열
    public static string ToJson<T>(T dto)
    {
        return JsonConvert.SerializeObject(dto, Settings);
    }

    /// JSON 문자열 -> DTO
    public static T FromJson<T>(string json)
    {
        return JsonConvert.DeserializeObject<T>(json, Settings);
    }

    /// DTO -> bytes (옵션: gzip)
    public static byte[] ToBytes<T>(T dto, bool compressGzip = true)
    {
        var json = ToJson(dto);
        var raw = Encoding.UTF8.GetBytes(json);
        return compressGzip ? Gzip(raw) : raw;
    }

    /// bytes -> DTO (옵션: gzip)
    public static T FromBytes<T>(byte[] data, bool compressedGzip = true)
    {
        if (data == null || data.Length == 0) return default;
        var raw = compressedGzip ? Gunzip(data) : data;
        var json = Encoding.UTF8.GetString(raw);
        return FromJson<T>(json);
    }

    /// PlayFab 같은 "문자열 저장"에 편한 Base64 래퍼
    public static string ToBase64<T>(T dto, bool compressGzip = true)
    {
        var bytes = ToBytes(dto, compressGzip);
        return Convert.ToBase64String(bytes);
    }

    public static T FromBase64<T>(string base64, bool compressedGzip = true)
    {
        if (string.IsNullOrEmpty(base64)) return default;
        var bytes = Convert.FromBase64String(base64);
        return FromBytes<T>(bytes, compressedGzip);
    }

    // ---------- Compression helpers ----------
    private static byte[] Gzip(byte[] input)
    {
        using var ms = new MemoryStream();
        using (var gz = new GZipStream(ms, CompressionLevel.Optimal, leaveOpen: true))
            gz.Write(input, 0, input.Length);
        return ms.ToArray();
    }

    private static byte[] Gunzip(byte[] input)
    {
        using var inputMs = new MemoryStream(input);
        using var gz = new GZipStream(inputMs, CompressionMode.Decompress);
        using var outMs = new MemoryStream();
        gz.CopyTo(outMs);
        return outMs.ToArray();
    }
}

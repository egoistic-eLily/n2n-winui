using System.Text.Json.Serialization;
using N2N_Saenai.Serialization;

namespace N2N_Saenai.Initialization;

/// <summary>Contract returned by the launcher service after a successful sign-in.</summary>
public sealed class LoginRequest
{
    [JsonPropertyName("userid")] public string UserId { get; set; } = string.Empty;
    [JsonPropertyName("password")] public string Password { get; set; } = string.Empty;
}

public sealed class N2NUserData
{
    [JsonPropertyName("user_id")]
    [JsonConverter(typeof(StringOrNumberConverter))]
    public string? UserId { get; set; }
    [JsonPropertyName("supernode_ip")] public string SupernodeIp { get; set; } = string.Empty;
    [JsonPropertyName("supernode_port")] public int SupernodePort { get; set; }
    [JsonPropertyName("community_name")] public string CommunityName { get; set; } = string.Empty;
    [JsonPropertyName("device_name")] public string DeviceName { get; set; } = string.Empty;
    [JsonPropertyName("password")] public string Password { get; set; } = string.Empty;
    [JsonPropertyName("community_key")] public string CommunityKey { get; set; } = string.Empty;
    [JsonPropertyName("encrypt_algorithm")] public int EncryptAlgorithm { get; set; }
}

public sealed class LoginResponse
{
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("data")] public N2NUserData? Data { get; set; }
}

/// <summary>
/// Global defaults. No default login server is shipped: users enter their own
/// server URL on the login screen (persisted locally), or set it here before building.
/// </summary>
public sealed class LauncherConfig { public string ApiUrl { get; init; } = string.Empty; }

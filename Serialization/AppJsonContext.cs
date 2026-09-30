using N2N_Saenai.Core;
using N2N_Saenai.Initialization;
using System.Text.Json.Serialization;

namespace N2N_Saenai.Serialization;

// Required because Release publishing enables trimming, which disables reflection-based JSON serialization.
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(LoginResponse))]
[JsonSerializable(typeof(PipeMessage))]
public partial class AppJsonContext : JsonSerializerContext;

using System.Text.Json.Serialization;

namespace ATT.Monitor.Api.Models.Crypto;

/// <summary>Cuerpo con payload cifrado en Base64 (paridad <c>EEncryp</c> / propiedad <c>En</c>).</summary>
public sealed class EncryptEnvelope
{
    [JsonPropertyName("En")]
    public string En { get; set; } = string.Empty;
}

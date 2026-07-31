namespace ATT.Monitor.Api.Models.Public;

/// <summary>Valores efectivos de <see cref="Configuration.RequestBodyLimitsOptions"/> (paridad Monitor).</summary>
public sealed record UploadLimitsResponseDto(int MaxMegabytes, long MaxBytes);

using System.Text.Json.Serialization;

namespace ATT.Monitor.Api.Models.Conciliacion;

public class AutopagoDetalleRow
{
    public string?  Region                { get; set; }
    public string?  PosId                 { get; set; }
    public string?  Tienda                { get; set; }
    public string?  Tipo                  { get; set; }
    public string?  OrdenCrmOms           { get; set; }
    public string?  TipoDocumento         { get; set; }
    public string?  NumeroDocumento       { get; set; }
    public string?  ConceptoPago          { get; set; }
    public string?  FormaPago             { get; set; }
    public string?  NombreCliente         { get; set; }
    public string?  TelefonoCuenta        { get; set; }
    public string?  CuentaCliente         { get; set; }
    public string?  Cajero                { get; set; }
    public string?  IdentificadorCorte    { get; set; }
    public string?  PolizaGl              { get; set; }
    public string?  EstatusCorte          { get; set; }
    public string?  FechaEnvioPoliza      { get; set; }
    public string?  Ticket                { get; set; }
    public string?  Cancelado             { get; set; }
    public string?  FechaTransaccion      { get; set; }
    public string?  HoraTransaccion       { get; set; }
    public decimal? Importe               { get; set; }
}

public class AutopagoCargaResponse
{
    public int IdCarga           { get; set; }
    public bool EsReproceso      { get; set; }
    public int? IdCargaOrigen    { get; set; }
    public int FilasLeidas       { get; set; }
    public int FilasConError     { get; set; }
    public List<string> Errores  { get; set; } = [];
}

/// <summary>Fila de <c>SP_GET_TRANSACCIONES</c> (mismo JSON que consume el Monitor Blazor).</summary>
public sealed class TransaccionOnlineRowApiDto
{
    [JsonPropertyName("ID_TRANSACCION")]
    public int IdTransaccion { get; set; }

    [JsonPropertyName("EP")]
    public string? Ep { get; set; }

    [JsonPropertyName("FECHA")]
    public string? Fecha { get; set; }

    [JsonPropertyName("TIPO")]
    public string? Tipo { get; set; }

    [JsonPropertyName("ESTATUS")]
    public string? Estatus { get; set; }

    [JsonPropertyName("TIPO_PAGO")]
    public string? TipoPago { get; set; }

    [JsonPropertyName("CLIENTE")]
    public string? Cliente { get; set; }

    [JsonPropertyName("NOMBRE_CLIENTE")]
    public string? NombreCliente { get; set; }

    [JsonPropertyName("MONTO")]
    public decimal? Monto { get; set; }

    [JsonPropertyName("CAMBIO")]
    public decimal? Cambio { get; set; }

    [JsonPropertyName("FOLIO")]
    public string? Folio { get; set; }

    [JsonPropertyName("DN")]
    public string? Dn { get; set; }

    [JsonPropertyName("REFERENCIA")]
    public string? Referencia { get; set; }

    [JsonPropertyName("REFERENCIA_EP")]
    public string? ReferenciaEp { get; set; }

    [JsonPropertyName("COLOR")]
    public string? Color { get; set; }

    [JsonPropertyName("FECHA_REGISTRO")]
    public string? FechaRegistro { get; set; }

    [JsonPropertyName("CAMBIO_INCOMPLETO")]
    public string? CambioIncompleto { get; set; }

    [JsonPropertyName("CODIGO_ERROR")]
    public string? CodigoError { get; set; }

    [JsonPropertyName("CODIGO_AUTORIZACION")]
    public string? CodigoAutorizacion { get; set; }

    [JsonPropertyName("NO_TARJETA")]
    public string? NoTarjeta { get; set; }

    [JsonPropertyName("MOTIVO_RECHAZO")]
    public string? MotivoRechazo { get; set; }

    [JsonPropertyName("JOURNAL")]
    public string? Journal { get; set; }

    [JsonPropertyName("Total")]
    public int? Total { get; set; }
}

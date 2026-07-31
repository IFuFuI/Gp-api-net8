using Microsoft.AspNetCore.Http;

namespace ATT.Monitor.Api.Models.Campana;

public enum TipoCampana
{
    PF = 1,
    TA = 2
}

public sealed class CampanaRequest
{
    public int Ignorar { get; set; }
    public int CantidadFila { get; set; } = 10;
    public string Filtro { get; set; } = string.Empty;
    public string Orden { get; set; } = "Id";
    public string Dir { get; set; } = "ASC";
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
}

public sealed class CampanaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string FechaInicio { get; set; } = string.Empty;
    public string FechaTermino { get; set; } = string.Empty;
    public string FechaCreacion { get; set; } = string.Empty;
    public string Estatus { get; set; } = string.Empty;
    public string Resultado { get; set; } = string.Empty;
    public int TotalRegistros { get; set; }
}

public sealed class ConsultaEpCampanaResponse
{
    public string EP { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string FechaFin { get; set; } = string.Empty;
}

public sealed class CampanaEPRequest
{
    public int ID_CAMPANA { get; set; }
}

public sealed class BajaCampanaRequest
{
    public int ID_CAMPANA { get; set; }
}

public sealed class ActualizarCampanaRequest
{
    public int ID_CAMPANA { get; set; }
    public string FechaInicio { get; set; } = string.Empty;
    public string FechaFin { get; set; } = string.Empty;
}

/// <summary>Resultado típico de SP de campaña (<c>ResultInt</c> / <c>ResultString</c>).</summary>
public sealed class CampanaSpResultDto
{
    public int ResultInt { get; set; }
    public string ResultString { get; set; } = string.Empty;
}

public sealed class UpdateCampanaZipRequest
{
    public int IdCampana { get; set; }
    public IFormFile? archivo { get; set; }
}

public sealed class AgregarEpsCampanaRequest
{
    public int ID_CAMPANA { get; set; }
    public List<string> EP { get; set; } = [];
}

public sealed class EliminarEpCampanaRequest
{
    public int ID_CAMPANA { get; set; }
    public string EP { get; set; } = string.Empty;
    public string? Usuario { get; set; }
}

public sealed class AgregarEpsCampanaResponse
{
    public List<string> Agregados { get; set; } = [];
    public List<string> OmitidosDuplicado { get; set; } = [];
    public List<string> Fallidos { get; set; } = [];
    public string Mensaje { get; set; } = string.Empty;
}

public sealed class DocumentoResponse
{
    public bool Exito { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public string RutaArchivo { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
}

public sealed class NewCampanaRequest
{
    public IFormFile? archivo { get; set; }
    public TipoCampana tipoCampana { get; set; }
    public string nombreCampana { get; set; } = string.Empty;
    public DateTime fechainicio { get; set; }
    public DateTime fechafin { get; set; }
    public List<string> EP { get; set; } = [];
}

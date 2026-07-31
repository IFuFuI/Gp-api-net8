using System.ComponentModel.DataAnnotations;

namespace ATT.Monitor.Api.Models.TransArchivo;

public sealed class BitacoraComandoDto
{
    public int ID_SOLICITUD { get; set; }
    public string? ID_CAJERO { get; set; }
    public string? TIPO { get; set; }
    public string? STATUS { get; set; }
    public DateTime FECHA_ALTA { get; set; }
}

public sealed class MArchivo
{
    public string NOMBRE { get; set; } = string.Empty;
    public string RUTA { get; set; } = string.Empty;
}

public sealed class InfoZipDto
{
    public string NOMBRE_ZIP { get; set; } = string.Empty;
    public DateTime? FECHA_ALTA { get; set; }
    public string NOMBRE { get; set; } = string.Empty;
}

public sealed class EidAtm
{
    public string IDATM { get; set; } = string.Empty;
    public int? ID_TIPO { get; set; } = 1;

    [Required]
    public int? APLICACION { get; set; }
}

public sealed class ArchivoZipDto
{
    public string NombreArchivo { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int Tipo { get; set; }
    public string? IdAtm { get; set; }
    public string? NombreZip { get; set; }
    public int Aplicacion { get; set; }
}

public sealed class ArchivoRequest
{
    public IFormFile? archivo { get; set; }
    public string idatm { get; set; } = string.Empty;
    public string tipoarchivo { get; set; } = string.Empty;
    public int? APLICACION { get; set; }
}

namespace ATT.Monitor.Api.Models.Administrador;

/// <summary>Paridad con <c>AceptadorRequest</c> API v1.</summary>
public sealed class AceptadorRequest
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public string? Buscar { get; set; }
    public string OrderBy { get; set; } = "ID";
    public string OrderDir { get; set; } = "ASC";
}

public sealed class ActualizarAceptadorRequest
{
    public int ID { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int Severidad { get; set; }
    public int ActivarAlerta { get; set; }
}

public sealed class CrearAceptadorRequest
{
    public string Descripcion { get; set; } = string.Empty;
    public int Severidad { get; set; }
    public int ActivarAlerta { get; set; }
}

public sealed class ActualizarDispensadorMonedasRequest
{
    public int ID { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Severidad { get; set; }
}

public sealed class CrearDispensadorMonedasRequest
{
    public string Nombre { get; set; } = string.Empty;
    public int Severidad { get; set; }
}

public sealed class EstatusAceptadorBilletes
{
    public int ID { get; set; }
    public string DESCRIPCION { get; set; } = string.Empty;
    public int SEVERIDAD { get; set; }
    public int ACTIVAR_ALERTA { get; set; }
    public string FECHA_CREACION { get; set; } = string.Empty;
    public string FECHA_ULTIMA_MODIFICACION { get; set; } = string.Empty;
    public int TOTAL_FILTRADOS { get; set; }
}

public sealed class EstatusAceptadorMonedas
{
    public int ID { get; set; }
    public string DESCRIPCION { get; set; } = string.Empty;
    public int SEVERIDAD { get; set; }
    public int ACTIVAR_ALERTA { get; set; }
    public string FECHA_CREACION { get; set; } = string.Empty;
    public string FECHA_ULTIMA_MODIFICACION { get; set; } = string.Empty;
    public int TOTAL_FILTRADOS { get; set; }
}

public sealed class EstatusDispensadorBilletes
{
    public int ID { get; set; }
    public string DESCRIPCION { get; set; } = string.Empty;
    public int SEVERIDAD { get; set; }
    public int ACTIVAR_ALERTA { get; set; }
    public string FECHA_CREACION { get; set; } = string.Empty;
    public string FECHA_ULTIMA_MODIFICACION { get; set; } = string.Empty;
    public int TOTAL_FILTRADOS { get; set; }
}

public sealed class EstatusDispensadorMonedas
{
    public int ID { get; set; }
    public string NOMBRE { get; set; } = string.Empty;
    public int SEVERIDAD { get; set; }
    public string FECHA_CREACION { get; set; } = string.Empty;
    public string FECHA_ULTIMA_MODIFICACION { get; set; } = string.Empty;
    public int TOTAL_FILTRADOS { get; set; }
}

public sealed class EstatusImpresora
{
    public int ID { get; set; }
    public string DESCRIPCION { get; set; } = string.Empty;
    public int SEVERIDAD { get; set; }
    public int ACTIVAR_ALERTA { get; set; }
    public string FECHA_CREACION { get; set; } = string.Empty;
    public string FECHA_ULTIMA_MODIFICACION { get; set; } = string.Empty;
    public int TOTAL_FILTRADOS { get; set; }
}

public sealed class EstatusLectorCodigoBarras
{
    public int ID { get; set; }
    public string DESCRIPCION { get; set; } = string.Empty;
    public int SEVERIDAD { get; set; }
    public int ACTIVAR_ALERTA { get; set; }
    public string FECHA_CREACION { get; set; } = string.Empty;
    public string FECHA_ULTIMA_MODIFICACION { get; set; } = string.Empty;
    public int TOTAL_FILTRADOS { get; set; }
}

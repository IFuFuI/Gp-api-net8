namespace ATT.Monitor.Api.Models.Login;

public sealed class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class ELoginAtmCajero
{
    public string ID_ATM { get; set; } = string.Empty;
    public string MODELO { get; set; } = string.Empty;
    public string IP { get; set; } = string.Empty;
    public string SOCKET { get; set; } = string.Empty;
}

/// <summary>JSON de cliente histórico usa <c>idgrupo</c> en minúsculas.</summary>
public sealed class GrupoPermisosRequest
{
    public string idgrupo { get; set; } = string.Empty;
}

public sealed class BitacoraUsuariosRequest
{
    public string ATTUID { get; set; } = string.Empty;
    public string EMAIL { get; set; } = string.Empty;
    public string GIVEN_NAME { get; set; } = string.Empty;
    public string SURNAME { get; set; } = string.Empty;
    public string NAME_IDENTIFIER { get; set; } = string.Empty;
    public string GROUPS { get; set; } = string.Empty;
    public string TENANTID { get; set; } = string.Empty;
    public DateTime? AUTHENTICATION_INSTANT { get; set; }
}

public sealed class PermisosResponseDto
{
    public List<MenuPermisoDto> Menu { get; set; } = [];
    public List<SubmenuPermisoDto> Submenu { get; set; } = [];
}

public sealed class MenuPermisoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public sealed class SubmenuPermisoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int IdModulo { get; set; }
}

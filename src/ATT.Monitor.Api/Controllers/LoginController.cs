using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Models.Login;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ATT.Monitor.Api.Controllers;

/// <summary>
/// Login administrador web y ATM (paridad funcional API v1; rutas bajo <c>api/login</c>).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
[EnableRateLimiting("login")]
public sealed class LoginController(
    ITokenIssuer tokenIssuer,
    IAtmAccountData atmAccountData,
    IConfiguration configuration) : ControllerBase
{
    private const int AuthSimulationDelayMs = 100;
    private const string EnvAdminUsername = "ADMIN_USERNAME";
    private const string EnvAdminPassword = "ADMIN_PASSWORD";
    private const string MsgCredencialesInvalidas = "Credenciales inválidas";
    private const string MsgSolicitudInvalida = "Solicitud inválida";
    private const string MsgAtmInvalido = "EP inválido";

    /// <summary>Login administrador (credenciales en configuración o variables de entorno).</summary>
    [HttpPost("AccesoWebAsync")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AccesoWebAsync([FromBody] LoginRequest request)
    {
        await Task.Delay(AuthSimulationDelayMs, HttpContext.RequestAborted).ConfigureAwait(false);

        var adminUsername = configuration["Authentication:AdminUsername"]
            ?? Environment.GetEnvironmentVariable(EnvAdminUsername);
        var adminPassword = configuration["Authentication:AdminPassword"]
            ?? Environment.GetEnvironmentVariable(EnvAdminPassword);

        if (string.IsNullOrEmpty(adminUsername) || string.IsNullOrEmpty(adminPassword))
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                "Credenciales de administrador no configuradas");
        }

        if (request.Username == adminUsername && request.Password == adminPassword)
        {
            var token = tokenIssuer.GenerateToken(request.Username);
            return Ok(token);
        }

        return Unauthorized(MsgCredencialesInvalidas);
    }

    /// <summary>Login cajero ATM vía <c>FUNC_VALIDA_ATM</c>; devuelve JWT con subject = ID_ATM.</summary>
    [HttpPost("LoginAsync")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginAsync([FromBody] ELoginAtmCajero? request)
    {
        if (request is null)
            return Unauthorized(MsgSolicitudInvalida);

        await Task.Delay(AuthSimulationDelayMs, HttpContext.RequestAborted).ConfigureAwait(false);

        var atmValidation = await atmAccountData.ValidatAtmAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        if (atmValidation.ResultInt != 1)
            return Unauthorized(string.IsNullOrEmpty(atmValidation.ResultString) ? MsgAtmInvalido : atmValidation.ResultString);

        var token = tokenIssuer.GenerateToken(request.ID_ATM);
        return Ok(new { token });
    }

    /// <summary>Menú y submenú de permisos por grupo.</summary>
    [HttpPost("permisos")]
    [ProducesResponseType(typeof(PermisosResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermisos([FromBody] GrupoPermisosRequest idGrupo)
    {
        var result = await atmAccountData.GetPermisosAsync(idGrupo, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>Bitácora de usuarios (Azure AD / similar).</summary>
    [HttpPost("CrearBitacoraUsuarioAsync")]
    [ProducesResponseType(typeof(ProcedureResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CrearBitacoraUsuarioAsync([FromBody] BitacoraUsuariosRequest request)
    {
        var resp = await atmAccountData.InsertBitacoraUsuariosAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(resp);
    }

    /// <summary>Prueba de rate limiting (sin lógica de negocio).</summary>
    [HttpPost("probe")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Probe()
    {
        return Ok(new { ok = true, message = "Política de rate limiting 'login' activa en esta ruta." });
    }
}

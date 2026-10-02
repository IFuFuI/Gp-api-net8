# Gp-api-net8 (ATT.Monitor.Api)

API REST v2 del monitoreo de estaciones de pago (EP / cajeros ATM) de AT&T, de GPSolutions. ASP.NET Core Web API en .NET 8, Dapper sobre `Microsoft.Data.SqlClient` contra SQL Server (`QA_MONITOR_ATT`), JWT HS256 con validación del token en base de datos. Reemplaza a la API v1 (`BC_MonitorAPIs`); muchos archivos dicen "Paridad v1" y deben conservar ese contrato.

Tiene dos tipos de cliente:
- **Portal** `Gp-monitor-net8` (MonitoATT.Web, Blazor Server): entra con una cuenta de servicio por `api/Login/AccesoWebAsync`.
- **Agente de la EP** (software en cada cajero): entra con `api/Login/LoginAsync` (`FUNC_VALIDA_ATM`, JWT con subject = `ID_ATM`) y reporta keep-alive, estado, contadores, transacciones, versión y comandos.

## Comandos

```bash
dotnet build ATT.Monitor.V2.slnx
dotnet test tests/ATT.Monitor.Api.Tests/ATT.Monitor.Api.Tests.csproj
dotnet test tests/ATT.Monitor.Api.Tests/ATT.Monitor.Api.Tests.csproj --filter ReporteExportFormattingTests   # una clase

# Paquete para el servidor Windows (IIS)
dotnet publish src/ATT.Monitor.Api/ATT.Monitor.Api.csproj -c Release -r win-x64 --self-contained false -o ~/Publicaciones/GP_API_NET8-$(date +%Y%m%d-%H%M)
```

Antepón `DOTNET_CLI_UI_LANGUAGE=en` a los comandos de dotnet: si no, la salida viene en español y los resúmenes en inglés no aparecen. La solución es `.slnx` (formato nuevo): requiere un SDK reciente; si el SDK no la abre, compila el `.csproj` directo.

- La base de datos es remota; las pruebas no la necesitan (25 pruebas: cálculos, parsers, exportación y smoke tests con `WebApplicationFactory`).
- Los SPs y funciones NO están completos en el repo. `db/` solo tiene scripts `.reference.sql` puntuales (con su `rollback`) que se aplican a mano; el script integral `QA_MONITOR_ATT_remediado.sql` vive fuera del repo (ver `db/REFERENCES.md`). Si un cambio depende de un SP, pide su código antes de suponer qué hace.

## Estructura

```
src/ATT.Monitor.Api/
  Controllers/          23 controladores, ruta api/[controller], [Authorize] a nivel de clase.
  Abstractions/         Interfaces I<Modulo>Data e I<Servicio>.
  Infrastructure/
    Persistence/        <Modulo>DataService: Dapper + SPs. DashboardDataService (1,200 líneas) y CatalogosMonitorDataService (1,050) son los grandes.
    Composition/        SecurityServiceExtensions: JWT + validación en BD + rate limiting + Swagger.
    Jobs/               Exportación del journal histórico en segundo plano (Channel + HostedService, en memoria).
    Files/              Archivos en disco (journal, campañas, rutas).
    DetalleEquipo/      DetalleEquipoSoSyncCoordinator (singleton en memoria).
    Resilience/         SqlTransientRetry (Polly, 2 reintentos para errores transitorios de SQL).
    ExceptionHandling/  GlobalExceptionHandler → ProblemDetails (detalle solo en Development).
    HealthChecks/       /health, /health/live, /health/ready (SQL).
  Services/             Parsers (autopago, transacciones), conciliación y Reportes/ (exportación a XLSX con plantillas).
  Models/<Modulo>/      DTOs. Nombres con guion bajo a propósito: son el contrato con SQL y con el portal (CA1707 suprimida en .editorconfig).
  Configuration/        Options por sección; varias leen variables de entorno en PostConfigure.
  Templates/Reportes/   7 plantillas .xlsx copiadas a la salida.
tests/ATT.Monitor.Api.Tests/   xUnit 2.9 + Mvc.Testing.
db/                     Scripts SQL de referencia (no se ejecutan desde la API).
scripts/                sync_reporte_plantillas_v2.py (normaliza encabezados de plantillas; rutas de Windows fijas).
tools/analizar.ps1      Análisis SonarCloud.
```

## Módulos (controladores)

| Ruta | Para qué |
|---|---|
| `api/Login` | `AccesoWebAsync` (portal), `LoginAsync` (agente EP), `permisos` (menú por grupo), `CrearBitacoraUsuarioAsync` (claims SAML) |
| `api/Dashboard`, `api/DashboardMetrics` | Tarjetas, fallas, EP totales, mapa, catálogo de equipos, detalle de equipo (start/ping/stop, rendimiento, sync de SO), rollout, reportes |
| `api/AtmManagement`, `api/AtmDevice`, `api/AtmVersion`, `api/Transaction`, `api/DetalleAtm`, `api/AtmCommand` | Lo que reporta el agente: registro, contadores, keep-alive, estado, hardware, versión, transacciones y comandos (algunos cifrados con AES) |
| `api/TransArchivos`, `api/FilePackage`, `api/JournalHistorico` | Transferencia de archivos a/desde EP (zip), journal histórico con exportación asíncrona |
| `api/Conciliacion` | Resumen transaccional y conciliación PBMX/autopago con carga de archivo (`AutopagoArchivoParser`, `SP_CONCILIAR_AUTOPAGO`) |
| `api/Campana` | Campañas de publicidad por EP (PF/TA) con archivo |
| `api/CatalogosMonitor`, `api/Administrador` | Catálogos (región, ruta, alerta, status/detalle/atributo de transacción, dispositivos eliminados) y estatus de dispositivos (aceptador/dispensador de billetes y monedas, impresora, lector) |
| `api/Historial`, `api/Encriptar`, `api/Other`, `api/System`, `api/public`, `api/Auth`, `api/Utility` | Utilidades, metadatos y pruebas |

## Patrón de un endpoint

1. Controlador `sealed` con constructor primario: `public sealed class XController(IXData data) : ControllerBase`.
2. `[HttpPost("NOMBRE_ACCION")]` + `[ProducesResponseType(...)]`. Casi todo es POST con `[FromBody]` aunque sea consulta: es la paridad con v1, no lo cambies.
3. Validación simple al inicio → `BadRequest(new { mensaje = "..." })`.
4. Llamada a la capa de datos con `HttpContext.RequestAborted` y `.ConfigureAwait(false)`.
5. Sin datos → `NotFound(new { mensaje })` u `Ok(Array.Empty<T>())` según lo que ya haga el módulo. SP con `ResultInt != 1` → `BadRequest(result)`.
6. Sin `try/catch` en el controlador: las excepciones las convierte `GlobalExceptionHandler` en ProblemDetails 500.

Referencia limpia: `Controllers/CampanaController.cs` + `Infrastructure/Persistence/CampanaDataService.cs`.

## Patrón de acceso a datos

- Servicio `sealed` con `IConfiguration`; conexión `new SqlConnection(configuration.GetConnectionString("SqlServer"))` con `await using`.
- Siempre `DynamicParameters` y `CommandDefinition(..., cancellationToken, commandType)`. Nunca concatenar ni interpolar SQL.
- SP: `"dbo.SP_..."` con `CommandType.StoredProcedure`. Resultados de escritura en DTOs tipo `ProcedureResultDto` / `CampanaSpResultDto` (`ResultInt`, `ResultString`).
- Para errores transitorios usa `SqlTransientRetry.ExecuteAsync`, como `DashboardDataService` y `TokenDbValidator`.
- Interfaz nueva en `Abstractions/` + `builder.Services.AddScoped<IX, X>()` en `Program.cs`.
- Opciones nuevas: clase en `Configuration/` con `SectionName` y, si aplica, `ApplyEnvironmentDefaults` para la variable de entorno.

## Configuración y secretos

- Cadena de conexión: `ConnectionStrings:SqlServer` (vacía en el repo; viene del servidor o de user-secrets `att-monitor-v2-api-dev`).
- Variables de entorno: `JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_EXPIRATION_MINUTES`, `TimeExpToken`, `CrypAES`, `Cryp2AES`, `ADMIN_USERNAME`, `ADMIN_PASSWORD`, `ARCHIVOS_ATM_PATH`, `JOURNALDIA`, `MAX_REQUEST_BODY_MB`.
- Rutas en disco del servidor: `C:\ArchivosATM` (archivos de EP), `C:\ArchivosATM\Journaldiario`, `C:\ArchivosATM\HistoricoReportes`.
- `Authentication:EnableJwtBearer = false` solo arranca en Development (bypass); en otro entorno la API se niega a iniciar.
- No leer, mostrar ni copiar valores de `appsettings*.json`, user-secrets ni variables de entorno.

## Seguridad (lo que ya está bien, no romper)

- JWT con issuer, audience, lifetime y firma validados; además `SP_ValidarToken` en cada petición (caché de 45 s): un token bloqueado en BD se rechaza.
- Rate limiting `login`: 40 peticiones/min por IP en `api/Login`.
- ProblemDetails sin detalle fuera de Development; HSTS; compresión; límite de cuerpo configurable (200 MB por defecto) aplicado a IIS, Kestrel y multipart.

## Pruebas

- Pruebas unitarias de lógica pura (calculadoras, parsers, formateo de exportación) y `Integration/SmokeTests` con `MonitorApiFixture` (`WebApplicationFactory<Program>`, `TestHost:DisableHttpsRedirection`).
- Si agregas lógica en `Services/`, agrega su prueba unitaria. Los controladores con BD no se prueban contra SQL.

## Análisis estático

SonarCloud: organización `ifufui`, projectKey `IFuFuI_Gp-api-net8`, rama `Master`. Script `tools/analizar.ps1` (requiere `SONAR_TOKEN`). Último export: 303 hallazgos (8 CRITICAL), 0 de seguridad y 0 hotspots. Pendientes de decidir: S6964 (86 campos sin `[Required]`) y documentar S3459/S1144 (propiedades que Dapper asigna por reflexión). Para revisarlos usa el skill `revision-sonarqube`.

## Forma de trabajo

- Un endpoint o un cambio a la vez. Compila y corre las pruebas antes de seguir.
- Código mínimo. Sin logging adicional, sin abstracciones nuevas, sin reformatear código que no tocas.
- El portal depende de las rutas y de los nombres de campos: antes de renombrar o quitar algo, búscalo en `../Gp-monitor-net8/MonitoATT.Web` (`appsettings*.json` → `ApiSettings:Endpoints` y `Models/`). El agente de la EP no está en este repo: no cambies contratos de `AtmManagement`, `AtmDevice`, `Transaction`, `DetalleAtm` ni `AtmCommand`.
- Revisión de SPs o funciones: skill `revision-objetos-sqlserver`. Revisión de seguridad: agente `revisor-seguridad-dotnet`.

## Deuda conocida (no corregir salvo que se pida)

- `Jwt:Secret` está versionado con valor real en `appsettings.json` y `appsettings.Development.json`: hay que rotarlo y moverlo a variable de entorno.
- Expiración del JWT: sin `JWT_EXPIRATION_MINUTES` el token dura **años** (`TimeExpToken`, 1 por defecto). La cuenta de servicio del portal y los agentes viven con tokens de larga duración.
- `api/Login` es `[AllowAnonymous]` completo: `permisos` y `CrearBitacoraUsuarioAsync` no piden token. También anónimos: `Other/GenerarClave`, `GenerarIV`, `GenerateKeyJWT` y `Dashboard/GET_CARDS_DASH`.
- `AccesoWebAsync` compara usuario y contraseña de configuración con `==` (una sola cuenta de servicio para todo el portal).
- AES con llave e IV fijos desde configuración (`CrypAES`/`Cryp2AES`), compartido con el agente.
- Swagger habilitado por defecto (`Swagger:Enabled = true`).
- `CampanaDataService` (3 métodos) y `DashboardDataService` devuelven `0`/`false` en un `catch` vacío: el error se pierde.
- Exportación de journal y sync de SO guardan estado en memoria: no soporta más de una instancia ni sobrevive a un reciclaje del app pool.
- `ReportePlantillas:TemplatesDirectory` y `scripts/*.py` apuntan a rutas `E:\` de otra máquina; si no existe, cae a `Templates/Reportes` del paquete.

using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Models.CatalogosMonitor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATT.Monitor.Api.Controllers;

/// <summary>CRUD catálogos <c>dbo.C_REGION</c>, <c>C_RUTA</c>, <c>C_ALERTA</c>, etc.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class CatalogosMonitorController(ICatalogosMonitorData data) : ControllerBase
{
    #region C_REGION

    [HttpPost("CRegion/List")]
    [ProducesResponseType(typeof(MonitorCatalogPageResult<CRegionRow>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCRegion([FromBody] MonitorCatalogListRequest request)
    {
        var r = await data.ListCRegionAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(r);
    }

    [HttpPost("CRegion/Create")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateCRegion([FromBody] CRegionCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Region))
            return BadRequest("REGION es obligatoria.");
        var id = await data.CreateCRegionAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(id);
    }

    [HttpPost("CRegion/Update")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCRegion([FromBody] CRegionUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Region))
            return BadRequest("REGION es obligatoria.");
        var ok = await data.UpdateCRegionAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(ok);
    }

    [HttpPost("CRegion/Delete")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteCRegion([FromBody] CRegionDeleteRequest request)
    {
        var ok = await data.DeleteCRegionAsync(request.Id, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(ok);
    }

    #endregion

    #region C_RUTA

    [HttpPost("CRuta/List")]
    [ProducesResponseType(typeof(MonitorCatalogPageResult<CRutaRow>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCRuta([FromBody] MonitorCatalogListRequest request)
    {
        var r = await data.ListCRutaAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(r);
    }

    [HttpPost("CRuta/Create")]
    public async Task<IActionResult> CreateCRuta([FromBody] CRutaCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Descripcion) || string.IsNullOrWhiteSpace(request.Ruta) || string.IsNullOrWhiteSpace(request.NombreArchivo))
            return BadRequest("DESCRIPCION, RUTA y NOMBRE_ARCHIVO son obligatorios.");
        var id = await data.CreateCRutaAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(id);
    }

    [HttpPost("CRuta/Update")]
    public async Task<IActionResult> UpdateCRuta([FromBody] CRutaUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Descripcion) || string.IsNullOrWhiteSpace(request.Ruta) || string.IsNullOrWhiteSpace(request.NombreArchivo))
            return BadRequest("DESCRIPCION, RUTA y NOMBRE_ARCHIVO son obligatorios.");
        var ok = await data.UpdateCRutaAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(ok);
    }

    [HttpPost("CRuta/Delete")]
    public async Task<IActionResult> DeleteCRuta([FromBody] CRutaDeleteRequest request)
    {
        var ok = await data.DeleteCRutaAsync(request.Id, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(ok);
    }

    #endregion

    #region C_ALERTA

    [HttpPost("CAlerta/List")]
    [ProducesResponseType(typeof(MonitorCatalogPageResult<CAlertaRow>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCAlerta([FromBody] MonitorCatalogListRequest request)
    {
        var r = await data.ListCAlertaAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(r);
    }

    [HttpPost("CAlerta/Create")]
    public async Task<IActionResult> CreateCAlerta([FromBody] CAlertaCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Descripcion))
            return BadRequest("DESCRIPCION es obligatoria.");
        var id = await data.CreateCAlertaAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(id);
    }

    [HttpPost("CAlerta/Update")]
    public async Task<IActionResult> UpdateCAlerta([FromBody] CAlertaUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Descripcion))
            return BadRequest("DESCRIPCION es obligatoria.");
        var ok = await data.UpdateCAlertaAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(ok);
    }

    [HttpPost("CAlerta/Delete")]
    public async Task<IActionResult> DeleteCAlerta([FromBody] CAlertaDeleteRequest request)
    {
        var ok = await data.DeleteCAlertaAsync(request.Id, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(ok);
    }

    #endregion

    #region C_STATUS_TRANSACCION

    [HttpPost("CStatusTransaccion/List")]
    [ProducesResponseType(typeof(MonitorCatalogPageResult<CStatusTransaccionRow>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCStatusTransaccion([FromBody] MonitorCatalogListRequest request)
    {
        var r = await data.ListCStatusTransaccionAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(r);
    }

    [HttpPost("CStatusTransaccion/Create")]
    public async Task<IActionResult> CreateCStatusTransaccion([FromBody] CStatusTransaccionCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Descripcion))
            return BadRequest("DESCRIPCION es obligatoria.");
        var id = await data.CreateCStatusTransaccionAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(id);
    }

    [HttpPost("CStatusTransaccion/Update")]
    public async Task<IActionResult> UpdateCStatusTransaccion([FromBody] CStatusTransaccionUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Descripcion))
            return BadRequest("DESCRIPCION es obligatoria.");
        var ok = await data.UpdateCStatusTransaccionAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(ok);
    }

    [HttpPost("CStatusTransaccion/Delete")]
    public async Task<IActionResult> DeleteCStatusTransaccion([FromBody] CStatusTransaccionDeleteRequest request)
    {
        var ok = await data.DeleteCStatusTransaccionAsync(request.Id, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(ok);
    }

    #endregion

    #region C_DETALLE_TRANSACCION

    [HttpPost("CDetalleTransaccion/List")]
    [ProducesResponseType(typeof(MonitorCatalogPageResult<CDetalleTransaccionRow>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCDetalleTransaccion([FromBody] MonitorCatalogListRequest request)
    {
        var r = await data.ListCDetalleTransaccionAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(r);
    }

    [HttpPost("CDetalleTransaccion/Create")]
    public async Task<IActionResult> CreateCDetalleTransaccion([FromBody] CDetalleTransaccionCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NombreDetalle))
            return BadRequest("NOMBRE_DETALLE es obligatorio.");
        var id = await data.CreateCDetalleTransaccionAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(id);
    }

    [HttpPost("CDetalleTransaccion/Update")]
    public async Task<IActionResult> UpdateCDetalleTransaccion([FromBody] CDetalleTransaccionUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NombreDetalle))
            return BadRequest("NOMBRE_DETALLE es obligatorio.");
        var ok = await data.UpdateCDetalleTransaccionAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(ok);
    }

    [HttpPost("CDetalleTransaccion/Delete")]
    public async Task<IActionResult> DeleteCDetalleTransaccion([FromBody] CDetalleTransaccionDeleteRequest request)
    {
        var ok = await data.DeleteCDetalleTransaccionAsync(request.Id, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(ok);
    }

    #endregion

    #region C_ATRIBUTO_TRANSACCION

    [HttpPost("CAtributoTransaccion/List")]
    [ProducesResponseType(typeof(MonitorCatalogPageResult<CAtributoTransaccionRow>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCAtributoTransaccion([FromBody] MonitorCatalogListRequest request)
    {
        var r = await data.ListCAtributoTransaccionAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(r);
    }

    [HttpPost("CAtributoTransaccion/Create")]
    public async Task<IActionResult> CreateCAtributoTransaccion([FromBody] CAtributoTransaccionCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Descripcion))
            return BadRequest("DESCRIPCION es obligatoria.");
        var id = await data.CreateCAtributoTransaccionAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(id);
    }

    [HttpPost("CAtributoTransaccion/Update")]
    public async Task<IActionResult> UpdateCAtributoTransaccion([FromBody] CAtributoTransaccionUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Descripcion))
            return BadRequest("DESCRIPCION es obligatoria.");
        var ok = await data.UpdateCAtributoTransaccionAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(ok);
    }

    [HttpPost("CAtributoTransaccion/Delete")]
    public async Task<IActionResult> DeleteCAtributoTransaccion([FromBody] CAtributoTransaccionDeleteRequest request)
    {
        var ok = await data.DeleteCAtributoTransaccionAsync(request.Id, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(ok);
    }

    #endregion

    #region C_DISPOSITIVO_ELIMINADO

    [HttpPost("CDispositivoEliminado/List")]
    [ProducesResponseType(typeof(MonitorCatalogPageResult<CDispositivoEliminadoRow>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCDispositivoEliminado([FromBody] MonitorCatalogListRequest request)
    {
        var r = await data.ListCDispositivoEliminadoAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(r);
    }

    [HttpPost("CDispositivoEliminado/Create")]
    public async Task<IActionResult> CreateCDispositivoEliminado([FromBody] CDispositivoEliminadoCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NombreDispositivo))
            return BadRequest("NOMBRE_DISPOSITIVO es obligatorio.");
        var id = await data.CreateCDispositivoEliminadoAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(id);
    }

    [HttpPost("CDispositivoEliminado/Update")]
    public async Task<IActionResult> UpdateCDispositivoEliminado([FromBody] CDispositivoEliminadoUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NombreDispositivo))
            return BadRequest("NOMBRE_DISPOSITIVO es obligatorio.");
        var ok = await data.UpdateCDispositivoEliminadoAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(ok);
    }

    [HttpPost("CDispositivoEliminado/Delete")]
    public async Task<IActionResult> DeleteCDispositivoEliminado([FromBody] CDispositivoEliminadoDeleteRequest request)
    {
        var ok = await data.DeleteCDispositivoEliminadoAsync(request.Id, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(ok);
    }

    #endregion

    #region Catalog_Locations

    [HttpGet("CatalogLocation/EstadosCombo")]
    [ProducesResponseType(typeof(IReadOnlyList<CEstadoComboRow>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCatalogLocationEstadosCombo()
    {
        var rows = await data.ListCEstadosComboAsync(HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(rows);
    }

    [HttpPost("CatalogLocation/List")]
    [ProducesResponseType(typeof(MonitorCatalogPageResult<CatalogLocationRow>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCatalogLocation([FromBody] MonitorCatalogListRequest request)
    {
        var r = await data.ListCatalogLocationAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        return Ok(r);
    }

    [HttpPost("CatalogLocation/Create")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateCatalogLocation([FromBody] CatalogLocationCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.LocationName))
            return BadRequest("Location_Name es obligatorio.");
        if (request.IdRegion <= 0)
            return BadRequest("Id_Region es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.Ubicacion))
            return BadRequest("Ubicacion (estado localización) es obligatoria.");
        if (string.IsNullOrWhiteSpace(request.Estado))
            return BadRequest("Estado es obligatorio.");
        if (request.LocationName.Trim().Length > 20)
            return BadRequest("Location_Name admite máximo 20 caracteres.");
        if (request.Ubicacion.Trim().Length > 25)
            return BadRequest("Ubicacion admite máximo 25 caracteres.");
        if (request.Estado.Trim().Length > 250)
            return BadRequest("estado admite máximo 250 caracteres.");

        try
        {
            var id = await data.CreateCatalogLocationAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
            return Ok(id);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("CatalogLocation/Update")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateCatalogLocation([FromBody] CatalogLocationUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.LocationName))
            return BadRequest("Location_Name es obligatorio.");
        if (request.IdRegion <= 0)
            return BadRequest("Id_Region es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.Ubicacion))
            return BadRequest("Ubicacion (estado localización) es obligatoria.");
        if (string.IsNullOrWhiteSpace(request.Estado))
            return BadRequest("Estado es obligatorio.");
        if (request.LocationName.Trim().Length > 20)
            return BadRequest("Location_Name admite máximo 20 caracteres.");
        if (request.Ubicacion.Trim().Length > 25)
            return BadRequest("Ubicacion admite máximo 25 caracteres.");
        if (request.Estado.Trim().Length > 250)
            return BadRequest("estado admite máximo 250 caracteres.");

        var ok = await data.UpdateCatalogLocationAsync(request, HttpContext.RequestAborted).ConfigureAwait(false);
        if (!ok)
            return BadRequest("No se actualizó el registro (id inexistente o región no válida).");
        return Ok(true);
    }

    [HttpPost("CatalogLocation/Delete")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteCatalogLocation([FromBody] CatalogLocationDeleteRequest request)
    {
        var (deleted, error) = await data.DeleteCatalogLocationAsync(request.Id, HttpContext.RequestAborted).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(error))
            return BadRequest(error);
        return Ok(deleted);
    }

    #endregion
}

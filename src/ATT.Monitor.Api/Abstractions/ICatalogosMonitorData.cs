using ATT.Monitor.Api.Models.CatalogosMonitor;

namespace ATT.Monitor.Api.Abstractions;

public interface ICatalogosMonitorData
{
    Task<MonitorCatalogPageResult<CRegionRow>> ListCRegionAsync(MonitorCatalogListRequest request, CancellationToken cancellationToken = default);
    Task<int> CreateCRegionAsync(CRegionCreateRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateCRegionAsync(CRegionUpdateRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteCRegionAsync(int id, CancellationToken cancellationToken = default);

    Task<MonitorCatalogPageResult<CRutaRow>> ListCRutaAsync(MonitorCatalogListRequest request, CancellationToken cancellationToken = default);
    Task<int> CreateCRutaAsync(CRutaCreateRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateCRutaAsync(CRutaUpdateRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteCRutaAsync(int id, CancellationToken cancellationToken = default);

    Task<MonitorCatalogPageResult<CAlertaRow>> ListCAlertaAsync(MonitorCatalogListRequest request, CancellationToken cancellationToken = default);
    Task<int> CreateCAlertaAsync(CAlertaCreateRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateCAlertaAsync(CAlertaUpdateRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteCAlertaAsync(int id, CancellationToken cancellationToken = default);

    Task<MonitorCatalogPageResult<CStatusTransaccionRow>> ListCStatusTransaccionAsync(MonitorCatalogListRequest request, CancellationToken cancellationToken = default);
    Task<int> CreateCStatusTransaccionAsync(CStatusTransaccionCreateRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateCStatusTransaccionAsync(CStatusTransaccionUpdateRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteCStatusTransaccionAsync(int id, CancellationToken cancellationToken = default);

    Task<MonitorCatalogPageResult<CDetalleTransaccionRow>> ListCDetalleTransaccionAsync(MonitorCatalogListRequest request, CancellationToken cancellationToken = default);
    Task<int> CreateCDetalleTransaccionAsync(CDetalleTransaccionCreateRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateCDetalleTransaccionAsync(CDetalleTransaccionUpdateRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteCDetalleTransaccionAsync(int id, CancellationToken cancellationToken = default);

    Task<MonitorCatalogPageResult<CAtributoTransaccionRow>> ListCAtributoTransaccionAsync(MonitorCatalogListRequest request, CancellationToken cancellationToken = default);
    Task<int> CreateCAtributoTransaccionAsync(CAtributoTransaccionCreateRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateCAtributoTransaccionAsync(CAtributoTransaccionUpdateRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteCAtributoTransaccionAsync(int id, CancellationToken cancellationToken = default);

    Task<MonitorCatalogPageResult<CDispositivoEliminadoRow>> ListCDispositivoEliminadoAsync(MonitorCatalogListRequest request, CancellationToken cancellationToken = default);
    Task<int> CreateCDispositivoEliminadoAsync(CDispositivoEliminadoCreateRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateCDispositivoEliminadoAsync(CDispositivoEliminadoUpdateRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteCDispositivoEliminadoAsync(int id, CancellationToken cancellationToken = default);

    Task<MonitorCatalogPageResult<CatalogLocationRow>> ListCatalogLocationAsync(MonitorCatalogListRequest request, CancellationToken cancellationToken = default);
    Task<int> CreateCatalogLocationAsync(CatalogLocationCreateRequest request, CancellationToken cancellationToken = default);
    Task<bool> UpdateCatalogLocationAsync(CatalogLocationUpdateRequest request, CancellationToken cancellationToken = default);
    Task<(bool Deleted, string? ErrorMessage)> DeleteCatalogLocationAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CEstadoComboRow>> ListCEstadosComboAsync(CancellationToken cancellationToken = default);
}

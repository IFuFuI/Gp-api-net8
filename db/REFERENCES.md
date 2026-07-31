# Referencias de base de datos (ATT / Monitor)

## Script completo de base de datos (remediado)

| Campo | Valor |
|--------|--------|
| **Carpeta** | `E:\GeneralDocumentacion\Documentos\GPSolution\ATT\DB` |
| **Archivo** | `QA_MONITOR_ATT_remediado.sql` |
| **Ruta completa** | `E:\GeneralDocumentacion\Documentos\GPSolution\ATT\DB\QA_MONITOR_ATT_remediado.sql` |

Este repositorio mantiene en `API\db` solo scripts puntuales o de referencia; el **dump / script integral** del esquema QA remedido vive en la ruta de documentación general indicada arriba.

## Scripts en este repo (`Ecosistema_ATT\API\db`)

| Archivo | Descripción |
|---------|-------------|
| `2026_05_08_journal_dia_disc_layout.reference.sql` | Layout journal día en disco |
| `2026_05_15_catalogo_equipos_region_c_region.reference.sql` | Alineación región EP: `FN_EP_DATOS`, `SP_GET_CatalogoEquipos`, `SP_GET_EquiposDetalle` vía `Catalog_Locations` + `C_REGION` |
| `2026_05_16_catalogo_equipos_kpi_region.reference.sql` | KPIs Mis equipos: `SP_GET_CatalogoEquiposResumenRegion` + `@REGION_FILTRO` en `SP_GET_CatalogoEquipos` |
| `SP_DETALLE_EQUIPO_UPSERT_SO_DD_ON_START.reference.sql` | Upsert SO/disco al arranque del agente |
| `2026_05_19_comando_atm_url.reference.sql` | Ruta en cajero: columna `RUTA_CAJERO`, `SP_INSERT_COMANDO_ATM(@URL)`, `SP_GET_COMANDO_ATM` con `URL` en JSON |
| `2026_05_19_dashboard_mapa_ep_por_estado.reference.sql` | Mapa dashboard: `SP_EP_MONITOR_UBICACION` con `EP_TOTAL` (todos los estados con EP activas) |
| `2026_05_19_dashboard_mapa_ep_por_estado.rollback.reference.sql` | Rollback del SP anterior (solo estados con fallas, sin `EP_TOTAL`) |
| `2026_05_26_fallas_consulta_dispositivo.reference.sql` | `FN_CONSULTA_FALLAS`: columna `DISPOSITIVO` para listado `/dashboard/fallas/0` |
| `2026_05_26_fallas_consulta_dispositivo.rollback.reference.sql` | Rollback de `FN_CONSULTA_FALLAS` sin `DISPOSITIVO` |
| `2026_05_19_reportes_bitacora_seis_tipos.reference.sql` | Reportes `/reportes`: catálogo 6 tipos + `SP_GET_REPORTE_*` (basado en **QA_MONITOR_ATT_remediado.sql**: `VW_BD_TRANSACCIONES_CONCILIACION`, `BD_CAMPANA`, `R_CAMPANA_EP`, `ATM_CONTADORES`, etc.) |
| `2026_05_19_reportes_vista_transacciones_conciliacion.reference.sql` | Solo la vista `VW_BD_TRANSACCIONES_CONCILIACION` (requerida si falla generación de transacciones/cierre) |
| `2026_05_19_reportes_ep_tipo_falla_string_agg_fix.reference.sql` | Fix error SQL 9829 en reporte EP activas (`FN_REPORTE_EP_TIPO_FALLA` → `NVARCHAR(MAX)`) |
| `2026_05_19_reportes_bitacora_seis_tipos.rollback.reference.sql` | Rollback catálogo 4 tipos legacy y drop SP reportes bitácora |

**Exportación XLSX (API):** plantillas en `API/src/ATT.Monitor.Api/Templates/Reportes/` (copia de `ReportesMonitorSolicitud`). La generación deja **una sola hoja** con datos en **Nuevo Layout** (reporte #3: **Hoja1**); se elimina la pestaña legacy del template. Config: `ReportePlantillas:TemplatesDirectory` y `LayoutSheetName`.

Ver el resto de archivos `*.sql` en esta carpeta. **No se ejecutan desde la API**; son referencia para aplicar manualmente en `QA_MONITOR_ATT`.

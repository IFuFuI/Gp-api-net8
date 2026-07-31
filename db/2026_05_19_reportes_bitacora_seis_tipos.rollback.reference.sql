/*
  Rollback: reportes bitácora 6 tipos → catálogo anterior (4 tipos) y elimina SP nuevos.
  Aplicar manualmente en QA_MONITOR_ATT.
*/

USE [QA_MONITOR_ATT];
GO

SET IDENTITY_INSERT dbo.CatalogoReportes ON;
MERGE dbo.CatalogoReportes AS t
USING (VALUES
    (1, N'Reporte de Transacciones'),
    (2, N'Reporte de Transacciones con Error'),
    (3, N'Reporte de Cierre de Caja'),
    (4, N'Reporte de Contadores')
) AS s (Id_Catalogo, DescripcionCatalogo)
ON t.Id_Catalogo = s.Id_Catalogo
WHEN MATCHED THEN
    UPDATE SET DescripcionCatalogo = s.DescripcionCatalogo;
DELETE FROM dbo.CatalogoReportes WHERE Id_Catalogo IN (5, 6);
SET IDENTITY_INSERT dbo.CatalogoReportes OFF;
GO

DROP PROCEDURE IF EXISTS dbo.SP_GET_REPORTE_EP_ACTIVAS;
DROP PROCEDURE IF EXISTS dbo.SP_GET_REPORTE_TRANSACCIONES_BITACORA;
DROP PROCEDURE IF EXISTS dbo.SP_GET_REPORTE_TRANSACCIONES_POR_EQUIPO;
DROP PROCEDURE IF EXISTS dbo.SP_GET_REPORTE_CAMPANAS_MKT;
DROP PROCEDURE IF EXISTS dbo.SP_GET_REPORTE_CIERRE_CAJA;
DROP PROCEDURE IF EXISTS dbo.SP_GET_REPORTE_CONTADORES;
DROP FUNCTION IF EXISTS dbo.FN_REPORTE_EP_TIPO_FALLA;
GO

PRINT N'Rollback reportes bitácora aplicado (catálogo 4 tipos legacy).';
GO

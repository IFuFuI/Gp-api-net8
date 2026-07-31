/*
  Dashboard — mapa por estado: todas las EP activas + distinción de fallas

  Consumido por:
    - GET api/Dashboard/GET_DASH_MONITOR_EP  →  mapa[]
    - Blazor Highcharts (hc-key en Catalog_Locations.Ubicacion, ej. mx-jal)

  Cambios:
    - SP_EP_MONITOR_UBICACION devuelve una fila por estado (Ubicacion) con EP activas,
      aunque no tengan fallas abiertas.
    - Nueva columna EP_TOTAL.

  Aplicar manualmente en QA_MONITOR_ATT (y luego en producción).
  Rollback: 2026_05_19_dashboard_mapa_ep_por_estado.rollback.reference.sql
*/

USE [QA_MONITOR_ATT];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.SP_EP_MONITOR_UBICACION', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.SP_EP_MONITOR_UBICACION AS SET NOCOUNT ON;');
GO

ALTER PROCEDURE [dbo].[SP_EP_MONITOR_UBICACION]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        LTRIM(RTRIM(C.Ubicacion)) AS UBICACION,
        COUNT(DISTINCT F.Mac_Address) AS EP_TOTAL,
        COUNT(DISTINCT CASE WHEN D.estado = 1 THEN F.Mac_Address END) AS EP_CON_FALLA,
        COUNT(CASE WHEN D.estado = 1 THEN D.Id_Falla END) AS TOTALFALLAS,
        MAX(NULLIF(LTRIM(RTRIM(C.estado)), N'')) AS estado
    FROM dbo.Device_Configuration F
    INNER JOIN dbo.Catalog_Locations C
        ON F.Id_Location = C.Id
    LEFT JOIN dbo.BD_FALLAS D
        ON D.Id_ATM = F.Mac_Address
       AND D.estado = 1
    WHERE F.IsActive = 1
      AND NULLIF(LTRIM(RTRIM(C.Ubicacion)), N'') IS NOT NULL
    GROUP BY LTRIM(RTRIM(C.Ubicacion));
END;
GO

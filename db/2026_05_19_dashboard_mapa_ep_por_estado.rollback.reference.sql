/*
  Rollback: restaura SP_EP_MONITOR_UBICACION (solo estados con fallas abiertas, sin EP_TOTAL)

  Aplicar manualmente en QA_MONITOR_ATT si se revierte
  2026_05_19_dashboard_mapa_ep_por_estado.reference.sql
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
        C.Ubicacion AS UBICACION,
        COUNT(DISTINCT F.Mac_Address) AS EP_CON_FALLA,
        COUNT(D.Id_Falla) AS TOTALFALLAS,
        C.estado
    FROM dbo.Device_Configuration F
    INNER JOIN dbo.BD_FALLAS D
        ON D.Id_ATM = F.Mac_Address
    INNER JOIN dbo.Catalog_Locations C
        ON F.Id_Location = C.Id
    WHERE F.IsActive = 1
      AND D.estado = 1
    GROUP BY C.Ubicacion, C.estado;
END;
GO

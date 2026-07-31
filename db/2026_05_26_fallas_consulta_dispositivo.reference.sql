/*
  FN_CONSULTA_FALLAS — expone DISPOSITIVO (C_DISPOSITIVOS_ATM) en el listado de fallas.

  Consumido por: POST api/Dashboard/GET_FALLAS → Dashboard /dashboard/fallas/0

  Aplicar manualmente en QA_MONITOR_ATT.
  Rollback: 2026_05_26_fallas_consulta_dispositivo.rollback.reference.sql
*/

USE [QA_MONITOR_ATT];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER FUNCTION [dbo].[FN_CONSULTA_FALLAS]
(
    @PAGE_NUMBER INT,
    @PAGE_SIZE INT,
    @BUSCAR NVARCHAR(255) = NULL,
    @ORDER_BY NVARCHAR(50) = 'Id_ATM',
    @ORDER_DIR NVARCHAR(4) = 'DESC',
    @ID_DISPO INT = NULL,
    @FECHA_INI DATETIME = NULL,
    @FECHA_FIN DATETIME = NULL
)
RETURNS TABLE
AS
RETURN
(
    WITH DATOS AS
    (
        SELECT
            A.Id_ATM,
            B.ID_PB_TIENDA,
            B.Name,
            B.Adress,
            R.REGION,
            C.Location_Name,
            DIS.DISPOSITIVO,
            E.DESCRIPCION AS SUBCATEGORIA,
            A.Fecha_Ini,
            A.Fecha_FIN,
            COUNT(*) OVER() AS TOTAL_FILTRADOS
        FROM dbo.BD_FALLAS A
        INNER JOIN dbo.C_DISPOSITIVOS_ATM DIS
            ON A.ID_DISPOSITIVO = DIS.ID_DISPOSITIVO
        INNER JOIN dbo.C_STATUS_DISPOSITIVOS E
            ON E.ID_DISPOSITIVO = A.ID_DISPOSITIVO
           AND E.ESTADO = A.ID_ESTADO
        INNER JOIN dbo.Device_Configuration B
            ON A.Id_ATM = B.Mac_Address
        INNER JOIN dbo.Catalog_Locations C
            ON C.Id = B.Id_Location
        INNER JOIN dbo.C_REGION R
            ON R.ID = C.Id_Region
        WHERE B.IsActive = 1
          AND (
                @ID_DISPO IS NULL
                OR @ID_DISPO = 0
                OR A.ID_DISPOSITIVO = @ID_DISPO
              )
          AND (
                (@FECHA_INI IS NULL OR @FECHA_FIN IS NULL)
                OR (
                    TRY_CONVERT(DATETIME, A.Fecha_Ini) IS NOT NULL
                    AND CONVERT(CHAR(8), TRY_CONVERT(DATETIME, A.Fecha_Ini), 112)
                        BETWEEN CONVERT(CHAR(8), @FECHA_INI, 112)
                            AND CONVERT(CHAR(8), @FECHA_FIN, 112)
                )
              )
          AND (
                @BUSCAR IS NULL
                OR A.Id_ATM LIKE N'%' + @BUSCAR + N'%'
                OR CONVERT(NVARCHAR(50), B.ID_PB_TIENDA) LIKE N'%' + @BUSCAR + N'%'
                OR B.Name LIKE N'%' + @BUSCAR + N'%'
                OR B.Adress LIKE N'%' + @BUSCAR + N'%'
                OR R.REGION LIKE N'%' + @BUSCAR + N'%'
                OR C.Location_Name LIKE N'%' + @BUSCAR + N'%'
                OR DIS.DISPOSITIVO LIKE N'%' + @BUSCAR + N'%'
                OR E.DESCRIPCION LIKE N'%' + @BUSCAR + N'%'
                OR CONVERT(NVARCHAR(19), A.Fecha_Ini, 120) LIKE N'%' + @BUSCAR + N'%'
                OR CONVERT(NVARCHAR(19), A.Fecha_FIN, 120) LIKE N'%' + @BUSCAR + N'%'
              )
    )
    SELECT *
    FROM DATOS
    ORDER BY
        CASE WHEN @ORDER_BY = 'Id_ATM' AND @ORDER_DIR = 'ASC' THEN Id_ATM END ASC,
        CASE WHEN @ORDER_BY = 'Id_ATM' AND @ORDER_DIR = 'DESC' THEN Id_ATM END DESC,
        CASE WHEN @ORDER_BY = 'ID_PB_TIENDA' AND @ORDER_DIR = 'ASC' THEN ID_PB_TIENDA END ASC,
        CASE WHEN @ORDER_BY = 'ID_PB_TIENDA' AND @ORDER_DIR = 'DESC' THEN ID_PB_TIENDA END DESC,
        CASE WHEN @ORDER_BY = 'Name' AND @ORDER_DIR = 'ASC' THEN Name END ASC,
        CASE WHEN @ORDER_BY = 'Name' AND @ORDER_DIR = 'DESC' THEN Name END DESC,
        CASE WHEN @ORDER_BY = 'DISPOSITIVO' AND @ORDER_DIR = 'ASC' THEN DISPOSITIVO END ASC,
        CASE WHEN @ORDER_BY = 'DISPOSITIVO' AND @ORDER_DIR = 'DESC' THEN DISPOSITIVO END DESC,
        CASE WHEN @ORDER_BY = 'SUBCATEGORIA' AND @ORDER_DIR = 'ASC' THEN SUBCATEGORIA END ASC,
        CASE WHEN @ORDER_BY = 'SUBCATEGORIA' AND @ORDER_DIR = 'DESC' THEN SUBCATEGORIA END DESC,
        CASE WHEN @ORDER_BY = 'Fecha_Ini' AND @ORDER_DIR = 'ASC' THEN Fecha_Ini END ASC,
        CASE WHEN @ORDER_BY = 'Fecha_Ini' AND @ORDER_DIR = 'DESC' THEN Fecha_Ini END DESC,
        CASE WHEN @ORDER_BY = 'Fecha_FIN' AND @ORDER_DIR = 'ASC' THEN Fecha_FIN END ASC,
        CASE WHEN @ORDER_BY = 'Fecha_FIN' AND @ORDER_DIR = 'DESC' THEN Fecha_FIN END DESC,
        Id_ATM DESC
    OFFSET (@PAGE_NUMBER - 1) * @PAGE_SIZE ROWS
    FETCH NEXT @PAGE_SIZE ROWS ONLY
);
GO

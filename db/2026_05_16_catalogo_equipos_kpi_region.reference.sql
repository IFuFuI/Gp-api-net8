/*
  Referencia: KPIs por región en Mis equipos + filtro al hacer clic

  Consumido por:
    - GET_CATALOGO_EQUIPOS_RESUMEN_REGION → tarjetas KPI (conteo por zona)
    - GET_CATALOGO_EQUIPOS con @REGION_FILTRO → tabla filtrada

  Requiere joins Catalog_Locations + C_REGION (ver también
  2026_05_15_catalogo_equipos_region_c_region.reference.sql).

  Aplicar manualmente en QA_MONITOR_ATT.
*/

USE [QA_MONITOR_ATT];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/* --------------------------------------------------------------------------
   Resumen: cantidad de cajeros activos por región
   -------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.SP_GET_CatalogoEquiposResumenRegion', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.SP_GET_CatalogoEquiposResumenRegion AS SET NOCOUNT ON;');
GO

ALTER PROCEDURE [dbo].[SP_GET_CatalogoEquiposResumenRegion]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        CASE
            WHEN NULLIF(LTRIM(RTRIM(ISNULL(R.REGION, D.REGION))), '') IS NULL
            THEN N'(Sin región)'
            ELSE LTRIM(RTRIM(ISNULL(R.REGION, D.REGION)))
        END AS REGION,
        COUNT(*) AS CANTIDAD
    FROM dbo.Device_Configuration D
    LEFT JOIN dbo.Catalog_Locations loc
        ON D.Id_Location = loc.Id
    LEFT JOIN dbo.C_REGION R
        ON R.ID = loc.Id_Region
    WHERE D.IsActive = 1
    GROUP BY
        CASE
            WHEN NULLIF(LTRIM(RTRIM(ISNULL(R.REGION, D.REGION))), '') IS NULL
            THEN N'(Sin región)'
            ELSE LTRIM(RTRIM(ISNULL(R.REGION, D.REGION)))
        END
    ORDER BY REGION;
END;
GO

/* --------------------------------------------------------------------------
   Catálogo paginado — añade filtro explícito por región (KPI)
   -------------------------------------------------------------------------- */
ALTER PROCEDURE [dbo].[SP_GET_CatalogoEquipos]
    @IGNORAR INT = 0,
    @CANTIDAD_FILA INT = 10,
    @FILTRO VARCHAR(150) = '',
    @REGION_FILTRO VARCHAR(150) = NULL,
    @ORDEN VARCHAR(50) = 'ID_ESTACION_PAGO',
    @DIR VARCHAR(10) = 'ASC'
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH DATOSPAGINADOS AS
    (
        SELECT
            D.ID AS ID,
            D.Mac_Address AS ID_ESTACION_PAGO,
            D.Branch AS NOMBRE_TIENDA,
            ISNULL(R.REGION, D.REGION) AS REGION,
            D.ID_PB_TIENDA,
            D.Model AS MODEL,
            ISNULL(loc.estado, loc.Location_Name) AS ESTADO
        FROM dbo.Device_Configuration D
        LEFT JOIN dbo.Catalog_Locations loc
            ON D.Id_Location = loc.Id
        LEFT JOIN dbo.C_REGION R
            ON R.ID = loc.Id_Region
        WHERE D.IsActive = 1
          AND (
                @REGION_FILTRO IS NULL
             OR LTRIM(RTRIM(@REGION_FILTRO)) = ''
             OR (
                    LTRIM(RTRIM(@REGION_FILTRO)) = N'(Sin región)'
                AND NULLIF(LTRIM(RTRIM(ISNULL(R.REGION, D.REGION))), '') IS NULL
                )
             OR LTRIM(RTRIM(ISNULL(R.REGION, D.REGION))) = LTRIM(RTRIM(@REGION_FILTRO))
          )
          AND (
                @FILTRO = ''
             OR CAST(D.ID AS VARCHAR(50)) LIKE '%' + @FILTRO + '%'
             OR D.Mac_Address LIKE '%' + @FILTRO + '%'
             OR D.Branch LIKE '%' + @FILTRO + '%'
             OR ISNULL(R.REGION, D.REGION) LIKE '%' + @FILTRO + '%'
             OR CAST(D.ID_PB_TIENDA AS VARCHAR(50)) LIKE '%' + @FILTRO + '%'
             OR D.Model LIKE '%' + @FILTRO + '%'
             OR loc.Location_Name LIKE '%' + @FILTRO + '%'
             OR loc.estado LIKE '%' + @FILTRO + '%'
          )
    ),
    DATOSFINALES AS
    (
        SELECT *
        FROM DATOSPAGINADOS
        ORDER BY
            CASE WHEN @ORDEN = 'ID' AND @DIR = 'ASC'  THEN ID END ASC,
            CASE WHEN @ORDEN = 'ID' AND @DIR = 'DESC' THEN ID END DESC,
            CASE WHEN @ORDEN = 'ID_ESTACION_PAGO' AND @DIR = 'ASC'  THEN ID_ESTACION_PAGO END ASC,
            CASE WHEN @ORDEN = 'ID_ESTACION_PAGO' AND @DIR = 'DESC' THEN ID_ESTACION_PAGO END DESC,
            CASE WHEN @ORDEN = 'NOMBRE_TIENDA' AND @DIR = 'ASC'  THEN NOMBRE_TIENDA END ASC,
            CASE WHEN @ORDEN = 'NOMBRE_TIENDA' AND @DIR = 'DESC' THEN NOMBRE_TIENDA END DESC,
            CASE WHEN @ORDEN = 'REGION' AND @DIR = 'ASC'  THEN REGION END ASC,
            CASE WHEN @ORDEN = 'REGION' AND @DIR = 'DESC' THEN REGION END DESC,
            CASE WHEN @ORDEN = 'ID_PB_TIENDA' AND @DIR = 'ASC'  THEN ID_PB_TIENDA END ASC,
            CASE WHEN @ORDEN = 'ID_PB_TIENDA' AND @DIR = 'DESC' THEN ID_PB_TIENDA END DESC,
            CASE WHEN @ORDEN = 'ESTADO' AND @DIR = 'ASC'  THEN ESTADO END ASC,
            CASE WHEN @ORDEN = 'ESTADO' AND @DIR = 'DESC' THEN ESTADO END DESC,
            CASE WHEN @ORDEN = 'MODEL' AND @DIR = 'ASC'  THEN MODEL END ASC,
            CASE WHEN @ORDEN = 'MODEL' AND @DIR = 'DESC' THEN MODEL END DESC,
            CASE WHEN @ORDEN = 'MODELO' AND @DIR = 'ASC'  THEN MODEL END ASC,
            CASE WHEN @ORDEN = 'MODELO' AND @DIR = 'DESC' THEN MODEL END DESC
        OFFSET @IGNORAR ROWS
        FETCH NEXT @CANTIDAD_FILA ROWS ONLY
    )
    SELECT
        ID,
        ID_ESTACION_PAGO,
        ISNULL(NOMBRE_TIENDA, '') AS NOMBRE_TIENDA,
        ISNULL(REGION, '') AS REGION,
        ISNULL(CAST(ID_PB_TIENDA AS VARCHAR(50)), '') AS ID_PB_TIENDA,
        ISNULL(ESTADO, '') AS ESTADO,
        ISNULL(MODEL, '') AS MODELO,
        'OK' AS ESTATUS,
        (SELECT COUNT(*) FROM DATOSPAGINADOS) AS TOTAL
    FROM DATOSFINALES;
END;
GO

/*
  Pruebas:

  EXEC dbo.SP_GET_CatalogoEquiposResumenRegion;
  EXEC dbo.SP_GET_CatalogoEquipos @IGNORAR=0, @CANTIDAD_FILA=10, @FILTRO='', @REGION_FILTRO=N'SURESTE';
*/

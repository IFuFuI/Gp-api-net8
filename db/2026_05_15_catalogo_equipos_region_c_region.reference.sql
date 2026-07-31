/*
  Referencia: alinear región de EP con catálogo normalizado
  Device_Configuration → Catalog_Locations → C_REGION

  Consumido por:
    - Mis equipos (Blazor/MVC): dbo.SP_GET_CatalogoEquipos → api/Dashboard/GET_CATALOGO_EQUIPOS
    - Detalle equipo: dbo.SP_GET_EquiposDetalle → GET_EQUIPOS_DETALLE_MVC
    - Datos EP: dbo.FN_EP_DATOS → api/Dashboard/GET_TOTAL_EP_DATOS (columna ZONA en API)

  Aplicar manualmente en QA_MONITOR_ATT (no se ejecuta desde la API).
  Script integral: E:\GeneralDocumentacion\Documentos\GPSolution\ATT\DB\QA_MONITOR_ATT_remediado.sql
*/

USE [QA_MONITOR_ATT];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/* --------------------------------------------------------------------------
   FN_EP_DATOS — mantiene alias ZONA (EPDatosResponse / EpDatosRowDto en C#)
   -------------------------------------------------------------------------- */
ALTER FUNCTION [dbo].[FN_EP_DATOS]
(
    @ID_EP VARCHAR(100) = NULL
)
RETURNS TABLE
AS
RETURN
(
    SELECT
        c.Mac_Address AS ID_EP,
        c.Branch AS SUCURSAL,
        c.Name AS NOMBRE,
        c.Adress AS DIRECCION,
        loc.estado AS ESTADO,
        ISNULL(NULL, '-') AS ADMINISTRADO_POR,
        ISNULL(R.REGION, c.REGION) AS ZONA,
        c.Model AS MODEL,
        ISNULL(c.Serie, '-') AS SERIE,
        ISNULL(NULL, '-') AS CANAL,
        ISNULL(NULL, '-') AS TIPO
    FROM dbo.Device_Configuration c
    LEFT JOIN dbo.Catalog_Locations loc
        ON c.Id_Location = loc.Id
    LEFT JOIN dbo.C_REGION R
        ON R.ID = loc.Id_Region
    WHERE (@ID_EP IS NULL OR c.Mac_Address = @ID_EP)
);
GO

/* --------------------------------------------------------------------------
   SP_GET_CatalogoEquipos — tabla Mis equipos (columna REGION)
   -------------------------------------------------------------------------- */
ALTER PROCEDURE [dbo].[SP_GET_CatalogoEquipos]
    @IGNORAR INT = 0,
    @CANTIDAD_FILA INT = 10,
    @FILTRO VARCHAR(150) = '',
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

/* --------------------------------------------------------------------------
   SP_GET_EquiposDetalle — encabezado detalle (campo REGION)
   -------------------------------------------------------------------------- */
ALTER PROCEDURE [dbo].[SP_GET_EquiposDetalle]
(
    @ID INT
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        d.Branch AS INFORMACION_DE_EQUIPO,
        d.ID_EP AS ID_EP,
        ISNULL(R.REGION, d.REGION) AS REGION,
        d.ID_PB_TIENDA AS ID_PB,
        d.Model AS MODELO,
        d.Branch AS SUCURSAL,
        d.Adress AS DIRECCION,
        d.SERIE AS SERIE,
        d.ID_NEO AS ID_NEO,
        d.Branch AS NOMBRE_EQUIPO,
        d.Ip_Address_F_Device AS IP,
        loc.Location_Name AS LOCALIDAD,
        d.App_Version AS VERSION_APLICATIVO,
        d.Installation_Version AS VERSION_INSTALACION,
        d.CAJA AS CAJA,
        d.USUARIO AS USUARIO,
        d.PASSWORD AS PASSWORD,
        d.MACADDRESS AS MACADDRESS,
        d.CUENTA AS CUENTA,
        d.VPNUSUARIO AS VPN_USUARIO,
        d.VPNPASSWORD AS VPN_PASSWORD,
        d.CASS1 AS CASS1,
        d.CASS2 AS CASS2,
        d.URL_RECARGA_TA AS URL_RECARGA_TA,
        d.URL_OTROS_SERVICIOS AS URL_OTROS_SERVICIOS,
        d.URL_PAGO_FACTURA AS URL_PAGO_FACTURA,
        d.CC_CODI AS CC_CODI,
        d.CC_EFE AS CC_EFE,
        d.CC_VD1 AS CC_VD1,
        d.STATUS_CAJA AS STATUS_CAJA,
        d.URL_BRANCH AS URL_BRANCH
    FROM dbo.Device_Configuration d
    LEFT JOIN dbo.Catalog_Locations loc
        ON d.Id_Location = loc.Id
    LEFT JOIN dbo.C_REGION R
        ON R.ID = loc.Id_Region
    WHERE d.Id = @ID;
END;
GO

/*
  Pruebas sugeridas:

  EXEC dbo.SP_GET_CatalogoEquipos @IGNORAR=0, @CANTIDAD_FILA=10, @FILTRO='', @ORDEN='REGION', @DIR='ASC';
  SELECT * FROM dbo.FN_EP_DATOS(NULL);
  EXEC dbo.SP_GET_EquiposDetalle @ID = 1;
*/

/*
  Reportes Monitor — 6 tipos (plantillas ReportesMonitorSolicitud).

  Esquema de referencia (origen de verdad):
    E:\GeneralDocumentacion\Documentos\GPSolution\ATT\DB\QA_MONITOR_ATT_remediado.sql

  Objetos base en remedido:
    - dbo.BD_CAMPANA, dbo.R_CAMPANA_EP, dbo.SP_GET_CAMPANAS
    - dbo.BD_TRANSACCIONES, dbo.VW_BD_TRANSACCIONES_CONCILIACION
    - dbo.Device_Configuration, dbo.C_REGION, dbo.Catalog_Locations
    - dbo.ATM_CONTADORES, dbo.SP_GET_REPORTEDECONTADORES
    - dbo.BD_INVENTARIO_ATM, dbo.BD_VERSION_DSC_ATM
    - dbo.FN_EP_DATOS, dbo.SP_GET_CatalogoEquipos

  Consumido por: POST api/Dashboard/GENERAR_REPORTE
  Rollback: 2026_05_19_reportes_bitacora_seis_tipos.rollback.reference.sql
*/

USE [QA_MONITOR_ATT];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'CatalogoReportes' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    RAISERROR(N'Tabla dbo.CatalogoReportes no encontrada. Aplique QA_MONITOR_ATT_remediado.sql primero.', 16, 1);
    RETURN;
END;
GO

SET IDENTITY_INSERT dbo.CatalogoReportes ON;
MERGE dbo.CatalogoReportes AS t
USING (VALUES
    (1, N'Reporte de Estaciones de Pago Activas'),
    (2, N'Reporte de Transacciones con Error'),
    (3, N'Reporte de Transacciones por Equipo'),
    (4, N'Reporte de Campañas MKT'),
    (5, N'Reporte de Cierre de Caja'),
    (6, N'Reporte de Contadores')
) AS s (Id_Catalogo, DescripcionCatalogo)
ON t.Id_Catalogo = s.Id_Catalogo
WHEN MATCHED THEN UPDATE SET DescripcionCatalogo = s.DescripcionCatalogo
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id_Catalogo, DescripcionCatalogo) VALUES (s.Id_Catalogo, s.DescripcionCatalogo);
SET IDENTITY_INSERT dbo.CatalogoReportes OFF;
GO

/* Vista requerida por reportes de transacciones y cierre — ver también 2026_05_19_reportes_vista_transacciones_conciliacion.reference.sql */
IF OBJECT_ID(N'dbo.VW_BD_TRANSACCIONES_CONCILIACION', N'V') IS NULL
    EXEC(N'CREATE VIEW dbo.VW_BD_TRANSACCIONES_CONCILIACION AS
SELECT B.ID_TRANSACCION, B.EP,
  UPPER(LTRIM(RTRIM(CONVERT(VARCHAR(50), B.EP)))) AS EP_NORM,
  B.CLIENTE, LTRIM(RTRIM(CONVERT(VARCHAR(100), B.CLIENTE))) AS CLIENTE_NORM,
  B.MONTO, TRY_CONVERT(DECIMAL(18,2), B.MONTO) AS MONTO_NORM, B.FECHA, N.FECHA_DT,
  B.TIPO, B.ESTATUS, TRY_CONVERT(INT, B.ESTATUS) AS ESTATUS_INT,
  B.TIPO_PAGO, B.FOLIO, B.CAMBIO, B.CAMBIO_INCOMPLETO, B.CODIGO_ERROR, B.MOTIVO_RECHAZO,
  B.DN, B.CODIGO_AUTORIZACION, B.NO_TARJETA, B.REFERENCIA, B.REFERENCIA_EP, B.COLOR, B.JOURNAL, B.FECHA_REGISTRO
FROM dbo.BD_TRANSACCIONES B
CROSS APPLY (SELECT FECHA_TXT = LTRIM(RTRIM(CONVERT(VARCHAR(200), B.FECHA)))) S
CROSS APPLY (SELECT FECHA_FLOAT = TRY_CONVERT(FLOAT, REPLACE(S.FECHA_TXT, '','', ''.''))) F
CROSS APPLY (SELECT FECHA_DT = COALESCE(
  TRY_CONVERT(DATETIME2(0), S.FECHA_TXT, 120), TRY_CONVERT(DATETIME2(0), S.FECHA_TXT, 121),
  TRY_CONVERT(DATETIME2(0), S.FECHA_TXT, 126), TRY_CONVERT(DATETIME2(0), S.FECHA_TXT, 103),
  TRY_CONVERT(DATETIME2(0), S.FECHA_TXT, 105),
  CASE WHEN F.FECHA_FLOAT BETWEEN 20000 AND 90000 THEN
    DATEADD(SECOND, CONVERT(INT, ROUND((F.FECHA_FLOAT - FLOOR(F.FECHA_FLOAT)) * 86400, 0)),
      DATEADD(DAY, CONVERT(INT, FLOOR(F.FECHA_FLOAT)), CONVERT(DATETIME2(0), ''18991230'')))
  ELSE NULL END)) N;');
GO

/* Estado HW por EP — último estado por dispositivo; STRING_AGG en NVARCHAR(MAX) (evita error 9829) */
CREATE OR ALTER FUNCTION dbo.FN_REPORTE_EP_TIPO_FALLA(@ID_EP VARCHAR(50))
RETURNS NVARCHAR(MAX)
AS
BEGIN
    DECLARE @txt NVARCHAR(MAX);

    ;WITH Ultimo AS (
        SELECT
            e.DISPOSITIVO,
            e.ESTADO,
            ROW_NUMBER() OVER (
                PARTITION BY e.DISPOSITIVO
                ORDER BY e.FECHA DESC, e.ID_ESTADO_DIS DESC
            ) AS rn
        FROM dbo.BD_ESTADO_DISPOSITIVOS_ATM e
        WHERE RTRIM(LTRIM(e.ID_CAJERO)) = RTRIM(LTRIM(@ID_EP))
    )
    SELECT @txt = STRING_AGG(
        CAST(
            CONCAT(
                ISNULL(d.DISPOSITIVO, N''),
                N' ',
                ISNULL(s.DESCRIPCION, N''),
                N' OK,')
            AS NVARCHAR(MAX)),
        N' ')
    FROM Ultimo u
    INNER JOIN dbo.C_DISPOSITIVOS_ATM d ON d.ID_DISPOSITIVO = u.DISPOSITIVO
    LEFT JOIN dbo.C_STATUS_DISPOSITIVOS s
        ON s.ID_DISPOSITIVO = u.DISPOSITIVO AND s.ESTADO = u.ESTADO
    WHERE u.rn = 1;

    RETURN ISNULL(LEFT(@txt, 4000), N'');
END;
GO

/* 1 — EP activas (snapshot; alineado SP_GET_CatalogoEquipos + inventario) */
CREATE OR ALTER PROCEDURE dbo.SP_GET_REPORTE_EP_ACTIVAS
    @FechaInicio DATE,
    @FechaFin DATE,
    @Ignorar INT = 0,
    @Cantidad_Fila INT = 500,
    @Filtro VARCHAR(150) = '',
    @Orden VARCHAR(50) = 'ID',
    @Dir VARCHAR(10) = 'ASC'
AS
BEGIN
    SET NOCOUNT ON;
    IF UPPER(@Dir) NOT IN ('ASC', 'DESC') SET @Dir = 'ASC';

    ;WITH Base AS (
        SELECT
            D.Mac_Address AS [ID],
            ISNULL(NULLIF(LTRIM(RTRIM(D.Branch)), N''), ISNULL(D.Name, N'')) AS [Nombre De EP],
            ISNULL(R.REGION, ISNULL(D.REGION, N'')) AS [Región],
            CASE WHEN ISNULL(D.ESTATUS_SOFTWARE, 1) = 1 THEN N'OK' ELSE N'OFF' END AS [Estatus SW],
            dbo.FN_REPORTE_EP_TIPO_FALLA(D.Mac_Address) AS [Tipo de Falla],
            ISNULL(INV.ZONA, N'') AS [Area ATT],
            N'' AS [Incidente Remedy],
            CONVERT(VARCHAR(30), ISNULL(D.ULTIMO_CAMBIO_ESTATUS, D.Update_Server_Date_Time), 120) AS [Fecha y Hora Ultima actualización],
            N'' AS [Nombre 1],
            N'' AS [Numero Tel 1],
            N'' AS [Nombre 2],
            N'' AS [Numero Tel 2],
            ISNULL(NULLIF(D.Adress, N''), ISNULL(INV.DIRECCION, N'')) AS [Dirección],
            ISNULL(D.App_Version, N'') AS [App Version],
            ISNULL(CAST(D.CUENTA AS VARCHAR(50)), N'') AS [User Bank],
            ISNULL(NULLIF(D.SISTEMA_OPERATIVO, N''), ISNULL(D.OS, ISNULL(INV.SO, N''))) AS [S.O],
            ISNULL(NULLIF(D.MACADDRESS, N''), RTRIM(D.Mac_Address)) AS [Host Name],
            ISNULL(CAST(D.MEMORIA_RAM AS VARCHAR(20)), ISNULL(CAST(D.Total_Ram AS VARCHAR(20)), N'')) AS [Memoria],
            ISNULL(D.SERIE, ISNULL(INV.SERIE, N'')) AS [Serie],
            ISNULL(INV.VERSION_DSC, ISNULL(VD.VERSION_DSC, N'')) AS [Version Template],
            D.Id AS SortId
        FROM dbo.Device_Configuration D
        LEFT JOIN dbo.Catalog_Locations loc ON loc.Id = D.Id_Location
        LEFT JOIN dbo.C_REGION R ON R.ID = loc.Id_Region
        LEFT JOIN dbo.BD_INVENTARIO_ATM INV ON INV.Id_ATM = D.Mac_Address
        OUTER APPLY (
            SELECT TOP 1 v.VERSION_DSC
            FROM dbo.BD_VERSION_DSC_ATM v
            WHERE v.ID_CAJERO = D.Mac_Address
            ORDER BY v.FECHA_ALTA DESC
        ) VD
        WHERE D.IsActive = 1
          AND (
                @Filtro = N''
             OR D.Mac_Address LIKE N'%' + @Filtro + N'%'
             OR D.Branch LIKE N'%' + @Filtro + N'%'
             OR ISNULL(R.REGION, D.REGION) LIKE N'%' + @Filtro + N'%'
          )
    ),
    Numerado AS (SELECT *, COUNT(*) OVER () AS Total FROM Base)
    SELECT
        [ID], [Nombre De EP], [Región], [Estatus SW], [Tipo de Falla], [Area ATT], [Incidente Remedy],
        [Fecha y Hora Ultima actualización], [Nombre 1], [Numero Tel 1], [Nombre 2], [Numero Tel 2],
        [Dirección], [App Version], [User Bank], [S.O], [Host Name], [Memoria], [Serie], [Version Template],
        Total
    FROM Numerado
    ORDER BY CASE WHEN @Orden = 'ID' AND @Dir = 'ASC' THEN [ID] END ASC,
             CASE WHEN @Orden = 'ID' AND @Dir = 'DESC' THEN [ID] END DESC,
             SortId
    OFFSET @Ignorar ROWS FETCH NEXT @Cantidad_Fila ROWS ONLY;
END;
GO

/* 2 — Transacciones con error (fechas vía VW_BD_TRANSACCIONES_CONCILIACION) */
CREATE OR ALTER PROCEDURE dbo.SP_GET_REPORTE_TRANSACCIONES_BITACORA
    @FechaInicio DATE,
    @FechaFin DATE,
    @SoloError BIT = 0,
    @EP VARCHAR(50) = NULL,
    @Ignorar INT = 0,
    @Cantidad_Fila INT = 500,
    @Filtro VARCHAR(150) = '',
    @Orden VARCHAR(50) = 'FECHA',
    @Dir VARCHAR(10) = 'ASC'
AS
BEGIN
    SET NOCOUNT ON;
    IF UPPER(@Dir) NOT IN ('ASC', 'DESC') SET @Dir = 'ASC';

    DECLARE @FinDt DATETIME2(0) = DATEADD(DAY, 1, CAST(@FechaFin AS DATETIME2(0)));

    ;WITH Filtrado AS (
        SELECT
            V.ID_TRANSACCION,
            V.EP,
            V.FECHA_DT AS DtTxn,
            ISNULL(TX.NOMBRETX, ISNULL(V.TIPO, N'')) AS TipoNom,
            ISNULL(R.DESCRIPCION, ISNULL(V.ESTATUS, N'')) AS EstatusNom,
            ISNULL(V.TIPO_PAGO, N'') AS TipoPago,
            ISNULL(V.CLIENTE, N'') AS Cliente,
            ISNULL(V.MONTO, 0) AS Monto,
            ISNULL(V.CAMBIO, 0) AS Cambio,
            ISNULL(V.FOLIO, N'') AS Folio,
            ISNULL(V.CAMBIO_INCOMPLETO, N'') AS CambioIncompleto,
            ISNULL(V.CODIGO_ERROR, N'') AS CodigoError,
            ISNULL(V.DN, N'') AS DN,
            ISNULL(V.CODIGO_AUTORIZACION, N'') AS CodigoAut,
            ISNULL(V.NO_TARJETA, N'') AS NoTarjeta,
            ISNULL(V.REFERENCIA, N'') AS Referencia,
            ISNULL(V.MOTIVO_RECHAZO, N'') AS MotivoRechazo,
            ISNULL(T.NOMBRE_CLIENTE, N'') AS NombreCliente,
            ISNULL(V.COLOR, N'') AS Color,
            ISNULL(V.REFERENCIA_EP, N'') AS ReferenciaEP,
            ISNULL(D.Branch, N'') AS NombreEp,
            ISNULL(REG.REGION, ISNULL(D.REGION, N'')) AS RegionEp
        FROM dbo.VW_BD_TRANSACCIONES_CONCILIACION V
        INNER JOIN dbo.BD_TRANSACCIONES T ON T.ID_TRANSACCION = V.ID_TRANSACCION
        LEFT JOIN dbo.C_TRANSACCIONES R ON V.ESTATUS_INT = R.idStatus
        LEFT JOIN dbo.C_TIPO_TX TX ON TRY_CAST(V.TIPO AS INT) = TX.ID_TIPO
        LEFT JOIN dbo.Device_Configuration D ON RTRIM(D.Mac_Address) = RTRIM(V.EP)
        LEFT JOIN dbo.Catalog_Locations loc ON loc.Id = D.Id_Location
        LEFT JOIN dbo.C_REGION REG ON REG.ID = loc.Id_Region
        WHERE V.FECHA_DT >= @FechaInicio AND V.FECHA_DT < @FinDt
          AND (@EP IS NULL OR LTRIM(RTRIM(@EP)) = N'' OR RTRIM(V.EP) = LTRIM(RTRIM(@EP)))
          AND (
                @SoloError = 0
             OR (
                    NULLIF(LTRIM(RTRIM(V.CODIGO_ERROR)), N'') IS NOT NULL AND V.CODIGO_ERROR NOT IN (N'-', N' ')
                 OR NULLIF(LTRIM(RTRIM(V.MOTIVO_RECHAZO)), N'') IS NOT NULL AND V.MOTIVO_RECHAZO NOT IN (N'-', N' ')
                 OR R.DESCRIPCION LIKE N'%ERROR%' OR R.DESCRIPCION LIKE N'%RECHAZ%' OR R.DESCRIPCION LIKE N'%FALL%'
                )
          )
          AND (
                @Filtro = N''
             OR V.EP LIKE N'%' + @Filtro + N'%'
             OR D.Branch LIKE N'%' + @Filtro + N'%'
             OR CAST(V.ID_TRANSACCION AS VARCHAR(20)) LIKE N'%' + @Filtro + N'%'
          )
    ),
    BitacoraError AS (
        SELECT
            CONVERT(VARCHAR(10), DtTxn, 23) AS [FECHA],
            CONVERT(VARCHAR(8), DtTxn, 108) AS [HORA],
            RegionEp AS [REGION],
            EP AS [ID],
            NombreEp AS [ESTACIÓN DE PAGO],
            EstatusNom AS [ESTATUS],
            TipoPago AS [FORMA DE PAGO],
            TipoNom AS [TIPO DE PAGO],
            Cliente AS [CLIENTE],
            Folio AS [FOLIO],
            DN AS [DN],
            CodigoAut AS [Codigo Aut],
            NoTarjeta AS [No de Tarjeta],
            Referencia AS [Referencia],
            MotivoRechazo AS [Motivo Rechazo],
            NombreCliente AS [Nombre cliente],
            Color AS [Color],
            ReferenciaEP AS [Referencia EP],
            ID_TRANSACCION AS SortKey,
            COUNT(*) OVER () AS Total
        FROM Filtrado
    )
    SELECT
        [FECHA], [HORA], [REGION], [ID], [ESTACIÓN DE PAGO], [ESTATUS], [FORMA DE PAGO], [TIPO DE PAGO],
        [CLIENTE], [FOLIO], [DN], [Codigo Aut], [No de Tarjeta], [Referencia], [Motivo Rechazo],
        [Nombre cliente], [Color], [Referencia EP], Total
    FROM BitacoraError
    ORDER BY SortKey
    OFFSET @Ignorar ROWS FETCH NEXT @Cantidad_Fila ROWS ONLY;
END;
GO

/* 3 — Transacciones por equipo */
CREATE OR ALTER PROCEDURE dbo.SP_GET_REPORTE_TRANSACCIONES_POR_EQUIPO
    @FechaInicio DATE,
    @FechaFin DATE,
    @EP VARCHAR(50) = NULL,
    @Ignorar INT = 0,
    @Cantidad_Fila INT = 500,
    @Filtro VARCHAR(150) = '',
    @Orden VARCHAR(50) = 'Fecha',
    @Dir VARCHAR(10) = 'ASC'
AS
BEGIN
    SET NOCOUNT ON;
    IF UPPER(@Dir) NOT IN ('ASC', 'DESC') SET @Dir = 'ASC';

    DECLARE @FinDt DATETIME2(0) = DATEADD(DAY, 1, CAST(@FechaFin AS DATETIME2(0)));

    ;WITH Raw AS (
        SELECT
            V.ID_TRANSACCION,
            V.EP,
            V.FECHA_DT AS DtTxn,
            ISNULL(TX.NOMBRETX, ISNULL(V.TIPO, N'')) AS TipoNom,
            ISNULL(R.DESCRIPCION, ISNULL(V.ESTATUS, N'')) AS EstatusNom,
            ISNULL(V.TIPO_PAGO, N'') AS TipoPago,
            ISNULL(V.CLIENTE, N'') AS Cliente,
            ISNULL(V.MONTO, 0) AS Monto,
            ISNULL(V.CAMBIO, 0) AS Cambio,
            ISNULL(V.FOLIO, N'') AS Folio,
            ISNULL(V.CAMBIO_INCOMPLETO, N'') AS CambioIncompleto,
            ISNULL(V.CODIGO_ERROR, N'') AS CodigoError,
            ISNULL(V.DN, N'') AS DN,
            ISNULL(V.CODIGO_AUTORIZACION, N'') AS CodigoAut,
            ISNULL(V.NO_TARJETA, N'') AS NoTarjeta,
            ISNULL(V.REFERENCIA, N'') AS Referencia,
            ISNULL(V.MOTIVO_RECHAZO, N'') AS MotivoRechazo,
            ISNULL(T.NOMBRE_CLIENTE, N'') AS NombreCliente,
            ISNULL(V.COLOR, N'') AS Color,
            ISNULL(V.REFERENCIA_EP, N'') AS ReferenciaEP
        FROM dbo.VW_BD_TRANSACCIONES_CONCILIACION V
        INNER JOIN dbo.BD_TRANSACCIONES T ON T.ID_TRANSACCION = V.ID_TRANSACCION
        LEFT JOIN dbo.C_TRANSACCIONES R ON V.ESTATUS_INT = R.idStatus
        LEFT JOIN dbo.C_TIPO_TX TX ON TRY_CAST(V.TIPO AS INT) = TX.ID_TIPO
        WHERE V.FECHA_DT >= @FechaInicio AND V.FECHA_DT < @FinDt
          AND (@EP IS NULL OR LTRIM(RTRIM(@EP)) = N'' OR RTRIM(V.EP) = LTRIM(RTRIM(@EP)))
          AND (
                @Filtro = N''
             OR V.EP LIKE N'%' + @Filtro + N'%'
             OR CAST(V.ID_TRANSACCION AS VARCHAR(20)) LIKE N'%' + @Filtro + N'%'
          )
    ),
    Base AS (
        SELECT
            EP AS [EP],
            CONVERT(VARCHAR(10), DtTxn, 23) AS [Fecha],
            TipoNom AS [Tipo],
            EstatusNom AS [Estatus],
            TipoPago AS [Tipo de pago],
            Cliente AS [Cliente],
            CONVERT(VARCHAR(32), Monto) AS [Monto],
            CONVERT(VARCHAR(32), Cambio) AS [Cambio],
            Folio AS [Folio],
            CambioIncompleto AS [Cambio Incompleto],
            CodigoError AS [Código de error],
            DN AS [DN],
            CodigoAut AS [Codigo Aut],
            NoTarjeta AS [No de Tarjeta],
            Referencia AS [Referencia],
            MotivoRechazo AS [Motivo Rechazo],
            NombreCliente AS [Nombre cliente],
            Color AS [Color],
            ReferenciaEP AS [Referencia EP],
            ID_TRANSACCION AS SortKey,
            COUNT(*) OVER () AS Total
        FROM Raw
    )
    SELECT
        [EP], [Fecha], [Tipo], [Estatus], [Tipo de pago], [Cliente], [Monto], [Cambio], [Folio],
        [Cambio Incompleto], [Código de error], [DN], [Codigo Aut], [No de Tarjeta], [Referencia],
        [Motivo Rechazo], [Nombre cliente], [Color], [Referencia EP], Total
    FROM Base
    ORDER BY SortKey
    OFFSET @Ignorar ROWS FETCH NEXT @Cantidad_Fila ROWS ONLY;
END;
GO

/* 4 — Campañas MKT (BD_CAMPANA + R_CAMPANA_EP, filtros de fecha activos) */
CREATE OR ALTER PROCEDURE dbo.SP_GET_REPORTE_CAMPANAS_MKT
    @FechaInicio DATE,
    @FechaFin DATE,
    @Ignorar INT = 0,
    @Cantidad_Fila INT = 500,
    @Filtro VARCHAR(150) = '',
    @Orden VARCHAR(50) = 'NOMBRE CAMPAÑA',
    @Dir VARCHAR(10) = 'ASC'
AS
BEGIN
    SET NOCOUNT ON;
    IF UPPER(@Dir) NOT IN ('ASC', 'DESC') SET @Dir = 'ASC';

    ;WITH Base AS (
        SELECT
            C.NOMBRE AS [NOMBRE CAMPAÑA],
            C.TIPO AS [TIPO],
            CONVERT(VARCHAR(10), C.FECHAINICIO, 23) AS [FECHA INICIO],
            CONVERT(VARCHAR(10), C.FECHATERMINO, 23) AS [FECHA FIN],
            C.ESTATUS AS [ESTATUS],
            ISNULL(D.Branch, RCE.EP) AS [ESTACIÓN DE PAGO],
            ISNULL(REG.REGION, ISNULL(D.REGION, N'')) AS [REGIÓN],
            C.ID AS SortId
        FROM dbo.BD_CAMPANA C
        INNER JOIN dbo.R_CAMPANA_EP RCE ON RCE.ID_CAMPANA = C.ID
        LEFT JOIN dbo.Device_Configuration D ON RTRIM(D.Mac_Address) = RTRIM(RCE.EP)
        LEFT JOIN dbo.Catalog_Locations loc ON loc.Id = D.Id_Location
        LEFT JOIN dbo.C_REGION REG ON REG.ID = loc.Id_Region
        WHERE C.FECHAINICIO <= @FechaFin
          AND ISNULL(C.FECHATERMINO, @FechaFin) >= @FechaInicio
          AND (
                @Filtro = N''
             OR C.NOMBRE LIKE N'%' + @Filtro + N'%'
             OR C.TIPO LIKE N'%' + @Filtro + N'%'
             OR RCE.EP LIKE N'%' + @Filtro + N'%'
          )
    ),
    Numerado AS (SELECT *, COUNT(*) OVER () AS Total FROM Base)
    SELECT [NOMBRE CAMPAÑA], [TIPO], [FECHA INICIO], [FECHA FIN], [ESTATUS], [ESTACIÓN DE PAGO], [REGIÓN], Total
    FROM Numerado
    ORDER BY SortId
    OFFSET @Ignorar ROWS FETCH NEXT @Cantidad_Fila ROWS ONLY;
END;
GO

/* 5 — Cierre de caja */
CREATE OR ALTER PROCEDURE dbo.SP_GET_REPORTE_CIERRE_CAJA
    @FechaInicio DATE,
    @FechaFin DATE,
    @Ignorar INT = 0,
    @Cantidad_Fila INT = 500,
    @Filtro VARCHAR(150) = '',
    @Orden VARCHAR(50) = 'FECHA',
    @Dir VARCHAR(10) = 'ASC'
AS
BEGIN
    SET NOCOUNT ON;
    IF UPPER(@Dir) NOT IN ('ASC', 'DESC') SET @Dir = 'ASC';

    IF OBJECT_ID(N'dbo.CierreCaja', N'U') IS NOT NULL
    BEGIN
        DECLARE @FinDt DATETIME = DATEADD(DAY, 1, CAST(@FechaFin AS DATETIME));
        ;WITH Src AS (
            SELECT cc.id, RTRIM(dc.Mac_Address) AS ID_ATT,
                ISNULL(R.REGION, ISNULL(dc.REGION, N'')) AS Region,
                ISNULL(dc.Branch, dc.Name) AS deviceName,
                CONVERT(VARCHAR(10), cc.Local_DT, 23) AS FECHA,
                CONVERT(VARCHAR(8), cc.Local_DT, 108) AS HORA,
                ISNULL(cc.referencia, N'') AS referencia,
                ISNULL(cc.banco, N'') AS banco,
                ISNULL(cc.cuenta, N'') AS cuenta,
                ISNULL(CAST(cc.efectivo AS VARCHAR(50)), N'0') AS efectivo,
                ISNULL(CAST(cc.tarjeta AS VARCHAR(50)), N'0') AS tarjeta,
                tav.Value, cta.AttributeID, cc.Local_DT AS SortDt
            FROM dbo.CierreCaja cc
            LEFT JOIN dbo.CierreCaja_Dispensed_Attributes_Values tav ON tav.Transaction_FieldKey = cc.Field_Key
            LEFT JOIN dbo.Catalog_CC_Dispensed_Attributes cta ON cta.AttributeID = tav.Attribute_ID
            LEFT JOIN dbo.Device_Configuration dc ON dc.Id = cc.Id_Device
            LEFT JOIN dbo.Catalog_Locations loc ON dc.Id_Location = loc.Id
            LEFT JOIN dbo.C_REGION R ON R.ID = loc.Id_Region
            WHERE cc.Local_DT >= @FechaInicio AND cc.Local_DT < @FinDt AND ISNULL(cc.Id_Status, 0) = 0
        ),
        Piv AS (
            SELECT ID_ATT, Region, deviceName, FECHA, HORA, referencia, banco, cuenta, efectivo, tarjeta, SortDt,
                MAX(CASE WHEN AttributeID = 0 THEN Value END) AS remCass1,
                MAX(CASE WHEN AttributeID = 1 THEN Value END) AS dispCass1,
                MAX(CASE WHEN AttributeID = 2 THEN Value END) AS rechCass1,
                MAX(CASE WHEN AttributeID = 3 THEN Value END) AS remCass2,
                MAX(CASE WHEN AttributeID = 4 THEN Value END) AS dispCass2,
                MAX(CASE WHEN AttributeID = 5 THEN Value END) AS rechCass2,
                MAX(CASE WHEN AttributeID = 6 THEN Value END) AS remCass3,
                MAX(CASE WHEN AttributeID = 7 THEN Value END) AS dispCass3,
                MAX(CASE WHEN AttributeID = 8 THEN Value END) AS rechCass3,
                MAX(CASE WHEN AttributeID = 9 THEN Value END) AS MXN20,
                MAX(CASE WHEN AttributeID = 10 THEN Value END) AS MXN50,
                MAX(CASE WHEN AttributeID = 11 THEN Value END) AS MXN100,
                MAX(CASE WHEN AttributeID = 12 THEN Value END) AS MXN200,
                MAX(CASE WHEN AttributeID = 13 THEN Value END) AS MXN500,
                MAX(CASE WHEN AttributeID = 14 THEN Value END) AS MXN1000
            FROM Src
            GROUP BY ID_ATT, Region, deviceName, FECHA, HORA, referencia, banco, cuenta, efectivo, tarjeta, SortDt, id
        ),
        Final AS (
            SELECT FECHA, HORA, Region AS [REGION], ID_ATT AS [ID], deviceName AS [ESTACIÓN DE PAGO],
                referencia AS [REFERENCIA], banco AS [BANCO], cuenta AS [CUENTA],
                efectivo AS [EFECTIVO], tarjeta AS [TARJETA],
                ISNULL(remCass1, N'0') AS [CASETERO 1 REM], ISNULL(dispCass1, N'0') AS [CASETERO 1 DISP], ISNULL(rechCass1, N'0') AS [CASETERO 1 RECH],
                ISNULL(remCass2, N'0') AS [CASETERO 2 REM], ISNULL(dispCass2, N'0') AS [CASETERO 2 DISP], ISNULL(rechCass2, N'0') AS [CASETERO 2 RECH],
                ISNULL(remCass3, N'0') AS [CASETERO 3 REM], ISNULL(dispCass3, N'0') AS [CASETERO 3 DISP], ISNULL(rechCass3, N'0') AS [CASETERO 3 RECH],
                ISNULL(MXN20, N'0') AS [MXN20], ISNULL(MXN50, N'0') AS [MXN50], ISNULL(MXN100, N'0') AS [MXN100],
                ISNULL(MXN200, N'0') AS [MXN200], ISNULL(MXN500, N'0') AS [MXN500], ISNULL(MXN1000, N'0') AS [MXN1000],
                SortDt, COUNT(*) OVER () AS Total
            FROM Piv
            WHERE @Filtro = N'' OR ID_ATT LIKE N'%' + @Filtro + N'%' OR deviceName LIKE N'%' + @Filtro + N'%'
        )
        SELECT [FECHA], [HORA], [REGION], [ID], [ESTACIÓN DE PAGO], [REFERENCIA], [BANCO], [CUENTA],
            [EFECTIVO], [TARJETA],
            [CASETERO 1 REM], [CASETERO 1 DISP], [CASETERO 1 RECH],
            [CASETERO 2 REM], [CASETERO 2 DISP], [CASETERO 2 RECH],
            [CASETERO 3 REM], [CASETERO 3 DISP], [CASETERO 3 RECH],
            [MXN20], [MXN50], [MXN100], [MXN200], [MXN500], [MXN1000], Total
        FROM Final ORDER BY SortDt
        OFFSET @Ignorar ROWS FETCH NEXT @Cantidad_Fila ROWS ONLY;
        RETURN;
    END;

    /* QA Monitor remedido: sin CierreCaja — contadores + txn (paridad SP_GET_REPORTEDECONTADORES join Id) */
    ;WITH Cont AS (
        SELECT CON.*,
            TRY_CONVERT(DATETIME2(0), CON.FECHA_REGISTRO, 120) AS DtReg,
            ROW_NUMBER() OVER (
                PARTITION BY CON.ID_CAJERO
                ORDER BY TRY_CONVERT(DATETIME2(0), CON.FECHA_REGISTRO, 120) DESC
            ) AS rn
        FROM dbo.ATM_CONTADORES CON
        WHERE TRY_CONVERT(DATE, CON.FECHA_REGISTRO, 120) BETWEEN @FechaInicio AND @FechaFin
    ),
    Ult AS (SELECT * FROM Cont WHERE rn = 1),
    Tx AS (
        SELECT V.EP,
            SUM(CASE WHEN UPPER(ISNULL(V.TIPO_PAGO, N'')) LIKE N'%EFE%' THEN ISNULL(V.MONTO, 0) ELSE 0 END) AS Efectivo,
            SUM(CASE WHEN UPPER(ISNULL(V.TIPO_PAGO, N'')) LIKE N'%TAR%' THEN ISNULL(V.MONTO, 0) ELSE 0 END) AS Tarjeta
        FROM dbo.VW_BD_TRANSACCIONES_CONCILIACION V
        WHERE CAST(V.FECHA_DT AS DATE) BETWEEN @FechaInicio AND @FechaFin
        GROUP BY V.EP
    ),
    Base AS (
        SELECT
            CONVERT(VARCHAR(10), U.DtReg, 23) AS [FECHA],
            CONVERT(VARCHAR(8), U.DtReg, 108) AS [HORA],
            ISNULL(REG.REGION, ISNULL(D.REGION, N'')) AS [REGION],
            ISNULL(RTRIM(D.Mac_Address), U.ID_CAJERO) AS [ID],
            ISNULL(D.Branch, N'') AS [ESTACIÓN DE PAGO],
            ISNULL(TX.EP, N'') AS [REFERENCIA],
            N'' AS [BANCO],
            ISNULL(CAST(D.CUENTA AS VARCHAR(50)), N'') AS [CUENTA],
            ISNULL(CAST(TX.Efectivo AS VARCHAR(50)), N'0') AS [EFECTIVO],
            ISNULL(CAST(TX.Tarjeta AS VARCHAR(50)), N'0') AS [TARJETA],
            ISNULL(CAST(U.REM_C1 AS VARCHAR(20)), N'0') AS [CASETERO 1 REM],
            ISNULL(CAST(U.DISP_C1 AS VARCHAR(20)), N'0') AS [CASETERO 1 DISP],
            ISNULL(CAST(U.RECH_C1 AS VARCHAR(20)), N'0') AS [CASETERO 1 RECH],
            ISNULL(CAST(U.REM_C2 AS VARCHAR(20)), N'0') AS [CASETERO 2 REM],
            ISNULL(CAST(U.DISP_C2 AS VARCHAR(20)), N'0') AS [CASETERO 2 DISP],
            ISNULL(CAST(U.RECH_C2 AS VARCHAR(20)), N'0') AS [CASETERO 2 RECH],
            ISNULL(CAST(U.REM_C3 AS VARCHAR(20)), N'0') AS [CASETERO 3 REM],
            ISNULL(CAST(U.DISP_C3 AS VARCHAR(20)), N'0') AS [CASETERO 3 DISP],
            ISNULL(CAST(U.RECH_C3 AS VARCHAR(20)), N'0') AS [CASETERO 3 RECH],
            ISNULL(CAST(U.CDOM_C1 AS VARCHAR(20)), N'0') AS [MXN20],
            ISNULL(CAST(U.CDOM_C2 AS VARCHAR(20)), N'0') AS [MXN50],
            ISNULL(CAST(U.CDOM_C3 AS VARCHAR(20)), N'0') AS [MXN100],
            ISNULL(CAST(U.CDOM_C4 AS VARCHAR(20)), N'0') AS [MXN200],
            N'0' AS [MXN500], N'0' AS [MXN1000],
            U.ID AS SortKey
        FROM Ult U
        LEFT JOIN dbo.Device_Configuration D
            ON CAST(D.Id AS VARCHAR(50)) = RTRIM(U.ID_CAJERO) OR RTRIM(D.Mac_Address) = RTRIM(U.ID_CAJERO)
        LEFT JOIN dbo.Catalog_Locations loc ON loc.Id = D.Id_Location
        LEFT JOIN dbo.C_REGION REG ON REG.ID = loc.Id_Region
        LEFT JOIN Tx TX ON TX.EP = D.Mac_Address
        WHERE @Filtro = N''
           OR ISNULL(D.Mac_Address, U.ID_CAJERO) LIKE N'%' + @Filtro + N'%'
           OR ISNULL(D.Branch, N'') LIKE N'%' + @Filtro + N'%'
    ),
    Numerado AS (SELECT *, COUNT(*) OVER () AS Total FROM Base)
    SELECT [FECHA], [HORA], [REGION], [ID], [ESTACIÓN DE PAGO], [REFERENCIA], [BANCO], [CUENTA],
        [EFECTIVO], [TARJETA],
        [CASETERO 1 REM], [CASETERO 1 DISP], [CASETERO 1 RECH],
        [CASETERO 2 REM], [CASETERO 2 DISP], [CASETERO 2 RECH],
        [CASETERO 3 REM], [CASETERO 3 DISP], [CASETERO 3 RECH],
        [MXN20], [MXN50], [MXN100], [MXN200], [MXN500], [MXN1000], Total
    FROM Numerado ORDER BY SortKey
    OFFSET @Ignorar ROWS FETCH NEXT @Cantidad_Fila ROWS ONLY;
END;
GO

/* 6 — Contadores bitácora (join remedido: ID_CAJERO = Device.Id) */
CREATE OR ALTER PROCEDURE dbo.SP_GET_REPORTE_CONTADORES
    @FechaInicio DATE,
    @FechaFin DATE,
    @Ignorar INT = 0,
    @Cantidad_Fila INT = 500,
    @Filtro VARCHAR(150) = '',
    @Orden VARCHAR(50) = 'FECHA',
    @Dir VARCHAR(10) = 'ASC'
AS
BEGIN
    SET NOCOUNT ON;
    IF UPPER(@Dir) NOT IN ('ASC', 'DESC') SET @Dir = 'ASC';

    ;WITH Base AS (
        SELECT
            CONVERT(VARCHAR(10), TRY_CONVERT(DATETIME2(0), CON.FECHA_REGISTRO, 120), 23) AS [FECHA],
            CONVERT(VARCHAR(8), TRY_CONVERT(DATETIME2(0), CON.FECHA_REGISTRO, 120), 108) AS [HORA],
            ISNULL(REG.REGION, ISNULL(DEV.REGION, N'')) AS [REGION],
            ISNULL(RTRIM(DEV.Mac_Address), RTRIM(CON.ID_CAJERO)) AS [ID],
            ISNULL(DEV.Branch, N'') AS [ESTACIÓN DE PAGO],
            ISNULL(CAST(CON.DISP_C1 AS VARCHAR(20)), N'0') AS [Dispensados 20],
            ISNULL(CAST(CON.DISP_C2 AS VARCHAR(20)), N'0') AS [Dispensados 50],
            ISNULL(CAST(CON.DISP_C3 AS VARCHAR(20)), N'0') AS [Dispensados 100],
            ISNULL(CAST(CON.DISP_C4 AS VARCHAR(20)), N'0') AS [Dispensados 200],
            ISNULL(CAST(CON.REM_C1 AS VARCHAR(20)), N'0') AS [Remanentes 20],
            ISNULL(CAST(CON.REM_C2 AS VARCHAR(20)), N'0') AS [Remanentes 50],
            ISNULL(CAST(CON.REM_C3 AS VARCHAR(20)), N'0') AS [Remanentes 100],
            ISNULL(CAST(CON.REM_C4 AS VARCHAR(20)), N'0') AS [Remanentes 200],
            ISNULL(CAST(CON.CDOM_C1 AS VARCHAR(20)), N'0') AS [Aceptados 20],
            ISNULL(CAST(CON.CDOM_C2 AS VARCHAR(20)), N'0') AS [Aceptados 50],
            ISNULL(CAST(CON.CDOM_C3 AS VARCHAR(20)), N'0') AS [Aceptados 100],
            ISNULL(CAST(CON.CDOM_C4 AS VARCHAR(20)), N'0') AS [Aceptados 200],
            N'0' AS [Aceptados 500],
            N'0' AS [Aceptados 1000],
            CON.ID AS SortKey
        FROM dbo.ATM_CONTADORES CON
        LEFT JOIN dbo.Device_Configuration DEV
            ON CAST(DEV.Id AS VARCHAR(50)) = RTRIM(CON.ID_CAJERO)
        LEFT JOIN dbo.Catalog_Locations loc ON loc.Id = DEV.Id_Location
        LEFT JOIN dbo.C_REGION REG ON REG.ID = loc.Id_Region
        WHERE TRY_CONVERT(DATE, CON.FECHA_REGISTRO, 120) BETWEEN @FechaInicio AND @FechaFin
          AND (
                @Filtro = N''
             OR DEV.Branch LIKE N'%' + @Filtro + N'%'
             OR DEV.Mac_Address LIKE N'%' + @Filtro + N'%'
             OR CON.ID_CAJERO LIKE N'%' + @Filtro + N'%'
          )
    ),
    Numerado AS (SELECT *, COUNT(*) OVER () AS Total FROM Base)
    SELECT [FECHA], [HORA], [REGION], [ID], [ESTACIÓN DE PAGO],
        [Dispensados 20], [Dispensados 50], [Dispensados 100], [Dispensados 200],
        [Remanentes 20], [Remanentes 50], [Remanentes 100], [Remanentes 200],
        [Aceptados 20], [Aceptados 50], [Aceptados 100], [Aceptados 200], [Aceptados 500], [Aceptados 1000],
        Total
    FROM Numerado ORDER BY SortKey
    OFFSET @Ignorar ROWS FETCH NEXT @Cantidad_Fila ROWS ONLY;
END;
GO

PRINT N'OK: reportes bitácora (6 tipos) — base QA_MONITOR_ATT_remediado.sql';
GO

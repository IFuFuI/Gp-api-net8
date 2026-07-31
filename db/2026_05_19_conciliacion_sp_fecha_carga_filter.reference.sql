/*
  SP_GET_CONCILIACION_CARGA — filtro por rango de FECHA_CARGA (inclusive).
  Aplicar en QA/producción según proceso de despliegue del equipo.

  Origen: QA_MONITOR_ATT_remediado.sql (sin filtro de fechas en FILTRADAS).
*/
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [dbo].[SP_GET_CONCILIACION_CARGA]
(
    @Ignorar        INT = 0,
    @Cantidad_Fila  INT = 10,
    @Filtro         VARCHAR(150) = '',
    @Orden          VARCHAR(50) = 'ID_CARGA',
    @Dir            VARCHAR(10) = 'asc',
    @FechaInicio    VARCHAR(10) = NULL,
    @FechaFin       VARCHAR(10) = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @JsonResult NVARCHAR(MAX);
    DECLARE @Ini DATE = TRY_CONVERT(DATE, NULLIF(LTRIM(RTRIM(@FechaInicio)), ''), 23);
    DECLARE @Fin DATE = TRY_CONVERT(DATE, NULLIF(LTRIM(RTRIM(@FechaFin)), ''), 23);

    ;WITH CARGAS AS
    (
        SELECT
            ISNULL(C.ID_CARGA, 0) AS ID_CARGA,
            ISNULL(C.NOMBRE_ARCHIVO, '-') AS NOMBRE_ARCHIVO,
            ISNULL(C.HASH_ARCHIVO, '-') AS HASH_ARCHIVO,
            ISNULL(C.USUARIO_CARGA, '-') AS USUARIO_CARGA,
            ISNULL(C.FECHA_CARGA, '1900-01-01') AS FECHA_CARGA,
            ISNULL(C.ESTATUS_CARGA, '-') AS ESTATUS_CARGA,
            ISNULL(C.ES_REPROCESO, 0) AS ES_REPROCESO,
            ISNULL(C.ID_CARGA_ORIGEN, 0) AS ID_CARGA_ORIGEN,
            ISNULL(C.TOTAL_REGISTROS, 0) AS TOTAL_REGISTROS,
            ISNULL(C.TOTAL_CONCILIADOS, 0) AS TOTAL_CONCILIADOS,
            ISNULL(C.TOTAL_SOLO_AUTOPAGO, 0) AS TOTAL_SOLO_AUTOPAGO,
            ISNULL(C.TOTAL_CANCELADOS, 0) AS TOTAL_CANCELADOS,
            ISNULL(C.TOTAL_NO_PROCESABLES, 0) AS TOTAL_NO_PROCESABLES,
            ISNULL(C.TOTAL_DUPLICADOS_LLAVE, 0) AS TOTAL_DUPLICADOS_LLAVE,
            ISNULL(C.OBSERVACION, '-') AS OBSERVACION
        FROM BD_AUTOPAGO_CARGA C
    ),
    FILTRADAS AS
    (
        SELECT *
        FROM CARGAS
        WHERE
            (@Ini IS NULL OR CAST(FECHA_CARGA AS DATE) >= @Ini)
            AND (@Fin IS NULL OR CAST(FECHA_CARGA AS DATE) <= @Fin)
            AND (
                NULLIF(LTRIM(RTRIM(@Filtro)), '') IS NULL
                OR CAST(ID_CARGA AS VARCHAR(50)) LIKE '%' + @Filtro + '%'
                OR NOMBRE_ARCHIVO LIKE '%' + @Filtro + '%'
                OR HASH_ARCHIVO LIKE '%' + @Filtro + '%'
                OR USUARIO_CARGA LIKE '%' + @Filtro + '%'
                OR CONVERT(VARCHAR(30), FECHA_CARGA, 120) LIKE '%' + @Filtro + '%'
                OR ESTATUS_CARGA LIKE '%' + @Filtro + '%'
                OR CAST(ES_REPROCESO AS VARCHAR(10)) LIKE '%' + @Filtro + '%'
                OR CAST(ID_CARGA_ORIGEN AS VARCHAR(50)) LIKE '%' + @Filtro + '%'
                OR OBSERVACION LIKE '%' + @Filtro + '%'
            )
    ),
    TOTAL AS
    (
        SELECT COUNT(*) AS Total
        FROM FILTRADAS
    )
    SELECT
        @JsonResult =
        (
            SELECT
                R.*,
                T.Total
            FROM FILTRADAS R
            CROSS JOIN TOTAL T
            ORDER BY
                CASE WHEN @Orden = 'ID_CARGA' AND @Dir = 'asc' THEN R.ID_CARGA END ASC,
                CASE WHEN @Orden = 'ID_CARGA' AND @Dir = 'desc' THEN R.ID_CARGA END DESC,
                CASE WHEN @Orden = 'NOMBRE_ARCHIVO' AND @Dir = 'asc' THEN R.NOMBRE_ARCHIVO END ASC,
                CASE WHEN @Orden = 'NOMBRE_ARCHIVO' AND @Dir = 'desc' THEN R.NOMBRE_ARCHIVO END DESC,
                CASE WHEN @Orden = 'USUARIO_CARGA' AND @Dir = 'asc' THEN R.USUARIO_CARGA END ASC,
                CASE WHEN @Orden = 'USUARIO_CARGA' AND @Dir = 'desc' THEN R.USUARIO_CARGA END DESC,
                CASE WHEN @Orden = 'FECHA_CARGA' AND @Dir = 'asc' THEN R.FECHA_CARGA END ASC,
                CASE WHEN @Orden = 'FECHA_CARGA' AND @Dir = 'desc' THEN R.FECHA_CARGA END DESC,
                CASE WHEN @Orden = 'ESTATUS_CARGA' AND @Dir = 'asc' THEN R.ESTATUS_CARGA END ASC,
                CASE WHEN @Orden = 'ESTATUS_CARGA' AND @Dir = 'desc' THEN R.ESTATUS_CARGA END DESC,
                CASE WHEN @Orden = 'TOTAL_REGISTROS' AND @Dir = 'asc' THEN R.TOTAL_REGISTROS END ASC,
                CASE WHEN @Orden = 'TOTAL_REGISTROS' AND @Dir = 'desc' THEN R.TOTAL_REGISTROS END DESC
            OFFSET @Ignorar ROWS
            FETCH NEXT @Cantidad_Fila ROWS ONLY
            FOR JSON PATH
        );

    SELECT ISNULL(@JsonResult, '[]') AS JsonResult;
END;
GO

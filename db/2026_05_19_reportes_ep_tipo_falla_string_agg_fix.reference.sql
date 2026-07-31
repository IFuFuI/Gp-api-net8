/*
  Fix error 9829 en reporte EP activas:
  STRING_AGG aggregation result exceeded the limit of 8000 bytes.

  Aplica en QA_MONITOR_ATT y vuelva a generar el reporte #1.
*/

USE [QA_MONITOR_ATT];
GO

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

PRINT N'OK: FN_REPORTE_EP_TIPO_FALLA actualizada (NVARCHAR(MAX) + último estado por dispositivo).';
GO

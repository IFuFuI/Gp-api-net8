/*
  Ruta en cajero para solicitar / enviar archivos (Monitor + FileReadingATT)

  Consumido por:
    - POST api/DetalleAtm/INSERT_COMANDO_ATM  (@URL opcional)
    - SP_GET_COMANDO_ATM → JSON { ID_SOLICITUD, COMANDO, URL }
    - FileReadingATT: REQUESTFILE (subir archivo) / DOWNLOAD (desplegar en RUTA_CAJERO)

  Tabla: dbo.BD_COMANDOS_ATMS (esquema QA_MONITOR_ATT_remediado.sql)

  Aplicar manualmente en QA_MONITOR_ATT (y luego en producción).
*/

USE [QA_MONITOR_ATT];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/* --------------------------------------------------------------------------
   1) Columna para ruta en cajero (evita truncar rutas largas en COMANDO)
   -------------------------------------------------------------------------- */
IF COL_LENGTH(N'dbo.BD_COMANDOS_ATMS', N'RUTA_CAJERO') IS NULL
BEGIN
    ALTER TABLE dbo.BD_COMANDOS_ATMS
        ADD RUTA_CAJERO NVARCHAR(500) NULL;
END
GO

/* Ampliar COMANDO para comandos tipo GET EJDATA_yyyymmdd.LOG (opcional) */
IF EXISTS (
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.BD_COMANDOS_ATMS')
      AND c.name = N'COMANDO'
      AND t.name = N'varchar'
      AND c.max_length < 300
)
BEGIN
    ALTER TABLE dbo.BD_COMANDOS_ATMS
        ALTER COLUMN COMANDO VARCHAR(200) NULL;
END
GO

/* --------------------------------------------------------------------------
   2) INSERT: guardar COMANDO + RUTA_CAJERO (@URL)
   -------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.SP_INSERT_COMANDO_ATM', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.SP_INSERT_COMANDO_ATM AS SET NOCOUNT ON;');
GO

ALTER PROCEDURE [dbo].[SP_INSERT_COMANDO_ATM]
    @ID_CAJERO   VARCHAR(50),
    @ID_PAQUETE  INT,
    @COMANDO     VARCHAR(200),
    @ID_USUARIO  VARCHAR(100) = NULL,
    @URL         NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE
        @ResultInt    INT,
        @ResultString NVARCHAR(255),
        @ComandoBase  VARCHAR(200),
        @Ruta         NVARCHAR(500),
        @PipePos      INT;

    IF @ID_USUARIO IS NULL
        SET @ID_USUARIO = N'Admin';

    SET @ComandoBase = LTRIM(RTRIM(ISNULL(@COMANDO, N'')));
    SET @Ruta = NULLIF(LTRIM(RTRIM(ISNULL(@URL, N''))), N'');

    /* Compatibilidad API: COMANDO|RUTA en un solo parámetro */
    SET @PipePos = CHARINDEX(N'|', @ComandoBase);
    IF @PipePos > 0
    BEGIN
        IF @Ruta IS NULL
            SET @Ruta = NULLIF(LTRIM(RTRIM(SUBSTRING(@ComandoBase, @PipePos + 1, 500))), N'');
        SET @ComandoBase = LTRIM(RTRIM(LEFT(@ComandoBase, @PipePos - 1)));
    END

    INSERT INTO dbo.BD_COMANDOS_ATMS (
        ID_CAJERO,
        COMANDO,
        FECHA,
        STATUS,
        Usuario,
        FechaFin,
        RUTA_CAJERO
    )
    VALUES (
        @ID_CAJERO,
        @ComandoBase,
        GETDATE(),
        N'NO_PROCESADO',
        @ID_USUARIO,
        NULL,
        @Ruta
    );

    SET @ResultInt = SCOPE_IDENTITY();
    SET @ResultString = N'Comando insertado correctamente.';

    SELECT
        @ResultInt    AS ResultInt,
        @ResultString AS ResultString;
END;
GO

/* --------------------------------------------------------------------------
   3) GET: devolver COMANDO limpio + URL desde RUTA_CAJERO (o pipe legado)
   -------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.SP_GET_COMANDO_ATM', N'P') IS NULL
    EXEC(N'CREATE PROCEDURE dbo.SP_GET_COMANDO_ATM AS SET NOCOUNT ON;');
GO

ALTER PROCEDURE [dbo].[SP_GET_COMANDO_ATM]
    @ID_CAJERO VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @JsonResult NVARCHAR(MAX);

    ;WITH CTE AS (
        SELECT TOP (1)
            ID_SOLICITUD,
            COMANDO,
            RUTA_CAJERO
        FROM dbo.BD_COMANDOS_ATMS
        WHERE ID_CAJERO = @ID_CAJERO
          AND STATUS = N'NO_PROCESADO'
        ORDER BY FECHA ASC
    ),
    Normalized AS (
        SELECT
            ID_SOLICITUD,
            CASE
                WHEN CHARINDEX(N'|', COMANDO) > 0
                THEN LTRIM(RTRIM(LEFT(COMANDO, CHARINDEX(N'|', COMANDO) - 1)))
                ELSE LTRIM(RTRIM(COMANDO))
            END AS COMANDO_BASE,
            COALESCE(
                NULLIF(LTRIM(RTRIM(RUTA_CAJERO)), N''),
                CASE
                    WHEN CHARINDEX(N'|', COMANDO) > 0
                    THEN NULLIF(LTRIM(RTRIM(SUBSTRING(COMANDO, CHARINDEX(N'|', COMANDO) + 1, 500))), N'')
                    ELSE NULL
                END
            ) AS URL_PATH
        FROM CTE
    )
    SELECT @JsonResult = (
        SELECT
            ID_SOLICITUD,
            COMANDO_BASE AS COMANDO,
            URL_PATH AS URL
        FROM Normalized
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    );

    IF @JsonResult IS NULL
        SET @JsonResult = N'{}';

    SELECT @JsonResult AS resultado_json;
END;
GO

PRINT N'2026_05_19_comando_atm_url: BD_COMANDOS_ATMS.RUTA_CAJERO + SP_INSERT_COMANDO_ATM(@URL) + SP_GET_COMANDO_ATM(JSON con URL).';
GO

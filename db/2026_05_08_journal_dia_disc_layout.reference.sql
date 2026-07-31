/*
  Referencia: journal diario en disco (ATT.Monitor.Api)
  ------------------------------------------------------------------
  Ruta base: FileStorage:JournalDiaPath (p. ej. C:\ArchivosATM\Journaldiario)

  Layout recomendado (extracción tras SubirZip / proceso especial):
    {JournalDiaPath}\{EP}\{lote}\**archivos**
    - EP: identificador de estación (carpeta por equipo).
    - lote: nombre del ZIP destino sin extensión (agrupa un envío).

  Layout legado (compatibilidad listado/descarga):
    {JournalDiaPath}\{EP}_*   (archivos sueltos en la raíz del journal)

  API:
    - POST api/JournalHistorico/listar   (JWT): filtra por EP + rango fechas (UTC) + paginación.
    - POST api/JournalHistorico/descargar (JWT): ZIP por ids devueltos en el listado.

  Límites típicos (appsettings sección JournalHistorico):
    - MaxRangeDays, MaxListScanFiles, MaxDownloadSelection,
      UnifiedPartMaxBytes, MaxSourceFileReadBytes

  No se crean tablas ni SP para este módulo: el índice operativo es el sistema de archivos.
*/

SELECT 1 AS referencia_ok;

namespace ATT.Monitor.Api.Services.Reportes;

/// <summary>Columnas de exportación (plantilla V2 + ajustes producto) por <c>IdReporte</c>.</summary>
internal static class ReporteExportColumnRegistry
{
    private static readonly string[] TxErrorColumns =
    [
        "ID", "ESTACION DE PAGO", "REGION", "FORMATO DE TIENDA", "CRITERIO DE PAGO",
        "FECHA", "HORA", "CUENTA", "DN", "TICKET", "NOMBRE CLIENTE", "TIPO DE OPERACION",
        "FORMA DE PAGO", "MONTO PAGADO", "MONTO INGRESADO", "CAMBIO ENTREGADO",
        "CAMBIO PENDIENTE", "MONTO REVERSADO","ESTATUS", "CODIGO ERROR", "COD AUTORIZACION", "NUM TARJETA",
        "ESTATUS APROBACION", "REFERENCIA"
    ];

    private static readonly string[] TxEquipoColumns =
    [
        "ID", "ESTACION DE PAGO", "REGION", "FORMATO DE TIENDA", "METODO DE PAGO",
        "FECHA", "HORA", "CUENTA", "DN", "TICKET", "NOMBRE CLIENTE", "TIPO DE OPERACION",
        "FORMA DE PAGO", "MONTO PAGADO", "MONTO INGRESADO", "CAMBIO ENTREGADO",
        "CAMBIO PENDIENTE", "MONTO REVERSADO","ESTATUS", "CODIGO ERROR", "COD AUTORIZACION", "NUM TARJETA",
        "ESTATUS APROBACION", "REFERENCIA"
    ];

    private static readonly string[] TxDiariasColumns =
    [
        "ID", "ESTACION DE PAGO", "REGION", "FORMATO DE TIENDA", "CRITERIO DE PAGO",
        "FECHA", "HORA", "CUENTA", "DN", "TICKET", "NOMBRE CLIENTE", "TIPO DE OPERACION",
        "FORMA DE PAGO", "MONTO PAGADO", "MONTO INGRESADO", "CAMBIO ENTREGADO",
        "CAMBIO PENDIENTE", "MONTO REVERSADO","ESTATUS", "CODIGO ERROR", "COD AUTORIZACION", "NUM TARJETA",
        "ESTATUS APROBACION", "REFERENCIA"
    ];

    private static readonly IReadOnlyDictionary<int, IReadOnlyList<string>> ColumnsByReport =
        new Dictionary<int, IReadOnlyList<string>>
        {
            [1] =
            [
                "ID", "ESTACION DE PAGO", "REGION", "FORMATO DE TIENDA", "CRITERIO DE PAGO",
                "DIRECCION", "VERSION APLICATIVO", "SERIE BANK", "PSW BANK", "S.O", "HOST NAME",
                "MEMORIA", "SERIE EP", "VERSION TEMPLATE", "IP", "MODELO", "STATUS CAJA",
                "ACEPTADOR", "DISPENSADOR", "IMPRESORA", "PIN PAD", "ESTATUS SW", "CODI", "EFE",
                "TAR", "URL BANK", "URL WEBSERVICE", "FECHA DE ULTIMA ACT"
            ],
            [2] = TxErrorColumns,
            [3] = TxEquipoColumns,
            [4] =
            [
                "ID", "ESTACION DE PAGO", "REGION", "FORMATO DE TIENDA", "CRITERIO DE PAGO",
                "NOMBRE CAMPANA", "TIPO", "FECHA INICIO", "FECHA TERMINO", "FECHA CREACION",
                "ESTATUS"
            ],
            [5] =
            [
                "ID", "ESTACION DE PAGO", "REGION", "FORMATO DE TIENDA", "CRITERIO DE PAGO",
                "FECHA", "HORA", "REFERENCIA", "BANCO", "CUENTA", "EFECTIVO", "TARJETA", "CODI",
                "CASETERO 1 REM", "CASETERO 1 DISP", "CASETERO 1 RECH",
                "CASETERO 2 REM", "CASETERO 2 DISP", "CASETERO 2 RECH",
                "CASETERO 3 REM", "CASETERO 3 DISP", "CASETERO 3 RECH",
                "MXN20", "MXN50", "MXN100", "MXN200", "MXN500", "MXN1000"
            ],
            [6] =
            [
                "ID", "ESTACION DE PAGO", "REGION", "FORMATO DE TIENDA", "CRITERIO DE PAGO",
                "DISPENSADO $20", "DISPENSADO $50", "DISPENSADO $100",
                "REMANENTE $20", "REMANENTE $50", "REMANENTE $100",
                "RECHAZADO $20", "RECHAZADO $50", "RECHAZADO $100",
                "ACEPTADOS $20", "ACEPTADOS $50", "ACEPTADOS $100", "ACEPTADOS $200",
                "ACEPTADOS $500", "ACEPTADOS $1000", "EFECTIVO"
            ],
            [7] = TxDiariasColumns
        };

    public static bool TryGetExportColumns(int idReporte, out IReadOnlyList<string> columns) =>
        ColumnsByReport.TryGetValue(idReporte, out columns!);
}

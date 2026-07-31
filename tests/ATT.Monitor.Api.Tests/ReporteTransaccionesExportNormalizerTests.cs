using ATT.Monitor.Api.Services.Reportes;
using Xunit;

namespace ATT.Monitor.Api.Tests;

public sealed class ReporteTransaccionesExportNormalizerTests
{
    [Fact]
    public void Apply_excluye_reinicio_estacion_de_pago()
    {
        var rows = new List<Dictionary<string, string>>
        {
            Row(("ESTATUS", "REINICIO ESTACION DE PAGO"), ("FOLIO", "1")),
            Row(("ESTATUS", "REINICIO ESTACIÓN DE PAGO"), ("FOLIO", "2")),
            Row(("ESTATUS", "OK"), ("FOLIO", "3"))
        };

        ReporteTransaccionesExportNormalizer.Apply(7, rows);

        Assert.Single(rows);
        Assert.Equal("3", rows[0]["FOLIO"]);
    }

    [Fact]
    public void Apply_excluye_reinicios_de_aplicativo()
    {
        var rows = new List<Dictionary<string, string>>
        {
            Row(("ESTATUS", "Reinicios de Aplicativo"), ("FOLIO", "1")),
            Row(("ESTATUS", "OK"), ("FOLIO", "2"))
        };

        ReporteTransaccionesExportNormalizer.Apply(2, rows);

        Assert.Single(rows);
        Assert.Equal("2", rows[0]["FOLIO"]);
    }

    [Fact]
    public void Apply_billete_atorado_normaliza_estatus_y_codigo()
    {
        var row = Row(("ESTATUS", "Billete Atorado en dispensador"), ("CODIGO ERROR", "X"));

        ReporteTransaccionesExportNormalizer.Apply(2, new List<Dictionary<string, string>> { row });

        Assert.Equal("Billete atorado", row["ESTATUS"]);
        Assert.Equal("111:213", row["CODIGO ERROR"]);
    }

    [Fact]
    public void Apply_deduplica_folios_en_fallas()
    {
        var rows = new List<Dictionary<string, string>>
        {
            Row(("FOLIO", "F100"), ("ESTATUS", "Error"), ("CODIGO ERROR", "10"), ("SortKey", "1")),
            Row(("FOLIO", "F100"), ("ESTATUS", "Error"), ("CODIGO ERROR", "10"), ("SortKey", "2"))
        };

        ReporteTransaccionesExportNormalizer.Apply(2, rows);

        Assert.Single(rows);
    }

    [Fact]
    public void SortPorEquipo_respeta_orden_de_ep_seleccionadas()
    {
        var rows = new List<Dictionary<string, string>>
        {
            Row(("ID", "EP-B"), ("FECHA", "01/01/2026"), ("HORA", "10:00:00")),
            Row(("ID", "EP-A"), ("FECHA", "02/01/2026"), ("HORA", "09:00:00")),
            Row(("ID", "EP-C"), ("FECHA", "01/01/2026"), ("HORA", "08:00:00"))
        };

        ReporteTransaccionesExportNormalizer.SortPorEquipo(rows, ["EP-C", "EP-A", "EP-B"]);

        Assert.Equal("EP-C", rows[0]["ID"]);
        Assert.Equal("EP-A", rows[1]["ID"]);
        Assert.Equal("EP-B", rows[2]["ID"]);
    }

    private static Dictionary<string, string> Row(params (string Key, string Value)[] cells)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in cells)
            d[key] = value;
        return d;
    }
}

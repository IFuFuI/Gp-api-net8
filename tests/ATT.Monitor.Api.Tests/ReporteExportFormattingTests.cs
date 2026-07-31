using ATT.Monitor.Api.Services.Reportes;
using Xunit;

namespace ATT.Monitor.Api.Tests;

public sealed class ReporteExportValueFormatterTests
{
    [Theory]
    [InlineData("CUENTA", "0012345678", "0012345678")]
    [InlineData("DN", "05512345678", "05512345678")]
    [InlineData("TICKET", "ABC123", "ABC123")]
    public void FormatForExport_columnas_texto_sin_parseo_numerico(string header, string raw, string expected)
    {
        Assert.Equal(expected, ReporteExportValueFormatter.FormatForExport(header, raw));
    }

    [Theory]
    [InlineData("MONTO PAGADO", "1524", "$1,524.00")]
    [InlineData("CAMBIO PENDIENTE", "10.5", "$10.50")]
    [InlineData("CANTIDAD REVERSADA", "99", "$99.00")]
    [InlineData("EFECTIVO", "2500", "$2,500.00")]
    public void FormatForExport_columnas_moneda(string header, string raw, string expected)
    {
        Assert.Equal(expected, ReporteExportValueFormatter.FormatForExport(header, raw));
    }

    [Fact]
    public void FormatForExport_id_quita_prefijo_alfabetico()
    {
        Assert.Equal("9001", ReporteExportValueFormatter.FormatForExport("ID", "AP9001"));
    }

    [Theory]
    [InlineData("1", "PAGO DE SERVICIO")]
    [InlineData("2", "RECARGA TIEMPO AIRE")]
    [InlineData("Pago mixto", "Pago mixto")]
    public void FormatForExport_tipo_de_operacion(string raw, string expected)
    {
        Assert.Equal(expected, ReporteExportValueFormatter.FormatForExport("TIPO DE OPERACION", raw));
    }

    [Theory]
    [InlineData("REGIÓN", "REGION")]
    [InlineData("Estación de Pago", "ESTACIONDEPAGO")]
    [InlineData("COD. AUTORIZACIÓN", "CODAUTORIZACION")]
    [InlineData("NUM TARJETA", "NUMTARJETA")]
    public void NormalizeHeaderKey_sin_acentos_y_mayusculas(string input, string expected)
    {
        Assert.Equal(expected, ReporteExportValueFormatter.NormalizeHeaderKey(input));
    }
}

public sealed class ReportePlantillaColumnMapperTests
{
    [Fact]
    public void ResolveValue_ticket_desde_folio_sp()
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["FOLIO"] = "TX-999"
        };

        var value = ReportePlantillaColumnMapper.ResolveValue(7, "TICKET", row);
        Assert.Equal("TX-999", value);
    }

    [Fact]
    public void ResolveValue_num_tarjeta_desde_sp_legacy()
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["N° TARJETA"] = "****1234"
        };

        var value = ReportePlantillaColumnMapper.ResolveValue(2, "NUM TARJETA", row);
        Assert.Equal("****1234", value);
    }

    [Fact]
    public void ResolveValue_metodo_pago_en_reporte_3()
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["CRITERIO DE PAGO"] = "EFE TAR"
        };

        var value = ReportePlantillaColumnMapper.ResolveValue(3, "METODO DE PAGO", row);
        Assert.Equal("EFE TAR", value);
    }
}

public sealed class ReporteExportColumnRegistryTests
{
    [Theory]
    [InlineData(1, 28)]
    [InlineData(2, 22)]
    [InlineData(3, 22)]
    [InlineData(4, 11)]
    [InlineData(5, 28)]
    [InlineData(6, 21)]
    [InlineData(7, 23)]
    public void TryGetExportColumns_cantidad_por_reporte(int id, int expectedCount)
    {
        Assert.True(ReporteExportColumnRegistry.TryGetExportColumns(id, out var cols));
        Assert.Equal(expectedCount, cols.Count);
        Assert.DoesNotContain(cols, c => c.Contains('Ó', StringComparison.Ordinal) || c.Contains('É', StringComparison.Ordinal));
        Assert.DoesNotContain(cols, c => string.Equals(c, "ESTADO", StringComparison.OrdinalIgnoreCase));
    }
}

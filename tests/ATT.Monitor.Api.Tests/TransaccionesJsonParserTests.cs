using System.Text.Json;
using ATT.Monitor.Api.Models.Conciliacion;
using ATT.Monitor.Api.Services;
using Xunit;

namespace ATT.Monitor.Api.Tests;

public sealed class TransaccionesJsonParserTests
{
    [Fact]
    public void ParseRows_accepts_array_directly()
    {
        var json = """[{"ID_TRANSACCION":1,"EP":"X","FECHA":"","TIPO":"","ESTATUS":"OK","MONTO":1.5,"FOLIO":"","FECHA_REGISTRO":"2026-01-01","Total":null}]""";
        var rows = TransaccionesJsonParser.ParseRows(json);
        Assert.Single(rows);
        Assert.Equal("OK", rows[0].Estatus);
        Assert.Equal(1.5m, rows[0].Monto);
    }

    [Fact]
    public void ParseRows_accepts_json_string_wrapped_value()
    {
        var inner = """[{"ID_TRANSACCION":2,"EP":"Y","FECHA":"","TIPO":"","ESTATUS":"ERR","MONTO":0,"FOLIO":"","FECHA_REGISTRO":"2026-02-02","Total":5}]""";
        var outer = JsonSerializer.Serialize(inner);
        var rows = TransaccionesJsonParser.ParseRows(outer);
        Assert.Single(rows);
        Assert.Equal(2, rows[0].IdTransaccion);
        Assert.Equal(5, rows[0].Total);
    }
}

using ATT.Monitor.Api.Models.Conciliacion;
using ATT.Monitor.Api.Services;
using Xunit;

namespace ATT.Monitor.Api.Tests;

public sealed class ConciliacionResumenTransaccionalFlowTests
{
    [Fact]
    public void BuildResponse_matches_expected_shape()
    {
        var rows = new List<TransaccionOnlineRowApiDto>
        {
            new() { FechaRegistro = "2026-05-10 10:00:00", Estatus = "OK", Monto = 100 },
            new() { FechaRegistro = "2026-05-12 11:00:00", Estatus = "ERR", Monto = 50 },
            new() { FechaRegistro = "2026-04-01 09:00:00", Estatus = "OK", Monto = 1 }
        };

        var filtered = ConciliacionTransaccionalResumenCalculator.FilterByRegistrationRange(
            rows,
            new DateTime(2026, 5, 1),
            new DateTime(2026, 5, 31));

        var response = new ConciliacionResumenTransaccionalResponse
        {
            FilasTraidasDeSp = rows.Count,
            MovimientosEnRango = filtered.Count,
            SumaMontos = ConciliacionTransaccionalResumenCalculator.SumMontos(filtered),
            PosiblesAnomaliasEstatus = ConciliacionTransaccionalResumenCalculator.CountWithErrorKeyword(filtered),
            PorEstatus = ConciliacionTransaccionalResumenCalculator.CountByEstatus(filtered).ToList(),
            FilasEnRango = filtered.ToList()
        };

        Assert.Equal(3, response.FilasTraidasDeSp);
        Assert.Equal(2, response.MovimientosEnRango);
        Assert.Equal(150m, response.SumaMontos);
        Assert.Equal(1, response.PosiblesAnomaliasEstatus);
        Assert.Equal(2, response.PorEstatus.Count);
    }
}

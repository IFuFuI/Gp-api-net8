using ATT.Monitor.Api.Models.Conciliacion;
using ATT.Monitor.Api.Services;
using Xunit;

namespace ATT.Monitor.Api.Tests;

public sealed class ConciliacionTransaccionalResumenCalculatorTests
{
    [Fact]
    public void FilterByRegistrationRange_keeps_rows_inside_window()
    {
        var rows = new List<TransaccionOnlineRowApiDto>
        {
            new() { FechaRegistro = "2026-01-10 12:00:00", Estatus = "OK", Monto = 10 },
            new() { FechaRegistro = "2026-01-20 08:00:00", Estatus = "OK", Monto = 5 },
            new() { FechaRegistro = "2025-12-01 08:00:00", Estatus = "OK", Monto = 1 }
        };

        var filtered = ConciliacionTransaccionalResumenCalculator.FilterByRegistrationRange(
            rows,
            new DateTime(2026, 1, 15),
            new DateTime(2026, 1, 31));

        Assert.Single(filtered);
        Assert.Equal(5m, filtered[0].Monto);
    }

    [Fact]
    public void CountByEstatus_groups_case_insensitive()
    {
        var rows = new[]
        {
            new TransaccionOnlineRowApiDto { Estatus = "ok" },
            new TransaccionOnlineRowApiDto { Estatus = "OK" },
            new TransaccionOnlineRowApiDto { Estatus = "ERR" }
        };

        var buckets = ConciliacionTransaccionalResumenCalculator.CountByEstatus(rows);
        Assert.Equal(2, buckets.Count);
        Assert.Contains(buckets, b => b.Cantidad == 2 && b.Estatus.Equals("ok", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CountWithErrorKeyword_detects_keywords()
    {
        var rows = new[]
        {
            new TransaccionOnlineRowApiDto { Estatus = "fallido" },
            new TransaccionOnlineRowApiDto { Estatus = "Aprobado" }
        };

        Assert.Equal(1, ConciliacionTransaccionalResumenCalculator.CountWithErrorKeyword(rows));
    }
}

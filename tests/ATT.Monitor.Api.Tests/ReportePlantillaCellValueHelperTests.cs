using ATT.Monitor.Api.Services.Reportes;
using Xunit;

namespace ATT.Monitor.Api.Tests;

public sealed class ReportePlantillaCellValueHelperTests
{
    [Theory]
    [InlineData("AP9001", "9001")]
    [InlineData("EP0123", "0123")]
    [InlineData("ep9001", "9001")]
    [InlineData("ATM1234", "1234")]
    [InlineData("9001", "9001")]
    [InlineData("  AP9001  ", "9001")]
    public void StripLeadingAlphaPrefixFromId_quita_prefijos_alfabeticos(string input, string expected)
    {
        Assert.Equal(expected, ReportePlantillaCellValueHelper.StripLeadingAlphaPrefixFromId(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void StripLeadingAlphaPrefixFromId_valores_vacios_sin_cambio(string? input)
    {
        Assert.Equal(input, ReportePlantillaCellValueHelper.StripLeadingAlphaPrefixFromId(input!));
    }
}

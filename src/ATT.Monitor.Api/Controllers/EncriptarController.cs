using System.Text.Json;
using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Models.Crypto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATT.Monitor.Api.Controllers;

/// <summary>Paridad <c>api/Encriptar</c> → <c>api/Encriptar</c>.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class EncriptarController(ICrypto crypto) : ControllerBase
{
    private const string MsgJsonNulo = "El objeto JSON no puede ser nulo.";
    private const string MsgJsonVacio = "El JSON no puede estar vacío.";

    [HttpPost("encrypt")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult EncryptExample([FromBody] EncryptEnvelope? body)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.En))
            return BadRequest("Cuerpo inválido: se requiere { \"En\": \"...\" }.");
        var result = crypto.Encrypt(body.En);
        return Ok(result);
    }

    [HttpPost("dencrypt")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult DEncryptExample([FromBody] EncryptEnvelope? body)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.En))
            return BadRequest("Cuerpo inválido: se requiere { \"En\": \"...\" }.");
        var result = crypto.Decrypt(body.En);
        return Ok(result);
    }

    [HttpPost("EncryptaJson")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult EncryptJson([FromBody] JsonElement json)
    {
        if (json.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return BadRequest(MsgJsonNulo);

        var jsonString = json.GetRawText();
        if (string.IsNullOrWhiteSpace(jsonString))
            return BadRequest(MsgJsonVacio);

        using (JsonDocument.Parse(jsonString))
        {
            // validar JSON
        }

        var result = crypto.Encrypt(jsonString);
        return Ok(result);
    }
}

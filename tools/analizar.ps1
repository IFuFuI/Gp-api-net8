$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot
 
if (-not $env:SONAR_TOKEN) { throw 'Falta la variable de entorno SONAR_TOKEN.' }
 
dotnet sonarscanner begin `
  /k:"IFuFuI_Gp-api-net8" `
  /o:"ifufui" `
  /d:sonar.token="$env:SONAR_TOKEN" `
  /d:sonar.sourceEncoding="UTF-8" `
  /d:sonar.projectBaseDir="$raiz"
if ($LASTEXITCODE -ne 0) { throw "sonarscanner begin fallo (codigo $LASTEXITCODE)." }
 
dotnet build --no-incremental
if ($LASTEXITCODE -ne 0) { throw "dotnet build fallo (codigo $LASTEXITCODE)." }
 
dotnet sonarscanner end /d:sonar.token="$env:SONAR_TOKEN"
if ($LASTEXITCODE -ne 0) { throw "sonarscanner end fallo (codigo $LASTEXITCODE)." }
 
Write-Host 'Analisis enviado. Espera 1 minuto antes de descargar los JSON.'
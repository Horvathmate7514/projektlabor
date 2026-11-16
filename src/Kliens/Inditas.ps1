param([switch]$CsakSzerver)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$eszkozok = Join-Path $env:LOCALAPPDATA 'Projektlabor-tools'
$dotnet = Join-Path $eszkozok 'dotnet\dotnet.exe'
$localDb = 'C:\Program Files\Microsoft SQL Server\160\Tools\Binn\SqlLocalDB.exe'
$szerverCim = 'http://localhost:5080/'

try {
    if (!(Test-Path -LiteralPath $dotnet)) {
        throw 'A helyi .NET SDK nem talalhato a Projektlabor-tools mappaban.'
    }
    $env:DOTNET_ROOT = Split-Path $dotnet
    $env:PATH = "$env:DOTNET_ROOT;$env:PATH"
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ConnectionStrings__Leltar = 'Server=(localdb)\Projektlabor;Database=ProjektlaborDemo;Integrated Security=True;TrustServerCertificate=True'
    $env:Jwt__Kulcs = (Get-Content -LiteralPath (Join-Path $eszkozok 'jwt-key.txt') -Raw).Trim()

    if ($CsakSzerver) {
        Set-Location (Join-Path $repo 'src\Szerver')
        & $dotnet (Join-Path $repo 'src\Szerver\bin\Debug\net8.0\LeltarKezelo.Szerver.dll') --urls $szerverCim
        exit $LASTEXITCODE
    }

    & $localDb start Projektlabor
    if ($LASTEXITCODE -ne 0) { throw 'Nem sikerult elinditani a Projektlabor LocalDB peldanyt.' }

    Set-Location $repo
    foreach ($projekt in @('src\Szerver', 'src\Kliens')) {
        & $dotnet build $projekt --nologo
        if ($LASTEXITCODE -ne 0) { throw "Sikertelen forditas: $projekt" }
    }

    $allapot = $null
    try { $allapot = Invoke-RestMethod ($szerverCim + 'api/allapot') -TimeoutSec 3 } catch { }
    if ($null -eq $allapot) {
        $szerver = Start-Process -FilePath 'powershell.exe' -ArgumentList @(
            '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', ('"' + $PSCommandPath + '"'), '-CsakSzerver'
        ) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $eszkozok 'szerver.log') -RedirectStandardError (Join-Path $eszkozok 'szerver-hiba.log')

        $hatarido = (Get-Date).AddSeconds(45)
        do {
            Start-Sleep -Milliseconds 500
            try { $allapot = Invoke-RestMethod ($szerverCim + 'api/allapot') -TimeoutSec 2 } catch { }
            if ($szerver.HasExited) { throw "A szerver leallt. Naplo: $eszkozok\szerver-hiba.log" }
        } while ($null -eq $allapot -and (Get-Date) -lt $hatarido)
    }
    if ($null -eq $allapot -or !$allapot.adatbazisElerheto) {
        throw "A szerver vagy az adatbazis nem erheto el. Naplo: $eszkozok\szerver.log"
    }

    # Only the client's generated output receives this local server address.
    $kliensMappa = Join-Path $PSScriptRoot 'bin\Debug\net8.0-windows'
    '{"Szerver":{"Cim":"http://localhost:5080/"}}' | Set-Content -LiteralPath (Join-Path $kliensMappa 'appsettings.Local.json') -Encoding UTF8
    Start-Process -FilePath $dotnet -ArgumentList ('"' + (Join-Path $kliensMappa 'Leltarkezelo.Client.dll') + '"') -WorkingDirectory $kliensMappa
    Write-Host 'A kliens elindult. Belepes: admin / Demo-Jelszo-2026'
    Write-Host "A szerver a hatterben fut: $szerverCim"
} catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

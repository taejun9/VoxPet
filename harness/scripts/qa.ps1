param([switch]$Publish, [switch]$Smoke)
$ErrorActionPreference = 'Stop'
Push-Location (Join-Path $PSScriptRoot '../..')
try {
    python -X utf8 harness/scripts/verify_base.py
    if ($LASTEXITCODE -ne 0) { throw 'Document validation failed' }
    python -X utf8 harness/scripts/verify_app.py
    if ($LASTEXITCODE -ne 0) { throw 'Asset/privacy contract failed' }
    git diff --check
    if ($LASTEXITCODE -ne 0) { throw 'Whitespace validation failed' }
    dotnet restore VoxPet.sln --locked-mode
    if ($LASTEXITCODE -ne 0) { throw 'Restore failed' }
    dotnet build VoxPet.sln -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
    dotnet test tests/VoxPet.Core.Tests/VoxPet.Core.Tests.csproj -c Release --no-build --logger 'trx;LogFileName=core.trx' --results-directory artifacts/test-results
    if ($LASTEXITCODE -ne 0) { throw 'Core tests failed' }
    if ($Publish) {
        dotnet publish src/VoxPet.App/VoxPet.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:RestoreLockedMode=true -o artifacts/win-x64
        if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
    }
    if ($Smoke) {
        $exe = if ($Publish) { 'artifacts/win-x64/VoxPet.exe' } else { 'src/VoxPet.App/bin/Release/net10.0-windows/win-x64/VoxPet.exe' }
        $process = Start-Process $exe -ArgumentList '--smoke-test' -PassThru
        if (-not $process.WaitForExit(30000)) {
            $process.Kill()
            throw 'WPF smoke timed out'
        }
        if ($process.ExitCode -ne 0) { throw "WPF smoke failed: $($process.ExitCode)" }
    }
} finally { Pop-Location }

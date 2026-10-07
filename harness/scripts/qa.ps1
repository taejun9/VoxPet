# Windows 전체 QA 진입점. Publish는 자체 포함 배포, Smoke는 실제 WPF의 마이크 없는 합성 시험을 추가한다.
param([switch]$Publish, [switch]$Smoke)
$ErrorActionPreference = 'Stop'
Push-Location (Join-Path $PSScriptRoot '../..')
try {
    # 외부 명령은 PowerShell 예외만으로 실패가 감지되지 않아 단계마다 LASTEXITCODE를 명시적으로 검사한다.
    python -X utf8 harness/scripts/verify_base.py
    if ($LASTEXITCODE -ne 0) { throw 'Document validation failed' }
    python -X utf8 harness/scripts/verify_app.py
    if ($LASTEXITCODE -ne 0) { throw 'Asset/privacy contract failed' }
    git diff --check
    if ($LASTEXITCODE -ne 0) { throw 'Whitespace validation failed' }
    # 잠금 파일 그대로 복원하고 서식/analyzer → 빌드 → 기존 Core 회귀 순서로 수행한다.
    dotnet restore VoxPet.sln --locked-mode
    if ($LASTEXITCODE -ne 0) { throw 'Restore failed' }
    dotnet format VoxPet.sln --verify-no-changes --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Formatting/analyzer validation failed' }
    dotnet build VoxPet.sln -c Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
    dotnet test tests/VoxPet.Core.Tests/VoxPet.Core.Tests.csproj -c Release --no-build --logger 'trx;LogFileName=core.trx' --results-directory artifacts/test-results
    if ($LASTEXITCODE -ne 0) { throw 'Core tests failed' }
    if ($Publish) {
        dotnet publish src/VoxPet.App/VoxPet.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:RestoreLockedMode=true -o artifacts/win-x64
        if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
    }
    # 자동 시험의 상한: 기본 60초, 개인 시트의 추가 모션/저장/화면 시험은 90초. 실제 사용자 앱에는 적용하지 않는다.
    if ($Smoke) {
        $exe = if ($Publish) { 'artifacts/win-x64/VoxPet.exe' } else { 'src/VoxPet.App/bin/Release/net10.0-windows/win-x64/VoxPet.exe' }
        $process = Start-Process $exe -ArgumentList '--smoke-test' -PassThru
        $smokeTimeoutMs = if ([string]::IsNullOrWhiteSpace($env:VOXPET_QA_SHEET)) { 60000 } else { 90000 }
        if (-not $process.WaitForExit($smokeTimeoutMs)) {
            $process.Kill()
            throw "WPF smoke timed out after $smokeTimeoutMs ms"
        }
        if ($process.ExitCode -ne 0) { throw "WPF smoke failed: $($process.ExitCode)" }
    }
} finally { Pop-Location }

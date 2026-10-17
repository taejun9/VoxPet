# Published EXE only: no SDK, microphone, personal fixture, or CI desktop capture.
param([string]$ResultsDirectory, [string]$EvidenceDirectory)
$ErrorActionPreference = 'Stop'
$process = $null
try {
    if (-not [Environment]::Is64BitOperatingSystem) { throw 'Windows x64 is required.' }
    $exe = Join-Path $PSScriptRoot 'VoxPet.exe'
    if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) { throw 'Extract the entire ZIP before running this test.' }
    $results = if ([string]::IsNullOrWhiteSpace($ResultsDirectory)) { Join-Path ([IO.Path]::GetTempPath()) ('VoxPet-tests/' + [Guid]::NewGuid().ToString('N')) } else { [IO.Path]::GetFullPath($ResultsDirectory) }
    New-Item -ItemType Directory -Path $results -ErrorAction Stop | Out-Null
    Write-Host 'Synthetic WPF tests: do not use the keyboard or mouse until the test finishes.'
    Write-Host "Results: $results"
    $start = New-Object System.Diagnostics.ProcessStartInfo
    $start.FileName = $exe
    $start.Arguments = '--smoke-test'
    $start.WorkingDirectory = $PSScriptRoot
    $start.UseShellExecute = $false
    $start.EnvironmentVariables['RUNNER_TEMP'] = $results
    foreach ($name in @('GITHUB_ACTIONS', 'VOXPET_QA_SHEET', 'VOXPET_QA_DATA_DIR')) { $start.EnvironmentVariables.Remove($name) }
    $process = [System.Diagnostics.Process]::Start($start)
    if (-not $process.WaitForExit(240000)) { $process.Kill(); $process.WaitForExit(); throw 'WPF test timed out after 240 seconds.' }
    $summaryPath = Join-Path $results 'voxpet-smoke-result.json'
    $summary = if (Test-Path -LiteralPath $summaryPath) { Get-Content -Raw -LiteralPath $summaryPath | ConvertFrom-Json } else { $null }
    if ($process.ExitCode -ne 0) {
        if ($null -ne $summary) {
            Write-Host "Failed checks: $(@($summary.failedChecks) -join ', ')"
            if ($summary.exceptionType) { Write-Host "Exception: $($summary.exceptionType)" }
        }
        throw "WPF test failed (exit $($process.ExitCode)). See $results"
    }
    if ($null -eq $summary.failures -or $summary.failures -ne 0 -or @($summary.passedChecks).Count -eq 0) { throw 'Test result is missing or reports failures.' }
    Write-Host "PASS: $(@($summary.passedChecks).Count) unique checks, 0 failures."
    Write-Host "Results: $results"
    exit 0
} catch {
    Write-Host "FAIL: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
} finally {
    if (-not [string]::IsNullOrWhiteSpace($EvidenceDirectory) -and $results -and (Test-Path -LiteralPath $results)) {
        $copy = [IO.Path]::GetFullPath($EvidenceDirectory)
        New-Item -ItemType Directory -Path $copy -ErrorAction Stop | Out-Null
        Get-ChildItem -LiteralPath $results | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $copy -Recurse }
    }
    if ($null -ne $process) { $process.Dispose() }
}

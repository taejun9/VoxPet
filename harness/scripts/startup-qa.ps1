# 일반 앱 프로세스의 저장 설정·12가족 시작 경로를 재현한다. 개인 PNG를 사용하지 않는다.
param([string]$Exe = 'artifacts/win-x64/VoxPet.exe')
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { throw 'Startup QA runs only on a disposable GitHub runner' }
$originalLocal = $env:LOCALAPPDATA
$originalExtract = $env:DOTNET_BUNDLE_EXTRACT_BASE_DIR
$root = Join-Path $env:RUNNER_TEMP ('VoxPet-startup-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $root | Out-Null
Copy-Item -LiteralPath $Exe -Destination (Join-Path $root 'VoxPet.exe')
try {
    $env:LOCALAPPDATA = Join-Path $root 'Local'
    $env:DOTNET_BUNDLE_EXTRACT_BASE_DIR = Join-Path $root 'bundle'
    Add-Type -AssemblyName PresentationCore
    $characters = Join-Path $root 'Characters'
    $variants = Join-Path $characters '늘보군-표정'
    New-Item -ItemType Directory -Path $variants -Force | Out-Null
    $cell = 352; $width = $cell * 8; $height = $cell * 2
    $pixels = New-Object byte[] ($width*$height*4)
    $line = New-Object byte[] ($cell*4)
    for ($x=40; $x -lt 312; $x++) { $line[$x*4]=70; $line[$x*4+1]=140; $line[$x*4+2]=220; $line[$x*4+3]=255 }
    for ($row=0; $row -lt 2; $row++) {
        for ($col=0; $col -lt 8; $col++) {
            for ($y=30; $y -lt 332; $y++) { [Array]::Copy($line,0,$pixels,(($row*$cell+$y)*$width+$col*$cell)*4,$line.Length) }
        }
    }
    $bitmap = [System.Windows.Media.Imaging.BitmapSource]::Create($width,$height,96,96,[System.Windows.Media.PixelFormats]::Bgra32,$null,$pixels,$width*4)
    $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $neutral = Join-Path $characters '늘보군.png'
    $stream = [IO.File]::Create($neutral)
    try { $encoder.Save($stream) } finally { $stream.Dispose() }
    foreach ($kind in @('happy','sad','angry','surprised','sleepy','shy','smug','confused','excited','love','playful')) {
        Copy-Item -LiteralPath $neutral -Destination (Join-Path $variants ($kind+'.png'))
    }
    $process = Start-Process (Join-Path $root 'VoxPet.exe') -PassThru -RedirectStandardError (Join-Path $root 'stderr.txt') -RedirectStandardOutput (Join-Path $root 'stdout.txt')
    Start-Sleep -Seconds 12
    $process.Refresh()
    $result = [ordered]@{ exited=$process.HasExited; mainWindowHandle=$process.MainWindowHandle.ToInt64(); exitCode=if($process.HasExited){$process.ExitCode}else{$null}; synthetic12family=$true }
    $result | ConvertTo-Json | Set-Content 'artifacts/test-results/startup.json' -Encoding utf8
    Copy-Item -LiteralPath (Join-Path $root 'stderr.txt') -Destination 'artifacts/test-results/startup-stderr.txt'
    Get-Content -LiteralPath (Join-Path $root 'stderr.txt') | Write-Output
    if ($process.HasExited -or $process.MainWindowHandle -eq 0) { throw 'Normal startup process exited or has no main window' }
} finally {
    if ($null -ne $process -and -not $process.HasExited) { $process.Kill(); $process.WaitForExit() }
    $env:LOCALAPPDATA=$originalLocal; $env:DOTNET_BUNDLE_EXTRACT_BASE_DIR=$originalExtract
    Remove-Item -LiteralPath $root -Recurse -Force
}

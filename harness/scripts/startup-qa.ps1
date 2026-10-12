# 일반 인수 없는 EXE의 실제 WPF 초기화/저장 상태/정상 종료. 개인 PNG를 사용하지 않는다.
param([string]$Exe = 'artifacts/win-x64/VoxPet.exe')
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { throw 'Startup QA runs only on a disposable GitHub runner' }
$priorData = $env:VOXPET_QA_DATA_DIR
$priorExtract = $env:DOTNET_BUNDLE_EXTRACT_BASE_DIR
$priorTray = $env:VOXPET_QA_START_IN_TRAY
$root = Join-Path $env:RUNNER_TEMP ('VoxPet-startup-' + [Guid]::NewGuid().ToString('N'))
$sourceExe = (Resolve-Path $Exe).Path
$process = $null
$cases = [Collections.Generic.List[object]]::new()
New-Item -ItemType Directory -Path $root | Out-Null
function New-Sheet([string]$File, [int]$Columns, [int]$Cell) {
    $width = $Cell * $Columns; $height = $Cell * 2
    $pixels = New-Object byte[] ($width*$height*4)
    $line = New-Object byte[] ($Cell*4)
    for ($x=16; $x -lt ($Cell-16); $x++) { $line[$x*4]=70; $line[$x*4+1]=140; $line[$x*4+2]=220; $line[$x*4+3]=255 }
    for ($row=0; $row -lt 2; $row++) {
        for ($col=0; $col -lt $Columns; $col++) {
            for ($y=16; $y -lt ($Cell-16); $y++) { [Array]::Copy($line,0,$pixels,(($row*$Cell+$y)*$width+$col*$Cell)*4,$line.Length) }
        }
    }
    $bitmap = [System.Windows.Media.Imaging.BitmapSource]::Create($width,$height,96,96,[System.Windows.Media.PixelFormats]::Bgra32,$null,$pixels,$width*4)
    $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $stream = [IO.File]::Create($File)
    try { $encoder.Save($stream) } finally { $stream.Dispose() }
}
try {
    Add-Type -AssemblyName PresentationCore
    $env:DOTNET_BUNDLE_EXTRACT_BASE_DIR = Join-Path $root 'bundle'
    foreach ($case in @('fresh12','restart-reversed12','legacy3-slot','corrupt-state','restart-pelly','missing-character','corrupt-character','restart-settings','public6')) {
        $location = Join-Path $root $case; $data = Join-Path $location 'data'; $slots = Join-Path $data 'Expressions'
        New-Item -ItemType Directory -Path $slots -Force | Out-Null
        Copy-Item -LiteralPath $sourceExe -Destination (Join-Path $location 'VoxPet.exe')
        $env:VOXPET_QA_DATA_DIR = $data
        if ($case -ne 'public6') {
            $characters = Join-Path $location 'Characters'; $variants = Join-Path $characters '늘보군-표정'
            New-Item -ItemType Directory -Path $variants -Force | Out-Null
            $neutral = Join-Path $characters '늘보군.png'; New-Sheet $neutral 8 352
            foreach ($kind in @('happy','sad','angry','surprised','sleepy','shy','smug','confused','excited','love','playful')) {
                Copy-Item -LiteralPath $neutral -Destination (Join-Path $variants ($kind+'.png'))
            }
        }
        $expectedId=0; $expectedFamily=if($case -eq 'public6'){6}else{12}; $expectedMouth=if($case -eq 'public6'){3}else{8}
        $expectedCharacter='neulbo'
        if ($case -in @('restart-pelly','missing-character','corrupt-character')) {
            @{Audio=@{Sensitivity=1.7};Topmost=$false;CharacterId='pelly'} | ConvertTo-Json | Set-Content (Join-Path $data 'settings.json') -Encoding utf8
            $pelly=Join-Path $characters '펠리.png'
            if ($case -eq 'restart-pelly') { New-Sheet $pelly 3 128; $expectedFamily=1; $expectedMouth=3; $expectedCharacter='pelly' }
            if ($case -eq 'corrupt-character') { 'broken PNG' | Set-Content $pelly -Encoding utf8 }
        }
        if ($case -eq 'restart-settings') {
            @{Audio=@{Sensitivity=1.9};Topmost=$false;GreenBackground=$false;MicrophoneId='QA-unavailable';MainWindow=@{Width=800;Height=560;Left=80;Top=60};BroadcastWindow=@{Width=410;Height=270;Left=220;Top=190};BroadcastOpen=$true;CharacterMuted=$true;SettingsTab=4;ExpressionSlotId=7} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $data 'settings.json') -Encoding utf8
            $expectedId=7
        }
        $orderPath=Join-Path $slots 'order.json'; $slotPath=Join-Path $slots 'slot-1.json'
        if ($case -eq 'restart-reversed12') { @(11..0) | ConvertTo-Json -Compress | Set-Content $orderPath -Encoding utf8; $expectedId=11 }
        if ($case -eq 'legacy3-slot') {
            $guid=[Guid]::NewGuid().ToString('N'); New-Sheet (Join-Path $slots ($guid+'.png')) 3 128
            @{Name='QA plan015 saved slot';Kind=2;Blink=$true;Tears=$true;TearLeft=.365;TearRight=.635;TearTop=.46;SheetId=$guid} | ConvertTo-Json | Set-Content $slotPath -Encoding utf8
            $expectedMouth=3
        }
        if ($case -eq 'corrupt-state') {
            'bad json' | Set-Content (Join-Path $data 'settings.json') -Encoding utf8
            '[0,0]' | Set-Content $orderPath -Encoding utf8
            'bad json' | Set-Content $slotPath -Encoding utf8
        }
        $beforeOrder=if(Test-Path $orderPath){(Get-FileHash $orderPath).Hash}else{$null}
        $beforeSlot=if(Test-Path $slotPath){(Get-FileHash $slotPath).Hash}else{$null}
        $stderr = Join-Path $location 'stderr.txt'
        $env:VOXPET_QA_START_IN_TRAY=if($case -eq 'public6'){'true'}else{$null}
        $process = Start-Process (Join-Path $location 'VoxPet.exe') -PassThru -RedirectStandardError $stderr
        Start-Sleep -Seconds 10
        $process.Refresh()
        $readyPath=Join-Path $data 'startup-ready.json'
        if ($process.HasExited -or -not (Test-Path $readyPath) -or (Test-Path (Join-Path $data 'last-error.json'))) {
            Copy-Item $stderr ('artifacts/test-results/startup-'+$case+'-stderr.txt')
            Get-Content $stderr | Write-Output
            throw ('Normal startup failed: '+$case)
        }
        $ready=Get-Content $readyPath -Raw | ConvertFrom-Json
        if (-not $ready.initialized -or -not $ready.canEdit -or ($case -ne 'public6' -and -not $ready.visible) -or $ready.selectedStorageId -ne $expectedId -or $ready.selectedPosition -ne $(if($case -eq 'restart-settings'){7}else{0}) -or $ready.familyCount -ne $expectedFamily -or $ready.mouthFrames -ne $expectedMouth -or $ready.characterId -ne $expectedCharacter) { throw ('Startup state mismatch: '+$case) }
        if ($case -eq 'restart-settings') {
            $r=$ready.remembered
            if ($r.Audio.Sensitivity -ne 1.9 -or $r.MainWindow.Width -ne 800 -or $r.MainWindow.Height -ne 560 -or $r.BroadcastWindow.Width -ne 410 -or $r.BroadcastWindow.Height -ne 270 -or -not $r.BroadcastOpen -or -not $r.CharacterMuted -or $r.SettingsTab -ne 4 -or $r.ExpressionSlotId -ne 7 -or -not $ready.microphonePreferencePresent) { throw 'Last settings normal process restore mismatch' }
        }
        $duplicatePassed=$false
        if($case -eq 'public6') {
            if($ready.visible){throw 'Tray startup fixture did not hide'}
            $second=Start-Process $sourceExe -PassThru
            if(-not $second.WaitForExit(8000) -or $second.ExitCode -ne 0){throw 'Duplicate app did not exit normally'}
            $activatedPath=Join-Path $data 'activated.json'
            for($i=0; $i -lt 50 -and -not (Test-Path $activatedPath); $i++){Start-Sleep -Milliseconds 100}
            if(-not (Test-Path $activatedPath)){throw 'Duplicate launch did not activate original tray window'}
            $activated=Get-Content $activatedPath -Raw | ConvertFrom-Json
            if(-not $activated.visible -or $activated.hwnd -ne $ready.hwnd){throw 'Duplicate launch changed original HWND'}
            $process.Refresh(); $duplicatePassed=$true
        }
        $hasWindow=$process.MainWindowHandle -ne 0
        if (-not $hasWindow -or -not $process.CloseMainWindow() -or -not $process.WaitForExit(8000) -or $process.ExitCode -ne 0) { throw ('Normal app graceful exit failed: '+$case) }
        $process=$null
        $savedSettings=Get-Content (Join-Path $data 'settings.json') -Raw | ConvertFrom-Json
        if ($case -eq 'restart-settings' -and ($savedSettings.MicrophoneId -ne 'QA-unavailable' -or -not $savedSettings.BroadcastOpen -or $savedSettings.SettingsTab -ne 4 -or $savedSettings.ExpressionSlotId -ne 7)) { throw 'Last settings exit flush mismatch' }
        if ($savedSettings.CharacterId -ne $expectedCharacter) { throw ('Character selection save mismatch: '+$case) }
        if ($case -in @('restart-pelly','missing-character','corrupt-character') -and ($savedSettings.Audio.Sensitivity -ne 1.7 -or $savedSettings.Topmost -ne $false)) { throw 'Character recovery lost unrelated audio/window settings' }
        if ($null -ne $beforeOrder -and (Get-FileHash $orderPath).Hash -ne $beforeOrder) { throw 'Startup changed existing order file' }
        if ($null -ne $beforeSlot -and (Get-FileHash $slotPath).Hash -ne $beforeSlot) { throw 'Startup changed existing slot file' }
        $cases.Add([ordered]@{case=$case;survivedSeconds=10;normalArguments=$true;ready=$ready;duplicateExitedAndRestoredSameWindow=$duplicatePassed;gracefulExit=$true;storedFilesPreserved=$true})
    }
    $cases | ConvertTo-Json -Depth 8 | Set-Content 'artifacts/test-results/startup.json' -Encoding utf8
} finally {
    if ($null -ne $process -and -not $process.HasExited) { $process.Kill(); $process.WaitForExit() }
    $env:VOXPET_QA_DATA_DIR=$priorData; $env:DOTNET_BUNDLE_EXTRACT_BASE_DIR=$priorExtract; $env:VOXPET_QA_START_IN_TRAY=$priorTray
    Remove-Item -LiteralPath $root -Recurse -Force
}

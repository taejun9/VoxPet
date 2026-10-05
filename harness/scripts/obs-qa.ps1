param([int]$LongRunMinutes = 0)
$ErrorActionPreference = 'Stop'
Push-Location (Join-Path $PSScriptRoot '../..')
$obsQaProcess = $null
$voxQaProcess = $null
try {
    if (-not $env:GITHUB_ACTIONS) { throw 'This installer is restricted to disposable GitHub Windows runners' }
    python -m pip install -r harness/scripts/obs-requirements.txt
    if ($LASTEXITCODE -ne 0) { throw 'QA dependency restore failed' }
    dotnet publish src/VoxPet.App/VoxPet.App.csproj -c Release -p:RestoreLockedMode=true -o artifacts/obs-app
    if ($LASTEXITCODE -ne 0) { throw 'App publish failed' }
    $obsQaRoot = Join-Path (Get-Location) 'artifacts/obs-install'
    $obsQaZip = Join-Path (Get-Location) 'artifacts/OBS-32.2.2.zip'
    Invoke-WebRequest 'https://github.com/obsproject/obs-studio/releases/download/32.2.2/OBS-Studio-32.2.2-Windows-x64.zip' -OutFile $obsQaZip
    if ((Get-FileHash $obsQaZip -Algorithm SHA256).Hash.ToLowerInvariant() -ne '4d6e40e3ab155f56b30de517380566a206d74b63cdf5ad49aa596924768f97e1') { throw 'OBS asset digest mismatch' }
    Expand-Archive $obsQaZip $obsQaRoot
    New-Item (Join-Path $obsQaRoot 'portable_mode.txt') -ItemType File | Out-Null
    $obsQaConfig = Join-Path $obsQaRoot 'config/obs-studio'
    New-Item (Join-Path $obsQaConfig 'plugin_config/obs-websocket') -ItemType Directory -Force | Out-Null
    New-Item (Join-Path $obsQaConfig 'basic/profiles/VoxPetQA') -ItemType Directory -Force | Out-Null
    New-Item (Join-Path $obsQaConfig 'basic/scenes') -ItemType Directory -Force | Out-Null
    $env:VOXPET_OBS_QA_PASSWORD = [Guid]::NewGuid().ToString('N')
    @{server_enabled=$true; server_port=4455; auth_required=$true; server_password=$env:VOXPET_OBS_QA_PASSWORD; first_load=$false; alerts_enabled=$false} | ConvertTo-Json | Set-Content (Join-Path $obsQaConfig 'plugin_config/obs-websocket/config.json') -Encoding utf8
    $obsQaIni = @'
[General]
ConfigOnNewProfile=false
FirstRun=true
[Basic]
Profile=VoxPetQA
ProfileDir=VoxPetQA
SceneCollection=VoxPetQA
SceneCollectionFile=VoxPetQA
'@
    $obsQaIni | Set-Content (Join-Path $obsQaConfig 'global.ini') -Encoding utf8
    $obsQaIni | Set-Content (Join-Path $obsQaConfig 'user.ini') -Encoding utf8
    @'
[General]
Name=VoxPetQA
[Video]
BaseCX=480
BaseCY=480
OutputCX=480
OutputCY=480
FPSType=0
FPSCommon=30
[Audio]
SampleRate=48000
ChannelSetup=Stereo
DesktopDevice1=disabled
DesktopDevice2=disabled
AuxDevice1=disabled
AuxDevice2=disabled
AuxDevice3=disabled
AuxDevice4=disabled
'@ | Set-Content (Join-Path $obsQaConfig 'basic/profiles/VoxPetQA/basic.ini') -Encoding utf8
    @{name='VoxPetQA'; current_scene='VoxPetQA'; current_program_scene='VoxPetQA'; scene_order=@(@{name='VoxPetQA'}); sources=@(@{id='scene'; versioned_id='scene'; name='VoxPetQA'; settings=@{items=@()}})} | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $obsQaConfig 'basic/scenes/VoxPetQA.json') -Encoding utf8
    $voxQaProcess = Start-Process 'artifacts/obs-app/VoxPet.exe' -ArgumentList '--qa-demo' -PassThru
    $obsQaBin = Join-Path $obsQaRoot 'bin/64bit'
    $obsQaProcess = Start-Process (Join-Path $obsQaBin 'obs64.exe') -WorkingDirectory $obsQaBin -ArgumentList '--portable','--disable-updater','--disable-missing-files-check','--only-bundled-plugins','--profile','VoxPetQA','--collection','VoxPetQA' -PassThru
    python -X utf8 harness/scripts/obs_capture_qa.py --output artifacts/obs-qa --voxpet-pid $voxQaProcess.Id --long-run-minutes $LongRunMinutes
    if ($LASTEXITCODE -ne 0) { throw 'OBS capture QA did not pass' }
} finally {
    if ($voxQaProcess -and -not $voxQaProcess.HasExited) { $null = $voxQaProcess.CloseMainWindow(); if (-not $voxQaProcess.WaitForExit(5000)) { $voxQaProcess.Kill() } }
    if ($obsQaProcess -and -not $obsQaProcess.HasExited) { $null = $obsQaProcess.CloseMainWindow(); if (-not $obsQaProcess.WaitForExit(5000)) { $obsQaProcess.Kill() } }
    if ($obsQaRoot) {
        New-Item 'artifacts/obs-qa' -ItemType Directory -Force | Out-Null
        Get-ChildItem (Join-Path $obsQaRoot 'config/obs-studio/logs') -Filter '*.txt' -ErrorAction SilentlyContinue | ForEach-Object {
            Select-String -Path $_.FullName -Pattern 'Loading up D3D11|D3D11 loaded|Failed to initialize video|Failed to initialize obs|OBS [0-9]|Adapter [0-9]|OpenGL|Available Video Adapters|obs-websocket.*(version|loaded|server started)' | ForEach-Object { $_.Line }
        } | Set-Content 'artifacts/obs-qa/renderer.txt' -Encoding utf8
    }
    Remove-Item Env:VOXPET_OBS_QA_PASSWORD -ErrorAction SilentlyContinue
    Pop-Location
}

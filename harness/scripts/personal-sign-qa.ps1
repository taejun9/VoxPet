# 개인 PC에서 직접 신뢰할 수 있는 자체 서명 패키지. 사용자 인증서/개인키를 받거나 내보내지 않는다.
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { throw 'Signing fixture is restricted to disposable GitHub runners' }
$certificate = $null
$trusted = $false
$output = 'artifacts/personal-win-x64'
try {
    New-Item -ItemType Directory -Path $output | Out-Null
    Copy-Item 'artifacts/win-x64/*' $output -Recurse
    $exe = (Resolve-Path "$output/VoxPet.exe").Path
    $certificate = New-SelfSignedCertificate -Type CodeSigningCert -Subject 'CN=김태중' -FriendlyName 'VoxPet personal test signer' -CertStoreLocation 'Cert:\CurrentUser\My' -KeyAlgorithm RSA -KeyLength 3072 -HashAlgorithm SHA256 -KeyExportPolicy NonExportable -NotAfter (Get-Date).AddYears(2)
    $cer = Join-Path (Resolve-Path $output).Path 'VoxPet-personal-publisher.cer'
    Export-Certificate -Cert $certificate -FilePath $cer | Out-Null
    Set-AuthenticodeSignature -LiteralPath $exe -Certificate $certificate -HashAlgorithm SHA256 | Out-Null
    $untrusted = Get-AuthenticodeSignature -LiteralPath $exe
    $name = $untrusted.SignerCertificate.GetNameInfo([Security.Cryptography.X509Certificates.X509NameType]::SimpleName, $false)
    if ($name -cne '김태중' -or $untrusted.SignerCertificate.Thumbprint -ne $certificate.Thumbprint) { throw 'Personal signer mismatch' }
    # 신뢰 설치는 이 일회용 QA 계정에만 수행한다. 사용자 PC에는 설치 스크립트를 제공하지 않는다.
    Import-Certificate -FilePath $cer -CertStoreLocation 'Cert:\CurrentUser\Root' | Out-Null
    $trusted = $true
    $verified = Get-AuthenticodeSignature -LiteralPath $exe
    if ($verified.Status -ne 'Valid') { throw ('Trusted signature verification failed: ' + $verified.Status) }
    Add-Type -AssemblyName System.Drawing
    $icon = [Drawing.Icon]::ExtractAssociatedIcon($exe)
    if ($null -eq $icon) { throw 'Executable icon missing' }
    $icon.Dispose()
    $tampered = Join-Path $env:RUNNER_TEMP ('VoxPet-tamper-' + [Guid]::NewGuid().ToString('N') + '.exe')
    try {
        Copy-Item $exe $tampered
        $stream = [IO.File]::Open($tampered, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite)
        try { $stream.Position=1024; $value=$stream.ReadByte(); $stream.Position=1024; $stream.WriteByte($value -bxor 1) } finally { $stream.Dispose() }
        $tamperStatus = (Get-AuthenticodeSignature -LiteralPath $tampered).Status.ToString()
        if ($tamperStatus -ne 'HashMismatch') { throw ('Tampered EXE accepted: ' + $tamperStatus) }
    } finally { Remove-Item $tampered -ErrorAction SilentlyContinue }
    [ordered]@{signer=$name;subject=$certificate.Subject;thumbprint=$certificate.Thumbprint;notAfter=$certificate.NotAfter.ToUniversalTime().ToString('O');untrustedStatus=$untrusted.Status.ToString();trustedStatus=$verified.Status.ToString();tamperedStatus=$tamperStatus;publicCertificateOnly=$true;certificateAuthorityVerified=$false;privateKeyExported=$false;iconPresent=$true;exeSha256=(Get-FileHash $exe -Algorithm SHA256).Hash;cerSha256=(Get-FileHash $cer -Algorithm SHA256).Hash} | ConvertTo-Json | Set-Content "$output/publisher-signature.json" -Encoding utf8
    & "$PSScriptRoot/startup-qa.ps1" -Exe $exe
    Copy-Item 'artifacts/test-results/startup.json' 'artifacts/test-results/signed-startup.json'
    Copy-Item "$output/publisher-signature.json" 'artifacts/test-results/personal-publisher.json'
} finally {
    if ($null -ne $certificate) {
        if ($trusted) { Remove-Item ('Cert:\CurrentUser\Root\' + $certificate.Thumbprint) }
        Remove-Item ('Cert:\CurrentUser\My\' + $certificate.Thumbprint) -DeleteKey
    }
}

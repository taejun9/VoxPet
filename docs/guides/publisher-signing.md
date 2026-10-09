# 김태중 게시자 서명

파일 속성의 회사 이름과 Windows 실행 경고의 게시자는 다릅니다. 회사 이름은 김태중으로 지정했지만 실행 경고는 디지털 서명의 인증서 신뢰로 판단합니다. 아래 작업은 Windows에서 수행합니다. 앱 자체와 기본 CI는 인증서를 설치하거나 신뢰 저장소를 변경하지 않습니다.

## 코드 서명 인증서를 보유한 경우

인증서를 발급한 제공자의 로컬 키/보안 토큰 도구로 최종 `VoxPet.exe`를 Authenticode 서명합니다. 인증서의 서명자 이름이 김태중이어야 해당 이름으로 표시됩니다. 이름을 파일 메타데이터로 덮어쓸 수는 없습니다. 게시자 이름이 다른 인증서라면 발급 정보를 먼저 확인합니다.

Windows 인증서 저장소에 개인 키가 있는 인증서는 PowerShell에서 사용할 수 있습니다. 먼저 코드 서명 인증서를 확인합니다.

```powershell
Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert | Select-Object Subject, Thumbprint, NotAfter
```

사용할 인증서 지문을 확인한 뒤 최종 EXE를 별도 폴더에 복사하고 서명합니다. 지문에는 개인 키가 포함되지 않습니다. 암호나 개인 키는 채팅/Git으로 보내지 않습니다.

```powershell
$cert = Get-Item 'Cert:\CurrentUser\My\확인한_인증서_지문'
Set-AuthenticodeSignature -LiteralPath '.\VoxPet.exe' -Certificate $cert -HashAlgorithm SHA256
Get-AuthenticodeSignature -LiteralPath '.\VoxPet.exe' | Format-List Status, StatusMessage, SignerCertificate
```

실제 서명 상태가 `Valid`이고 서명자 이름이 김태중인지 확인합니다. 공용 배포는 제공자의 타임스탬프 서비스를 사용해 인증서 만료 후 검증 가능성도 확보합니다. 서명 후 EXE를 수정하거나 다시 publish하면 재서명이 필요합니다. 새 파일에는 SmartScreen 평판 경고가 남을 수 있습니다.

## 인증서가 없고 개인 PC에서만 시험하는 경우

자체 서명 인증서는 개인 시험용으로 만들 수 있지만 Windows가 기본적으로 신뢰하지 않습니다. 본인이 생성한 인증서를 해당 개인 PC에서 직접 신뢰하도록 설치해야 합니다. 그 인증서로 서명한 코드 전체를 신뢰하게 되므로 인증서의 이름과 지문을 확인합니다. 공용 배포용 신원 확인을 대신하지 않습니다.

Windows PowerShell에서 앱 폴더의 복사본을 열어 다음 명령으로 개인 시험 인증서를 만들고 공개 인증서를 내보낸 뒤 EXE에 서명할 수 있습니다. 개인 키는 현재 사용자의 인증서 저장소에 남으며 내보내지 않습니다.

```powershell
$cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject 'CN=김태중' -FriendlyName 'VoxPet 개인 시험 서명' -CertStoreLocation 'Cert:\CurrentUser\My' -KeyExportPolicy NonExportable -HashAlgorithm SHA256 -KeyLength 3072 -NotAfter (Get-Date).AddYears(1)
$cert | Select-Object Subject, Thumbprint, NotAfter
Export-Certificate -Cert $cert -FilePath '.\김태중-VoxPet-개인서명.cer'
Set-AuthenticodeSignature -LiteralPath '.\VoxPet.exe' -Certificate $cert -HashAlgorithm SHA256
```

공개 `.cer`를 직접 열어 이름/지문을 확인한 뒤 **인증서 설치 → 로컬 컴퓨터 → 모든 인증서를 다음 저장소에 저장 → 신뢰할 수 있는 루트 인증 기관**을 선택합니다. Windows의 관리자 확인과 신뢰 확인은 사용자가 직접 판단합니다. 다른 사람이 보낸 인증서에는 이 개인 시험 절차를 적용하지 않습니다.

설치 후 `Get-AuthenticodeSignature`의 `Status=Valid`와 서명자 김태중을 확인하고 Windows의 실제 실행 경고를 확인합니다. 만료일 이후 또는 다른 PC에는 별도 확인이 필요합니다. 여기서는 경고가 자동으로 사라졌다고 보장하지 않습니다. 기본 자동 빌드 ZIP에는 이 인증서나 서명이 포함되지 않습니다.

공식 근거: [Microsoft 코드 서명 옵션](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options), [Set-AuthenticodeSignature](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.security/set-authenticodesignature?view=powershell-5.1), [New-SelfSignedCertificate](https://learn.microsoft.com/en-us/powershell/module/pki/new-selfsignedcertificate?view=windowsserver2025-ps). 확인일:2026-10-09.

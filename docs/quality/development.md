# 개발 환경과 현재 실행 명령

현재 Core/App/Tests 솔루션과 WASAPI/WPF MVP 구현이 있다. 실제 마이크·OBS 실기 상태는 [검증 기록](windows-checklist.md)을 따른다.
프로젝트 개요와 구조는 [README](../../README.md), 작업 규칙은 [AGENTS.md](../../AGENTS.md), 사용자 조작은 [사용 설명서](../user-guide.md)를 따른다.

## 문서와 자산 검증

저장소 루트에서 Python 3.10 이상(표준 라이브러리만 사용):

```sh
python3 harness/scripts/verify_base.py
python3 harness/scripts/verify_app.py
git diff --check
```

첫 명령은 문서 구조 검증이다. 앱 빌드나 동작 성공을 의미하지 않는다.

## 확정한 빌드 환경

| 항목 | 기획 기준 / 결정 상태 |
|---|---|
| OS/CPU | Windows 11 x64의 지원 중인 버전 우선; Windows 10은 edition/build별 별도 실기 필요 |
| SDK/TFM | .NET SDK 10.0.401 고정, Core net10.0 / App net10.0-windows, win-x64, WPF |
| IDE | 선택 SDK를 지원하는 Visual Studio 또는 dotnet CLI; 검증은 CLI로 수행 |
| 오디오 | NAudio.Wasapi 2.2.1 + transitive NAudio.Core 2.2.1, packages.lock.json 고정 |
| 렌더링 | 내장 WPF PNG 사용; SkiaSharp/Live2D 패키지는 필요 없음 |
| MVVM | 자체 ObservableObject/RelayCommand/AsyncCommand; 외부 MVVM/DI 없음 |
| 테스트 | xunit 2.9.3, runner 3.0.2, Test SDK 17.14.1; packages.lock.json 고정 |

[공식 근거](../references/official-sources.md): WPF는 Windows에서 실행한다.
.NET 10 LTS로 전환한 근거는 공식 지원 정책과 실행 계획 Decision Log에 기록했다.
NuGet/SDK 업데이트 시 잠금 파일을 갱신하고 전체 QA를 다시 수행한다.

## 빌드와 테스트

아래 CLI 명령은 생성한 솔루션에 적용한다. WPF 실행은 Windows에서만 가능하다.

```sh
dotnet restore VoxPet.sln --locked-mode
dotnet format VoxPet.sln --verify-no-changes --no-restore
dotnet build VoxPet.sln --configuration Release
dotnet test tests/VoxPet.Core.Tests/VoxPet.Core.Tests.csproj --configuration Release
dotnet run --project src/VoxPet.App/VoxPet.App.csproj
```

2026-10-05 macOS arm64에서 임시 SDK 설치 후 실제 restore/build/Core test를 수행했다.
로컬 sandbox에서 MSBuild IPC가 제한되면 `-m:1 -p:UseSharedCompilation=false -nodeReuse:false` 또는 승인된 실행 환경을 사용한다.
WPF 실행·WASAPI·OBS QA는 Windows에서 한다. Windows CI에는 실제 마이크가 없다. 별도 OBS workflow는 공식 portable OBS를 일회용 runner에서 실행해 합성 데모 이미지만 시험한다.

## Windows 전체 QA와 배포

Python 3.10+, Git, global.json의 .NET SDK가 필요하다. PowerShell에서:

```powershell
./harness/scripts/qa.ps1 -Publish -Smoke
```

문서/자산 검증 → locked restore → format/analyzer 검사 → Release build → Core test → 자체 포함 publish → 마이크 없는 WPF smoke 순서다.
`.github/workflows/windows.yml`이 같은 명령을 Windows runner에서 실행하고 실행 폴더/테스트 결과/화면 PNG를 업로드한다.

별도 `.github/workflows/obs.yml`은 `obs-qa.ps1`/`obs_capture_qa.py`로 마이크 없는 Window Capture와 Chroma Key를 검증한다.
`OBS-QA-evidence`에 캡처 PNG/결과 JSON/선별한 renderer 정보만 보관한다. OBS 설치·설정·인증 암호는 업로드하지 않는다.
`obs-qa.ps1`은 일회용 GitHub runner로 실행을 제한한다. 실제 사용자 OBS 설정을 변경하는 명령으로 사용하지 않는다.
장시간 검증은 10분 warmup 후 60분 합성 UI의 RSS/핸들과 매분 캡처 변화만 측정하며 실제 마이크 장시간 검증과 구분한다.

```sh
dotnet publish src/VoxPet.App/VoxPet.App.csproj -c Release --no-restore -o artifacts/win-x64
```

win-x64/single-file/self-contained 설정은 csproj에 고정했다. 배포 폴더의 VoxPet.exe는 .NET 설치 없이 실행한다.
Licenses/와 자산 manifest/USER-GUIDE.txt를 함께 배포한다. macOS에서는 생성은 가능하지만 EXE 실행은 불가능하다.
실제 설치 가능한 정식 배포 판정에는 [Windows 체크리스트](windows-checklist.md)의 통과 기록이 필요하다.
자체 포함 runtime의 보안 업데이트는 SDK 갱신과 앱 재배포로 적용한다.
기본 PNG 재생성은 개발용 Pillow가 필요하다. 앱 실행/빌드에는 Pillow가 필요 없다.

## 개인 캐릭터 Windows 합성 시험

plan008의 smoke는 로컬 PNG 시트 불러오기/잘못된 파일 복구/행열 매핑과 정렬/프리셋/음소거를 마이크 없이 시험한다.
실행 프로세스에 `VOXPET_QA_SHEET`를 지정하면 해당 개인 PNG도 적용해 여섯 상태와 실제 합성 반응 PNG를 runner temp에 기록한다.
`windows.yml`의 수동 `use_personal_sheet` 입력은 임시 `VOXPET_QA_SHEET_URL` secret의 단일 GitHub release asset 만료 URL만 읽는다.
모든 workflow는 contents:read이며 계정 토큰을 전달하지 않는다. 개인 시트는 Git/기본 배포에 넣지 않는다. 외부 전송은 명시 승인을 받고, 로컬 증거 확보 후 임시 URL secret을 제거한다. 개인 draft/화면 artifact의 보존·삭제는 사용자 요청을 따른다. plan012는 직접 시험에도 사용하도록 늘보군을 삭제하지 말라는 요청에 따라 원본·private draft·화면 증거·개인 테스트 패키지를 보존한다.
마이크 권한/물리 장치 검증이나 공용 캐릭터 배포 권한 확인을 대체하지 않는다.


## 표정 슬롯 Windows 합성 시험

plan011의 ExpressionQa는 임시 저장소/메모리만 사용해 슬롯 JSON/관리 PNG 복사, 재시작과 손상 복구, 240ms 전환과 무음 눈물/blink를 검증한다. RegisterHotKey 충돌·해제와 실제 WM_HOTKEY는 합성 키 입력으로 검사하며 물리 키보드 실기와 구분한다. F12는 앱 내부 입력이다. 긴 이름/TextBox를 포함한 LayoutQa와 기존 음소거/창 수명 회귀도 실행한다.
`Windows-QA-evidence`에는 voxpet-expression-preview.png/voxpet-expression-broadcast.png 및 기존 smoke/layout JSON을 포함한다. 음성/실제 사용자 PNG를 포함하지 않는 기본 fixture다.

`VOXPET_QA_SHEET`를 지정한 실행은 슬롯 관리 복사본의 저장·재시작·원본 제거·손상 복구에도 그 개인 시트를 사용한다. 늘보군 평상/눈물 슬롯의 여섯 frozen 상태, 전환 시작/완료, 무음 blink·눈물과 두 창 공유를 검사하고 `voxpet-personal-expression-*.png`에 미리보기 및 초록/투명 방송창을 렌더한다. `voxpet-smoke-result.json`의 passedChecks/failedChecks로 개인 시험 실행 여부를 확인한다. 이 PNG는 개인 자료이므로 위 수집·삭제 절차를 동일하게 적용한다.

자동 smoke 프로세스의 상한은 기본60초, 개인 시트 추가 시험은90초다. 시험 항목과 판정 기준은 동일하게 유지하며 추가 개인 시트 저장/모션/캡처 시간만 확보한다. 실제 사용자 앱 실행에는 이 상한을 적용하지 않는다.

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

게시자 메타데이터는 App csproj의 Company/Authors=김태중으로 지정한다. 이는 Authenticode 서명을 대체하지 않는다. `qa.ps1 -Publish`는 최종 단일 EXE의 CompanyName을 검사하고 실제 서명 상태/서명자/검증 여부를 `artifacts/test-results/publisher.json`에 기록한다. 공용 배포는 unsigned이며 별도 personal-sign-qa.ps1이 일회용 runner 키로 개인 패키지를 서명한다. 해당 runner에만 임시 신뢰를 등록/제거하며 개인키는 내보내지 않는다. 사용자 신뢰 저장소는 변경하지 않는다. 실사용 인증서 서명은 최종 publish 후 수행하며 개인 키/암호는 공개 Git/CI 증거로 보내지 않는다. [게시자 안내](../guides/installation.md#게시자-이름과-실행-경고)를 따른다.

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

통합 사이드바 및 의상17종/동작9종의 사용·샘플 파일은 [Studio 샘플 안내](../guides/studio-samples.md)를 따른다.

설치 없이 직접 실행하는 ZIP과 마이크 없는 테스트 실행 도구는 [Windows 테스트 패키지](windows-test-package.md)를 따른다.

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

## macOS 테스트 범위와 프로그램 아이콘

macOS에서는 `EnableWindowsTargeting=true`로 Windows 대상 솔루션을 빌드·게시하고 Core 자동 테스트 및 Python 문서/자산 검사를 실행할 수 있다. 화면·WASAPI 마이크·트레이·전역 입력·OBS 출력은 Windows에서 검증한다. WPF는 Windows에서만 실행하므로 macOS에서 EXE를 직접 실행할 수 없다. 맥에서 화면을 직접 시험하려면 Windows 가상머신/원격 Windows 환경이 필요하며, macOS 네이티브 실행은 UI·오디오·입력·창 구현의 별도 이식 작업이다. [Microsoft WPF](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/), [Windows 대상 빌드](https://learn.microsoft.com/en-us/dotnet/core/tools/sdk-errors/netsdk1100)를 따른다.

지정한 프로그램 그림 원본은 `src/VoxPet.App/Assets/VoxPet-source.png`다. `python3 harness/scripts/generate_icon.py`는 Pillow로 원본의 배경과 여백을 보존해256px PNG 및16/24/32/48/64/128/256px ICO를 만든다. PNG는 앱 머리글, ICO는 EXE·두 창·트레이에 사용한다. 개인 캐릭터 시트와 별개인 사용자 지정 프로그램 아이콘이며 배포 리소스에 포함한다. Windows 개인 서명 QA는 EXE에서 추출한 아이콘 픽셀이 ICO와 일치하는지도 검사한다.

## 다섯 캐릭터 선택 회귀

plan021 CharacterSelectionQa는 개인 자료 없이 합성 시트로 다섯 WPF 선택 항목, frozen 입/눈 상태, 두 창 공유, 누락/손상 시 선택 복구, 연속 선택의 최신 요청, 전체 표정 테스트 중 변경, 종료 중 읽기를 검사한다. LayoutQa는 캐릭터 선택 ComboBox와 안내까지4창크기에서 확인한다. startup-qa.ps1은 일반/서명 EXE의 펠리 ID 복원·종료 저장과 파일 누락/손상 복구를 추가 검사한다. 신규 실제 개인 PNG는 CI에 전송하지 않고 로컬 자산 검사와 개인 ZIP에만 포함한다. [캐릭터 목록](../product/character-library.md)을 따른다.

## 개인 캐릭터 Windows 합성 시험

plan008의 smoke는 로컬 PNG 시트 불러오기/잘못된 파일 복구/행열 매핑과 정렬/프리셋/음소거를 마이크 없이 시험한다.
실행 프로세스에 `VOXPET_QA_SHEET`를 지정하면 해당 개인 PNG도 적용해 여섯 상태와 실제 합성 반응 PNG를 runner temp에 기록한다.
`windows.yml`의 수동 `use_personal_sheet` 입력은 임시 `VOXPET_QA_SHEET_URL` secret의 단일 GitHub release asset 만료 URL만 읽는다.
모든 workflow는 contents:read이며 계정 토큰을 전달하지 않는다. 개인 시트는 Git/기본 배포에 넣지 않는다. 외부 전송은 명시 승인을 받고, 로컬 증거 확보 후 임시 URL secret을 제거한다. 개인 draft/화면 artifact의 보존·삭제는 사용자 요청을 따른다. plan012는 직접 시험에도 사용하도록 늘보군을 삭제하지 말라는 요청에 따라 원본·private draft·화면 증거·개인 테스트 패키지를 보존한다.
마이크 권한/물리 장치 검증이나 공용 캐릭터 배포 권한 확인을 대체하지 않는다.


## 표정 슬롯 Windows 합성 시험

plan011의 ExpressionQa는 임시 저장소/메모리만 사용해 슬롯 JSON/관리 PNG 복사, 재시작과 손상 복구, 240ms 전환과 무음 눈물/blink를 검증한다. RegisterHotKey 충돌·해제와 실제 WM_HOTKEY는 합성 키 입력으로 검사하며 물리 키보드 실기와 구분한다. F12는 앱 내부 입력이다. 긴 이름/TextBox를 포함한 LayoutQa와 기존 음소거/창 수명 회귀도 실행한다.
`Windows-QA-evidence`에는 voxpet-expression-preview.png/voxpet-expression-broadcast.png 및 기존 smoke/layout JSON을 포함한다. 음성/실제 사용자 PNG를 포함하지 않는 기본 fixture다.

`VOXPET_QA_SHEET`를 지정한 실행은 슬롯 관리 복사본의 저장·재시작·원본 제거·손상 복구에도 그 개인 시트를 사용한다. 늘보군 평상/눈물 슬롯의 여섯 frozen 상태, 전환 시작/완료, 무음 blink·눈물과 두 창 공유를 검사하고 `voxpet-personal-expression-*.png`에 미리보기 및 초록/투명 방송창을 렌더한다. `voxpet-smoke-result.json`의 passedChecks/failedChecks로 개인 시험 실행 여부를 확인한다. 이 PNG는 개인 자료이므로 위 수집·보존·정리 절차를 동일하게 적용한다.

자동 smoke 프로세스의 상한은 plan047부터 실제 desktop 픽셀 대조를 포함한 CI300초, 개인 시트 추가 CI330초다. SDK 없는 TEST-SMOKE 도구는240초를 유지한다. 기존 항목·판정 기준은 유지하며90시점의 실제 표시 픽셀 대조를 추가했다. 추가 reference 렌더/DWM 동기화와 개인 시트 저장·모션·캡처 시간을 확보한다. 실제 사용자 앱 실행에는 이 상한을 적용하지 않는다.

## 주변 소음·입 음소거 사용성 합성 시험

plan013의 UsabilityQa는 실제 마이크 대신 20ms 주기의 숫자 입력을 주입해 DispatcherTimer 기반 3초 측정/직접 적용/취소/Stop/프리셋/무신호/장치 중단/종료를 검사한다. ExpressionQa는 Ctrl+Shift+M 등록·충돌·최소화 중 WM_HOTKEY·해제 재등록을 기존 표정 키와 함께 검사한다. LayoutQa는 새 권한/측정/취소/적용 버튼까지 4창크기에서 접근성을 검사한다. 권한 버튼의 명령 전달은 OS 설정을 열지 않는 fixture에서 확인하며 실제 Settings 페이지와 물리 권한 변경은 실기 항목이다.

## 탭 화면·개인 기본·방송창 크기 시험

plan014 LayoutQa는 4창크기×5탭에서 조작·탭 머리글 가시성, 스크롤 없음, 셀렉트 박스 선택값/팝업의 검정 글씨, 편집값 유지와 탭별 PNG를 검사한다. CharacterQa는 합성 PNG로 개인 기본 불러오기/복원/표정/실패 복구를 확인한다. 방송창 너비·높이 제어, native 크기 변경의 모델 반영, 유한 범위와 재열기 크기도 smoke에서 검사한다. 공개 CI는 기존 합성/CC0 자산만 사용하며 개인 패키지에만 Characters/늘보군.png를 동봉한다.

## 상세8열 입 반응·기본 표정 가족 시험

plan015 MouthFrameTests는8단계 정확한 경계/비유한값/dBFS sweep/Gate와3열·8열 크기 제한을 검사합니다. DetailedCharacterQa는 합성8열PNG로16frozen상태/행열/기본6표정/인코딩·슬롯 저장 재시작/손상 표정 평상 복구/기본 누락 보존을 확인합니다. 공개 CI에 개인 늘보군 생성 PNG를 전달하지 않습니다. 개인 자산의 실제 얼굴/투명/단계는 로컬 시각 검사와 시트 검증, 패키지 CRC로 구분합니다.


## 표정 순서·12종 테스트 합성 시험

plan016 ExpressionOrderTests는12종 기본값/기존 enum 숫자 호환/순열 저장·재시작/손상과 실패 복구를 확인한다. ExpressionManagementQa는 실제 방송창 우클릭 메뉴의 검정 헤더와 PNG, 이동 시 슬롯 파일·초안 유지/위치-F키 대응/재시작/경계/메모리 PNG 저장ID,12종 개별·전체 테스트·취소·키 전환·종료·원래 표정 복원, 자동 숫자 데모의 마이크 미사용과 종료를 검사한다. LayoutQa는 테스트 탭과12개 버튼도4창크기에서 확인한다. DetailedCharacterQa의 합성 표정 가족은12종×16상태로 확장했다. 개인 늘보군12PNG는 로컬에서 별도 검증하며 공개 CI로 보내지 않는다.


## 일반 시작·트레이·OBS 호환 회귀

plan018 `startup-qa.ps1`은 일회용 GitHub runner에서만 실행합니다. 일반 인수 없는 EXE를 별도 프로세스로10초씩 실행하여 fresh12/reversed12/이전3열 저장PNG/손상 상태/공용6얼굴을 확인하고 초기화 완료·선택·편집 가능·창 가시성·정상 종료·슬롯/순서 파일 불변을 검사합니다. 환경 VOXPET_QA_DATA_DIR은 GITHUB_ACTIONS=true 및 RUNNER_TEMP 내부에 한해서 임시 데이터 경로로 사용합니다. 사용자 실제 설정을 변경하지 않습니다. 기본 `qa.ps1 -Publish`에 포함됩니다.

StartupTrayQa는 실제 WPF Selector의 임시 선택 해제/순서 변경과 트레이 메뉴·hide/restore/숨겨진 창 종료·데모 유지, plan019의 layered 방송창과 배경 변경 HWND·크기/위치 유지, 최소 오류 로그의 메시지/경로 제외를 확인합니다. OBS QA는 설정창을 트레이로 숨긴 상태에서 별도 방송창을 캡처합니다. 실제 사용자 Shell 클릭/Windows11 GPU·물리 마이크 시험과 구분합니다.


## 의상·단일 실행·개인 서명 회귀

plan019 CostumeTests/CostumeQa는8분류, 원자적 저장 실패/원본 보존, 관리 복사본/재시작/손상 복구, 한벌과 상하의 배타·겉옷/복수 장식 공존, 미세 위치·균일 크기·레이어 순서, 미리보기/방송 합성 픽셀을 검사한다. LayoutQa는6탭을4창크기에서 검사한다. startup-qa.ps1은 마지막 public6에서 실제 추가 EXE가0으로 종료하고 숨겨진 기존 HWND를 복원하는지 확인한다. 일반 시작은 mutex를 사용하고 smoke/OBS fixture는 격리 실행한다.

개인 서명 하네스는 일회용 GitHub runner에만 실행한다. NonExportable 키를 생성해 복사된 최종 단일 EXE에 SHA256 서명하고 공개 CER만 내보낸다. 서명자 김태중/EXE 아이콘, 임시 Root 신뢰 후 Valid, 변조 HashMismatch, 서명된 EXE의5개 일반 시작 케이스를 검사한다. finally에서 임시 Root와 개인키를 제거한다. 공개 배포와 개인 서명 artifact를 구분한다. 개인 늘보군 PNG는 포함하지 않는다.

OBS QA는 WGC 소스를 생성한 뒤 같은 HWND에서 초록→투명→초록 전환을 요청하고 소스를 다시 만들지 않은 캡처와 alpha를 확인한다. 일반 앱에는 QA 전환 메시지 hook이 없다. 고정 HWND를 위해 항상 투명 layered 창을 사용하므로 BitBlt 실패는 호환 제한으로 기록하고 WGC는 필수 통과다.

## 마지막 설정 자동 저장·재실행 QA

plan022 SettingsPersistenceQa는 가짜 장치/임시 저장소로 실제 WPF 창2회 생성·닫기, 자동 저장/종료 flush, 설정창 정상 bounds와 최대화·최소화/트레이, 방송창 크기/좌표/재열기·닫음·TwoWay 바인딩, 마지막 마이크와 목록 순서/분리/재연결·빈 목록, 음소거/탭/저장 슬롯·내장 얼굴과 임시 전체 테스트, 쓰기 실패/이전 파일 보존·재시도를 검사한다. 실제 입력 factory 호출0을 확인한다. startup-qa.ps1의 restart-settings는 일반/서명 EXE 프로세스에서도 크기/열림/음소거/탭/표정/마이크 선호 복원을 확인한다. Core는 새 필드 왕복/이전 설정/필드 복구 및 음수 모니터·분리/작업 영역·DIP 배율을 검사한다. 실제 mixed-DPI 다중 모니터/물리 마이크는 실기 대상이다.

## 손동작·의상 연결 QA (plan020)

HandMotionTests는 지연 간격/중복 입력/한 동작/시간 및 관절 유한값/예약 키·중복/캐릭터5종 기본 영역/구 의상 enum·연결 기본값/원자적 저장·손상 복구를 검사한다. HandMotionQa는 실제 WPF 합성 색상 표식의 중심점과 공유 관절 변환을 비교해 소매 이동·몸통 고정·머리/입 상속·0% 영향도를 확인하고5동작별 얼굴 보존을 검사한다. 임시 motion.json의 자동 저장/종료 flush/캐릭터별 재시작과 별도 HWND 등록 키 충돌·WM_HOTKEY·실제 합성 좌클릭·해제/재등록을 시험한다. LayoutQa는7탭과 새 하위 탭을4창크기에서 검사한다. 마이크/개인 PNG는 사용하지 않는다. 추가 시험에 맞춰 plan023부터 기본 smoke 상한180초/개인210초이며 통과 조건은 동일하다.


## Windows 실제 데스크톱 화면 증거

plan023은 기존 WPF RenderTargetBitmap 검사와 별도로 표시된 HWND 영역의 데스크톱 픽셀을 SRCCOPY|CAPTUREBLT로 수집한다. 가려짐 방지를 위해 캡처 동안만 Topmost/Activate를 적용하고 원래 Topmost를 복원한다. 빈/단색/화면 밖/foreground HWND 불일치는 실패이며 합성 렌더로 대체하지 않는다. 투명 창의 데스크톱 캡처는 뒤 창까지 합성된 화면이므로 native alpha 판정은 기존 OBS WGC 검사를 따른다.

`ScreenSceneQa`는 공개 고양이와 실제 Wardrobe.ImportAsync로 읽은 합성 의상을 사용한다. 7탭21페이지×4창크기84장,12표정×3입×2눈72장과 설정창12장,5손동작×4시점20장,머리/입 연결6장,방송/트레이4장,선택5장,설정 초기/재생성4장,일반/서명9사례씩18장으로225장을 수집한다. `python3 harness/scripts/verify_screens.py <RUNNER_TEMP>`는 소스의 탭/표정 정의와 coverage·foreground·PNG 크기/CRC를 검사한다. 직접 시각 검토는 자동 검사와 별도 단계다.

화면 증거는 GITHUB_ACTIONS=true와 RUNNER_TEMP가 있는 일회용 runner만 생성한다. 일반 시작 캡처에는 추가로 검증된 AppPaths.QaFolder와 VOXPET_QA_SCREEN_NAME이 필요하다. 정상 사용자 실행에는 화면 저장을 추가하지 않는다. 기본 workflow는 개인 PNG/실제 설정/음성 대신 공용·합성 자료만 사용한다.

## 이미지 조합 세트 회귀

plan025부터 현재 캐릭터 선택은 늘보군/펠리 두 종이다. 이전 plan021 다섯 종 회귀 기록은 과거 결과다. ImageSetTests는 파츠 대체/숨김·소매/무기 포즈·Anchor·표정/입 우선순위·연타 재시작·레이어 순서·경로·설정 복구를 검사한다. ImageRuntimeQa는 제공 합성 틀의 로컬 등록/두 창 공유/frozen 이미지/손상 등록 보존/키 기록·해제와 배경 창 키 입력 전달을 Windows에서 시험한다. 개인 파츠는 CI에 보내지 않는다. `python3 harness/scripts/generate_imageset_template.py`는 표준 라이브러리만 사용해 배포 가능한 합성 제작 틀을 재생성한다.

## 카드 이름·문자열 변환·접근성 회귀

plan043 `CardDisplayQa`는 실제 worker에서 만든 frozen PNG를 UI 카드에 넣고 `ToString`·WPF의 암시적 텍스트 바인딩·worker/병렬 이름 읽기를 검사한다. 그림을 동결해 렌더링할 수 있어도 이미지의 문자열 변환이 원래 decoder에 접근할 수 있으므로, 이미지 보유 UI 모델은 이름/레이블만 반환한다. `CharacterCard`/`EditorRow`/`RuntimeLayer`와 목록·편집기의 명시적 `TextSearch.TextPath=Name`을 확인한다.

공개 합성 캐릭터로 실제 Home/End 키, 영문/한글 text composition 검색, UI Automation 이름 읽기, 이름/ID 필터, 빈 결과, 반복 선택/새로고침, 손상 등록 후 기존 출력, 편집/내보내기/재읽기, 복사본 삭제의 원본 보존, 재시작 선택 복원을 실행한다. 실제 마이크/사용자 설정/개인 PNG는 쓰지 않는다. 수정 전 재현의 최소 진단은 `voxpet-runtime-card-display-repro.json`으로 보존하고 실패를 통과로 처리하지 않는다.

근거는 Microsoft WPF의 [BitmapFrameDecode 문자열 변환](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/Media/Imaging/BitmapFrameDecode.cs), [BitmapDecoder 스레드 확인](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/Media/Imaging/BitmapDecoder.cs), [TextSearch의 이름 경로와 ToString fallback](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/TextSearch.cs)이다(확인2026-10-16).

## native 입력 단기 진단

Windows workflow 수동 실행의 `native_input_probe:true`는 가짜 입력과 runner temp 설정으로 표정/트레이/의상 직전 UI 상태를 준비한 뒤 HandMotionQa를 실행한다. core/lint/build를 수행하지만 배포·서명·전체 화면·portable 검사를 생략하므로 release 합격이 아니다. 결과에 `probeOnly:true`를 기록한다. 정상 push/일반 수동 실행은 기존 전체 QA를 그대로 수행한다. 짧은 경로는 GITHUB_ACTIONS와검증된RUNNER_TEMP 내부QA폴더에서만활성화한다. hook trace의1=down진입,2=UI예약,3=전달,4=세대/해제거부,6=disposed,7=handle0,8=이미예약된신호병합이다.

진단에는1000+WM_MOUSE 메시지정수와전용시험창의PreviewMouseDown 수도포함한다. 좌표와외부창내용은읽지않는다. 짧은재현도숫자데모를켜서UI타이머가활성화된상태를사용한다.

단기입력재현은100회hook등록/press1회/해제/press1회를대조한다. 정상전체와배포본은20회를유지한다. 이확대진단은간헐누락에대한추가근거이며배포합격을대체하지않는다.

입력시험은각hook설치뒤owner HWND가전용native준비메시지를실제로처리한ack를확인하고주입한다. 이메시지는마우스동작을가짜로실행하지않으며이벤트수에포함하지않는다. 그다음down/up은한배열로1회만보낸다.

손동작 좌클릭 hook는 전용 STA dispatcher에서 설치/수신/해제하고 UI에는 Input 작업만 예약한다. 등록과 owner loop 준비는 MouseReady로 비동기 통지하며 UI 스레드를 기다리게 하지 않는다. QA는 매회 ready와 실제 unhook 완료, press1회 전달/해제후0을 확인한다.

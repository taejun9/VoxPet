# VoxPet

**목소리로 움직이는 캐릭터** — 마이크 음량에 맞춰 PNG 캐릭터의 입과 몸이 반응하는 Windows 데스크톱 앱입니다. 얼굴캠 없이 캐릭터를 표시하거나 OBS 방송 화면에 넣을 수 있습니다.

선택한 마이크의 입력을 로컬 메모리에서 분석합니다. 음성 녹음·저장·원격 전송, 웹캠 및 얼굴 추적 기능은 없습니다. 말의 내용이나 감정을 인식하는 앱이 아니라 **음량에 반응하는 앱**입니다.

## 주요 기능

- 마이크 선택·새로고침과 Start/Stop, RMS·Peak·dBFS 및 Voice Level 표시
- Noise Gate, Sensitivity, Attack/Release, 고급 입력 범위 조정
- 3초 주변 소음 측정으로 Gate 추천·직접 적용/취소, Windows 마이크 권한 설정 바로가기
- 일반 대화·조용한 목소리·빠른 반응 프리셋, Ctrl+Shift+M 전역 캐릭터 입 음소거
- 입 3단계와 눈 깜빡임, 대기 움직임을 표현하는 기본 CC0 고양이
- 로컬 투명 PNG 시트 불러오기와 기본 캐릭터 복원
- 12개 영구 표정 슬롯, 기본 표정 6종과 슬롯별 사용자 PNG, 눈물 모션·위치 조정
- Ctrl+Shift+F1~F11 전역 전환과 약 240ms 표정 전환, F12는 앱 창 내부에서 사용
- 설정 화면과 상태를 공유하는 별도 방송창, 초록 배경·투명 배경·항상 위 옵션
- 마이크 없는 합성 데모, 오디오 조정값과 방송창 옵션의 로컬 저장

## 빠른 시작

**처음 사용하는 분은 [설치 방법](docs/guides/installation.md) → [사용 방법](docs/guides/usage.md) → [설정 방법](docs/guides/settings.md) 순서로 읽어 주세요.** 다운로드와 압축 풀기부터 실제 버튼을 누르는 순서까지 설명합니다. 프로그래밍 지식이나 명령어 입력은 필요 없습니다.

1. Windows x64용 자체 포함 배포 폴더를 모두 풀고 `VoxPet.exe`를 실행합니다. 별도 .NET 설치는 필요 없습니다. `Licenses/`, `Assets/Characters/manifest.json`, `USER-GUIDE.txt`도 함께 보관합니다.
2. **마이크 없이 데모**로 캐릭터를 확인하거나, 마이크를 선택하고 **Start**를 누른 뒤 말합니다.
3. **조용한 목소리** 등의 프리셋으로 반응을 맞춥니다. 마이크를 해제하려면 **Stop**을 누릅니다.
4. 방송에 사용하려면 **캐릭터 방송창 열기 ↗**를 누르고 OBS Window Capture에서 `VoxPet Character`를 선택합니다. 초록 배경과 Chroma Key를 함께 사용할 수 있습니다.

캐릭터 음소거는 입 반응만 멈추고 마이크 캡처는 계속합니다. 장치 변경은 Stop 후에 합니다. 상세 조정, PNG 규격, OBS 연결 및 문제 해결은 **[사용 설명서](docs/user-guide.md)**에 있습니다. 배포 폴더에서도 [텍스트 설명서](docs/distribution/USER-GUIDE.txt)를 읽을 수 있습니다.

개인 PNG는 **3열×2행 투명 RGBA 시트**(입 3단계 × 눈 뜸/감음)를 사용합니다. 일반 불러오기는 현재 실행에만 적용됩니다. **표정 · 단축키 슬롯**에서 PNG와 눈물 위치를 선택하고 **슬롯 저장**을 누르면 관리 복사본이 다음 실행에도 유지됩니다. 시작 시 F1 슬롯을 표시합니다. [표정 슬롯 사용법](docs/user-guide.md#표정-슬롯과-단축키)에 저장·단축키 충돌·F12 사용 조건을 설명했습니다.

## 프로젝트 구조

```text
VoxPet/
├── VoxPet.sln                   # Core + Windows App + Core Tests 솔루션
├── global.json                 # .NET SDK 10.0.401 고정
├── Directory.Build.props       # 공통 nullable·분석기 설정
├── src/
│   ├── VoxPet.Core/             # net10.0: UI·NAudio에 독립적인 계산과 수명 계약
│   │   ├── Models/             # 측정값·설정·캐릭터 상태·프리셋·PNG 시트 규격
│   │   └── Services/           # PCM 분석·평활화·세션·캡처 루프·애니메이션
│   └── VoxPet.App/              # net10.0-windows: WPF 앱, win-x64
│       ├── App.xaml[.cs]        # 공통 스타일·실행 진입점·Windows smoke QA
│       ├── Services/           # WASAPI 입력·로컬 설정·PNG 시트 로딩·전역 단축키
│       ├── ViewModels/         # UI 명령·표시 상태·공유 캐릭터·변경 알림
│       ├── Views/              # 설정창·방송창 및 WPF QA
│       └── Assets/Characters/  # 기본 PNG 36장(표정6×입3×눈2)과 출처·anchor manifest
├── tests/VoxPet.Core.Tests/     # 합성 PCM·가짜 입력을 사용하는 xUnit 테스트
├── harness/
│   ├── scripts/                # 문서·자산 검사, Windows/OBS QA, 자산 생성
│   └── templates/              # 실행 계획·리뷰·회의 템플릿
├── docs/                       # 제품·설계·개발·사용·검증·개인정보 문서
│   ├── distribution/           # 배포용 안내와 라이선스 고지
│   ├── exec_plans/             # active 실행 계획과 completed 기록
│   └── reviews/                # 완료 계획의 리뷰 미러
└── .github/workflows/          # Windows 전체 QA와 OBS 합성 캡처 QA
```

`artifacts/`는 로컬 배포본과 QA 증거, `.worktree/`는 작업별 Git checkout을 위한 경로이며 Git 추적에서 제외됩니다.

## 동작 흐름과 책임

```text
마이크 → WASAPI → PCM 분석(RMS/Peak/dBFS) → 최신 숫자 snapshot
       → Gate·정규화·Sensitivity → Attack/Release → VoiceLevel(0~1)
       → 입 상태·눈 깜빡임·몸 이동 → WPF 미리보기 / 방송창
```

`AudioAnalyzer`는 전체 채널의 에너지를 계산하고, `AudioLevelProcessor`는 시간 기반 평활화로 유한한 0~1 레벨을 만듭니다. `AudioSession`은 시작·중지·해제를 직렬화하고 세션별 콜백과 최신 측정값을 분리합니다.

Windows의 `AudioCaptureService`는 전용 스레드에서 WASAPI를 읽고 PCM 대신 숫자만 전달합니다. `MainViewModel`은 UI 타이머에서 최신 값으로 표시를 갱신합니다. `CharacterViewModel` 하나를 두 창이 공유하므로 방송창을 열어도 마이크 캡처가 중복되지 않습니다. 자세한 계약과 자원 해제 순서는 [앱 설계](docs/architecture/application.md)에 있습니다.

## 개발과 검증

저장소에서 고정한 환경은 **.NET SDK 10.0.401**, **WPF**, **NAudio.Wasapi 2.2.1**입니다. App의 실행은 Windows에서만 가능합니다. 우선 검증 대상은 Windows 11 x64이며 OS별 실제 검증 상태는 아래 기록을 따릅니다.

저장소 루트에서 문서·자산 검사(Python 3.10 이상):

```sh
python3 harness/scripts/verify_base.py
python3 harness/scripts/verify_app.py
git diff --check
```

빌드와 Core 테스트:

```sh
dotnet restore VoxPet.sln --locked-mode
dotnet format VoxPet.sln --verify-no-changes --no-restore
dotnet build VoxPet.sln --configuration Release
dotnet test tests/VoxPet.Core.Tests/VoxPet.Core.Tests.csproj --configuration Release
```

Windows에서 개발 실행:

```sh
dotnet run --project src/VoxPet.App/VoxPet.App.csproj
```

Windows PowerShell에서 배포와 마이크 없는 WPF smoke를 포함한 전체 QA:

```powershell
./harness/scripts/qa.ps1 -Publish -Smoke
```

배포 결과는 `artifacts/win-x64/`에 생성됩니다. SDK·잠금 파일·교차 빌드의 제한, 개별 publish 명령과 OBS QA 실행 조건은 [개발 환경](docs/quality/development.md)을 참고합니다. Core 테스트는 실제 마이크를 사용하지 않습니다.

## 현재 검증 상태

plan013에서 macOS/Windows Core **110개 테스트**, format/analyzer, Release 빌드(경고·오류 0건), 자체 포함 배포, Windows WPF 화면 검사 **239개**와 OBS WGC 합성 캡처 회귀를 통과했습니다. 4개 창 크기에서 새 버튼을 포함한 49개 컨트롤의 접근성과 Start/Stop 위치를 확인했습니다. Ctrl+Shift+M과 3초 소음 추천·취소·종료는 합성 입력 시험이며 물리 마이크/권한/키보드 실기와 구분합니다. [기능 조사](docs/product/user-feature-research.md)에 18개 후보와 우선순위를 정리했습니다.

이전 plan012에서 macOS/Windows Core **97개 테스트**, format/analyzer, Release 빌드와 자체 포함 배포, WPF smoke 및 OBS WGC 합성 데모가 통과했습니다. 늘보군 개인 시트로 입·눈 여섯 상태, 슬롯 저장·재실행·손상 복구, 표정 전환·무음 눈 깜빡임·눈물과 실제 Windows 미리보기/방송창 PNG를 확인했습니다. 빌드·CI 경고와 오류는 0건입니다. 늘보군은 사용자 요청에 따라 개인 시험 자료로 보존하며 기본 공개 배포는 CC0 고양이를 사용합니다. **실제 마이크·권한·장치 제거, 사용자 GPU/OBS, 물리 키보드·DPI 변경, 실제 입력 장시간 시험은 미실행**입니다. 환경·커밋·화면 증거와 시험 방법은 [Windows 검증 기록](docs/quality/windows-checklist.md)을 확인합니다.

## 문서 안내와 기여

- 사용자: [사용 설명서](docs/user-guide.md), [개인정보 원칙](docs/privacy/principles.md)
- 기능 조사: [사용자 기능 목록·구현 격차·우선순위](docs/product/user-feature-research.md)
- 제품과 개발: [제품 정의](docs/product/product.md), [로드맵](docs/product/roadmap.md), [앱 설계](docs/architecture/application.md), [개발 환경](docs/quality/development.md)
- 작업 절차: [AGENTS.md](AGENTS.md), [하네스와 Git](docs/architecture/harness.md), [QA·리뷰 규칙](docs/quality/rules.md)
- 근거와 기록: [공식 자료](docs/references/official-sources.md), [실행 계획](docs/exec_plans/), [리뷰](docs/reviews/)

변경 작업은 실행 계획을 먼저 작성하고 `codex/plan-NNN-<task>` 브랜치와 `.worktree/plan-NNN-<task>`에서 진행합니다. 기본 캐릭터의 CC0 출처와 제3자 패키지 라이선스 고지는 [manifest](src/VoxPet.App/Assets/Characters/manifest.json) 및 [배포 고지](docs/distribution/)에서 확인합니다. 외부 캐릭터는 이용 권리가 있는 파일을 사용합니다.

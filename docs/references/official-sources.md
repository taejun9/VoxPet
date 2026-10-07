# 공식 자료 레지스트리

확인일은 아래 표에 사용자 시간대 기준으로 기록한다. 사용자 첨부는 제품 요구사항이며 API/호환성의 공식 근거와 구분한다.
링크는 실제 확인한 페이지이며 사용되는 수식·폴더·기본값은 VoxPet의 설계 결정이다.

| source | url | scope | checked_at | used_for |
|---|---|---|---|---|
| Microsoft WPF Overview | https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/ | Windows UI | 2026-10-05 | WPF Windows 실행 제한, XAML/바인딩/애니메이션 |
| WPF ScrollViewer Source | https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/ScrollViewer.cs | 휠 입력 | 2026-10-05 | 내측 OnMouseWheel의 handled 처리 확인; 단일 scroll 영역과 실제 routed-event 검사 |
| WPF SystemParameters Source | https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/SystemParameters.cs | 초기 창 bounds | 2026-10-05 | WorkArea의 pixel→logical 변환 확인; 주 화면 작업 영역에 초기 크기 제한 |
| Microsoft WPF Threading Model | https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/threading-model | UI 스레드 | 2026-10-05 | Dispatcher 경계, UI thread 작업량 제한 |
| Microsoft .NET Support Policy | https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core | SDK/runtime 수명 | 2026-10-05 | .NET 10 LTS 선택 및 .NET 8 지원 종료 확인 |
| NAudio 공식 저장소 | https://github.com/naudio/NAudio | 오디오 라이브러리 | 2026-10-05 | WASAPI 지원, NAudio 3 net9.0 이상 요구와 2.x 구분 |
| NAudio 2.x WasapiCapture | https://github.com/naudio/NAudio/blob/release/2.x/NAudio.Wasapi/WasapiCapture.cs | 캡처 API | 2026-10-05 | 기획의 WasapiCapture는 2.x 계열 API임을 확인 |
| NAudio 2.2.1 NuGet | https://www.nuget.org/packages/NAudio/2.2.1 | 패키지 후보 | 2026-10-05 | .NET 8 호환 후보 확인; 최신 2.x 선택은 구현 때 재검토 |
| OBS Window Capture Sources | https://obsproject.com/kb/window-capture-sources | 방송 캡처 | 2026-10-06 | 창 단위 캡처와 방식 옵션; 초보자 OBS 안내, WPF alpha 보장 근거는 아님 |
| Microsoft dotnet test | https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test | 테스트 CLI | 2026-10-05 | 후속 테스트 실행 경로; SDK/러너 옵션은 생성 후 확인 |
| Microsoft .NET 10 Supported OS | https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md | OS/architecture | 2026-10-05 | 지원 중인 Windows edition/build 우선, 전체 Windows 10 보장 제외 |
| Microsoft single-file deployment | https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview | 배포 | 2026-10-05 | 자체 포함 win-x64 단일 실행파일과 native extraction 옵션 |
| xunit 2.9.3 NuGet | https://www.nuget.org/packages/xunit/2.9.3 | 테스트 패키지 | 2026-10-05 | pinned 테스트 프레임워크 |
| Microsoft.NET.Test.Sdk 17.14.1 NuGet | https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/17.14.1 | 테스트 패키지 | 2026-10-05 | pinned 테스트 호스트 |
| Microsoft Window.Owner | https://learn.microsoft.com/en-us/dotnet/api/system.windows.window.owner?view=windowsdesktop-10.0 | WPF 창 수명 | 2026-10-05 | 소유 창 최소화 시 방송창도 최소화됨; 독립 창과 명시적 종료 선택 |
| OBS Portable Mode | https://obsproject.com/kb/portable-mode | QA 설치 격리 | 2026-10-05 | disposable Windows runner의 portable config |
| OBS Launch Parameters | https://obsproject.com/kb/launch-parameters | QA 실행 | 2026-10-05 | 실행 working directory와 업데이트/외부 플러그인 억제 옵션 |
| OBS Remote Control Guide | https://obsproject.com/kb/remote-control-guide | 테스트 제어 | 2026-10-05 | 로컬 인증 WebSocket; 생산 앱에는 미사용 |
| OBS WebSocket Protocol | https://github.com/obsproject/obs-websocket/blob/master/docs/generated/protocol.md | 캡처 QA API | 2026-10-05 | Window Capture property 조회, Source Screenshot/Chroma Key/상태 확인 |
| OBS 32.2.2 Release | https://github.com/obsproject/obs-studio/releases/tag/32.2.2 | QA binary | 2026-10-05 | 공식 x64 ZIP SHA256 검증, OBS 32.2.2/WS 5.7.4 실제 실행 |
| WPF Window Template | https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/Themes/XAML/Window.xaml | 방송창 resize | 2026-10-05 | ResizeGrip 영역을 유지하고 표시만 숨김 |
| WPF Window Source | https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Window.cs | native hit test | 2026-10-05 | WM_NCHITTEST와 HTBOTTOMRIGHT 회귀 검사 |
| GitHub Downloading Workflow Artifacts | https://docs.github.com/en/actions/how-tos/manage-workflow-runs/download-workflow-artifacts?tool=webui | 실행 파일 다운로드 | 2026-10-06 | 웹 로그인·실행 결과 Artifacts 다운로드·파일 만료 안내 |
| Microsoft Windows 마이크 권한 | https://support.microsoft.com/ko-kr/windows/privacy/turn-on-app-permissions-for-your-microphone-in-windows | 마이크 설정 | 2026-10-06 | Windows 11 개인정보 및 보안과 데스크톱 앱 마이크 접근 |
| Microsoft Fix Microphone Problems | https://support.microsoft.com/en-us/windows/hardware/drivers/fix-microphone-problems | 입력 장치·볼륨 | 2026-10-06 | Windows 소리 설정의 입력 선택과 입력 볼륨 |
| Microsoft 파일 압축 및 압축 해제 | https://support.microsoft.com/ko-kr/windows/experience/storage-filemanagement/zip-and-unzip-files | ZIP 압축 풀기 | 2026-10-06 | 초보자 설치 안내의 마우스 오른쪽 메뉴·전체 추출 |
| Microsoft 32-bit and 64-bit Windows FAQ | https://support.microsoft.com/en-gb/windows/experience/compatibility/32-bit-and-64-bit-windows-frequently-asked-questions | 컴퓨터 종류 확인 | 2026-10-06 | 설정 → 시스템 → 정보의 시스템 종류 확인 경로 |

## 남은 근거와 실기 확인

- 정확한 Windows 10/11 빌드와 .NET 지원 OS 매트릭스, SDK/Visual Studio 버전 조합.
- 패키지 버전은 고정했고 공식 소스로 이벤트 수명을 확인했다. 실제 장치의 WaveFormat은 Windows 실기로 확인한다.
- WPF 투명 창의 OBS alpha 처리: OS/OBS/캡처 방식별 실기 검증 필요.
- 기본 PNG는 독자 제작 CC0로 manifest를 추가했다. 성능/지연 측정 결과는 아직 없다.

외부 API/지원 정책이 바뀌는 작업에서는 확인일과 사용 범위를 갱신한다.
기획의 Windows 10/11 목표를 모든 빌드에 대한 지원 보장으로 해석하지 않는다.

| Microsoft RegisterHotKey | https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey | 전역 단축키 | 2026-10-07 | MOD_NOREPEAT, 충돌 처리, F12 디버거 예약 |
| Microsoft UnregisterHotKey | https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-unregisterhotkey | 단축키 수명 | 2026-10-07 | 소유 thread/window에서 전역 등록 해제 |
| Microsoft HwndSource.AddHook | https://learn.microsoft.com/en-us/dotnet/api/system.windows.interop.hwndsource.addhook?view=windowsdesktop-10.0 | WPF native 메시지 | 2026-10-07 | WM_HOTKEY 처리와 RemoveHook 정리 |
| GitHub actions/checkout v7.0.1 | https://github.com/actions/checkout/releases/tag/v7.0.1 | CI checkout | 2026-10-07 | 공식 릴리스와 v7 action.yml의 Node24 실행 확인 |
| GitHub actions/setup-dotnet v6.0.0 | https://github.com/actions/setup-dotnet/releases/tag/v6.0.0 | SDK CI 설치 | 2026-10-07 | Node24/ESM 및 의존성 갱신, 기존 global-json-file 입력 유지 |
| GitHub actions/setup-python v7.0.0 | https://github.com/actions/setup-python/releases/tag/v7.0.0 | Python CI 설치 | 2026-10-07 | Node24/ESM 및 의존성 갱신, 기존 python-version 입력 유지 |
| GitHub actions/upload-artifact v7.0.1 | https://github.com/actions/upload-artifact/releases/tag/v7.0.1 | QA 증거 업로드 | 2026-10-07 | Node24 및 의존성 갱신, 기존 name/path/if-no-files-found 입력 유지 |

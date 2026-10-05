# 공식 자료 레지스트리

확인일은 사용자 시간대 기준 2026-10-05. 사용자 첨부는 제품 요구사항이며 API/호환성의 공식 근거와 구분한다.
링크는 실제 확인한 페이지이며 사용되는 수식·폴더·기본값은 VoxPet의 설계 결정이다.

| source | url | scope | checked_at | used_for |
|---|---|---|---|---|
| Microsoft WPF Overview | https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/ | Windows UI | 2026-10-05 | WPF Windows 실행 제한, XAML/바인딩/애니메이션 |
| Microsoft WPF Threading Model | https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/threading-model | UI 스레드 | 2026-10-05 | Dispatcher 경계, UI thread 작업량 제한 |
| Microsoft .NET Support Policy | https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core | SDK/runtime 수명 | 2026-10-05 | .NET 10 LTS 선택 및 .NET 8 지원 종료 확인 |
| NAudio 공식 저장소 | https://github.com/naudio/NAudio | 오디오 라이브러리 | 2026-10-05 | WASAPI 지원, NAudio 3 net9.0 이상 요구와 2.x 구분 |
| NAudio 2.x WasapiCapture | https://github.com/naudio/NAudio/blob/release/2.x/NAudio.Wasapi/WasapiCapture.cs | 캡처 API | 2026-10-05 | 기획의 WasapiCapture는 2.x 계열 API임을 확인 |
| NAudio 2.2.1 NuGet | https://www.nuget.org/packages/NAudio/2.2.1 | 패키지 후보 | 2026-10-05 | .NET 8 호환 후보 확인; 최신 2.x 선택은 구현 때 재검토 |
| OBS Window Capture Sources | https://obsproject.com/kb/window-capture-sources | 방송 캡처 | 2026-10-05 | 창 단위 캡처와 방식 옵션; WPF alpha 보장 근거는 아님 |
| Microsoft dotnet test | https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test | 테스트 CLI | 2026-10-05 | 후속 테스트 실행 경로; SDK/러너 옵션은 생성 후 확인 |

| Microsoft .NET 10 Supported OS | https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md | OS/architecture | 2026-10-05 | 지원 중인 Windows edition/build 우선, 전체 Windows 10 보장 제외 |
| Microsoft single-file deployment | https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview | 배포 | 2026-10-05 | 자체 포함 win-x64 단일 실행파일과 native extraction 옵션 |
| xunit 2.9.3 NuGet | https://www.nuget.org/packages/xunit/2.9.3 | 테스트 패키지 | 2026-10-05 | pinned 테스트 프레임워크 |
| Microsoft.NET.Test.Sdk 17.14.1 NuGet | https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/17.14.1 | 테스트 패키지 | 2026-10-05 | pinned 테스트 호스트 |

## 남은 근거와 실기 확인

- 정확한 Windows 10/11 빌드와 .NET 지원 OS 매트릭스, SDK/Visual Studio 버전 조합.
- 패키지 버전은 고정했고 공식 소스로 이벤트 수명을 확인했다. 실제 장치의 WaveFormat은 Windows 실기로 확인한다.
- WPF 투명 창의 OBS alpha 처리: OS/OBS/캡처 방식별 실기 검증 필요.
- 기본 PNG는 독자 제작 CC0로 manifest를 추가했다. 성능/지연 측정 결과는 아직 없다.

외부 API/지원 정책이 바뀌는 작업에서는 확인일과 사용 범위를 갱신한다.
기획의 Windows 10/11 목표를 모든 빌드에 대한 지원 보장으로 해석하지 않는다.

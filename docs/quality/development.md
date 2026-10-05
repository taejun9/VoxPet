# 개발 환경과 현재 실행 명령

현재는 설계·하네스 저장소다. dotnet 프로젝트, NuGet 참조, 앱 실행 명령은 아직 없다.
기존 README는 보존했으며 개발 진입점은 [AGENTS.md](../../AGENTS.md)이다.

## 지금 실행 가능

저장소 루트에서 Python 3.10 이상(표준 라이브러리만 사용):

```sh
python3 harness/scripts/verify_base.py
git diff --check
```

첫 명령은 문서 구조 검증이다. 앱 빌드나 동작 성공을 의미하지 않는다.

## 구현 시 확정할 환경

| 항목 | 기획 기준 / 결정 상태 |
|---|---|
| OS/CPU | Windows 10/11 x64 기획; 실제 지원 빌드와 .NET OS 지원 정책 확인 필요 |
| SDK/TFM | .NET 8, Core net8.0 / App net8.0-windows, WPF; 아직 global.json 없음 |
| IDE | Visual Studio 2022 / VS Code 기획; 선택 SDK와 호환 버전은 scaffold 시 확인 |
| 오디오 | .NET 8에서는 NAudio 2.x. 2.2.1은 공식 NuGet에서 확인한 호환 후보이며 최신 버전이라고 주장하지 않는다 |
| 렌더링 | 내장 WPF PNG 사용; SkiaSharp/Live2D 패키지는 필요 없음 |
| MVVM | 작은 ViewModel/command 구현부터; 외부 MVVM/DI 패키지는 미선정 |
| 테스트 | Core의 합성 신호 테스트; 테스트 SDK/프레임워크와 버전은 scaffold 시 고정 |

[공식 근거](../references/official-sources.md): WPF는 Windows에서 실행한다.
.NET 8의 공식 지원 종료는 **2026-11-10**이다. NAudio 3는 net9.0 이상을 요구한다.
기획대로 .NET 8을 기록했지만 실제 새 앱 착수 전에 유지/현행 LTS 전환 결정을 남긴다.

## 솔루션 생성 후 도입할 명령 — 지금은 실행 불가

아래는 [설계](../architecture/application.md)의 폴더를 생성한 뒤 검증하여 등록할 예정인 명령이다.

```sh
dotnet restore VoxPet.sln
dotnet build VoxPet.sln --configuration Release
dotnet test tests/VoxPet.Core.Tests/VoxPet.Core.Tests.csproj --configuration Release
dotnet run --project src/VoxPet.App/VoxPet.App.csproj
```

선택한 SDK/테스트 러너에 따라 옵션을 확인한다. 문서상 예시는 실행된 결과가 아니다.
WPF 실행·WASAPI·OBS QA는 Windows 호스트에서 한다.
현재 macOS 호스트에는 dotnet CLI가 없으며 SDK나 Windows 도구를 설치하지 않았다.
순수 Core 테스트의 타 OS 실행은 솔루션 생성 후 검증한다.

# plan-007-test-lint Review

## Summary

모든 적용 가능한 프로젝트 자동 테스트·린트와 실제 Windows 앱 화면 QA를 수행한 뒤 단일 에이전트 자체 리뷰를 진행했다.
C# 8파일의 공백 오류179건 수정 및 qa.ps1 format gate가 범위이며 앱 동작 변경은 없다.

## QA

- macOS arm64 SDK10.0.401: locked restore 통과, Release 경고/오류0, Core71/71(실패0/skip0).
- `dotnet format VoxPet.sln --verify-no-changes --no-restore`: 수정 후 exit0/report=[]; 최초179건은 모두 WHITESPACE.
- `python3 harness/scripts/verify_base.py`, `verify_app.py`, `git diff --check`: 통과. Python 하네스 전체 구문 검사 통과.
- 312916f [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37294330257): format/build/test/publish/실제 WPF smoke 통과. TRX71/71 및 smoke failures0/failedChecks=[] 직접 확인.
- 실제 WPF 창4크기(1000×730,960×540,640×480,480×320)에서 15컨트롤 접근, 작은창 wheel, 주입한3작업영역의 startup bounds 통과. 각크기 PNG 직접 확인.
- [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/37294330480): OBS32.2.2/WindowsServer2025 build26100 WGC green/keyed/native alpha PNG 직접 확인. Chroma Key 후 green0%/alpha66.04%, native alpha65.08%, green/transparent 각7프레임 변화, errors=[]/passed=true.
- 증거는 root artifacts/qa/plan007/summary.json 및 TRX/format reports/PNG/로그에 보존한다. 생성 증거는 Git에 넣지 않는다.

## Findings

1. 기존 기본 QA에는 .NET format gate가 없어 서식 오류179건이 빌드 성공 뒤에도 남았다. SDK formatter로 수정하고 locked restore 뒤 verify-no-changes를 필수 실행하도록 해결했다.
2. 소스8파일의 공백 제거 전후 내용이 동일하다. 전체 테스트를 수정 후 재실행했고 오디오 스레드/수명/렌더러/VoiceLevel/설정/개인정보 계약 변경이 없다.
3. 하네스가 format 비정상 exit를 즉시 실패로 처리한다. 검사 비활성화·warning suppression·잠금 패키지 변경은 없다.
4. README와 root Markdown 정책을 유지했다. 개발/품질 문서에 새 명령을 반영했다. 기능 회귀나 미해결 프로젝트 lint 오류는 발견하지 않았다.

## Residual Risk

실제 마이크/권한/USB 제거/실제 모니터 DPI 변경/실제 입력60분은 미실행이다.
WPF screenshot의 1.5/2.0 scale은 raster 검증이며 실제 DPI 전환이 아니다. smoke는 실제 WPF 앱 안의 합성 입력과 UI fixture이며 수동 마이크 조작 증거가 아니다.
OBS BitBlt는 유효 프레임을 얻지 못했다. WGC 지원 경로 합격과 전체 캡처 방식 호환성은 구분한다.
기존 GitHub Actions 버전의 Node20/Node API deprecation 알림은 남지만 앱 빌드 warning/error는0이며 format 검사도 통과했다.

## Follow-Ups

Windows 사용자 환경의 실기는 windows-checklist.md의 기존 매트릭스에 따라 수행한다.
CI action runtime 갱신은 공식 버전/runner 호환성을 확인하는 별도 계획에서 진행한다.

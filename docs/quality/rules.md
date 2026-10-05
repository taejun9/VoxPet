# QA와 리뷰 규칙

## 계획과 Git

No Exec Plan, No Work. `docs/exec_plans/active/plan-NNN-<task>.md`를 먼저 작성한다.
제목과 파일명을 일치시키고 Status는 active, 완료 후 completed로 바꾼다.
범위/접근법은 Decision Log에 기록. `docs/plan` 금지. root Markdown은 AGENTS.md/README.md만.
작업은 `codex/plan-NNN-<task>` / `.worktree/plan-NNN-<task>`에서 수행한다.
구체적인 [Git 절차](../architecture/harness.md)를 따른다.

## 자동 QA

- `python3 harness/scripts/verify_base.py`: 필수 파일, 로컬 Markdown 링크 대상, 계획명/제목/상태, 완료 리뷰 미러, 공식 출처 URL·날짜, 미작성 표시 잔여를 확인.
- `python3 harness/scripts/verify_app.py`: PNG/manifest/잠금/금지 API 계약 검사.
- `dotnet build VoxPet.sln -c Release`: 컴파일과 XAML 검사.
- `dotnet test tests/VoxPet.Core.Tests/VoxPet.Core.Tests.csproj -c Release`: 합성 수치·수명·캐릭터·설정 복구 검사.
- `git diff --check`: 공백 오류 확인. 새 파일은 stage 후에도 검사한다.
- 기존 README 보존과 기획 대비 범위를 사람이 확인한다.

검증기는 외부 링크의 최신성, 음성 처리 정확도, 실제 테스트 수행, 리뷰의 독립성을 판정하지 않는다.
미작성 표시는 템플릿에만 허용. 아직 결정하지 않은 항목은 이유와 다음 확인 단계를 적는다.

## 자동 검사 및 Windows 실기 합격 조건

- RMS: 무음 0, full-scale 일정 신호 1, full-scale sine 약 0.7071 (-3.0103 dBFS).
- Peak/채널/PCM 형식: signed sample, 다채널 반대 위상, clipping, 빈/잘못된 입력.
- Processor: gate 미만·동일·이상, min/max, sensitivity, 유한한 0~1 보장, 잘못된 설정.
- 시간: attack/release의 monotonic 변화, 주기 독립성, 0ms, 무음 정착, 무입력 timeout, Stop reset.
- 합성 dB 배열: 무음 → 짧은 음절 → 유지 발화 → 무음. 실제 마이크 없이 재현한다.
- Windows 실기: no device, 권한 거부, 연결 제거, 반복 Start/Stop, 장치 교체, 종료 중 callback, 예외 복구.
- 캐릭터: 0.2/0.6 경계, idle/blink 독립, body 상한, UI 응답성, DIP/DPI와 자산 정렬.
- OBS: 버전·OS 빌드·GPU·캡처 방식·배경/alpha·창 가림 상태·장시간 실행을 기록.

gate만으로 음성/소음을 구별했다고 주장하지 않는다. 성능/지연 수치는 측정 환경과 합격 기준을 계획에 먼저 작성한다.

## QA → 리뷰 → 완료

QA 결과를 계획에 남긴 후 리뷰를 시작한다. 요구사항·수식·thread/lifetime·privacy·문서 최신성을 별도로 검토한다.
검증/리뷰 역할은 별개 단계이며 단일 에이전트가 수행했다면 자체 리뷰로 명시한다. 요청 없이 worker를 생성하지 않는다.
실패는 수정하거나 범위에 대한 이유를 기록한다. 합격하지 않은 앱을 합격이라고 쓰지 않는다.
완료 계획을 completed/로 이동하고 `docs/reviews/plan-NNN-<task>-review.md`를 작성한다.
성공한 QA 명령, 발견/수정 사항, 남은 제한, 후속 작업을 기록한 뒤 Git 수명 절차를 완료한다.

Windows 자동 실행은 `qa.ps1 -Publish -Smoke`, 실제 마이크/OBS/장시간은 [실기 체크리스트](windows-checklist.md)를 따른다.

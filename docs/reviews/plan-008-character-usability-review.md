# plan-008-character-usability Review

## Summary

2026-10-06, f5f1757 코드와 완료 문서를 전체 QA 후 단일 에이전트가 자체 리뷰했다. 사용자의 요청에 따라 수요가 있는 캐릭터 교체/반응 프리셋/음소거를 구현하고 늘보군 개인 시트로 실제 Windows 합성 데모를 수행했다. 별도 reviewer 에이전트는 실행하지 않았다.

## QA

- Python verify_base.py/verify_app.py, git diff --check 통과. 기존 README와 기본 PNG 변경 없음.
- SDK10.0.401 macOS arm64 locked restore, format/analyzer, Release build 경고·오류0 및 Core84/84 통과. 초기 sandbox NuGet/IPC 실패는 승인된 환경에서 재실행했다.
- [최종 Windows 기본 QA](https://github.com/taejun9/VoxPet/actions/runs/37338899300)와 [늘보군 QA](https://github.com/taejun9/VoxPet/actions/runs/37338981751): Core84/84, Release/format/publish, WPF smoke failures0, 4크기×21컨트롤/3startup bounds/작은 창 휠 및 미리보기 전체 가시성 통과.
- 늘보군 six-state PNG 및 실제 preview PNG를 시각 확인했다. 파일 오류 후 기존 캐릭터 보존, 원본 파일 해제, freeze/행열 매핑/정렬, mute→unmute→Stop와 blink/입력 계속 표시를 확인했다.
- [최종 OBS QA](https://github.com/taejun9/VoxPet/actions/runs/37338899198): 기본 캐릭터 WGC의 7프레임 변화/Chroma Key/native alpha65.08% 유지 통과. BitBlt 유효 프레임은 얻지 못했다.
- 증거와 SHA256은 무시된 artifacts/qa/plan008/summary.json에 있다. 개인 이미지가 있는 원격 증거 artifact는 로컬에 수집한 뒤 삭제했다.

## Findings

- 해결: 캐릭터 교체 결과가 마이크 Running/Stop 상태를 덮어쓰던 초안을 별도 CharacterStatus로 분리했다. 실행 중 실패와 성공 후 상태 보존 smoke를 확인했다.
- 해결: 늘어난 controls 높이로 preview 카드 하단이 첫 화면에서 잘리던 회귀를 실제 Windows PNG에서 발견했다. 상단 정렬과 viewport 가시성 검사를 추가했고 최종 PNG에서 레벨/metrics/방송창 버튼까지 모두 보였다.
- 해결: 생성 시트의 셀별 위치 편차를 알파 영역 중앙/바닥 정렬로 보정했다. 여섯 상태를 모두 만든 뒤 UI에서 원자적으로 교체하고 실패 시 이전 상태를 유지한다.
- 해결: draft 입력 조회의 read-only 토큰 실패 후 contents:write 초안은 자동 승인 검토에서 거부됐다. 모든 workflow를 contents:read로 유지하며 단일 파일의 만료 URL만 임시 secret으로 전달했다. 계정 토큰은 전달/출력하지 않았다. 임시 secret/draft/개인 이미지 artifact 정리를 완료했다.
- 요구사항/회귀 검토: 프리셋은 AudioSettings 검증 범위 내이고 quiet input/attack/release 효과를 수치 테스트했다. 음소거는 출력 반응만 0으로 만들고 원본 숫자/세션을 보존하며 UI에서 Stop과 차이를 명시한다.
- 스레드/개인정보 검토: 파일 IO/디코딩은 Task.Run, 프레임은 OnLoad/Freeze, 닫기 중 결과는 적용하지 않는다. 마이크 엔진/PCM/설정 저장 형식은 변경하지 않는다. 개인 PNG/경로/음소거는 영구 저장하지 않는다.
- 미해결 코드 결함은 발견하지 않았다.

## Residual Risk

실제 마이크·권한·제거·물리 DPI·사용자 GPU/OBS·실제 입력 장시간은 미실행이다. OBS 회귀는 내장 고양이로 수행했으며 늘보군의 OBS 캡처 실기는 별도다.
외부 PNG는 8비트 RGBA 및 여백이 있는 3×2 정사각형 셀만 지원한다. 팔레트 PNG/16비트/다른 레이아웃은 안내대로 변환이 필요하다.
늘보군은 사용자 첨부의 생성 파생 개인 시험 자산이다. 공개 재배포 권리를 확인하지 않았고 CC0로 주장하지 않으며 기본 공개 자산에 포함하지 않는다.

## Follow-Ups

실제 Windows 마이크/권한/장치 제거와 늘보군 사용자 OBS 조합을 windows-checklist.md 기준으로 시험한다. 캐릭터를 공개 배포하려면 이용 권리부터 확인한다.

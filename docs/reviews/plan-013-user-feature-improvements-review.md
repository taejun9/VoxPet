# plan-013-user-feature-improvements Review

## Summary

사용자 기능 조사18개와 전역 입 음소거/주변 소음 Gate 추천/권한 설정 바로가기의 구현 및 전체 회귀 QA를 완료한 후 동일 에이전트가 자체 리뷰했다. 서브에이전트는 사용하지 않았다. 실행 코드7297d02, 최종 기록 변경은 문서만 수정한다.

## QA

- macOS arm64/SDK10.0.401: locked restore, format/analyzer, Release build 경고0/오류0, Core110/110, 문서/자산/diff 검사 통과. sandbox의 Git metadata와 .NET IPC 제한은 승인 환경 동일 명령으로 해결했다.
- [Windows 전체 QA](https://github.com/taejun9/VoxPet/actions/runs/37743242592): Core110/110, locked restore/format/analyzer/Release/publish, 실제 WPF smoke239개 검사 실패0. 4창크기×49컨트롤/3startup bounds/작은창 스크롤/Start·Stop 초기 가시성 통과. 실제1000×730/480×320 PNG를 직접 확인했다.
- 새 기능: 합성 숫자로3초 추천/직접 적용/Gate 외 설정 보존/측정·대기 추천 취소/Stop/프리셋/무신호/장치 중단/측정 도중 종료 검사. 실제 RegisterHotKey/WM_HOTKEY + 합성 키 입력으로 전역 입 음소거/충돌/최소화/해제 재등록, 로컬 키/반복/중복 방지 검사.
- [OBS 회귀 QA](https://github.com/taejun9/VoxPet/actions/runs/37743242633): OBS32.2.2/WindowsServer2025 build26100의 WGC green+Chroma Key/native alpha 및 각7프레임 변화 통과. BitBlt는 기존 runner의 유효 프레임 없음 제한이다.
- 두 최종 CI annotation0. Windows 로그의 빌드 경고·오류0, Core110/110과 실패/건너뜀0 확인. 모든4 XAML 구문 검사 통과. 배포 ZIP9파일과 CRC 검사를 통과했다.

## Findings

- 해결: 방송 중 입 음소거는 체크박스만 있었으므로 Ctrl+Shift+M 전역 키와 두 창 내부 대체 입력/충돌 안내를 추가했다. 기존 표정키 ID와 분리하고 MOD_NOREPEAT·등록 여부로 반복/중복 토글을 막는다. Shutdown/Closed는 hook/등록을 해제한다.
- 해결: Gate를 수동 설정해야 하던 흐름에 fresh timestamp 숫자만 수집하는3초 추천과 직접 적용/취소를 추가했다. Core는 비유한 값/중복·역전 시간/상한을 방어하며30개·2초 이상, 90백분위+6dB, -80~-15dB 경계를 시험한다. 무신호와 큰 소음은 기존 값을 유지한다. 추천은 음성/소음 의미 분류로 표시하지 않는다.
- 해결: 완료된 추천도 취소 버튼으로 폐기하고 Stop/장치 중단/프리셋/종료에 추천과 측정을 함께 정리했다. 설정에는 사용자가 적용한 Gate만 기존 방식으로 저장한다. 캡처 worker/PCM 데이터 계약과 ReadLevel의250ms 무음 계약은 보존했다.
- 해결: 첫 Windows37742692699의237검사 중480×320 초기 Start/Stop 가시성1건 실패를 발견했다. 권한 버튼을 Start/Stop 뒤로 옮긴 최종239검사는 전부 통과했다. 시험 항목이나 합격 기준을 제거하지 않았다.
- 권한 설정은 고정 OS URI만 사용자 클릭으로 연다. 권한을 자동 변경하지 않으며 실패 시 수동 경로를 안내한다. 자동 QA에서는 명령 전달만 확인했으므로 OS 페이지/실제 권한 변경 실기를 통과로 주장하지 않는다.
- 조사 근거는 공식 제품/API 안내와 날짜를 기록하고 오래된 공개 사용자 사례를 별도 표시했다. 인터뷰나 수요 규모를 측정했다고 주장하지 않는다. 빠른 시작/상세 안내/배포 안내/설계/개인정보/로드맵의 기능과 제한을 동기화했다. 기존 README 내용을 보존하고 개인 PNG/기존 늘보군 자료를 변경·전송하지 않았다.
- QA 이후 요구사항·스레드/수명·설정·개인정보·기존 표정/시트/OBS/작은창 회귀 자체 리뷰에서 미해결 구현 결함은 발견하지 않았다.

## Residual Risk

Gate의6dB 여유와 백분위는 VoxPet 설계이며 다양한 실제 마이크/팬/키보드·작은 목소리에서 아직 실측하지 않았다. 측정 중 말하면 Gate가 높아질 수 있어 직접 적용/말하기 확인과 재측정을 안내한다. 실제 Windows Settings 페이지/권한, 물리 마이크/제거/키보드/DPI, 사용자 GPU/OBS, 실제 입력 장시간은 미실행이다. 이번 OBS 장시간60분 재시험은 하지 않았다. 자동 QA와 실제 합격을 구분한다.

## Follow-Ups

[기능 목록](../product/user-feature-research.md)의 두 PNG 직접 적용, 단축키 사용자화/PTT/Stream Deck, GIF/레이어 편집, 클릭 통과/창 위치 복구/프레임율은 별도 설계 후보로 남겼다. 실제 장치 시험은 [Windows 체크리스트](../quality/windows-checklist.md)의 시나리오와 환경을 기록한다. 새 자체 포함 배포본은 root artifacts/VoxPet-win-x64-plan013.zip이며 기존 늘보군 개인 자료는 보존한다.

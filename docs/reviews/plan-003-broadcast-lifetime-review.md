# plan-003-broadcast-lifetime Review

## Summary

단일 에이전트의 QA 이후 자체 리뷰. 방송창 최소화/재열기/종료와 모델 공유를 검토했다.
수정 범위 승인. 전체 실사용 목표는 실제 Windows 마이크/OBS/장시간 결과가 없어 미완료다.

## QA

- 수정 전 [Windows CI](https://github.com/taejun9/VoxPet/actions/runs/37266351482): Core 70/70, build/publish 통과 후 방송창 최소화 검사만 실패. Win32 결과 JSON을 확인했다.
- 수정 후 [Windows CI](https://github.com/taejun9/VoxPet/actions/runs/37266740645): build/publish/70 Core tests/WPF smoke 통과.
- native IsIconic으로 설정창이 실제 최소화된 것을 확인하고 IsWindowVisible/IsIconic으로 방송창이 계속 표시되는 것을 확인했다.
- 최소화된 방송창을 열기로 복구, 방송창 닫기/재열기, 동일 모델 공유, 메인 종료 후 방송창 닫힘 검사 추가.
- macOS 교차 빌드 경고/오류 0, Core 70개 통과, 문서/자산/diff 검사 통과.

## Findings

높음: MainWindow.ShowCharacter의 Owner=this는 설정창 최소화 시 방송창까지 최소화시킨다.
수정 전 실제 Windows에서 실패를 재현했고 소유 관계 제거 후 동일 검사에서 통과했다.

중간: 이미 최소화된 방송창에서 열기를 누르면 기존 창을 명시적으로 복구해야 한다.
WindowState.Normal 후 Activate 경로를 추가하고 실제 native 가시성 회귀 검사를 통과했다.

Owner 자동 close를 제거해도 ShutdownAsync가 캡처 종료 이후 방송창을 명시적으로 닫는다.
닫은 방송창의 Closed 이벤트가 참조를 비워 재열기는 새 창을 만들며 오디오/모델은 재사용한다.
마이크를 사용하는 테스트나 음성/장치 ID 로그를 새로 추가하지 않았다. README 원문 보존.
확인한 수정 범위 내 미조치 발견 사항은 없다.

## Residual Risk

native 가시성 검사는 OBS의 실제 캡처 지속이나 프레임/alpha를 증명하지 않는다.
실제 마이크 권한/장치 제거/소음/지연, DPI/장시간/OBS는 [실기 기록](../quality/windows-checklist.md)의 미실행 상태를 유지한다.

## Follow-Ups

실제 Windows PC에서 마이크와 OBS 매트릭스를 수행한다. 실패를 새 실행 계획으로 수정한 뒤 전체 실사용 목표를 완료 판정한다.

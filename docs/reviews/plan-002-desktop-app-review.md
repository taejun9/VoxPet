# plan-002-desktop-app Review

## Summary

QA 이후 단일 에이전트 자체 리뷰. Core 수식/스레드, native WASAPI 수명, WPF 바인딩/종료, PNG/설정/배포/개인정보 계약을 검토했다.
구현과 자동 QA 범위는 승인한다. 실제 Windows 마이크/OBS/장시간 근거가 없어 전체 실사용 목표와 정식 배포 판정은 보류한다.

## QA

- macOS arm64, SDK 10.0.401: locked restore, Release build 경고/오류 0, 최종 70개 테스트 통과, 자체 포함 win-x64 publish 성공.
- 문서/자산 계약 및 diff 공백 검사 통과. README 원문 보존 확인.
- [Windows CI ad1d266](https://github.com/taejun9/VoxPet/actions/runs/37265531490): 70/70 테스트, build/publish, 실제 WPF UI smoke 성공.
- WPF smoke: 마이크 사용 없이 데모 VoiceLevel 반응/blink/유한 범위, 방송창, 고급 normalize min<max와 reset, Stop zero, 일반 Closing 이벤트, 바인딩 오류 0 검사. 사용자 설정을 읽거나 쓰지 않는다.
- Windows 렌더링 PNG를 직접 확인했다. 실제 마이크/OBS는 미실행이며 코드 및 fake tests를 실기 성공으로 해석하지 않는다.

## Findings

| 중요도 | 발견 사항 | 조치와 근거 |
|---|---|---|
| 높음 | extreme normalize 설정/animation time이 NaN을 만들 수 있음 | AudioSettings dBFS 범위 검증, CharacterAnimator phase modulo, 회귀 테스트 |
| 높음 | Stop 실패 후 자원이 남아도 Stop 버튼이 비활성 | AudioSession.HasResources로 재시도와 장치 선택 조건 분리, fake stop failure 회귀 |
| 높음 | WasapiCapture 소스의 native Stop 예외가 completion 밖으로 나갈 수 있음 | NAudio AudioClient shared worker + CaptureLoop의 capture/Stop 이중 예외 처리, 3개 회귀 테스트; 실기 제거 검증은 별도 |
| 높음 | 동기 종료 시 Closing 이벤트 안에서 Close 재진입 | Dispatcher 예약과 종료 완료 flag; 실제 Windows 일반 창 닫기 smoke 통과 |
| 중간 | 고정 normalize min에서는 작은 목소리가 gate 조정만으로 반응하지 않음 | 고급 입력 반응 범위 UI, min<max/reset/save 검증 |
| 중간 | publish locked restore가 RID/ILLink 참조와 다름 | SDK 10.0.401/RID/single-file/self-contained 설정 고정, Windows locked publish 통과 |
| 중간 | 영문 Windows cp1252에서 한국어 QA 출력 실패 | qa.ps1의 Python UTF-8 실행; Windows 재검증 |
| 낮음 | 초록 배경과 기본 캐릭터의 녹색이 겹침 | 독자 제작 보라색 PNG로 변경; 실제 chroma key 검증 남음 |
| 낮음 | 공식 라이선스 CRLF 공백 검사가 실패 | 문구 보존 후 LF 정규화 및 gitattributes, 재검증 |

PCM은 worker의 짧은 고정 메모리 버퍼에서 분석한 뒤 숫자만 공유하고 Dispose에서 지운다.
파일 저장은 조정값과 방송창 옵션만 포함하며 장치 ID/음성은 저장하지 않는다. 런타임에 네트워크/loopback/녹음 API가 없다.
확인한 구현 범위 내 미조치 결함은 없지만 실제 마이크와 OBS에서 발견되지 않은 문제가 있을 수 있다.

## Residual Risk

실제 WASAPI 포맷/권한/USB 제거/장시간/DPI와 OBS 캡처가 미검증이다.
native 드라이버 호출이 멈출 경우 Stop timeout을 표시하고 자원을 강제로 dispose하지 않는다.
투명 창의 OBS alpha 유지와 실측 성능/지연을 보장하지 않는다. Windows 10 전체 edition/build 지원을 보장하지 않는다.
자체 포함 runtime은 SDK 갱신과 앱 재배포로 보안 패치를 적용해야 한다.

## Follow-Ups

사용 가능한 Windows PC에서 [실기 매트릭스](../quality/windows-checklist.md)를 수행하고 결과/환경을 기록한다.
합격 이후 전체 실사용 목표를 완료로 판정한다. 실패 항목은 다음 실행 계획에서 수정/재검증한다.

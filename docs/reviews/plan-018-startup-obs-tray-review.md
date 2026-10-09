# plan-018-startup-obs-tray Review

## Summary

실행 직후 조용한 종료/OBS32.2.2 미표시/트레이 요청을 QA 이후 동일 에이전트가 자체 리뷰했다. 서브에이전트를 사용하지 않았다. 코드4403f8d, 개인PNG는 Git/CI에 포함하지 않는다.

## QA

- 수정 전 [일반 실행 재현](https://github.com/taejun9/VoxPet/actions/runs/37935023184): persistSettings=true/같은352px셀12가족을 동봉한 인수 없는 EXE가-532462766 종료. NullReferenceException의 CanExecute→SetBusy→InitializeAsync→Loaded stack을 보존했다.
- macOS SDK10.0.401 locked restore/format/analyzer/Release0경고0오류/Core140. 문서/자산/diff 통과.
- 수정 후 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37937258075): Core140/WPF725·실패0, publish. fresh12/reversed12/이전3열 저장PNG/손상 상태/public6 각각10초 생존·ready/가시성/선택·정상 종료/슬롯·순서 파일 불변. 실제 Selector 선택 해제와 순서 이동, 트레이 메뉴/복원/숨김 상태 종료·방송창/데모 유지, native non-layered/모드 변경 geometry 유지·최소 진단 privacy 검사.
- [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/37937258114): main트레이 숨김 상태의 OBS32.2.2/Server2025 build26100. green BitBlt·WGC 모두7프레임 변화/Chroma Key 통과. transparent WGC/native alpha65.08% 유지. 캡처 PNG와 WPF의 트레이/OBS 버튼 화면을 직접 확인했다.
- 새 개인ZIP26파일·CRC 통과,12개PNG/원본 해시 동일·이전ZIP 보존. 게시자 회사는김태중·실제서명NotSigned를 명시했다.

## Findings

- 원인: 기존 smoke는 persistSettings=false라 저장 설정의 일반 시작/개인 기본 가족/실제 초기 Selector 동작을 놓쳤다. WPF가 잠깐 null을 전달했는데 Selected setter가 수용하고 이동 버튼 조건에서 역참조했다. 마지막 유효 선택만 수용하고 동일 위치 Move를 생략/목록 갱신 후 선택을 재통지한다. guard를 가정한 모사 대신 일반 실제 프로세스와 실제 WPF Selector로 재검증했다.
- OBS: 이전 초록 창도 layered여서 BitBlt에 빈 프레임이 있었다. 초록=불투명/non-layered, 투명=AllowsTransparency를 native 생성 전에 선택한다. 모드 전환은 방송창만 재생성하면서 크기/위치/상태와 공유모델을 유지한다. 실제 OBS에서 기존 실패하던 BitBlt도 통과했다. 사용자 안내는 전용 VoxPet Character 대상/명시적 WGC/크롭·소스 가시성/크로마 키를 구분한다.
- 트레이/수명: NotifyIcon/메뉴/아이콘은 Dispose를 중복 호출해도 정리한다. 설정창 Hide는 마이크나 방송창을 멈추지 않으며 Restore는 최소화 상태도 복원한다. 숨긴 창의 종료도 두 번째 Close를 예약하도록 수정하여 보이지 않는 main이 남는 회귀를 방지했다. 실제 캡처 종료가 실패하면 설정창을 복원하여 재시도한다.
- 진단/개인정보: last-error.json은 마지막 오류 타입/HResult/앱 메서드/시간·버전만 보관한다. 메시지/파일 경로/장치 ID/음성은 제외하고 원격 전송하지 않는다. CI 데이터 경로는 GITHUB_ACTIONS=true와RUNNER_TEMP 내부로 제한하며 사용자 설정을 쓰지 않는다. 보안 정책·인증서 신뢰를 자동 변경하지 않았다.
- 문서: 일반 시작 QA 사각지대/재현-수정 근거, 트레이의 입력 유지와 종료, OBS 모드/대상 재확인, 오류 기록 위치, 미서명 상태와 개인PC 안내를 반영했다. 미해결 기능 결함은 발견하지 않았다.

## Residual Risk

Windows 자동 실행은 Server2025/합성 가족이며 사용자 Windows11 GPU/장면과 정확히 같지 않다. 개인PNG는 CI에 보내지 않았다. 이번 종료의 원인은 그림 내용과 무관한 UI 선택 null로 재현했다. 실제 Shell 물리 클릭/마이크/사용자 OBS와 새 개인PNG 실행 화면은 별도 실기다. 자체 서명/인증서 신뢰는 대상 개인PC의 수동 단계이며 경고가 제거되었다고 보고하지 않는다.

## Follow-Ups

새 개인ZIP을 새 폴더에 풀어 실행하고 OBS 호환 방송창/명시적 대상·방식과 트레이 복원을 확인한다. 재발하면 로컬 최소 오류 로그로 해당 환경을 좁힌다. 개인PC 게시자 서명 안내를 따른다.

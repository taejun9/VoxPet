# Windows 실사용 검증 기록

## 환경과 현재 증거

2026-10-05, 구현 호스트 macOS arm64, SDK 10.0.401.
Core 자동 테스트와 WPF 교차 컴파일은 실행했다. Windows GitHub Actions에서도 70개 테스트와 WPF UI smoke를 통과했다.
초기 검증 commit ad1d266, [CI 실행 결과](https://github.com/taejun9/VoxPet/actions/runs/37265531490).
방송창 수명 회귀: ddbaee7, [CI 실행 결과](https://github.com/taejun9/VoxPet/actions/runs/37266740645).
OBS 합성 캡처: e41194f, [CI 실행 결과](https://github.com/taejun9/VoxPet/actions/runs/37268938486).
최종 green/transparent 캡처: 86374ad, [CI 실행 결과](https://github.com/taejun9/VoxPet/actions/runs/37276105483). WGC의 투명 배경 alpha65.1%/캐릭터 유지/7프레임 변화 확인.
OBS 32.2.2/obs-websocket 5.7.4, Windows Server 2025 build26100, Microsoft Basic Render Driver/D3D11.
장시간 합성 데모: d35f707, [CI 실행 결과](https://github.com/taejun9/VoxPet/actions/runs/37269646209). 10분 warmup 후 실제60.17분 측정, RSS 증가1.58MiB/핸들 증가0/68회 프레임 변화 검사 통과.
실제 Windows 마이크와 사용자 GPU/OBS 조합은 아직 통과로 기록하지 않는다.

| 항목 | 현재 상태 | 확인 방법/합격 기준 |
|---|---|---|
| Core 수치·수명·캐릭터 자동 테스트 | 70/70 통과 (macOS/Windows) | 합성 입력과 fake input; 모든 테스트 통과 |
| WPF Release 교차 컴파일 | 통과, 경고/오류 0 | `dotnet build VoxPet.sln -c Release` |
| win-x64 자체 포함 배포 | 통과 | 잠금 복원 후 publish, EXE/라이선스 확인 |
| Windows WPF smoke | 통과 | UI 바인딩 오류 0, 이미지/별도 창/데모 반응/blink/입력 범위/reset/Stop/일반 창 종료 통과 |
| 방송창 최소화/복구/재열기/종료 | Windows CI 통과 | native 창 가시성/최소화 상태와 shared model, 메인 종료 후 창 닫힘 확인 |
| 실제 마이크 | 미실행 | 아래 실기 매트릭스 수행 |
| OBS 합성 데모 | WGC 통과, BitBlt 유효 프레임 없음 | 설정창 최소화 중 Window Capture, 7개 서로 다른 프레임, Chroma Key 후 green0%/alpha66.0%/캐릭터 유지; 실제 PNG 확인 |
| OBS 합성 장시간 UI | 통과 (d35f707) | 60.17분 RSS +1.58MiB/핸들 +0, 68회 변화 검사 및 최종 유효 프레임. 실제 마이크 장시간 시험과 구분 |
| OBS 사용자 환경 | 미실행 | 아래 OBS 매트릭스 수행 |

## 마이크/창 실기 매트릭스

시험자는 OS edition/build, 앱 commit, 장치 종류/채널/샘플 형식, 화면 DPI, 결과/실패 증상을 기록한다.
녹음·개인 장치 ID는 기록하지 않는다.

| 시나리오 | 합격 기준 | 결과 |
|---|---|---|
| 마이크 없는 PC 시작 | 앱 유지, 목록 없음/연결 안내, Start 비활성 | 미실행 |
| Windows 데스크톱 마이크 권한 거부 | 오류 안내, 앱 유지, 권한 허용 후 Start 복구 | 미실행 |
| USB/내장 마이크 Start→발화→Stop | 입 반응/값 표시, 중지 후 마이크 점유 해제와 레벨 0 | 미실행 |
| 100회 Start/Stop | 중복 캡처/멈춤/핸들 증가 없음 | 미실행 |
| 시작/종료 중 창 닫기 | UI 멈춤 없음, 프로세스/마이크 점유 종료 | 미실행 |
| USB 장치 캡처 중 제거 | 오류 안내와 레벨 0, 재연결/새로고침/Start 복구 | 미실행 |
| 장치 Stop→교체→Start | 새 장치만 캡처, 이전 세션 값 없음 | 미실행 |
| 작은 소음/말/침묵 | gate 조정, attack/release 반응, 침묵 후 입 닫힘 | 미실행 |
| 100/150/200% DPI, 최소 창 크기 | 잘림 없이 조정/Start/Stop 접근, PNG 정렬 | 미실행 |
| 설정창 최소화 중 방송창/복구/재열기 | 방송창 가시성 유지, 열기 명령 복구, 재열기와 메인 종료 정리 | Windows CI 통과 |
| 실제 입력 중 창 최소화와 UI 조작 | 입력 callback 대기열 증가/화면 정지 없음 | 미실행 |
| 설정 저장/손상/읽기 전용 | 정상 보존/기본값 복구/앱 종료 가능 | 미실행 |
| 60분 캡처 | 메모리/핸들 누적 증가 없음, 마이크 오류 없이 응답 | 미실행 |

## OBS 실기 매트릭스

OS/OBS version, GPU/driver, 캡처 방식(자동/Windows Graphics Capture/BitBlt), DPI, 창 가림/최소화, 녹화 시간과 결과를 기록한다.
실제 방송 영상·음성은 저장소에 넣지 않는다.

| 조합 | 합격 기준 | 결과 |
|---|---|---|
| 초록 배경 + Window Capture + Chroma Key | 배경 제거, 입/눈/몸 반응, UI가 송출되지 않음 | 위 CI 환경에서 WGC 합성 데모 통과. 실제 마이크/사용자 GPU 조합은 미실행 |
| 투명 배경 + Window Capture | alpha 유지 여부를 측정, 실패 시 초록 배경 경로 사용 | 위 CI의 WGC에서 alpha65.1%/캐릭터 유지/7프레임 변화 통과. 사용자 GPU 조합은 미실행 |
| 방송창 가림/항상 위 해제/최소화 | 캡처 지속 조건과 제한 기록 | 미실행 |
| 크기 조절/화면 DPI 변경 | OBS 화면에서 anchor/정렬 유지 | 미실행 |
| 60분 캡처 | 프레임 정지/핸들·메모리 누적 증가 없음 | 위 CI의 합성 데모60.17분/68회 변화 검사 통과. 실제 마이크/사용자 GPU 조합은 미실행 |

## 성능 측정

UI 60Hz는 타이머 목표이며 성능 측정 결과가 아니다.
실기에서 발화→표시 지연, CPU, 메모리, 핸들을 측정하고 목표 기준을 먼저 결정한다.
지연은 녹음 파일 대신 테스트 신호와 화면 시각을 사용해 측정한다.

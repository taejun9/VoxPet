# Windows 실사용 검증 기록

## plan012 재점검 진행 상태

2026-10-07, 코드1d8cd05의 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37612609047)와 [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/37612608423) 통과. macOS/Windows Core97/97, locked restore/format/analyzer/Release build/publish, WPF smoke failures0/명명 검사200개,4창크기×45컨트롤과 작은 창 스크롤을 확인했다. 구형 Actions Node20 경고를 공식 Node24 Actions로 갱신하여 workflow/의존성 deprecation/빌드 경고·오류0을 확인했다. C#35파일의XML doc100블록, XAML4개/Python4개 구문도 통과했다. 실제 WPF 기본 캐릭터의 슬픔/눈물과480×320 PNG를 직접 확인했다.

OBS32.2.2 WGC green+Chroma Key와 native alpha 및 각7프레임 변화는 통과한다. BitBlt 유효 프레임 없음은 기존 runner 제한이다. 로컬 증거는 root `artifacts/qa/plan012/summary.json`과 `windows-clean/`, `obs-clean/`에 보관한다.

**이번 늘보군 화면 시험은 개인 PNG 외부 전송의 명시 승인 대기이며 미실행이다.** 개인 PNG를 전송하거나 임시 draft/secret을 생성하지 않았다. 추가한 개인 슬롯 저장/재시작·모션·두 창 캡처 fixture는 승인 후 Windows에서 실행한다. plan012는 아직 active이며 최종 리뷰/main 통합은 그 시험 이후다. 아래 과거 늘보군 기록을 이번 코드의 개인 화면 검증으로 해석하지 않는다. 실제 물리 마이크/키보드/DPI와 사용자 OBS 실기 상태도 유지한다.

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
| Core 수치·수명·캐릭터 자동 테스트 | 71/71 통과 (macOS/Windows, 4ad3180) | 합성 입력과 fake input; 모든 테스트 통과 |
| WPF Release 교차 컴파일 | 통과, 경고/오류 0 | `dotnet build VoxPet.sln -c Release` |
| win-x64 자체 포함 배포 | 통과 | 잠금 복원 후 publish, EXE/라이선스 확인 |
| Windows WPF smoke | 통과 | UI 바인딩 오류 0, 이미지/별도 창/데모 반응/blink/입력 범위/reset/Stop/일반 창 종료 통과 |
| 방송창 최소화/복구/재열기/종료 | Windows CI 통과 | native 창 가시성/최소화 상태와 shared model, 메인 종료 후 창 닫힘 확인 |
| 실제 마이크 | 미실행 | 아래 실기 매트릭스 수행 |
| OBS 합성 데모 | WGC 통과, BitBlt 유효 프레임 없음 | 설정창 최소화 중 Window Capture, 7개 서로 다른 프레임, Chroma Key 후 green0%/alpha66.0%/캐릭터 유지; 실제 PNG 확인 |
| OBS 합성 장시간 UI | 통과 (d35f707) | 60.17분 RSS +1.58MiB/핸들 +0, 68회 변화 검사 및 최종 유효 프레임. 실제 마이크 장시간 시험과 구분 |
| OBS 사용자 환경 | 미실행 | 아래 OBS 매트릭스 수행 |

## 최종 통합 자동 QA와 실행 패키지

plan004/005/006 통합4ad3180: [Windows CI](https://github.com/taejun9/VoxPet/actions/runs/37278367856) 및 [OBS CI](https://github.com/taejun9/VoxPet/actions/runs/37278367918) 통과.
Core71/71, smoke 실패0, 4창크기×15컨트롤/3startup bounds/휠과 native 방송창 수명·resize를 확인했다.
OBS32.2.2 WGC green+Chroma Key 및 native alpha65.1%/7프레임 변화 통과. BitBlt는 유효 프레임 없음.
오디오 Stop 중 native Ended 오류의 누락을 재현·수정했고, 무음/자원정리/새 Start 복구 자동 회귀를 추가했다.
Windows runner의 배포본은 로컬 `artifacts/VoxPet-win-x64-plan006.zip`, 증거와 SHA256은 `artifacts/qa/plan006/integrated/summary.json`에 보존한다.
이 패키지의 실제 마이크/실제 DPI/실제 입력 장시간 검증은 아래 미실행 항목으로 남는다.

## 마이크/창 실기 매트릭스

plan007의312916f에서 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37294330257)와 [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/37294330480)를 재실행했다.
새 필수 format/analyzer 검사 통과, Core71/71, Release 경고/오류0, publish 및 실제 WPF smoke failures0을 확인했다.
4창크기×15컨트롤/3startup bounds/작은창 wheel JSON과 실제 WPF PNG를 직접 확인했다.
OBS WGC green+Chroma Key green0%/alpha66.04%, native alpha65.08% 및 각7프레임 변화와 캡처PNG를 확인했다.
BitBlt 유효 프레임 없음과 아래 실제 마이크 미실행 상태는 유지한다. 증거: root artifacts/qa/plan007/summary.json.

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

## 작은 창 자동 검사 범위

plan005의 LayoutQa는 실제 WPF 창을 1000×730, 960×540, 640×480, 480×320 논리 크기로 조절한다.
3dcded2, [Windows CI](https://github.com/taejun9/VoxPet/actions/runs/37272421104)에서 Core70/기존 smoke와 함께 통과했다.
plan004 통합 후87e14a6의 [Windows CI](https://github.com/taejun9/VoxPet/actions/runs/37277539708) 및 [OBS CI](https://github.com/taejun9/VoxPet/actions/runs/37277539743)에서도 native resize/배치/green·transparent 캡처가 통과했다.
마이크 선택/Start/Stop/데모/6개 슬라이더/기본값/방송창 열기/방송 옵션 등 15개 컨트롤이 scroll viewport 안에 들어오는지 확인한다.
작은 창의 휠 routed event, 초기 Start/Stop 가시성, 가상 작업 영역을 주입한 실제 창의 초기 bounds/종료도 검사한다.
100/150/200% PNG는 raster 출력 검사다. 실제 모니터 배율 변경/다중 모니터 이동/사용자 GPU 결과로 기록하지 않는다.

## 성능 측정 범위

UI 60Hz는 타이머 목표이며 성능 측정 결과가 아니다.
실기에서 발화→표시 지연, CPU, 메모리, 핸들을 측정하고 목표 기준을 먼저 결정한다.
지연은 녹음 파일 대신 테스트 신호와 화면 시각을 사용해 측정한다.

## plan008 캐릭터와 편의 기능 검증

2026-10-06 코드 f5f1757의 [기본 Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37338899300),
[늘보군 Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37338981751),
[OBS 회귀 QA](https://github.com/taejun9/VoxPet/actions/runs/37338899198)가 모두 통과했다.
Core84/84, Release 경고/오류0, format/analyzer, 자체 포함 publish, 실제 WPF smoke failures0을 확인했다.
로컬 증거는 `artifacts/qa/plan008/summary.json`과 `final-personal/`에 보존했다. 개인 이미지가 있는 원격 QA artifact와 임시 입력/secret은 수집 후 삭제했다.

- PNG 시트: 6상태 매핑, 작업 스레드 로드와 Freeze, 자동 중앙/바닥 정렬, 파일 점유 해제, 빈/불투명/잘못된/과대/없는 파일 복구 및 기존 캐릭터 보존 통과.
- 편의 기능: 조용한 목소리/빠른 반응/일반 대화 프리셋, 음소거 중 입 0 및 입력/blink 유지, 해제 후 반응, Stop reset, 마이크 상태 표시 보존 통과.
- 레이아웃: 4창크기×21컨트롤 모두 접근 가능, 3startup bounds, 작은 창 휠, 1/1.5/2배 합성 렌더, 넓은 창 미리보기 전체 가시성 통과.
- 늘보군: 사용자 참고 이미지에서 만든 개인 RGBA 시트 적용, 여섯 상태 PNG와 실제 합성 반응/미리보기 PNG 확인. 마이크를 캡처하지 않았다.
- OBS32.2.2: 기본 캐릭터 WGC의 7프레임 변화, green Chroma Key 및 native alpha65.08% 유지 통과. BitBlt는 기존처럼 유효한 캐릭터 프레임 없음.

배포본과 개인 시트를 묶은 로컬 파일은 `artifacts/VoxPet-win-x64-neulbo-plan008.zip`이다.
기본 공개 배포는 CC0 고양이를 유지하며 늘보군은 이용 권리 미확인 개인 시험 팩이다.
실제 마이크/권한/제거, 사용자 GPU/OBS, 물리 DPI 변경, 실제 입력 장시간 시험은 위 미실행 상태를 유지한다.

## plan009 문서·주석 회귀 검증

2026-10-06 checkpoint246a61e의 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37413935262) 및
[OBS QA](https://github.com/taejun9/VoxPet/actions/runs/37413935334)가 통과했다.
README·사용 설명서·배포 안내와 한국어 코드 주석만 보강했으며 실행 로직·자산·의존성·workflow는 변경하지 않았다.

- Windows: locked restore/format/analyzer/Release/publish, Core84/84, 경고·오류0, WPF smoke failures0.
- 배치: 4창크기×21컨트롤, 3startup bounds, 작은 창 스크롤 통과.
- OBS WGC: green+Chroma Key와 native alpha65.08%, 각7프레임 변화 통과. BitBlt 유효 프레임 없음 유지.
- 증거: root artifacts/qa/plan009/windows/ 및 obs/. 로컬 주석 제외 실행 내용 동등성 및 XML summary 검사 통과.
- 실제 마이크·사용자 GPU/OBS·물리 DPI·실제 입력 장시간 시험의 기존 미실행 상태는 유지한다.


## plan011 표정·단축키·모션 검증

2026-10-07 c18a8c2의 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37576998278) 및
[OBS QA](https://github.com/taejun9/VoxPet/actions/runs/37576998263) 통과.
Core97/97, format/analyzer, Release 경고/오류0, 자체 포함 publish와 실제 WPF smoke 통과.

- 표정: 내장6종×6상태, 12슬롯 로컬 저장/재시작/원본 PNG 제거 후 관리 복사본 적용, 손상 PNG의 기존 표시 보존과 내장 복구 확인.
- 전환/모션: 240ms 중간·완료·중도 전환 snapshot, 연속 요청의 최신 우선, 마이크 없이 blink/tears 이동, blink 끄기, 눈물 위치 조정 확인. 미리보기/방송창 눈물 PNG를 직접 확인했다.
- 단축키: 실제 RegisterHotKey/WM_HOTKEY + 합성 키 입력으로 F1~F11 등록, 충돌 안내, 메인 최소화 중 F3 전환, F12 앱 내부, 잘못된 modifier 무시, 해제 후 재등록 확인. 물리 키보드 시험과 구분한다.
- 배치: 4창크기, 3startup bounds, 작은 창 스크롤, 40자 슬롯 이름/이름 TextBox 포함. 1/1.5/2배 PNG는 합성 렌더이며 물리 DPI 시험이 아니다.
- OBS32.2.2 WGC green+Chroma Key 및 native alpha, 각7프레임 변화 통과. BitBlt는 기존처럼 유효 프레임 없음.
- 첫 Windows QA의 저장 실패 테스트 예외 종류 차이는 수정 후 재검증했다. 앱 저장 오류 처리의 결함은 없었다.
- 증거: root artifacts/qa/plan011/windows/ 및 final-obs/, 배포 ZIP artifacts/VoxPet-win-x64-plan011.zip.

물리 키보드/다른 프로그램과의 실사용 충돌, 실제 마이크/권한/제거, 사용자 GPU/OBS, 물리 DPI, 실제 입력 장시간은 기존 미실행 조건으로 남는다.

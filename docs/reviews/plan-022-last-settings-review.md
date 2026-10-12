# plan-022-last-settings Review

## Summary

QA 후 단일 에이전트 자체 리뷰다. 마지막 마이크·창 크기/위치·최대화/방송창 열림·음소거/음량/캐릭터/표정/탭 복원과 자동 저장/정상 종료 flush를 검토했다. 독립 에이전트 리뷰는 수행하지 않았다.

## QA

고정 SDK locked restore/format/Release build 경고0·오류0, Core168/168, 문서/PNG 계약/diff --check 통과. [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/38239838261) Core168/WPF929·실패0, 실제 설정 검사19종, unsigned/signed 각각9개 일반 시작·종료 사례 통과. [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/38238891072)는 같은 앱 소스 d9ce823의 WGC/alpha/동일 HWND 배경 전환을 통과했다. 이후5202b20은 종료 하네스/계획 기록만 수정했으며 앱 소스는 동일하다. 개인 ZIP32파일/100483996bytes의 CRC/SHA256·서명 EXE/공개 인증서 해시·기존16PNG byte-identical 검사를 통과했다. 개인 설정/음성/개인키는 포함하지 않았다. 공개 CI는 가짜 ID/임시 데이터와 공개·합성 PNG만 사용한다.

## Findings

- P2 해결: 자동 복원된 owner 없는 방송창이 Process.MainWindowHandle로 선택되어 종료 fixture가 방송창만 닫았다. 기록한 설정창 HWND로 WM_CLOSE를 보내 정확한 대상 종료를 검사한다. 앱에서 복원된 값을 잘못 저장한 결과와 구분했다.
- 저장은350ms 지연으로 UI 변경을 모으고 불변 snapshot/worker/SemaphoreSlim/임시 파일 교체로 순차 기록한다. 정상 종료는 타이머를 먼저 중단하고 이전 저장 뒤에 마지막 snapshot을 쓴다. 쓰기 실패는 이전 파일을 보존하고 별도 UI 안내와 재시도를 제공한다.
- 초기 복원은 일반 시작의 worker 읽기 결과를 주입하고 settingsReady까지 자동 저장을 막는다. 선택 마이크 ID만 로컬 저장하며 시작/오류 증거는 ID를 기록하지 않는다. Capture factory를 호출하지 않고 사용자의 Start를 유지한다.
- 없는 마이크의 선호 ID를 유지해 목록 순서/빈 목록/재연결에 대응한다. 사용자가 다른 장치를 선택하거나 Start한 장치가 마지막 선호가 된다. 오디오 조정과 신규 필드는 이전 파일 기본값/정규화로 호환된다.
- native 위치와 WPF DIP 크기를 분리하고 모니터 작업 영역/배율로 창을 보정한다. 복원은 SetCurrentValue로 TwoWay 크기 바인딩을 유지한다. 최대화/최소화·트레이 상태에서도 마지막 정상 bounds를 보존하며 수동 방송창 닫음과 앱 종료를 구분한다.
- 실제 사용한 슬롯 ID는 순서 변경과 독립적이며 내장 표정과 임시 전체 테스트를 구분한다. 슬롯/의상 편집의 명시적 저장 흐름은 유지한다. 기존 다섯 캐릭터 PNG/원본/이전 ZIP과 plan020은 변경하지 않았다.

## Residual Risk

혼합 DPI 다중 모니터/RDP/물리 마이크와 실제 사용자 GPU/OBS 실기는 별도다. Native API 복원은 Windows 자동 실행과 수치 fixture에서 확인하며 실제 모니터를 분리한 결과로 표현하지 않는다. 자동 저장 전의350ms 안에 프로세스를 강제 종료하면 마지막 변경이 남지 않을 수 있다. 자체 서명은 인증기관 확인/SmartScreen 평판과 구분한다.

## Follow-Ups

대상PC에서 창 이동/크기·최대화/트레이 종료 후 재실행, USB 마이크 재연결, 물리 DPI 전환과 실제 OBS 방송창 복원을 확인한다.

# plan-039-runtime-hardening Review

## Summary

실제 앱의 native 캐릭터 적용·보관함·편집기·입력 수명과 Windows 직접 시험 파일을 검토했다. 단일 에이전트가 QA 후 자체 리뷰했다. 초기 QA 후 발견한 Canvas 범위 누락을 보완하고 최신 bdde9ae에서 전체 QA를 다시 통과시킨 뒤 재검토했다. 미해결 필수 수정 사항은 없다.

## QA

- 로컬 SDK10.0.401: NuGet audit 포함 locked restore, format verify/analyzer, Release build 경고0·오류0, Core366/366, verify_base/verify_app, Python compileall, diff check 통과.
- 최종 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/38714843197): Core366/366, WPF1768고유검사/실패0, 신규 safety24검사 전부 실행. 실제 desktop250장·PNG/coverage, unsigned/signed 각9개 일반 시작·정상 종료·저장 파일 보존 통과. JSON/TRX를 직접 대조했다.
- 생성한 공용 ZIP을 다시 풀고 Windows PowerShell5.1의 동봉 TEST-SMOKE.ps1로1536고유검사/실패0. CI desktop 전용232검사는 일반 사용자용 실행에서는 활성화하지 않으므로 기본 WPF1768과 검사 수가 다르다.
- 최종 [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/38714843135): OBS32.2.2 WGC/초록+크로마/native alpha·동일 HWND 배경 왕복·각7프레임 변화 통과. 결과 JSON의 source commit을 확인했다. BitBlt는 기존 layered 창 호환 제한이다.
- 로컬 개인 ZIP `artifacts/VoxPet-Windows-test-plan039-final.zip`:50파일/95,456,545bytes, CRC 통과, SHA256 `d782eeb7540f4f12039c3506904ecf4096840542791d8b322b1b03e2e2a387c6`. EXE는 최종 Windows 공용 publish와 동일하며 SHA256 `89190e0bf2a6889e99c9f8aea9ae3a5ab80797667fc2733b6df1aea424394509`. 이전 개인 ZIP의13PNG 원본 bytes/해시/PNG CRC 일치. 개인 PNG는 외부 전송하지 않았다.
- 증거는 `artifacts/test-results/plan039/final/`에 보존한다. 이후 변경은 완료 기록·체크리스트·테스트 안내 텍스트이며 앱과 실행 도구 코드는 최종 CI 기준과 같다.

## Findings

| 중요도 | 근거 | 조치와 확인 |
|---|---|---|
| P1 | NativeCharacterViewModel.ReadAsync / CharacterEditorViewModel.TransactionAsync | 첫 frame 계산 전에 scene/document/저장 선택을 바꾸면 실패가 기존 상태를 오염시켰다. worker에서 첫 frame까지 준비한 뒤 교체한다. 실패한 import는 보관함에 쓰지 않으며 기존 잘못된 항목의 thumbnail 오류도 개별 격리한다. 실패 적용/선택/문서/보관함 회귀로 확인했다. |
| P1 | NativeCharacterViewModel.Update / CharacterEditorViewModel.Seek·UpdateFrame | 지연 이벤트의 극단적 변환이 Dispatcher까지 예외를 올릴 수 있었다. native는 마지막 frame을 유지하고 입력을 해제하며 재선택으로 복구한다. 편집 preview도 중단/수정/다시 재생 경로를 유지한다. overflow 및 후속 tick/입력 회귀가 통과했다. |
| P1 | VoxPetPackageValidator / VoxPetPackageLimits | 양수·유한값만으로는 WPF에서 표현할 수 없는 극소/과대 Canvas를 차단하지 못했다. package 기본 범위를1~16384로 정하고 로드/편집/export에 적용했다. Core11회귀와 WPF의 미적용 편집/기존 문서 보존 회귀가 통과했다. 일반 Core geometry API는 변경하지 않았다. |
| P2 | CharacterEditorViewModel의 preview ID / DeleteAsync·MoveAnchorAsync | 삭제된 동작/표정/부착물의 참조가 남아 명령이 실패했고, 미적용 입력이 삭제/드래그로 사라질 수 있었다. preview ID를 현재 목록으로 복구하고 미적용 입력이 있으면 구조 변경을 차단한다. 해당 입력은 이후 Apply로 보존됨을 확인했다. |
| P2 | CharacterEditorViewModel.AddAsync·AddDetailAsync | 유효한 앵커 없는 캐릭터에서 First() 가정으로 입/부착물/동작 추가가 실패했다. 입·부착물은 root를 만들고 transform 이벤트는 기존 파츠를 사용할 수 있게 했다. |
| P2 | CharacterRuntimeViewModel.Input / NativeCharacterInputs.Mouse | UI 지연 중 입력 목록과 Dispatcher 예약이 누적될 수 있었다.128입력/프레임과 버튼별 예약1개로 제한했다.10000입력의 상한/소진·기존 실제 합성 입력/해제 회귀를 확인했다. |

## Residual Risk

실제 사용자 Windows11 물리 마이크/권한/USB 제거, GPU·혼합 DPI·OBS 조합과60분 실기는 실행하지 않았다. CI의 실제 WPF 창·합성 입력·공개 자산 검사와 구분한다. 개인13PNG는 로컬 무결성만 확인했고 Windows로 보내지 않았다.

직접 시험 EXE는 unsigned다. CI의 별도 일회용 자체 서명/신뢰 시험은 사용자 컴퓨터의 실행 경고 제거를 의미하지 않는다. 자동 도구는 합성 키·좌클릭을 사용하므로 다른 앱을 닫은 전용 Windows/VM에서 실행하도록 안내했다. 작업 중인 사용자 데스크톱에서는 수동 체크리스트를 사용한다.

## Follow-Ups

동봉 TEST-README.txt 및 [직접 시험 안내](../quality/windows-test-package.md)로 대상PC 실기를 수행하고 [Windows 체크리스트](../quality/windows-checklist.md)에 OS/GPU/마이크/OBS 버전·재현 순서를 기록한다. 음성·장치 ID·개인 PNG를 공개 증거에 넣지 않는다.

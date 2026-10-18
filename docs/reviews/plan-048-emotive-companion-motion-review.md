# plan-048-emotive-companion-motion Review

## Summary

늘보군·펠리의9동작을 준비→주동작→여운→복귀로 연결하고 각 체형의 목·몸통·팔 리듬을 구별했다. 원본 캐릭터와 기존 의상17종/14분류·헤어3종은 유지한다. 앱1.4.0/두 pack5.0.0이다. 최종 전체 QA 후 단일 에이전트가 별도 단계로 자체 리뷰했다.

## QA

최종 앱·QA·자산은 `e055e91c10b7bfcbcd60c99c6a1d78c4ea663c7e`다. [Windows 전체](https://github.com/taejun9/VoxPet/actions/runs/39126972370)와 [OBS](https://github.com/taejun9/VoxPet/actions/runs/39126972317)가 통과했다.

- SDK10.0.401 locked restore·format/analyzer·Release 경고0/오류0·Core451/451/skip0. 이전446 Core 이름을 모두 보존했다. 문서/자산/compileall/diff 검사 통과.
- WPF3296고유검사/실패0·이전3096 이름 보존·신규200개. 실제 desktop541장/coverage·90시점 FrameCurrent/진행률/발사 효과/양손·몸통·머리 rig와 GDI/reference 픽셀 대조 통과. 채널차24·전체1% 미만·4×4 tile75% 이하 기준을 유지하며 최대 변경 비율0.000760이다.
- 일반/서명 각각9개 시작/재실행·정상 종료·저장 원본 보존, 서명 Valid/변조 HashMismatch/아이콘·임시 키 정리 통과. SDK 없는 ZIP을 다시 풀어 동봉 PowerShell 도구2683고유검사/실패0 확인.
- 실제 서비스의 native 입력을 전체/portable 각각20회, [별도100회](https://github.com/taejun9/VoxPet/actions/runs/39126983072) 검증했다. 매회 등록·전용 owner thread·owner loop 준비·단일 down/up 주입2개 수락·정확한 전달1/해제후0·실제 unhook 성공을 확인했다. 재주입/제외/skip은 없다. probeOnly100회는 전체 배포 합격을 대신하지 않는다.
- 공개 Windows36동작화면/4page·OBS5캡처를 직접 검토했다. OBS32.2.2 WGC/chroma/native alpha·동일 HWND 왕복·각7프레임 변화 통과. 녹화/스트리밍은 꺼져 있다. BitBlt layered 제한은 유지한다.
- 개인 로컬558프레임·18 contact page/162선별프레임·512px full mesh6시점·18GIF를 검토했다. 현재 Core/안무/pack/렌더러는 직접 재내보내기558프레임과 byte-identical(SHA256 d0b84b540abb0f64f1bc1bacc001b725dae0b8ce86b983e2f52732edf8a939a6)이다. SVG proxy256px와 full512px 정밀 합성을 구분하며 실제 개인 Windows 실행으로 주장하지 않는다.

## Requirements and evidence

| 요구 | 구현·검토 |
|---|---|
| 중간 멈춤 없는 연결 | opt-in monotoneCubic의 형상 보존 Hermite·연속 속도, 의도한 hold/reversal과 양 끝은 속도0. 기본 step/smoothstep·중복 미래 키의 기존 의미를 보존한다. |
| 체형별 자연스러움 | 늘보군은 느긋한 준비/여운, 펠리는 이른 도착과 작은 정착. 10ms Cartesian 안무를 역변환하고 머리는 얼굴 중심 대신 목 pivot으로 회전한다. 몸통 squash와 발 기준선 보정도 포함한다. |
| 파지·깃털·활 | 양손23px 손잡이 접점 오차.35px 이하·목 수평.01px/수직1.8px 이하를7ms 간격에서 검사한다. 비균일 몸통 배율의 깃털 정렬은 어깨 로컬 좌표로 판정하고 기존 양의 면적 검사를 유지한다. 권총 지지손도 반동을 함께 따른다. |
| 편집 호환 | 곡선 필드를 읽기/쓰기·편집·이벤트/트리거 추가·삭제·내보내기에서 유지한다. 알 수 없는 enum은 거부하며 모드가 없는 기존 파일은 이전 의미를 사용한다. |
| 기존 샘플·원본 | 각 pack114파일/17부착물/9동작 유지,86PNG는 이전과 byte-identical. 원본 캐릭터6파츠 분리/의상 연결·날개/회전 모자·헤어·표정·복구 기존 회귀를 전부 유지했다. |

## Findings

첫 Core12실패는 월드 좌표의 비균일 배율 정렬 판정과 새 끝점/효과 앵커 기대값을 구분해 수정했다. 기존 면적/원본 픽셀 기준을 완화하지 않았다. 원본 실패 TRX와 수정 근거는 완료 계획에 기록했다.

실제 native 입력의 간헐 누락은 기존50ms 판정만의 문제로 단정할 수 없었다. 원자적 SendInput2개 수락·시험창 PreviewMouseDown1인데 hook에는UP만 남는 회차가 반복됐고 owner HWND의 준비ACK 뒤에도42회차에서 재현됐다. 이전 실패는 candidate/probe별 원본 JSON으로 보존했다. 단순 대기나 계측으로 해결했다고 주장하지 않는다.

[Microsoft LowLevelMouseProc](https://learn.microsoft.com/en-us/windows/win32/winmsg/lowlevelmouseproc)의 전용 스레드 권고를 대조했다(확인2026-10-18). 손동작 hook는 전용 STA Dispatcher에서 설치/수신/해제하고 UI에는 작업 하나만 예약한다. Ready/Stopped는 비동기 통지하며 UI 동기 대기·raw data 역참조가 없다. Dispose/Configure 세대 변경으로 오래된 UI 예약을 거부하고 새 준비 작업과 다른 상태 안내도 덮어쓰지 않는다. QA trace는 ConcurrentQueue에 정수만 기록한다. 이 구조에서 전체/portable20+20/추가100회가 모두 통과했으며 OS 전체에서의 무실패 보장으로 확대하지 않는다.

## Package and privacy

`artifacts/VoxPet-Windows-test-plan048.zip`:54파일/104144125bytes, SHA256 `4f2e1c18c1c6ebdb3090123861f48d347d6894c098b0cef11b346d39deee35e1`, EXE SHA256 `36dc64e51bae6ab97313d8ff4ff426b37d40334d98cd10d3be863e1e797e0fc3`. 시험한 공용41파일+이전 개인13PNG가 byte-identical이며 CRC/PE x64/세 pack 소스 일치다. SHA256 sidecar를 함께 만들었다.

개인 PNG는 로컬 합성과 ZIP에만 사용하며 Git/CI/imagegen 전송이 없다. 음성 엔진·녹음/전송·자동 마이크 변경 없음. README/AGENTS·다른 plan024 작업트리·이전 ZIP 보존을 확인했다. 전달 EXE는 unsigned이며 runner 임시 서명은 사용자 신뢰 저장소를 바꾸지 않는다.

## Residual Risk

미해결 필수 수정 사항은 없다. 물리 마이크/권한/USB·실제 게임/입력 권한·사용자 GPU/혼합 DPI·개인 PNG의 실제 Windows 실행·새60분 실기는 [Windows 체크리스트](../quality/windows-checklist.md)의 별도 항목이다. 공개 Windows와 개인 로컬 합성 결과를 구분한다.

## Follow-Ups

기존 앱을 트레이에서도 종료하고 새 폴더에 ZIP 전체를 푼 뒤 START.cmd를 실행한다. TEST-SMOKE.cmd는 합성 키/클릭을 쓰는 별도 자동 검사다. [완료 계획](../exec_plans/completed/plan-048-emotive-companion-motion.md)에 상세 이력을 남겼다. 문서 변경을 확인한 뒤 표준 main ff/push·branch -d·worktree 제거를 수행하며 실제 종료 상태는 로컬 Git audit에 기록한다.

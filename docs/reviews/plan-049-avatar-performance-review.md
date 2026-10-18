# plan-049-avatar-performance Review

## Summary

2026-10-19, 구현6e3b0d0/앱1.5.0/pack6.0.0에 대해 **전체 QA 이후 단일 에이전트 자체 리뷰**를 수행했다. 별도 reviewer/서브에이전트의 독립 리뷰가 아니다. 사용자 요구·원본 외형·연속 연기·회귀·thread/lifetime·privacy·배포 일치를 검토했다. 미해결 필수 수정 사항은 없다.

## QA

- [Windows 전체](https://github.com/taejun9/VoxPet/actions/runs/39151511538): locked restore/format/analyzer·Release 경고0/오류0·Core465/465/skip0·WPF3385/0·desktop549장/CRC·90시점 실제 표시 픽셀 대조·일반/서명 각9시작·ZIP 자체 도구2764/0. 기존451 Core/3296 WPF/2683 portable 이름은 누락0이다.
- [OBS](https://github.com/taejun9/VoxPet/actions/runs/39151511518): WGC/chroma/native alpha·같은 HWND 왕복·각7변화 프레임 통과. [추가 입력100회](https://github.com/taejun9/VoxPet/actions/runs/39151711356):1081/0, probeOnly이며 배포 합격을 대체하지 않는다. 전체/portable 각각20회도 통과했다.
- 로컬 SDK10.0.401 전체 formatter/analyzer·Release·Core465/skip0, Python base/app/compileall 및 diff 검사를 통과했다. macOS에서 WPF를 실행한 것으로 쓰지 않는다.
- 개인790연속 프레임/22 overview page·512px8상세·전후2page와12표정 시제품, 공개 Windows26화면/5page·OBS5캡처를 직접 검토했다.20GIF는 로컬에 준비했다. 최신 Core 재내보내기790프레임은 byte-identical이다.

## Findings

| 구분 | 발견·수정 | 최종 근거 |
|---|---|---|
| 외형 | 고정된 어깨 cap·각진 소매를 shoulder root와 둥근 SVG 패턴으로 바꾸고 고립 신발 조각을 제거했다. 펠리의 원본 바깥 잉크와 재구성한 반대편 날개의 숨은 root를 구분했다. | 두 캐릭터 기본/착용9동작의790프레임, 실제 파츠/의상 공유 변환과 픽셀 검사. |
| 입력/연기 | 기본 입력이 빈 uppercase5동작을 사용하던 우회를 실제 timeline에 연결했다. 시작/중단/동일 재입력의 현재 자세를220ms로 연결하고 음성 관성·호흡/목 회전·발 위치/밑창을 적용했다. | Core 위치 연속/정착/유한값/30·60·144Hz held voice·9동작 양발 기준·WPF5경로×두 캐릭터. |
| 얼굴 | 입 혼합 시제품이 부리/코를 겹치게 해 제외했다. 원본8/3단계와 기존 곡선은 그대로 사용하며 눈만4단계로 준비한다. 이전 얼굴을 함께 fade해 alpha가.75가 되던 경로는 이전 opacity1 위에 새 얼굴을 Blend로 그리도록 고쳤다. | 각 level의 runtime mouth/출력 asset 일치·frozen bank·눈 닫기/열기·실제 전환3시점 alpha255×두 캐릭터. |
| 수명 | caller context 없는 완료가 UI로 전달되도록 명시적 Dispatcher/세대/취소를 사용한다. Studio 종료 await 동안 선택이 유효하던 경합은 Model 종료도 즉시 시작해 막았다. | context를 표준형으로 바꾼 실제 worker 준비2검사·즉시 신규 선택 차단·진행 선택false/이전 sprite 보존·실제 Closed 신호. |
| 편집/호환 | 선택형 companionPerformance는 none 기본값과 원래 timeline 보간을 유지하고 내보내기/필드 수정/재등록에 보존한다. source bitmap과 atlas를 분리하고 원본을 수정하지 않는다. | 기존451 Core를 포함한 전체 회귀·native JSON 왕복/unknown 거부·두 캐릭터 실제 의상 편집/재읽기·일반 rig는 없음 profile. |
| 렌더링 | source clip을 bounded thread cache로, alpha bounds/행렬을 weak frozen cache로 재사용하고 투명 삼각형을 보수적으로 제외한다. |128/256/512px 기존 draw와 byte-exact·warm clip/scan/matrix 생성0. 기존 원본 재합성의1%/채널차24 기준을 올리지 않았다. |

초기 ce8241a의 중앙 leg mask 확대는 원본 보존 검사를 실패시켜 원복했다. 정지 원본 비교는 독립 bind pose로 유지하고 새 연기는 별도 stance/연속 검사로 확인했다. f8dcae2의 Windows 종료 선택 실패는 위 수명 수정과 실제 종료 신호 검사로 해결했다. 실패 이력과 채택하지 않은 mouth/윤곽 시제품은 [완료 계획](../exec_plans/completed/plan-049-avatar-performance.md)에 남겼다.

## Performance evidence

256px warm bitmap 합성·읽기8회 평균은 늘보군76.96→33.42ms, 펠리46.70→20.53ms다. source triangle draw1440→416/864→152와 pixel equality가 함께 통과했다. tier2 합성 입력의1.5초 composition callback 중앙값17.37/16.83ms(p95 31.82/31.92ms)는 관측치다. callback마다 상태를 주입한 해당 시험의 결과이며 일반 timer·물리 GPU의FPS나60fps 보장이 아니다. 정량 결과로 원본 외형 검토를 대체하지 않았다.

## Package and privacy

`artifacts/VoxPet-Windows-test-plan049.zip`:54파일/104157815bytes, SHA256 `92a02080f3063e9e470b022446b60f2434117c3b0ee21ac60f5fed4ca8f74040`, EXE SHA256 `e267cb17269a3e4d72e926e023811ab1296a89dad75fd6482db613e0ede5d61e`. 실제 시험한 공용41파일과 기존 개인13PNG가 byte-identical이며 CRC/PE x64·세 pack 소스 일치다. CMD는 Windows의CRLF를 보존했다. 각 캐릭터 pack114파일/17착용항목/14분류/헤어3/9동작과 단일 사이드바 경로를 유지한다.

개인 원본은 로컬 proxy와 ZIP에만 사용했다. Git/CI/imagegen/음성 전송과 자동 마이크 시작이 없고 사용자 신뢰 저장소를 바꾸지 않았다. 전달 EXE는unsigned이며 별도 서명 검사는 runner의 임시 키/신뢰만 사용했다. README/AGENTS·plan024 작업트리·이전 ZIP의 보존을 재확인했다.

## Residual Risk

미해결 필수 수정 사항은 없다. 물리 마이크/권한/USB·실제 게임 입력/권한·사용자 GPU/혼합 DPI·개인 그림의 실제 Windows 실행·새60분 실기는 [기존 매트릭스](../quality/windows-checklist.md)의 별도 항목이다. 개인 로컬 SVG proxy와 공개 합성 Windows를 구분하며 layered BitBlt 제한은 유지한다. PNG 파츠/격자 연기이며 새 Live2D SDK나 웹캠 추적을 도입한 것이 아니다.

## Follow-Ups

기존 앱을 트레이에서도 종료하고 새 폴더에 ZIP 전체를 푼 뒤 START.cmd를 실행한다. TEST-SMOKE.cmd는 마이크 없는 합성 자동 검사다. 문서/리뷰 미러 확인 뒤 표준 main ff/push와 동일 HEAD detach·branch -d·worktree 제거를 수행하며 실제 결과는 로컬 Git audit에 기록한다.

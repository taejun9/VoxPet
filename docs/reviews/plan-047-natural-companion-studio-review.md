# plan-047-natural-companion-studio Review

## Summary

원본 늘보군·펠리 위에 의상17종/14분류/동작9종을 연결하고 파츠 경계·측면 헤어·몸통/소매/발·무기 파지와 연속 동작을 개선했다. 표정은 사이드바 독립 페이지에서12슬롯과 편집·테스트·중지를 조작한다. 앱1.3.0, 두 의상 pack4.0.0이다. QA 이후 단일 에이전트가 별도 단계로 자체 리뷰했다.

## QA

- SDK10.0.401 locked restore·format/analyzer·Release 경고0/오류0·Core446/446/skip0·문서/자산/compileall/diff 통과. 이전421 Core와2515 WPF 사례 이름을 모두 유지했다. 최종 앱/QA/자산 기준 `8cd4fed39d4aadc9e4b0a45896a850499c18cd7e`다.
- [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/39103837307): WPF3096고유검사/실패0·새581개, 실제 desktop541장. 일반/서명 각9시작·정상 종료·저장 원본 보존, 서명 Valid/변조 HashMismatch/일회용 키 정리 통과. SDK 없는 ZIP을 다시 풀어 동봉 PowerShell5.1 도구2483고유검사/실패0 확인.
- 두 캐릭터의9동작×5시점90개에서 실제 진행률·양손/몸통 rig·발사 효과를 독립 runtime 기준과 대조했다. 실제 그리기 Frame 일치와 WPF 참고 렌더/DwmFlush 뒤 GDI preview 픽셀 일치를90시점에서 확인했다. 채널차24/전체1% 미만·4×4 tile75% 이하 기준을 유지하며 참고 PNG로 실제 화면을 대체하지 않는다. 썸네일20회 및 표정 손상 복구20회를 앱/배포본에서 각각 통과했고, frozen decoder 경계를 강제로 넘기는 대조 재현과 같은 decoder worker 준비를 함께 확인했다.
- 원본 정지 합성은 기존 채널차24/전체1% 기준을 유지했다. 늘보군1428/펠리1008픽셀(각262144)로 통과했다. 기존 얼굴5회귀·카드 문자열 오류·표정/의상/편집/재시작·마이크 factory0 회귀를 유지했다.
- 최종 공개 Windows88화면(10 contact page)과 OBS5캡처를 직접 검토했다. 개인 로컬 원본 합성230개/26 page·512px12개와 별도로 구분한다. [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/39103837391): OBS32.2.2 WGC/chroma/native alpha·같은 HWND 배경 왕복·각7프레임 변화 통과. Windows Server2025 build26100/합성 입력이며 BitBlt layered 제한은 그대로다.
- `artifacts/VoxPet-Windows-test-plan047.zip`:54파일/103939157bytes, SHA256 `2b30edef679e18b96c4226316d6a7c69c9d8ecbebcdb1c558a5c28890805b0fc`, EXE SHA256 `c4d02e7589d06872ba91787b1a0ee88a73fcdc1a52a7344cae16b40c5beabb64`. 공용41파일은 실제 시험한 CI ZIP, 개인13PNG는 이전 plan046과 byte-identical이다. 세 샘플 pack 소스 일치·PE x64·CRC 검증. 이전 ZIP·개인 원본을 덮어쓰지 않았다. 결과 JSON/화면/해시/실패 이력은 `artifacts/test-results/plan047/`에 보존한다.

## Requirements and evidence

| 요구 | 구현과 확인 |
|---|---|
| 파츠 이음새 | 원본 alpha 보존6px 중첩, JointSkinning만 .5px 삼각형 중첩. 기존 legacy 얼굴 고정 회귀를 유지하고 머리/몸통/소매/발목·출력배율·목 닫힘선 검사. 표정 전환은 머리만 같은 관절을 공유한다. |
| 표정 메뉴 | 독립 사이드바 표정12개·전체 테스트/중지·설정 편집, UI Automation 실제 선택과 편집3왕복. 과거 Page0~5 저장번호 유지. |
| 펠리 헤어 | 단발·장발·폭탄머리를 측면 부리/눈 빈 공간에 맞춰 등록. 실제 개인 원본 로컬 합성과 공개 Windows 착용/입/눈/모자 공존 검증. |
| 의상 분류 | 전체+14분류,17항목 정확한 표시·헤어3개는 머리장식, 다른 분류에서 숨김. |
| 전체 의상 맞춤 | 상의/겉옷의 몸통·양소매와 한벌/하의/신발 좌우 연결. 펠리 날개와 늘보군 팔을 전경 배치. 신발은 실제 resolve된 경우에만 발을 대체한다. 원본 모자/얼굴장식 유지. |
| 자연스러운9동작 | opt-in smoothstep, 총 조준/두 발/반동/회복, 검 준비/스윙/복귀, 양손 지지, 활 당김/놓기. 실제 재생 진행률·양손/몸통 rig·발사 효과를 독립 runtime5시점과90회 대조. |
| 무기/파지 | 무기7개 별도 그림/손잡이·뒷손/앞손 순서, 활시위 두 앵커 연결. 선택 변경 시 이전 동작 중지, 상시 들기와 일시 동작 분리. |

## Findings

기존421 Core/2515 WPF 검사 이름을 보존한다. 구9동작 구별 검사는 한 시점 legacy 어깨각 대신4시점 실제 양손 rig와 실제 WPF9픽셀 해시로 보강했다. 신발/레이어 순서는 새 발 대체와 팔 전경이라는 요구에 맞추며 원본 무변형 허용치를1%/채널차24로 유지한다. 처음 발견한 얼굴5회귀는 삼각형 중첩을 전역 적용한 원인을 고치고 기존 판정을 통과시켰다.

편집 내보내기/재읽기는 새 end/root anchor·보간·효과 z·대체 파츠 메타데이터를 유지하고 알 수 없는 참조는 거부한다. 기존 타임라인은 명시적 보간 없이 step을 유지한다. 이미지 모델은 Name 전용 문자열 변환과 명시적 TextSearch를 유지하며 사용자 카드 오류 회귀를 실행한다. 파츠 frozen bitmap은 선택/표정 변경 시 생성하며 매 tick 파일 IO/인코딩을 하지 않는다. 음성 엔진·원격 전송·마이크 자동 시작 변경 없음.

공개 소품만 내장 imagegen으로 제작했다. 새 원본11개/프롬프트/해시는 [제작 기록](../art/companion-samples.md)에 보존하고 이전15원본도 유지했다. 개인13PNG는 로컬 해시/합성에만 사용했으며 Git/CI/imagegen 업로드가 없다. README/AGENTS·다른 plan024 작업트리·이전 ZIP/사용자 설정 보존을 확인한다.

목록 읽기 실패가 UI 안내 없이 제외되는 경로를 추가 수정했고 잠금/정상 복구 회귀를 보강했다. 그 후20회 반복 중 실패한 실제 JSON에서 정상 카드까지0개가 된 원인을 확인했다. `BitmapDecoder.get_IsDownloading → BitmapSource.FreezeCore → DrawingDrawingContext.DrawImage`의 스레드 예외이며, frozen PNG라도 다른 worker에서 DrawingGroup으로 그릴 때 원래 decoder를 다시 참조했다. decode와 frozen card 준비를 하나의 worker 호출로 합쳐 UI를 막지 않고 경계를 지킨다. 공개 fixture를 UI/worker 사이에서 강제로 넘기는 대조 검사는 원래 예외를 결정적으로 확인하고, 실제 새 경로는 decoder.CheckAccess와 frozen 출력 및20회 목록 격리를 검증한다. 진단 원본은6ce64ce/39048044541의13회차에 보존했다. Microsoft WPF의 [DrawingDrawingContext.DrawImage](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/Media/DrawingDrawingContext.cs)와 [BitmapSource.FreezeCore](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/Media/Imaging/BitmapSource.cs)도 frozen 이미지 처리 중 decoder 상태를 다시 읽는 경로와 대조했다(확인2026-10-18). Core 단독1,000회는 모두 의도한 overflow 거부로 일관돼 Windows 그리기 경로와 분리했다.

별도 상태 진단 파일의 경로가 system TEMP여서 초기 artifact에서 빠진 문제도 RUNNER_TEMP 결과 폴더에 바로 기록하도록 고쳤다. 최종 실패/정상 관찰 기록과 대조 예외 스택은 수집된 JSON을 근거로 삼는다.

기존 `expression_corrupt_sheet_preserves_display`는 live Main 타이머가 계속 눈을 깜빡이는 동안 await 전후의 Sprite 참조를 비교해 다른 눈 프레임을 실패로 볼 수 있었다. 같은 입·눈 입력의 참조와 기존 오류 상태 판정은 유지하고, open/closed 두 프레임·custom sheet 유지 및20회 반복을 추가했다. 각 회차의 live frame 변경 여부와 두 고정 프레임 보존을 JSON으로 대조한다. 원본 계약을 완화하지 않고 살아 있는 애니메이션과 파일 실패의 복구를 분리한다.

모델 진행률이 맞아도 실제 GDI 화면은 이전 pose일 수 있었다. 9fca64c의 Neulbo405ms/590ms PNG가 검/권총에서 각각 바이트까지 동일함을 확인했다. MotionSurface가 OnRender에 사용한 Frame을 기록하고, WPF reference 렌더와 [DwmFlush](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/nf-dwmapi-dwmflush) 뒤 실제 GDI preview ROI를 대조한다. 참고이미지는 진단용이며 screen PNG는 계속 실제 desktop pixels다. 첫 대조는 VisualBrush의 부모 offset을 빠뜨려54px 아래로 밀렸으므로 source Viewbox를 GetOffset+ActualSize로 수정했다. 비교 허용치는 그대로 두고 원인 좌표만 바로잡았다. CI smoke는 추가90픽셀 검사를 위해300초/개인330초·job20분, SDK 없는 소비자 도구는240초를 유지한다.

## Residual Risk

Windows CI는 공개 교정 도형과 가짜 입력을 사용한다. 개인 그림은 실제 원본을 사용한 로컬 Core 합성230개(기본226+복장조합4),26 contact page와512px12개로 검토했으며 private Windows 실행으로 주장하지 않는다. 일부 contact sheet는 성능을 위한 근사 격자를 사용해 원래 크기의 정밀 합성을 함께 대조했다. 펠리6입/눈 상태의 실제6px head mask에서 강한 부리 색 누락0도 확인했다. 실제 물리 마이크/권한/USB·사용자 GPU/혼합 DPI·새60분 실기는 별도다. 임의 PNG 자동 해부 기능은 제공하지 않는다.

의상 착용은 현재 실행 중 캐릭터별로 유지하고 재실행 시 해제한다. 마지막 캐릭터·무기 선택/항상 들기는 저장한다. 편집 내보내기에는 개인 그림이 포함된다. 전달 EXE는 unsigned이며 runner 임시 서명 QA는 사용자 신뢰 저장소를 바꾸지 않는다. OBS는 WGC를 사용하며 기존 layered BitBlt 제한은 남는다.

## Follow-Ups

기존 앱을 종료하고 새 폴더에 ZIP 전체를 푼 뒤 START.cmd를 실행한다. TEST-SMOKE.cmd는 가짜 마이크와 합성 키/클릭을 쓰므로 전용 테스트 환경에서 실행한다. [완료 계획](../exec_plans/completed/plan-047-natural-companion-studio.md)과 [Windows 체크리스트](../quality/windows-checklist.md)를 따른다. 표준 main ff push/branch·worktree 정리를 수행하며 실패 시 강제 우회 없이 중단·보고한다. 실제 종료 상태는 로컬 Git 완료 audit에 기록한다.

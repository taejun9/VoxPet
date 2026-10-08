# 사용자 기능 조사와 구현 격차

확인일: 2026-10-08 (Asia/Seoul). 대상은 얼굴캠 없이 PNG 캐릭터를 쓰는 초보 방송 사용자와 게임 중 설정창을 자주 열기 어려운 스트리머다. 공식 제품 안내를 기능 기준으로, 공개 사용자 글을 불편 사례로 사용했다. 직접 인터뷰·설문·사용량 데이터는 없으므로 우선순위는 VoxPet 목적과 현재 구현에 따른 설계 판단이며 시장 전체의 요구 빈도를 뜻하지 않는다.

## 조사 근거

- [S1: veadotube mini 공식 사용 안내](https://veado.tube/docs/usage/mini/): 입·눈 이미지, 표정 상태와 단축키, 마이크 선택/반응 조절, 화면 크기/배경과 OBS 연결을 안내한다. 이미지를 넣는 단계와 마이크 조정 단계를 별도로 제공한다.
- [S2: 공개 사용자 음소거 요청](https://olmewe.itch.io/veadotube-mini/comments?after=99): Toukalein의 글은 방송 소프트웨어에서 음소거해도 캐릭터가 계속 반응하는 문제와 음소거 키를 요청한다. 오래된 단일 사례이므로 수요 규모와 현재 경쟁 제품 상태를 증명하지 않는다.
- [S3: veadotube 공식 데이터 폴더](https://veado.tube/docs/tech/data/): 설정과 자동 저장 캐릭터를 앱 데이터에 유지한다. 재설정 비용을 줄이는 로컬 저장의 비교 근거다.
- [S4: OBS 공식 Window Capture 안내](https://obsproject.com/kb/window-capture-sources): 선택한 창을 캡처하고 캡처 방식·창 매칭·커서 옵션을 제공한다. 안정적인 별도 방송창과 사용자 설정 안내가 필요한 근거다.
- [S5: Microsoft 마이크 권한 안내](https://support.microsoft.com/en-us/windows/privacy/turn-on-app-permissions-for-your-microphone-in-windows): Windows에서 마이크 접근과 데스크톱 앱 접근을 확인하는 경로를 설명한다.
- [S6: Microsoft 설정 URI 안내](https://learn.microsoft.com/en-us/windows/apps/develop/launch/launch-settings): 민감 자원 접근 실패 때 해당 개인정보 설정으로 이동하는 링크를 권장한다.

## 기능 목록과 확인 결과

P0는 핵심 사용 흐름, P1은 방송/초기 설정 개선, P2는 별도 설계가 필요한 확장이다. 기존은 plan012 기준이며 개선 후는 plan013 코드 기준이다. 실물 장치 합격은 [Windows 검증 기록](../quality/windows-checklist.md)을 따르며 아래 구현 여부와 구분한다.

| 사용자 필요 / 기능 | 근거 | 우선순위 | 기존 포함 여부 / 확인 파일 | 개선 후와 후속 작업 |
|---|---|---|---|---|
| 마이크 선택·새로고침·명시적 Start/Stop | S1/S5 | P0 | 포함: MainViewModel, AudioCaptureService, AudioSession | 유지; 물리 장치 제거/권한 QA 필요 |
| 말할 때 입 반응, 무음 때 입 닫힘 | S1 | P0 | 포함: AudioLevelProcessor, CharacterAnimator | 유지; Core 경계/타이밍 회귀 실행 |
| 반응 세기/닫힘 속도와 프리셋 | S1 | P0 | 포함: AudioSettings, ReactionPresets, MainWindow | 유지; 일반/조용한/빠른 3종 |
| 마이크 권한 문제를 앱에서 바로 해결 | S5/S6 | P1 | 부분: 오류 메시지와 사용자 문서만 있음 | 추가: Windows 마이크 권한 설정 열기, 실패 시 수동 경로 |
| 주변 소음에 맞는 초기 Gate 추천 | S1의 조정 필요에서 추론 | P1 | 부분: 수동 Gate와 프리셋만 있음 | 추가: 3초 측정, 추천/직접 적용/취소; 무신호·큰 소음 거부 |
| 방송 중 캐릭터 입 음소거 | S2 | P1 | 부분: 체크박스만 있음 | 추가: Ctrl+Shift+M 전역 키, 충돌 안내와 두 창 내부 대체 키 |
| 마이크 없는 사전 미리보기 | 초보 설정 실패를 줄이는 제품 판단 | P1 | 포함: DemoCommand | 유지; 측정은 데모에서 비활성화 |
| 사용자 PNG 캐릭터 적용 | S1 | P0 | 포함: CharacterSheetLoader, CharacterQa | 유지; RGBA 3열×2행만 지원 |
| 닫힘/열림 PNG 두 장을 바로 적용 | S1 | P2 | 없음: 현재 시트 필요 | 후속: 슬롯 저장과 정렬을 함께 설계해야 함 |
| 자동 눈 깜빡임과 자연스러운 움직임 | S1 | P1 | 포함: CharacterAnimator, ExpressionMotion | 유지; blink/눈물 개별 설정 |
| 여러 표정·전역 단축키·부드러운 전환 | S1 | P1 | 포함: ExpressionViewModel/Hotkeys, 12슬롯 | 유지; F12는 앱 내부, 전역 충돌 검사 |
| 표정/설정 재시작 보존과 손상 복구 | S3 | P0 | 포함: SettingsStore, ExpressionSlotStore | 유지; 원본 삭제/손상 회귀 검사 |
| 게임 화면과 분리된 OBS 방송창 | S4 | P0 | 포함: CharacterWindow, green/alpha/topmost | 유지; 설정창 최소화/닫기/재열기 및 OBS 회귀 |
| 작은 화면에서도 모든 설정 접근 | 초보·노트북 사용의 제품 판단 | P1 | 포함: 반응형 MainWindow, LayoutQa | 새 버튼까지 4개 창 크기에서 스크롤 접근 검사 |
| 원하는 키로 변경·PTT·Stream Deck 연동 | S1 | P2 | 없음: 고정 키 전환만 있음 | 후속: 키 충돌/해제/키를 놓쳤을 때 복구 설계 필요 |
| GIF/APNG·레이어 캐릭터 편집 | S1 | P2 | 없음: frozen PNG만 지원 | 후속: 메모리·CPU·정렬 예산을 별도 정의 |
| 클릭 통과·창 위치 저장·프레임율 선택 | S1 화면 조정과 제품 판단 | P2 | 부분: 이동/크기/배경 지원 | 후속: 창 복구와 화면 밖 이동 방지 포함 설계 |
| 로컬 음성 처리와 저장/전송 차단 | VoxPet 개인정보 계약 | P0 | 포함: 숫자 snapshot, verify_app 금지 API 검사 | 유지; 소음 측정도 숫자만 임시 사용 |

## 이번 개선의 동작과 완료 기준

Ctrl+Shift+M은 입 반응을 전환한다. OBS나 Windows의 마이크 음소거와 동기화되지 않으며 PCM 캡처는 계속된다. Stop만 캡처를 해제한다. 전역 등록 실패 때 화면에서 충돌을 안내하고 VoxPet 설정창/방송창 내부 단축키 및 체크박스를 제공한다. 반복 키 입력으로 토글이 여러 번 발생하지 않게 한다.

주변 소음 측정은 이미 Start한 마이크에서만 실행한다. 3초간 말하지 않고 측정한 fresh 숫자 snapshot을 중복 없이 수집하고, 2초 이상에 걸친 30개 이상 표본의 90백분위에 6dB를 더해 -80~-15 dBFS로 제한한 추천값을 보여준다. 이 수식은 VoxPet 설계다. 무신호(-119 dBFS 이하)와 과도한 주변 입력(-21 dBFS 초과)은 추천하지 않는다. 음성 판별·노이즈 제거는 수행하지 않으며 말하면서 측정하면 잘못된 추천이 될 수 있다. 적용은 Gate만 바꾸고 기존 정규화 범위/감도/시간은 보존한다. 취소/Stop/장치 중단/프리셋/종료는 측정과 대기 추천을 폐기한다. 숫자 표본은 메모리에만 잠시 두며 설정 파일에는 사용자가 적용한 Gate만 기존 방식으로 저장한다.

권한 설정 버튼은 사용자 클릭 때 Windows의 마이크 설정 페이지만 연다. 접근 허용이나 자동 Start를 수행하지 않는다. 실제 OS 설정 페이지 열기와 물리 권한 변경은 실기 항목이며 자동 smoke는 UI 버튼과 명령 전달만 확인한다.

전체 QA를 통과했고 QA 이후 [자체 리뷰](../reviews/plan-013-user-feature-improvements-review.md)를 완료했다. 전체 결과는 [실행 계획](../exec_plans/completed/plan-013-user-feature-improvements.md)에 기록했다.

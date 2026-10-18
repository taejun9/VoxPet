# plan-048-emotive-companion-motion

## Status

active

## Owner

project_lead / harness_builder / quality_runner

## User Request

동작을 이모티콘처럼 부드럽고 자연스럽게 개선하고 늘보군과 펠리 각각의 체형과 성격에 맞춰 작업한다.

## Goal

두 원본 캐릭터의9종 동작에 준비·주동작·여운·복귀가 이어지는 타이밍과 몸통/머리/팔의 겹치는 움직임을 부여한다. 모든 키에서 속도가0이 되는 기계적인 멈춤과 시작/끝 튐을 줄이고, 늘보군은 둥글고 느긋한 탄성, 펠리는 측면 부리/날개에 맞는 가벼운 리듬으로 구별한다. 실제 원본 연속 프레임과 Windows 표시 화면으로 검토하고 전체 QA 및 새 Windows 테스트 ZIP을 전달한다.

## Non-Goals

개인 원본13PNG·이전 ZIP·README/AGENTS·다른 plan024 작업트리를 덮어쓰지 않는다. 개인 자료는 Git/CI/생성 도구로 보내지 않는다. 음성 저장/전송·자동 마이크·추적·서브에이전트를 추가하지 않는다. 기존 사용자 타임라인의 step/보간 의미를 임의 변경하지 않는다.

## Context Map

- [앱 설계](../../architecture/application.md), [QA 규칙](../../quality/rules.md), [개발 환경](../../quality/development.md), [개인정보](../../privacy/principles.md)
- [이전 완료 계획](../completed/plan-047-natural-companion-studio.md), [샘플 안내](../../guides/studio-samples.md), [패키지 규격](../../product/voxpet-package-format.md)
- ActionTimeline/ActionDefinition/ActionEngine·PackageJson·CharacterDocument·companion_actions.py·LegacySpriteSheetRuntime·JointSkinning

## Constraints

시작 main/origin04c23e8·0/0·깨끗함. 전용 codex/plan-048-emotive-companion-motion 및 .worktree/plan-048-emotive-companion-motion을 사용한다. 이전446 Core/3096 WPF/2483 portable 사례를 보존하고 변경 요구로 달라지는 기대값은 이유를 기록한다. QA→자체 리뷰→완료 이동/미러→main ff/push→branch -d→worktree 제거를 따른다.

## Implementation Plan

- [x] 현재9동작의 속도·시작/종료·파지와 두 체형의 연속 원본 프레임을 조사한다.
- [x] 기존 호환을 보존하는 연속 보간과 캐릭터별 타이밍/겹침/복귀를 구현한다.
- [ ] 머리/몸통과 팔·소매·양손/활 연결을 실제 원본9동작 연속 프레임에서 검토한다.
- [ ] lint/Release/Core·Windows 실제 표시/portable/OBS 전체 QA를 수행한다.
- [ ] QA 후 자체 리뷰·완료 문서·원본 포함 새 Windows ZIP·Git 수명을 완료한다.

## QA Plan

python3 harness/scripts/verify_base.py, verify_app.py, compileall, git diff --check. SDK10.0.401 locked restore/format verify/Release 경고0·오류0/Core 전체. 보간의 키값/단조성/유한값/중간 속도 연속/끝점0·거울반전·pose reset·지연tick과 기존 기본동작을 수치 검사한다. 두 캐릭터9동작의 고밀도 시점에서 손 접점·양손 지지·부리/목/발 경계·시작/끝 자세와 본동작 구별을 확인한다. 개인 원본은 로컬에만 사용하며 연속 contact sheet와 재생 가능한 로컬 애니메이션으로 자연스러움을 검토한다. Windows는 공개 교정 그림으로 기존 실제 GDI90시점·표정/의상/시작/스레드 복구·portable·OBS를 재실행한다. 실제 시험한 CI 공용 bytes와 개인13PNG를 합친 새 ZIP의 source/CRC/PE/해시를 대조한다. 물리 마이크/GPU/DPI 실기와 구분한다.

## Review Plan

QA 이후 단일 에이전트 자체 리뷰를 별도 단계로 수행한다. 부드러움의 수치만으로 자연스러움을 주장하지 않고 두 원본의9종 실제 연속 출력을 함께 대조한다. 호환/수명/성능/개인정보·ZIP 일치와 남은 실기 제한을 기록한다.

## Decision Log

| date | decision | reason |
|---|---|---|
| 2026-10-18 | 기존 smoothstep과 step은 유지하고 새 샘플에 연속 속도 보간을 명시적으로 적용한다. | 이전 구현은 매 키에서 속도가0이 되어 동작 중간도 여러 번 멈추는 인상이 남는다. |

| 2026-10-18 | 날개 정렬은 shoulder의 로컬 좌표에서 비교한다. | 첫 전체 Core에서 squash의 비균일 배율이 월드 각도 판정에 들어가9동작의 깃털 면이 뒤집혔다. 기존 양의 면적 검사를 유지하고 좌표계 원인을 고친다. |

| 2026-10-18 | 고개 여운은 머리 중심 대신 실제 목 연결점을 중심으로 회전한다. | 실제 원본 합성에서 얼굴 중심의 회전이 어깨/목 접점을 밀어, 부리/얼굴의 여운과 목 연결을 함께 유지한다. |

| 2026-10-18 | 같은 시각의 미래 transform 키를 선택하는 개선은 새 monotoneCubic에만 적용한다. | 기존 smoothstep은 다음 키가 중복이면 첫 키를 향하고 그 시각에 마지막 키를 적용하는 기존 의미를 보존한다. 새 경로와 각각 회귀 검사한다. |

| 2026-10-18 | 기존 native 좌클릭 QA는50ms 고정 기다림 대신 한 번 주입한 이벤트의 Input 작업 완료를 최대500ms 기다린다. | 5dfcdec/39118289422는 Core451/빌드/실제90시점 모두 통과했으나 좌클릭 전달과 그 후속 dispose4개만 실패했다. 이벤트 재주입 없이 정확히1회/해제 후 증가0 기준을 유지하고50ms/완료/해제 후 수치를20회 JSON으로 보존한다. |

| 2026-10-18 | 입력 주입을 반환값 있는 SendInput의 down/up 단일 배열로 바꾸고 회차별 delta를 검사한다. | 394035b의18회차는500ms에도 클릭0,19/20은 각각1회가 다시 전달됐다. 단순50ms 지연으로 확정하지 않는다. 기존 mouse_event는 반환값이 없어 주입과 hook 실패를 구분하지 못했다. hook 등록/전후 주입2개 수락/정확한1회·해제 후0·오류 코드를 기록하며 재주입하지 않는다. |

| 2026-10-18 | hook 진입/예약/전달/거부 단계를 QA에만 기록하고 수동 input probe를 분리한다. | 913efaa의17회차는등록성공·SendInput2개 수락인데500ms에도0회였다. 타이밍/주입 실패로 단정한 가설을 철회하고 실제 서비스 경계를 진단한다. probe는배포/서명/전체화면 검증을 생략하므로 합격판정에 사용하지 않으며 정상 push QA는변경없이 전체를 수행한다. |

| 2026-10-18 | native 메시지 종류와 전용 창 PreviewMouseDown을 함께 기록하고 짧은 재현에도 숫자 데모를 켠다. | 907f06f의전체13회차는stage[]·등록성공·입력2개수락·delta0이고분리20회는모두1/2/3이다. UI예약지연/거부단계에들어가지않았으므로그가설을제외하고hook이전/다른메시지/실제창수신을구분한다. |

| 2026-10-18 | hook의nCode와전체wParam 정수를최대256개까지QA에만기록한다. | 3fefdd8의9/13회차는창수신1회·native trace는UP(1514)만있다. hook해제나UI예약지연은아니며DOWN의다른메시지/음수code/상위비트여부를구분해야한다. raw포인터data는읽지않는다. |

| 2026-10-18 | 전체검증통과후에도원인미확정인입력누락의계측영향을100회단기재현으로대조한다. | 77e1f41전체39123811785와폴더수정후단기39124555986이통과했다. 단순계측추가만으로사라진실패를원인해결로주장하지않고동일조건의100회엄격전달/해제를추가확인한다. 정상전체/portable의20회는유지한다. |

| 2026-10-18 | 새hook설치후owner HWND의native메시지ack를기다린뒤1회주입한다. | cf4c3c1전체실패의6/7/10/19/20회차모두창수신1회인데콜백에는code0/UP514만있다. 음수code/다른메시지/상위비트/UI예약지연을제외했다. 매회설치직후같은continuation에서주입하는시험순서를owner메시지루프확인후로바꿔등록준비경계를대조한다. 단순지연이나재주입은하지않는다. |

## Progress Log

| date | role | note |
|---|---|---|
| 2026-10-18 | 설계 | 현재 main/원격의 깨끗한 상태와 이전 결과를 확인하고 별도 계획을 작성했다. 새 목표의 시각 검증은 이전 합격과 구분한다. |

## Completion Notes

구현과 실제 QA/시각 검토 결과를 확인한 뒤 기록한다. 현재는 완료 상태가 아니다.

## 중간 QA 기록

첫 Release 경고0/오류0, Core451중439통과/12실패.9건은 비균일 body scale의 월드 정렬 판정 오류,2건은 보이지 않는 arrow 효과 앵커를 몸의 idle 복귀에 포함한 새 검사 오류,1건은 새 수치 검사1ms 꼬리 예상값(실제0.001249px)이다. 기존 깃털 면적 판정은 보존하고 어깨 로컬 좌표를 사용하며, 몸 복귀는 신체 앵커/머리/발 트랙으로 검사한다. 원본 실패 TRX를 보존한다.

두 번째 전체 Core451/451·skip0 통과, 전체 format verify 및 Release 경고0/오류0 통과. recoil 회복은 몸통의 작은 표현 움직임과 분리해 같은 torso-local 손 위치로 비교하며 발사/반동 기존 판정은 유지한다. 개인 자료 없는558개 Core 프레임을 로컬 내보냈고 원본 픽셀 합성을 진행한다.

5dfcdec Windows39118289422: WPF3095통과/4실패. motion_native_left_down_signal·platform_neutral_mouse_pulse 및 그 두 후속 dispose 판정이다. 입력 service 변경은 없으며 기존50ms timer continuation이 Input 우선순위 동작을 앞설 수 있는 판정 경계를 보완한다. 이전7e2dbd4/c1bb60c의 전체 Windows/portable/OBS는 모두 통과했고 이번 실패 원본은 candidate-5dfcdec에 보존했다. 허용 검사 개수나 이벤트 수 기준을 낮추지 않는다.

동일 입력 검사는 새 hook로20회 반복하고 각 회차 press1회/해제 후 press1회를 주입한다. 첫4개 이름과 정확한 이벤트 수 판정을 유지하며19회 추가 회귀를 둔다. 실패 조건에서 이벤트를 다시 보내지 않는다.

합성 클릭은 이전 편집기 드래그의 커서 위치에 의존하지 않도록 같은 프로세스의 전용 빈 시험 창 중앙에 고정한다. 임의 앱/설정 버튼을 누르지 않으며 hook는 원래 Main HWND에 설치한 global 입력을 계속 검증한다. 창은 finally에서 닫고 Main을 복원한다.

394035b/39119598272: Core451/빌드/lint/동작90시점은 통과, native입력은1~17회 정상,18회차1회 누락,19/20회차는 각각 전달됐으나 누적 기대값 때문에12개 후속 판정이 실패했다. 실패 원본과20회 JSON은 candidate-394035b에 보존한다. 후속 검사는 각 회차 시작 대비 정확히1회로 분리하여 한 번의 누락이 다음 회차의 진단까지 흐리지 않게 하고, 누락 회차 자체는 계속 실패로 처리한다.

913efaa/39120552708:4실패가17회차로 정확히 분리됐다. registeredBefore=true, inserted=insertedAfterDispose=2, 두 Win32 오류0인데 클릭/신호 delta0이며 후속18~20회는정상이다. candidate-913efaa의JSON과실패화면을보존한다. 진단trace는좌표나data구조를읽지않고QA콜백에서작은정수만기록한다. 원래public3인수생성자와제품경로는trace=null을유지한다.

907f06f: 전체39121666315는13회차4실패(stage[]), 짧은39121709720은20회모두stage[1,2,3]/실패0. 격리재현은probeOnly:true이며배포합격으로간주하지않는다. 두JSON을각candidate-907f06f와probe-907f06f에보존했다. raw trace는1000+WM 메시지정수이며lParam/좌표를읽지않는다. 전용창수신수는실제시험창의PreviewMouseDown만집계한다.

3fefdd8: 분리39122819887은데모중20회모두Down/Up와창수신정상, 전체39122744491은9/13회차8실패. 두회차모두실제시험창은클릭1회를수신했고같은hook의UP는기록됐으나Down은없다. 전체입력누락이나hook자동해제라는가설로단정하지않는다. candidate-3fefdd8와probe-3fefdd8에근거를보존했다.

짧은 재현에도 전체 입력 검사 직전의 표정 관리·트레이·의상 UI 상태를 준비한다. 단독 데모만으로는 누락이 재현되지 않아 전체 선행 조건을 좁혀 대조하는 진단이며 최종 전체 QA를 대체하지 않는다.

77e1f41의확장단기39123856011은입력검사전StartupTrayQa→RestoreFromTray의QA activated.json 기록에서DirectoryNotFoundException으로중단했다. probe의격리QA폴더를명시적으로생성하도록설정을수정한다. 이는입력결과가아니며입력실패와구분해보존한다.

100회단기39125213445는모든회차등록/주입2개/전달1회/해제후0회/원시code0의Down513·Up514통과. 그러나동일앱코드의cf4c3c1전체39124528190은5회차누락으로20실패였다. 계측만으로해결됐다고보지않으며owner메시지루프의등록완료경계를추가대조한다. 이전77e1f41전체39123811785통과도보존한다.

708436b의100회단기39126498488은42회차에서ownerReady=true·창수신1회인데nativeUP514만기록됐다. 준비ack만으로해결되지않았으며단순기다림/재주입으로회피하지않는다. Microsoft LowLevelMouseProc가권장하는전용메시지루프스레드로손동작hook를옮기고등록완료를비동기로알린다. UI는동기대기하지않으며callback은UI작업하나만예약한다. 종료는세대무효화후전용dispatcher를종료하고실제unhook완료도QA에서확인한다. 이전실패와확장진단을보존한다.

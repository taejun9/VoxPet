# plan-014-settings-tabs Review

## Summary

검정 셀렉트 글씨, 개인 패키지의 늘보군 기본, 상단/세부 설정 탭, 방송창 너비·높이·우클릭 크기 메뉴를 QA 이후 동일 에이전트가 자체 리뷰했다. 서브에이전트를 실행하지 않았다. 실행 코드 a81bc4e이며 개인 늘보군은 공개 저장소/CI/공용 배포에 포함하지 않는다.

## QA

- macOS: 문서/자산 검증, git diff --check, SDK10.0.401 locked restore, format/analyzer, Release 경고·오류0, Core110/110. sandbox의 NuGet 네트워크와 formatter IPC 제한은 승인된 실행 환경에서 해결했다.
- [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37768105239): locked restore/format/analyzer/Release/publish/Core110/110/WPF smoke 통과, 고유385검사·실패0. 4창크기와 전체 상단/세부 탭에서 조작 가시성/스크롤 없음/검정 선택값·목록/편집 유지 확인. 선택값, 작은 창 반응 탭, 방송창 조절 페이지의 실제 PNG를 직접 확인했다.
- 개인 기본: 합성 PNG로 기본 시트 불러오기·표정 적용 시 캐릭터 유지·복원·누락 실패 보존 통과. 초기 코드의 캐릭터/표정/음소거/창 수명 회귀 포함.
- 방송창: 공유 너비·높이 적용, native 크기 변경의 모델 반영, 유한한 범위, 재열기·최소화·종료·투명 grip 회귀 통과.
- [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/37768105246): OBS32.2.2 WGC green/Chroma Key/native alpha65.08% 유지 및 각각7프레임 통과. 기존 BitBlt 유효 프레임 없음 유지.
- 개인 ZIP13파일, CRC 정상, 기존 늘보군 원본 SHA256 일치. 배포 안내는 최종 탭/기본 PNG/방송 크기 조절에 맞췄다.

## Findings

- 해결: 첫 Windows37767394347은 생성 TextBlock의 밝은 글씨 때문에24개 선택값/팝업 색 검사에 실패했다. DisplayMemberPath 대신 명시적 ItemTemplate TextBlock.Foreground=Black으로 수정하고 모든 크기에서 재검증했다.
- 해결: 480×320 최초 화면에서 설정 전체 축소가 지나쳤다. 각 기능의 세부 탭으로 분배하고 모든 조작의 실제 가시성을 재검증했다.
- 요구사항: 기존 바인딩과 명령을 이동했으며 표정 편집의 CanEdit 잠금을 유지한다. 탭 전환으로 설정/편집 초안을 초기화하지 않는다. 기본 캐릭터 버튼과 기본 표정은 개인 패키지의 PNG를 사용하며 저장된 사용자 PNG 슬롯을 덮어쓰지 않는다.
- 개인정보/라이선스: 사용자의 개인 실행용 선택을 지켰다. 공개 코드에는 선택적 로컬 파일 로딩만 있고 개인 이미지 파일은 없다. 오디오 분석·설정 저장/음성 데이터 경계는 변경하지 않았다.
- 방송창: 기존 invisible native grip과 DragMove를 유지하고 ViewModel 크기·우클릭 메뉴를 추가했다. 캐릭터 비율/투명 렌더링은 기존 CharacterView를 따른다.
- 문서: 기존 스크롤/접힌 입력 범위/고양이 복원 안내를 탭/패키지 기본으로 갱신했다. 과거 실행 기록은 보존했다. 미해결 결함은 발견하지 않았다.

## Residual Risk

Windows CI의 마이크 없는 합성 시험이다. 실제 마이크/권한/장치 제거/물리 DPI·키보드/사용자 OBS·GPU 실기를 대체하지 않는다. 480×320에서는 일부 글씨가 줄어드므로 가독성이 부족하면 창을 키운다. 방송 크기는 이번 실행만 유지한다. 개인 PNG의 이번 Windows 시작 화면은 CI에 전송·캡처하지 않았으며 합성 기본 경로 시험과 기존 plan012의 개인 시트 시험으로 구분한다.

## Follow-Ups

개인 패키지를 Windows에서 압축 해제해 실제 늘보군 시작/방송 크기를 확인한다. 기존 F1에 다른 사용자 PNG가 저장되어 있으면 그 슬롯이 우선이므로 캐릭터→PNG의 기본 캐릭터 버튼을 누른다. 실제 장치 시험 결과는 Windows 체크리스트에 추가한다.

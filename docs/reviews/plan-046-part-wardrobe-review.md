# plan-046-part-wardrobe Review

## Summary

늘보군/펠리 원본을 실제6파츠 구조로 분리하고 의상의 몸통·양소매·좌우 하의/한벌 하단/신발을 각 관절에 연결했다. 어깨 경계를 고정하는 연결 격자를 팔과 소매가 공유하고 편집·내보내기·재등록에도 보존한다. 세 헤어를 의상 화면 상단에 표시하고 무기 선택/상시 들기·캐릭터별 저장, 법규 이름과 새 손가락 그림을 제공한다. 앱1.2.0, 시험한 앱/자산/하네스 기준 a3e83393ca56abab386cdbffc97eb31de0a4131b다. QA 이후 단일 에이전트가 별도 단계로 자체 리뷰했다.

## QA

- 로컬/Windows SDK10.0.401 locked restore·format/analyzer·Release 경고0/오류0·Core421/421/skip0·문서/자산/compileall/diff 통과. 이전405 Core 사례 이름을 모두 유지했다. 새16사례와 기존 사례의 보강으로 독립 양발 포즈에서 하의/한벌/신발 연결, 팔/소매 동일 격자, 경계 연속/잘못된 skinning, 무기7선택/on/off/None/동작 종료/법규 복귀·원자적 파일 보존을 검사했다.
- 최종 [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/38986207016): WPF2515고유검사/실패0, 이전2370개 이름 모두 보존/새145개. 실제 desktop437장/foreground HWND/PNG CRC, 일반/서명 각9시작10초/정상 종료/저장 파일 보존, 서명 Valid/변조 HashMismatch/아이콘·일회용 키/신뢰 정리 통과. 시험한 CI ZIP을 새 폴더에 풀고 동봉 Windows PowerShell5.1 도구로2096고유검사/실패0 확인.
- 실제 두 캐릭터 선택에서6 frozen 512px 파츠/worker PNG 인코딩, 세 헤어 버튼의 UI Automation 착용, 무기 ComboBox7개와 상시 체크·idle/재생/중복1개/idle복귀/법규 중 숨김·복원, 양쪽 선택의 저장/실제 WPF 재시작을 검사했다. 편집기의 실제 팔/소매 적용→내보내기→재읽기에서 관절 정보와 공유 격자를 확인했다. 입력 factory0.
- 원본을 다시 합친 정지 프레임의 픽셀 비교는 채널 차24 초과가 전체1% 미만이어야 한다. 이전 결함은 늘보군18312/펠리5695픽셀로 실패했고 수정 후656/490(각262144픽셀)으로 통과했다. 허용치를 낮추거나 검사를 제거하지 않았다. 부리를 넓히는 공개 시트로 머리 중심이1px 미만 차이로 유지되는지도 확인했다.
- 최종 [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/38986206969): OBS32.2.2 WGC/크로마/native alpha·동일 HWND/소스 배경 왕복·각7프레임 변화 통과. Windows Server2025 build26100/Microsoft Basic Render Driver의 공개 합성 입력이며 녹화/송출/물리 마이크 없음. BitBlt 유효 프레임 없음은 기존 layered 창 제한이다.
- 최종 공개 Windows117화면(13 contact page)·개인 로컬64합성(11 page)·OBS4캡처를 직접 검토했다. 추가로 분리 몸통 PNG와 Windows3화면을 원래 크기로 확인했다. 정적 개인 합성은 실제 loader 중앙/발밑 등록과 같은 좌표를 사용한다. Pelly 첫 모자 캡처의 중첩은240ms 전환 중인 snapshot이며 이후 정착 화면/별도.4초 검사와 구분했다.
- artifacts/VoxPet-Windows-test-plan046.zip:54파일/104527606bytes, SHA256 `561b899f2b43f88bb0772b9b31bedc9bfcdf57dba08e32bd859149c2326c101f`, EXE SHA256 `abc112f99cd76b76b43f1f9eca2376cb8e0fc4c37ad27e532f2ab9d1a0778143`. 공용41파일은 시험한 CI ZIP과 동일하고 개인13PNG는 이전 plan045 ZIP/해시와 byte-identical이다. 세 샘플 패키지 소스 일치·PE x64/CRC 확인. 이전 ZIP 덮어쓰기 없음. 근거 JSON/로그/화면/요구사항별 audit은 artifacts/test-results/plan046에 보존한다.

## Findings

초기 강체 회전에서 어깨 틈을 확인해 어깨0→손목1 영향의 연속 격자로 바꿨다. 전체 그림에서 파츠를 분리하고 가려진 몸통을 로컬에서 복원한다. 클립 벡터 OpacityMask의 사각 흔적은 실제 Windows 화면과 새 픽셀 비교에서 발견했으며 원본 alpha·팔 영역·다리 제외를 명시적으로 premultiply해 제거했다. 파츠는 선택/표정 변경 때512px bitmap으로 캐시하고 프레임마다 파일을 읽거나 인코딩하지 않는다.

발 사이 원본 영역 누락·뒤팔 위의 복원 몸통·머리 뒤에 가려지는 눈물 z-order를 수정했다. 펠리 실제 등록(-18,+17), 열린 부리 끝, 입 너비에 따른 머리 흔들림, 신발 범위/좌우 분리 경계를 보완했다. 원본 PNG loader와 기존 설정은 유지하고 분리 머리만 아래 몸통에 정합한다. 정면 늘보군과 측면 펠리의 앞뒤 레이어를 각각 검토했다.

기존 중앙 ActionEngine/음성 분석 경계를 유지한다. 무기 설정은 두 캐릭터의 문자열 ID·bool만 제한된 JSON에 저장하고 worker의 직렬/원자적 쓰기와 종료 flush를 사용한다. 이미지 문자열 변환은 기존 Name 전용 ToString/명시적 검색·접근성 경로를 유지하며 worker·병렬·키보드·한글 검색 회귀도 통과했다. 개인 원본/이전 ZIP/README/AGENTS/다른 plan024 작업트리를 수정하지 않았고 개인 PNG를 Git/CI/생성 도구에 보내지 않았다. 추가 필수 수정 사항 없음.

## Residual Risk

Windows 자동 실행은 공개 교정 그림과 가짜 입력을 사용한다. 개인 PNG는 로컬 정적 합성과 해시로 검토했으며 Windows 개인 그림 실행으로 주장하지 않는다. 실제 마이크/권한/USB·사용자 GPU/OBS/혼합 DPI·새60분 실기는 별도다. 모든 환경의 결함0을 보장하지 않는다. 전달 EXE는 unsigned이며 일회용 runner 서명 QA는 사용자 신뢰 저장소를 바꾸지 않는다. OBS는 WGC를 사용한다.

기본 두 체형에 맞춘 분리이며 임의 PNG의 자동 해부를 제공하지 않는다. 측면 펠리의 안 보이는 뒤팔은 투명 부위로 남긴다. 무기 선택/상시 들기는 기본 늘보군/펠리 경로에 제공하며 일반 외부 native 패키지의 별도 장비 UI를 추가하지 않는다. 의상 선택은 현재 실행에서 캐릭터별로 기억하고 재실행하면 해제한다. 무기 선택/상시 옵션과 마지막 캐릭터는 저장한다. 편집 내보내기에는 현재 개인 그림이 포함된다.

## Follow-Ups

기존 앱을 종료하고 새 폴더에 ZIP 전체를 풀어 START.cmd를 실행한다. 의상 위쪽 헤어스타일과 동작의 장착 무기/항상 들기를 확인한다. TEST-SMOKE.cmd는 합성 키/클릭을 사용하므로 전용 Windows/VM에서 실행한다. [Studio 안내](../guides/studio-samples.md), [Windows 체크리스트](../quality/windows-checklist.md), [완료 계획](../exec_plans/completed/plan-046-part-wardrobe.md)을 따른다. 표준 main ff push/branch·worktree 정리를 수행하며 Git 실패 시 중단·상태 보고 규칙을 따른다.

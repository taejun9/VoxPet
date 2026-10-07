# plan-011-expression-motion Review

## Summary

사용자 요구사항의 12개 표정 저장 슬롯, 단축키 전환, 평상시 blink와 표정 눈물 모션을 검토했다.
QA 이후 동일 에이전트의 자체 리뷰이며 별도 worker를 실행하지 않았다. 기준 구현은 c18a8c2다.

## QA

- macOS arm64, SDK10.0.401: locked restore, format/analyzer, Release build(경고0/오류0), Core97/97, verify_base.py, verify_app.py(36 PNG), diff --check 통과.
- [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37576998278): qa.ps1 -Publish -Smoke 전체 통과. Core97/97, 자체 포함 EXE, 실제 WPF 바인딩/슬롯/전역 키/무음 모션/창 수명/작은 화면 회귀.
- 이전 [Windows 기능 QA](https://github.com/taejun9/VoxPet/actions/runs/37576732932)도 통과했으며 눈물 표시/배치 렌더를 직접 확인했다.
- [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/37576998263): WGC green+Chroma Key/native alpha/7프레임 변화 통과. BitBlt 유효 프레임 없음은 기존 환경 제한이다.
- Windows 물리 키보드 입력, 실제 마이크와 사용자 GPU/OBS, 물리 DPI 변경, 이번 변경의60분 실기는 수행하지 않았다. native 전역 키 검사는 합성 키 입력이며 UI60Hz는 측정값이 아니다.

## Findings

- 해결: 저장 실패 fixture가 macOS IOException만 예상하여 첫 Windows QA에서96/97로 중단했다. Windows UnauthorizedAccessException도 정확히 허용하고 임시 파일/새 PNG 정리 및 다른 슬롯 보존을 계속 검증했다. production은 처음부터 두 오류를 처리한다.
- 해결: 비동기 초기 슬롯 로드 후 이름/표정/blink/tears/좌표 바인딩을 모두 통지한다.
- 해결: 허용40자 이름이 작은 창의 슬롯 버튼을 넘치지 않도록 최대폭/말줄임/전체 이름 tooltip을 제공하고 실제 배치 회귀에 긴 이름/TextBox를 포함했다.
- 요구사항: F1~F11은 전역, F12는 Microsoft의 디버거 예약 규정에 따라 설정창/방송창 내부 fallback으로 제공한다. 등록 충돌은 안내하고 버튼으로 전환 가능하다.
- 상태/수명: 최신 요청 revision과 직렬 IO로 오래된 결과를 버린다. 저장 중 도착한 새로운 전환이 저장 완료로 덮이지 않는다. 진행 중 전환을512px snapshot으로 고정해 참조가 누적되지 않는다. UI의 파일 IO 및 오디오 callback 변경이 없다.
- 데이터/개인정보: 원본 시트는 검증 후 앱 소유 PNG로 인코딩·저장한다. 외부 경로/음성/장치 ID/키 입력 로그를 저장하지 않는다. GUID와 크기/좌표/표정 enum을 검증하고 슬롯 JSON을 마지막에 원자적으로 교체한다. 사용자 원본을 수정·삭제하지 않는다.
- 회귀: 기존 일반 PNG 불러오기/기본 복원, 반응 프리셋/음소거/Stop, 별도 방송창과 OBS 캡처를 보존했다.

## Residual Risk

사용자 PC에서 다른 앱이 Ctrl+Shift+F 키를 점유할 수 있다. 충돌 키는 앱 내부 또는 슬롯 버튼으로 사용하고 키 점유를 해제한 뒤 앱을 다시 시작한다.
사용자 PNG마다 눈 위치가 다르므로 슬롯의 눈물 좌표를 조정해야 한다. 3×2 시트의 눈/입 그림은 사용자가 준비한다.
폴더 권한·저장 공간 문제나 외부 수정으로 저장이 실패할 수 있으며 이전 저장값/표시를 유지하고 복구 안내를 제공한다.
실제 마이크·사용자 OBS/GPU·물리 DPI/키보드·장시간 조건은 기존 Windows 체크리스트의 미실행 항목으로 남는다.

## Follow-Ups

사용자 Windows PC에서 실제 키보드로 F1~F11/F12 내부 전환과 OBS 방송 중 표정/눈물 표시를 확인한다.
실기 결과는 docs/quality/windows-checklist.md에 환경과 함께 기록한다. 키 편집/추가 모션은 별도 요청 시 계획한다.

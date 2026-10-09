# plan-016-expression-order Review

## Summary

방송창 우클릭 메뉴 검정 글씨, 저장되는 표정 순서 변경과F1~F12 대응, 개인 늘보군12실제 얼굴 및 개별/전체 테스트를 QA 이후 동일 에이전트가 자체 리뷰했다. 서브에이전트를 실행하지 않았다. 실행 코드6caff4b. 개인 이미지와 실행 ZIP은 root artifacts에만 보관하며 Git/공개 CI에 포함하지 않는다.

## QA

- macOS SDK10.0.401: locked restore/format/analyzer/Release 경고·오류0/Core140통과. verify_base.py/verify_app.py/diff --check 통과.
- [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37889406934): Core140/140·publish·WPF690고유검사·실패0. 메뉴 팝업 실제 검정 글씨/PNG,12종×16frozen상태와 매핑, 순서·재시작·경계·초안/슬롯 파일 유지·메모리 PNG 저장ID, 개별/전체·취소·키·종료·복원·숫자 데모 소유권.4창크기 모든 탭/셀렉트/스크롤 없음 통과. 메뉴와 편집/테스트 탭 PNG를 직접 확인했다.
- [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/37889406892): OBS32.2.2/WGC green+Chroma Key/native alpha 및 각각7프레임 변화 통과. 기존 BitBlt 유효 프레임 없음 유지.
- 개인 자산:352px셀/2816×704/8비트RGBA/16MB 제한/12고유 얼굴·192고유프레임/투명 여백/각8입 평균 면적순서·해시 확인. 기존6PNG byte-identical 및 원본SHA256 보존. 새 개인 ZIP24파일·CRC/시트 해시 확인.

## Findings

- 해결: 최초 Windows 실행의 기존 F12 assertion이 오래된 이름 "표정12"를 가정했다. 실제 저장 슬롯 이름으로 확인하도록 수정하고 최종 재검증했다. F12 앱 내부 전환 계약은 유지한다.
- 순서/저장: 유효한0~11 순열을 별도 order.json에 임시파일→원자적 교체로 저장한 뒤 UI에 반영한다. 실패는 기존 순서 유지, 손상은 identity 복구. Slots.Index는 저장ID, Position은 표시/F키이며 편집 초안과 이미지 파일을 이동/덮어쓰지 않는다. 메모리 PNG 재배열도 저장ID로 참조한다.
- 테스트 수명: 세마포어·취소 토큰과 revision으로 오래된 복원을 차단한다. 종료/중지 후 원래 profile·sprites·name을 복원하고 최신 키/개별 선택을 우선한다. finally에서 gate/busy/테스트 명령 상태를 정리한다. 전체 테스트는 입력 중지 상태일 때 숫자 데모만 소유하며 기존 입력은 유지한다.
- 자산: 기존6종은 그대로 복사하고 부끄러움/뿌듯함/갸우뚱/신남/애정/장난을 내장 imagegen 참조 편집으로 추가했다. 얼굴과 몸을 직접 확인한 뒤 독립 캐릭터 컴포넌트를 추출·패딩·8입 순서로 정렬했다. 코드로 얼굴 픽셀을 그리지 않았다. 모든 최종PNG/prompts/manifest는 로컬에 보존했다.
- 오디오/개인정보: 자동 마이크 시작/녹음/음성 전송/감정 인식을 추가하지 않았다. 공개 고양이는 기존6그림을 재사용하며 개인 늘보군 PNG는 별도 실행 패키지만 제공한다.
- 문서: 사용자/단계별/배포 안내에 순서 즉시 저장/새F키 의미/order.json 복구/테스트·취소/기존PNG 우선/12종 경로를 반영했다. 미해결 기능 결함은 발견하지 않았다.

## Residual Risk

개인PNG는 CI에 보내지 않았으며 실제 새 개인 시작 화면은 수집하지 않았다. 로컬 계약/시각 검사와 합성 Windows QA를 구분한다. 실제 마이크/권한·물리 DPI/키보드·사용자 GPU/OBS는 기존 실기 범위다. 생성 프레임 사이에 미세한 픽셀 차이가 있을 수 있다. 기존 저장한 PNG는 자동 대체하지 않으므로 새12종 확인은 테스트 탭, 저장 슬롯 교체는 내장 표정 사용→저장으로 수행한다.

## Follow-Ups

개인 ZIP을 Windows의 새 폴더에 풀고 표정→테스트의12개 버튼/전체 테스트, 표정→편집의 위아래 순서와 단축키를 확인한다. 실제 마이크와 개인 OBS 결과는 Windows 체크리스트에 기록한다.

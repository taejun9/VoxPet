# 늘보군·펠리 의상 그림 제작

2026-10-17 두 캐릭터의 기존 로컬 PNG 위에 직접 착용하는 그림으로 개선했다. 개인 캐릭터 원본을 생성 도구나 공개 저장소에 전달하지 않았다. 의상만 새로 제작하고 로컬에서 체형 배치와 겹침을 확인했다.

## 원본과 프롬프트

내장 **imagegen** 도구를 사용했다. [생성 기록](../../harness/assets/companion-art/generation.json)에 각 파일·SHA256·프롬프트를 보존한다. 같은 폴더의 원본 PNG는 투명 alpha를 가진 생성 결과다. 선택한 날개는 cape-v2.png, 장발은 hair-long-v2.png이며 앞선 시안도 보존했다. 모자는8프레임, 날개는6프레임 시트다.

니트·카디건·멜빵·반바지·운동화·모자·날개·세 헤어·물총/장난감 칼/마법봉의 선과 질감을 수채화/과슈로 통일했다. 작은 안경·볼터치·귀걸이·리본·밴드·빛/하트/할퀴기/활은 편집 가능한 곡선과 음영으로 제작했다. 날개 시트의 실제 여백 경계와 캐릭터별 배치 숫자는 [그림 조립 코드](../../harness/scripts/generate_companion_art.cjs)에 둔다.

## 재생성

개발 환경에 Node와 sharp0.35.5가 필요하다. sharp를 해당 환경의 모듈 경로에 제공한 뒤 저장소 루트에서:

```sh
node harness/scripts/generate_companion_art.cjs /tmp/voxpet-companion-art
python3 harness/scripts/package_companion_samples.py /tmp/voxpet-companion-art
```

첫 명령은 원본 alpha를 보존해 프레임을 분리하고512px 논리 좌표/160px 썸네일에 배치한다. 두 번째 명령은 표준 라이브러리로 두 공개 의상 패키지를 조립한다. 개인 PNG는 읽지 않는다. 결과는 Assets/Samples의 companion-neulbo.voxpet와 companion-pelly.voxpet다. 실행 앱과 배포 ZIP은 Node/Python/sharp 없이 이 결과 파일을 읽는다.

## 합성 검토

늘보군은 둥근 정면 머리·넓은 몸통, 펠리는 측면 얼굴·부리와 앞 날개를 고려한다. 장발의 얼굴 빈 공간을 넓히고 펠리의 얼굴/부리 앞 헤어와 뒤쪽 머리카락을 곡선 안전 영역으로 나눠 눈과 입을 가리지 않게 한다. 수평으로 잘린 앞머리를 사용하지 않고 펠리의 날개는 의상 앞에 다시 합성한다. 원본 시트·표정·입 프레임은 수정하지 않는다. 실제 개인 그림 합성 증거는 ignored 로컬 artifacts에만 둔다.

Windows 공개 QA는 두 체형의 교정 도형을 만들어17의상·9동작·헤어/모자/날개·입/눈·방송 공유·편집/내보내기를 시험한다. 공개 합성 QA와 개인 그림의 로컬 시각 검토를 구분한다. 기존 standalone 샘플은 과거 native/편집기 회귀 fixture이며 새 일반 화면의 캐릭터 목록에는 추가하지 않는다.

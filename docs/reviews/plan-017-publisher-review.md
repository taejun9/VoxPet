# plan-017-publisher Review

## Summary

QA 이후 동일 에이전트 자체 리뷰. Company/Authors 김태중 및 실제 EXE 속성/서명 상태 검사, 인증서 없는 개인PC의 설정 방법을 제공했다. 사용자가 인증서 없음/개인PC용 설정 안내를 선택했다. 서브에이전트를 사용하지 않았다.

## QA

- SDK10.0.401 macOS locked restore/format/Release0경고0오류, 문서/자산/diff 검사 통과.
- [Windows QA](https://github.com/taejun9/VoxPet/actions/runs/37932695017): Core140/WPF690/실패0, EXE CompanyName=김태중·NotSigned·publisherVerified=false 확인.
- [OBS QA](https://github.com/taejun9/VoxPet/actions/runs/37932694997): 합성 WGC/green/Chroma Key/native alpha 회귀 통과.
- 개인 ZIP26파일/CRC 통과, 기존12PNG 바이트/해시 동일. 이전 ZIP·원본 보존.

## Findings

메타데이터와 인증서 신뢰를 구분하고 미서명 상태를 기록했다. 사용자 PC의 신뢰 저장소/보안 정책을 자동 변경하거나 키를 생성/전송하지 않았다. 설정 안내는 사용자 Windows에서 개인 키 생성·서명·이름/지문 확인 후 직접 신뢰 등록하는 방법이다. 실행 경고 수정 완료로 오인하는 표현을 사용하지 않았다.

## Residual Risk

실제 EXE는 미서명이다. 대상 Windows PC에서 안내에 따라 서명과 인증서 신뢰 설정이 필요하다. 자체 서명은 개인 시험용이며 공용 신원 검증이나 SmartScreen 평판을 대신하지 않는다. 사용자 보고의 plan016 실행 종료/plan015 OBS 표시 문제는 별도 plan018에서 실제 시작 경로로 재검증한다.

## Follow-Ups

개인PC 서명 안내를 따르고 실제 실행 경고를 확인한다. 시작 오류/OBS/트레이 요청은 plan018에서 처리한다.

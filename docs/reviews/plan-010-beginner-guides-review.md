# plan-010-beginner-guides Review

## Summary

설치·사용·설정 안내 3개와 README/전체 설명서의 연결, 공식 출처 기록을 검토했다. 문서 QA 완료 후 단일 에이전트가 수행한 자체 리뷰이며 독립 리뷰는 아니다.

## QA

- python3 harness/scripts/verify_base.py: 새 문서의 구조·로컬 링크·계획·출처 검사 통과. 완료/리뷰 추가 후 재검증한다.
- python3 harness/scripts/verify_app.py: 기존 자산·잠금·개인정보 계약 통과.
- git diff --check: 통과. 신규 문서는 stage 후 다시 검사한다.
- 별도 읽기 전용 검사: 초보자 안내의 local heading anchor, 실제 WPF 버튼 라벨, 명령어 실행 단계 없음, Markdown 변경 범위 확인.
- 다운로드: 공개 taejun9/VoxPet의 Release 없음 확인. 성공한 main Windows37414425676의 VoxPet-win-x64 artifact가 존재하고 expired=false임을 API로 확인했다. 설치 안내는 로그인 및 최신 main 성공 항목 탐색/만료 대응을 포함한다.
- Microsoft의 마이크 권한·입력/볼륨·압축 풀기·컴퓨터 종류와 GitHub artifact 다운로드 공식 페이지를 2026-10-06 확인해 해당 단계에 링크하고 출처 레지스트리에 기록했다.
- 문서만 변경하여 새 .NET 빌드·Core 테스트·Windows 마이크/OBS 실기 실행은 수행하지 않았다. 기존 통과 기록은 그대로 유지한다.

## Findings

- 요구사항: 별도 Markdown 3개, 설치 → 사용 → 설정 순서, 프로그래밍 지식 없이 다운로드/압축/실행/조정/종료할 수 있는 조작 설명 충족.
- 해결: 선택 기능 heading의 dash로 생길 수 있는 GitHub anchor 차이를 괄호 표기로 정리하고 링크를 검사했다.
- 정확성: 실행 ZIP 이름과 source ZIP 차이, 숨겨진 .exe 표시, 프리셋/슬라이더 예시, Stop과 캐릭터 음소거의 차이, 방송창만 닫으면 분석 계속, 개인 PNG의 여섯 상태 규격과 재실행 시 재선택, 저장 범위를 구현과 대조했다.
- 개인정보: Windows 데스크톱 마이크 권한을 존중하고 음성 저장/전송이 없음을 설명했다. 문제 문의에 음성/개인정보가 필요하지 않다고 안내했다. OBS 마이크 오디오는 별도 제어임을 명시했다.
- 범위: 기존 문서를 최소한의 링크 추가로 보존했다. 앱/자산/설치 방식 변경이나 새 Release 발행은 하지 않았다. 중요 미해결 발견 없음.

## Residual Risk

현재 다운로드는 GitHub 로그인 및 기간이 남은 Actions 실행 파일에 의존한다. 파일이 만료되면 최신 성공 항목 또는 제공자의 실행 ZIP이 필요함을 안내했다. 실제 마이크·사용자 GPU/OBS·물리 DPI·실제 입력 장시간의 기존 미실행 항목은 그대로이며 이 문서가 실사용 검증을 대체하지 않는다.

## Follow-Ups

정식 실행 파일 배포 경로가 생기면 installation.md의 다운로드 안내를 갱신한다. UI/설정 변경 시 3문서와 전체 사용 설명서를 함께 대조한다. 완료 문서 검증 후 저장소 절차대로 main 병합/push 및 로컬 branch/worktree 정리를 수행한다.

# Team Vox — VoxPet 에이전트 지도

VoxPet은 마이크 음량으로 PNG 캐릭터가 반응하는 Windows 데스크톱 앱이다.
현재 저장소는 설계와 개발 기반 단계이며 실행 가능한 앱은 아직 없다.
상세 규칙은 docs/에 둔다. 사용자 보고는 한국어로 `<별칭>: <내용>` 형식을 쓴다.

## 역할

| id | 별칭 | 책임 |
|---|---|---|
| project_lead | 조율 | 범위 결정, 진행 조정, 사용자 보고 |
| plan_keeper | 설계 | 실행 계획, 결정 기록, 완료 이동 |
| repo_cartographer | 지도 | 저장소 구조와 문서 탐색 |
| harness_builder | 제작 | 반복 가능한 검증 스크립트 |
| quality_runner | 검증 | QA 실행, 실패와 제한 기록 |
| review_judge | 심사 | QA 이후 요구사항·회귀·위험 리뷰 |
| privacy_guard | 수호 | 마이크 권한, 로컬 음성 처리, 데이터 경계 |
| doc_gardener | 정리 | 문서 동기화, 리뷰 미러, 오래된 기록 관리 |

역할은 책임 구분이며 상주 에이전트가 아니다. 사용자의 명시 요청 없이는 서브에이전트를 실행하지 않는다.

## 시작 문서

- [제품과 MVP](docs/product/product.md)
- [앱 구조·오디오·스레드 설계](docs/architecture/application.md)
- [구현 로드맵](docs/product/roadmap.md)
- [개발 환경과 명령](docs/quality/development.md)
- [하네스와 Git 절차](docs/architecture/harness.md)
- [QA와 리뷰 규칙](docs/quality/rules.md)
- [마이크·음성 개인정보 원칙](docs/privacy/principles.md)
- [공식 자료](docs/references/official-sources.md)
- [회의 기록](docs/meetings/index.md)
- 실행 계획: docs/exec_plans/active/, 완료: docs/exec_plans/completed/, 리뷰: docs/reviews/

## 작업 규칙

1. **No Exec Plan, No Work.** 먼저 `docs/exec_plans/active/plan-NNN-<task>.md` 작성.
2. 범위나 접근법 변경은 계획의 Decision Log에 기록. `docs/plan` 금지.
3. main에서 구현·기능 커밋 금지. `codex/plan-NNN-<task>`와 `.worktree/plan-NNN-<task>` 사용.
4. QA → 리뷰 → 계획 완료 이동 → 리뷰 미러 → main 병합 → main push → `git branch -d` → worktree 제거.
5. 실패한 Git 단계는 중단하고 정확한 원인 보고. 강제 push/삭제로 우회하지 않는다.
6. 검증: `python3 harness/scripts/verify_base.py`, `git diff --check`. 앱 명령은 개발 환경 문서의 상태를 확인한다.
7. 오디오 엔진은 UI/렌더러와 분리. VoiceLevel은 유한한 0~1. UI 스레드를 막지 않는다.
8. 음성 저장·원격 전송·웹캠·추적 기능은 MVP에 넣지 않는다.
9. root Markdown은 README.md와 AGENTS.md만. 기존 파일은 허가 없이 덮어쓰지 않는다.

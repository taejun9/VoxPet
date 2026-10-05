# 개발 하네스와 Git 수명

AGENTS.md는 짧은 지도, docs/는 제품·설계·품질의 기준, harness/는 반복 검증과 템플릿이다.
계획은 active/에서 시작해 완료 후 completed/로 이동하며 reviews/에 완료 리뷰를 남긴다.

## 기본 절차

1. `git status --short --branch`로 사용자 작업을 확인하고 `git fetch origin`으로 원격 상태 확인.
2. main이 깨끗하며 origin/main과 동기화되어야 한다. 차이나 사용자 작업은 강제로 초기화하지 않는다.
3. 다음 plan 번호를 확인한 뒤 `git worktree add -b codex/plan-NNN-<task> .worktree/plan-NNN-<task> main`.
4. worktree로 이동하고 active 계획 작성. 구현 전에 목표/제약/QA를 기록한다.
5. 범위 변경을 Decision Log에 반영하고 구현한다.
6. QA 실행 후 별도 리뷰. 완료 상태를 기록하고 계획 이동, 리뷰 미러 생성.
7. 완료 기록 변경을 포함해 문서 검증과 diff 확인 후 작업 브랜치에서 커밋.
8. main checkout에서 fetch와 작업 트리 상태를 다시 확인한다. 원격이 진전되면 fast-forward로 동기화하고 통합 영향을 worktree에서 재검증.
9. `git merge --ff-only codex/plan-NNN-<task>` 가능한 상태로 통합. 충돌/분기 상태는 작업 브랜치에서 해결하고 필요한 QA 재실행.
10. `git push origin main` 성공 확인.
11. 완료 브랜치가 worktree에서 사용 중이므로 worktree를 같은 커밋의 detached HEAD로 바꾼다.
12. `git branch -d codex/plan-NNN-<task>` 후 `git worktree remove .worktree/plan-NNN-<task>`.

main에는 검증된 task 결과를 병합하며 직접 기능 구현 커밋을 만들지 않는다.
merge/push/삭제/제거 실패 시 완료했다고 보고하지 않고 정확한 단계와 복구 상태를 남긴다.
force push, `branch -D`, `worktree remove --force`, 사용자 변경 삭제로 우회하지 않는다.
`.worktree/`는 Git에서 무시한다. CLI worktree는 이 저장소의 엄격한 경로 규칙을 따른다.

## 사용 가능한 템플릿

- [실행 계획](../../harness/templates/exec-plan.md)
- [회의](../../harness/templates/meeting.md)
- [리뷰](../../harness/templates/review.md)

검증 명령과 구현 후 예정 명령은 [개발 환경](../quality/development.md), 합격 기준은 [품질 규칙](../quality/rules.md)에 둔다.
README는 사용자 기존 내용을 보존한다. 다음 명시적인 문서 수정 요청 때 앱의 실제 명령과 안내를 동기화한다.

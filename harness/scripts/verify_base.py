#!/usr/bin/env python3
"""VoxPet의 문서 계약을 검사한다. 앱 QA나 리뷰 수행 여부를 증명하지 않는다."""

from __future__ import annotations

import argparse
from datetime import date
from pathlib import Path
import re
from urllib.parse import unquote, urlparse


# 문서 계약은 명시적 목록으로 고정한다. 실행 계획/리뷰 내용의 진실성은 자동 판정하지 않는다.
REQUIRED = (
    "AGENTS.md", "README.md",
    "docs/architecture/harness.md", "docs/architecture/application.md",
    "docs/product/product.md", "docs/product/roadmap.md",
    "docs/quality/rules.md", "docs/quality/development.md",
    "docs/privacy/principles.md", "docs/references/official-sources.md",
    "docs/meetings/index.md", "harness/scripts/verify_base.py",
    "harness/templates/exec-plan.md", "harness/templates/meeting.md",
    "harness/templates/review.md",
)
ROLES = (
    "project_lead", "plan_keeper", "repo_cartographer", "harness_builder",
    "quality_runner", "review_judge", "privacy_guard", "doc_gardener",
)
PLAN_NAME = re.compile(r"plan-(\d{3})-[a-z0-9]+(?:-[a-z0-9]+)*")
PLAN_SECTIONS = (
    "Status", "Owner", "User Request", "Goal", "Non-Goals", "Context Map",
    "Constraints", "Implementation Plan", "QA Plan", "Review Plan",
    "Decision Log", "Progress Log", "Completion Notes",
)
REVIEW_SECTIONS = ("Summary", "QA", "Findings", "Residual Risk", "Follow-Ups")


# 파일 존재·링크·역할 지도·계획 수명·출처 형식을 검사하고 모든 실패를 한 번에 모은다.
def verify(root: Path) -> tuple[list[str], int]:
    root = root.resolve()
    errors: list[str] = []
    for name in REQUIRED:
        if not (root / name).is_file():
            errors.append(f"필수 파일 없음: {name}")
    for name in ("docs/exec_plans/active", "docs/exec_plans/completed", "docs/reviews"):
        if not (root / name).is_dir():
            errors.append(f"필수 디렉터리 없음: {name}")
    if (root / "docs/plan").exists():
        errors.append("금지된 경로: docs/plan")
    for path in root.iterdir():
        if path.is_file() and path.suffix.lower() == ".md" and path.name not in {"README.md", "AGENTS.md"}:
            errors.append(f"root Markdown 금지: {path.name}")

    paths = [root / name for name in ("AGENTS.md", "README.md") if (root / name).is_file()]
    for directory in ("docs", "harness/templates"):
        paths.extend(sorted((root / directory).rglob("*.md")))
    texts: dict[Path, str] = {}
    for path in paths:
        rel = path.relative_to(root).as_posix()
        try:
            content = path.read_text(encoding="utf-8")
        except (OSError, UnicodeError) as exc:
            errors.append(f"읽기 실패: {rel}: {exc}")
            continue
        texts[path] = content
        if not content.strip():
            errors.append(f"빈 문서: {rel}")
        if not rel.startswith("harness/templates/") and re.search(r"\bTODO\b", content):
            errors.append(f"템플릿 외 TODO: {rel}")
        # 현재 문서가 사용하는 inline Markdown 링크의 파일 존재만 검사한다.
        prose = re.sub(r"```.*?```", "", content, flags=re.S)
        for target in re.findall(r"\]\(([^)]+)\)", prose):
            target = target.strip().strip("<>")
            parsed = urlparse(target)
            if parsed.scheme or target.startswith("#"):
                continue
            destination = (path.parent / unquote(parsed.path)).resolve()
            if not destination.is_relative_to(root):
                errors.append(f"저장소 외 로컬 링크: {rel}: {target}")
            elif not destination.exists():
                errors.append(f"깨진 로컬 링크: {rel}: {target}")

    agent_map = texts.get(root / "AGENTS.md", "")
    nicknames = []
    for role in ROLES:
        matches = re.findall(rf"^\| {role} \| ([^|]+) \|", agent_map, re.M)
        if len(matches) != 1:
            errors.append(f"에이전트 역할 행은 하나여야 함: {role}")
        else:
            nicknames.append(matches[0].strip())
    if len(set(nicknames)) != len(nicknames):
        errors.append("에이전트 별칭 중복")

    ids: set[str] = set()
    completed: set[str] = set()
    plan_count = 0
    # 계획 번호는 상태 폴더를 넘어 유일해야 한다. 완료 계획에는 완료 리뷰 미러가 있어야 한다.
    for state in ("active", "completed"):
        for path in sorted((root / f"docs/exec_plans/{state}").glob("*.md")):
            plan_count += 1
            rel = path.relative_to(root).as_posix()
            content = texts.get(path, "")
            match = PLAN_NAME.fullmatch(path.stem)
            if not match:
                errors.append(f"계획 파일명 위반: {rel}")
            elif match[1] in ids:
                errors.append(f"계획 번호 중복: {match[1]}")
            else:
                ids.add(match[1])
            if content.splitlines()[:1] != [f"# {path.stem}"]:
                errors.append(f"계획 제목 불일치: {rel}")
            for section in PLAN_SECTIONS:
                if f"## {section}\n" not in content:
                    errors.append(f"계획 섹션 없음: {rel}: {section}")
            status = re.search(r"^## Status\s*\n\s*(\w+)", content, re.M)
            if not status or status[1] != state:
                errors.append(f"계획 상태/경로 불일치: {rel}")
            if state == "completed":
                completed.add(path.stem)
                if "- [ ]" in content:
                    errors.append(f"완료 계획의 미완료 체크리스트: {rel}")
                if not (root / f"docs/reviews/{path.stem}-review.md").is_file():
                    errors.append(f"완료 리뷰 미러 없음: {path.stem}")
    if not plan_count:
        errors.append("실행 계획 없음")
    for path in sorted((root / "docs/reviews").glob("*.md")):
        content = texts.get(path, "")
        stem = path.stem.removesuffix("-review")
        if stem not in completed or not path.stem.endswith("-review"):
            errors.append(f"완료 계획과 연결되지 않은 리뷰: {path.name}")
        if content.splitlines()[:1] != [f"# {stem} Review"]:
            errors.append(f"리뷰 제목 불일치: {path.name}")
        for section in REVIEW_SECTIONS:
            if f"## {section}\n" not in content:
                errors.append(f"리뷰 섹션 없음: {path.name}: {section}")

    # 외부 URL에 접속하지 않고 HTTPS 및 확인일 형식만 검사한다. 근거 최신성은 리뷰 책임이다.
    sources = texts.get(root / "docs/references/official-sources.md", "")
    rows = [line for line in sources.splitlines() if line.startswith("| ")]
    if len(rows) < 2:
        errors.append("공식 자료 레지스트리가 비어 있음")
    for row in rows[1:]:
        fields = [field.strip() for field in row.strip("|").split("|")]
        if len(fields) != 5 or not all(fields):
            errors.append(f"공식 자료 필드 오류: {row}")
            continue
        url = urlparse(fields[1])
        if url.scheme != "https" or not url.netloc:
            errors.append(f"공식 자료 URL 오류: {fields[0]}")
        try:
            date.fromisoformat(fields[3])
        except ValueError:
            errors.append(f"공식 자료 확인일 오류: {fields[0]}")
    return errors, len(paths)


# --root로 별도 checkout도 검사할 수 있다. 종료 코드 0/1을 QA 스크립트에 전달한다.
def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    args = parser.parse_args()
    root = args.root.resolve()
    if not root.is_dir():
        parser.error(f"디렉터리 없음: {root}")
    errors, count = verify(root)
    if errors:
        for error in errors:
            print(f"FAIL: {error}")
        return 1
    print(f"PASS: {count}개 Markdown 문서의 구조·링크·계획·리뷰·출처 검사")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

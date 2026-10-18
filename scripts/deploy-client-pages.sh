#!/usr/bin/env bash
# 로컬 WebGL 빌드 결과를 gh-pages 브랜치에 올려 GitHub Pages로 배포합니다. 전체 절차: docs/DEPLOYMENT.md
#
# 사용법: scripts/deploy-client-pages.sh [빌드 폴더]
#   빌드 폴더 기본값: Client/Builds/WebGL (Unity 메뉴 MutatIO > Build > WebGL (GitHub Pages) 출력)
#
# gh-pages는 매번 빌드 결과만 담은 커밋 하나로 강제 푸시합니다. 빌드 바이너리가 이력에 쌓여
# 저장소가 커지는 것을 막기 위함이며, 이전 배포는 남지 않습니다.
set -euo pipefail

ROOT="$(git rev-parse --show-toplevel)"
BUILD_DIR="${1:-$ROOT/Client/Builds/WebGL}"
REMOTE="${DEPLOY_REMOTE:-origin}"
BRANCH="gh-pages"
TEMP_BRANCH="gh-pages-deploy-$$"

if [[ ! -f "$BUILD_DIR/index.html" ]]; then
  echo "빌드 결과가 없습니다: $BUILD_DIR/index.html" >&2
  echo "Unity 메뉴 MutatIO > Build > WebGL (GitHub Pages)로 먼저 빌드하십시오." >&2
  exit 1
fi

SOURCE_REV="$(git -C "$ROOT" rev-parse --short HEAD)"
if [[ -n "$(git -C "$ROOT" status --porcelain -- Client Shared)" ]]; then
  SOURCE_REV="$SOURCE_REV (커밋되지 않은 변경 포함)"
fi

WORK="$(mktemp -d)"
cleanup() {
  git -C "$ROOT" worktree remove --force "$WORK" >/dev/null 2>&1 || rm -rf "$WORK"
  git -C "$ROOT" branch -D "$TEMP_BRANCH" >/dev/null 2>&1 || true
}
trap cleanup EXIT

git -C "$ROOT" worktree add --quiet --orphan -b "$TEMP_BRANCH" "$WORK"
cp -R "$BUILD_DIR"/. "$WORK"/
# Jekyll 처리를 건너뛰고 파일을 그대로 서비스합니다.
touch "$WORK/.nojekyll"

git -C "$WORK" add -A
git -C "$WORK" commit --quiet -m "배포: 클라이언트 WebGL 빌드 (main $SOURCE_REV)"
git -C "$WORK" push --force "$REMOTE" "HEAD:refs/heads/$BRANCH"

echo "배포 완료: $REMOTE/$BRANCH ← $SOURCE_REV"
echo "GitHub Pages 반영까지 1~2분 걸릴 수 있습니다."

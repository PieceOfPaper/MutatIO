# 배포 가이드

MutatIO 클라이언트(Unity WebGL)를 GitHub Pages에 배포하는 절차입니다. 결정 배경은 [결정 기록](DECISIONS.md)의 2026-10-18 항목을 참고하십시오.

- 배포 주소: https://pieceofpaper.github.io/MutatIO/
- 배포 대상: `Client/Assets/MutatIO/Scenes/Prototype.unity` 한 씬. 서버 없이 로컬 시뮬레이션으로 플레이합니다.
- 배포 시점: 필요할 때 사람이 직접 실행합니다. 커밋·푸시로 자동 배포되지 않습니다.
- 서버 배포: 아직 정하지 않았습니다. 정해지면 이 문서에 추가합니다.

## 구조

```text
로컬 Unity 빌드 ──► Client/Builds/WebGL (git 제외)
                          │
                          ▼
     scripts/deploy-client-pages.sh
                          │  빌드 결과 + .nojekyll을 단일 커밋으로 강제 푸시
                          ▼
              origin/gh-pages 브랜치 ──► GitHub Pages (브랜치 배포, /(root))
```

| 구성 요소 | 위치 | 역할 |
| --- | --- | --- |
| 빌드 스크립트 | `Client/Assets/MutatIO/Editor/WebGLPagesBuild.cs` | `Prototype` 씬을 WebGL로 빌드. 빌드하는 동안만 Gzip + Decompression Fallback 적용 |
| 배포 스크립트 | `scripts/deploy-client-pages.sh` | 빌드 결과를 `gh-pages`에 푸시 |
| Pages 설정 | 저장소 Settings → Pages | Source: Deploy from a branch, `gh-pages` / `/(root)` |

GitHub Pages는 `Content-Encoding` 헤더를 지정할 수 없습니다. 그래서 Brotli 빌드는 그대로 서비스하면 로드되지 않으므로 Gzip으로 압축하고 브라우저 JavaScript가 직접 압축을 풀게(Decompression Fallback) 합니다. 프로젝트의 원래 압축 설정(Brotli)은 빌드가 끝나면 복원됩니다.

## 1. 빌드

다음 중 하나로 빌드합니다. 결과는 `Client/Builds/WebGL`에 생기며 커밋하지 않습니다.

빌드 대상이 WebGL이 아니면 Unity가 활성 플랫폼을 WebGL로 전환합니다. 처음 전환할 때는 재임포트로 시간이 더 걸립니다.

### A. Unity 에디터 메뉴

`MutatIO > Build > WebGL (GitHub Pages)`를 실행합니다. 성공하면 Finder에서 빌드 폴더가 열립니다.

### B. batchmode (에디터를 닫은 상태)

같은 프로젝트를 연 에디터가 있으면 프로젝트 잠금 때문에 실패합니다.

```bash
/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app/Contents/MacOS/Unity -batchmode \
  -projectPath "$(pwd)/Client" -executeMethod MutatIO.Editor.WebGLPagesBuild.BuildForPages -logFile -
```

빌드가 실패하면 종료 코드 1로 끝납니다. 출력 경로는 `-customBuildPath <경로>`로 바꿀 수 있습니다.

### C. Unity CLI (열린 에디터, 코딩 에이전트용)

```bash
unity command --project-path "$(pwd)/Client" --timeout 590 eval \
  --code 'MutatIO.Editor.WebGLPagesBuild.BuildForPages();'
```

- 이 명령은 `Main thread operation timed out after 5000ms` 오류를 내며 바로 끝나지만 빌드는 에디터에서 계속 진행됩니다. 빌드하는 동안 에디터와 CLI는 응답하지 않습니다.
- 콘솔에 `[WebGLPagesBuild] Succeeded: ...` 줄이 찍히면 끝난 것입니다.

```bash
unity command --project-path "$(pwd)/Client" console --tail 50 | grep -o '\[WebGLPagesBuild\][^"]*'
```

- 콘솔에 남는 `Failed to handle /api/exec request` 오류는 위 시간 초과 때문이며 빌드 실패가 아닙니다.

### 빌드 확인

```bash
ls Client/Builds/WebGL                                    # index.html, Build/, TemplateData/
xxd -l 2 Client/Builds/WebGL/Build/WebGL.wasm.unityweb    # 1f8b 이면 Gzip
git status --short Client/ProjectSettings                 # 비어 있어야 함
```

`ProjectSettings.asset`에 `webGLCompressionFormat`·`webGLDecompressionFallback` 변경이 남아 있으면 커밋하지 말고 되돌리십시오. 빌드 스크립트가 복원 후 저장하므로 정상이라면 남지 않습니다.

## 2. 배포

저장소 루트에서 실행합니다.

```bash
scripts/deploy-client-pages.sh                # 기본: Client/Builds/WebGL
scripts/deploy-client-pages.sh <빌드 폴더>     # 다른 폴더를 배포할 때
```

- `gh-pages`에는 이번 빌드만 담은 커밋 하나가 강제 푸시됩니다. 이전 배포는 남지 않으며, 빌드 바이너리가 저장소 이력에 쌓이지 않습니다.
- 커밋 메시지에 원본 `main` 커밋이 기록됩니다(`배포: 클라이언트 WebGL 빌드 (main 5ad1509)`). 빌드 시점에 `Client`·`Shared`에 커밋되지 않은 변경이 있으면 `(커밋되지 않은 변경 포함)`이 붙습니다. 가능하면 커밋한 뒤 빌드·배포하십시오.
- 원격을 바꾸려면 `DEPLOY_REMOTE` 환경 변수를 지정합니다. 스크립트 동작만 시험할 때는 로컬 bare 저장소를 지정하면 실제 Pages에 영향이 없습니다.

```bash
git init --bare /tmp/pages-test.git
DEPLOY_REMOTE=/tmp/pages-test.git scripts/deploy-client-pages.sh
```

## 3. 배포 확인

Pages 반영에는 보통 1~2분 걸립니다.

```bash
gh api repos/PieceOfPaper/MutatIO/pages/builds/latest --jq .status    # built
curl -s -o /dev/null -w "%{http_code}\n" https://pieceofpaper.github.io/MutatIO/
```

브라우저에서 배포 주소를 열어 첫 화면이 뜨고 `PLAY`로 입장되는지 확인합니다. 브라우저 캐시 때문에 이전 빌드가 보이면 강력 새로고침(macOS `Cmd+Shift+R`)을 합니다.

콘솔의 다음 로그는 URP WebGL에서 흔히 나오며 화면에 영향이 없었습니다.

```text
Hidden/CoreSRP/CoreCopy shader is not supported on this GPU
Hidden/Universal/HDRDebugView shader is not supported on this GPU
```

## 최초 설정 (완료됨)

새 저장소나 포크에서만 필요합니다.

1. 첫 배포로 `gh-pages` 브랜치를 만듭니다.
2. Settings → Pages → Build and deployment → Source를 **Deploy from a branch**, `gh-pages` / `/(root)`로 지정합니다. CLI로는 다음과 같습니다.

```bash
gh api -X PUT repos/<owner>/<repo>/pages -f build_type=legacy -f 'source[branch]=gh-pages' -f 'source[path]=/'
# Pages가 아직 없다면 -X POST로 생성합니다.
```

## 이전 버전으로 되돌리기

`gh-pages`에 이전 배포가 남지 않으므로 되돌릴 커밋을 체크아웃해 다시 빌드·배포합니다.

```bash
git switch --detach <커밋>
# 1. 빌드 → 2. 배포
git switch main
```

## 문제 해결

| 증상 | 원인과 대처 |
| --- | --- |
| 페이지가 로딩 바에서 멈추고 콘솔에 압축 관련 오류 | Brotli 또는 Fallback 없는 Gzip으로 빌드됨. 반드시 `WebGLPagesBuild`로 빌드하고 `xxd`로 Gzip인지 확인 |
| batchmode 빌드가 바로 실패 | 같은 프로젝트를 연 에디터가 있음. 에디터를 닫거나 메뉴·CLI 방식 사용 |
| `빌드 결과가 없습니다` | `Client/Builds/WebGL/index.html`이 없음. 먼저 빌드 |
| 푸시 거부 | `origin` 쓰기 권한·인증 확인(`gh auth status`) |
| 배포했는데 이전 화면이 보임 | Pages 반영 대기(1~2분) 후 강력 새로고침 |
| `ProjectSettings.asset` 압축 설정 diff | 빌드 중 저장된 임시 설정. 커밋하지 말고 되돌림 |

## GitHub Actions를 쓰지 않는 이유

GameCI로 CI 빌드를 시도했지만 Unity Personal 라이선스 활성화에 필요한 계정 로그인이 계정의 2단계 인증 때문에 실패했습니다(HTTP 401). 2단계 인증이 없는 CI 전용 Unity 계정을 마련하면 다시 검토할 수 있습니다.

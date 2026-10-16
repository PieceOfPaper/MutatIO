# MutatIO

> 먹고, 성장하고, 변이하는 생태계형 실시간 IO 게임.

MutatIO는 모든 플레이어가 작은 기본 생명체로 시작하여 필드의 먹이를 먹고 성장하는 Unity WebGL 기반 IO 게임입니다. 개체가 죽으면 몸집에 비례한 생체 물질을 남기며, 이를 계속 섭취한 기본 생명체는 다른 개체를 사냥해야 살아갈 수 있는 포식자로 변이합니다.

현재는 **프리프로덕션 및 핵심 프로토타입 준비 단계**입니다. 온라인 멀티플레이, 전투 방식, 수치 밸런스 등은 아직 구현되거나 확정되지 않았습니다.

## 현재 확정된 방향

| 항목 | 내용 |
| --- | --- |
| 엔진 | Unity 6.3 LTS — 6000.3.26f1 |
| 렌더링 | 2D URP |
| 주 플랫폼 | 웹 브라우저 — Unity WebGL |
| 입력 | 키보드·마우스와 화면 내 가상 조이스틱 |
| 접속 구조 | 라운드에 구속되지 않고 자유롭게 입장·퇴장하는 IO 형태 |
| 시작 상태 | 모든 플레이어가 작은 기본 생명체로 시작 |
| 핵심 성장 | 먹은 양이 점수와 몸집으로 연결 |
| 생태 순환 | 먹이 부족 → 죽음과 사체 → 변이 포식자 발생 → 개체 수 변화 |
| 개발 방식 | 기획 문서는 GPT와 갱신하고, 구현은 Codex·Claude Code·직접 작업을 병행 |

## 먼저 읽을 문서

- [게임 기획서](docs/GAME_DESIGN.md): 현재 합의된 게임 규칙과 핵심 경험
- [미결정 기획 항목](docs/OPEN_DESIGN_QUESTIONS.md): 구현 전에 구체화해야 할 질문과 우선순위
- [기술 방향](docs/TECHNICAL_DIRECTION.md): 현재 프로젝트 사양과 구현 원칙
- [개발 워크플로](docs/DEVELOPMENT_WORKFLOW.md): 사람·GPT·코딩 에이전트 간 작업 규칙
- [개발 로드맵](docs/ROADMAP.md): 검증 순서를 기준으로 한 단계별 계획
- [결정 기록](docs/DECISIONS.md): 변경 이유와 확정 사항의 이력
- [에이전트 지침](AGENTS.md): Codex를 포함한 코딩 에이전트 공통 규칙
- [Claude Code 지침](CLAUDE.md): Claude Code 진입 지침

## 시작하기

### 필요 환경

- Unity Hub
- Unity Editor **6000.3.26f1**
- 해당 버전의 **WebGL Build Support**
- Git
- 선택: Unity CLI와 Unity Pipeline

### 프로젝트 열기

```bash
git clone https://github.com/PieceOfPaper/MutatIO.git
cd MutatIO
```

Unity Hub에서 저장소 루트를 프로젝트로 추가한 뒤 `6000.3.26f1`로 여십시오.

현재 시작 씬은 `Assets/Scenes/SampleScene.unity`입니다.

### Unity CLI 연결

이 프로젝트에는 `com.unity.pipeline` `0.8.0-exp.1`이 포함되어 있습니다. Unity Editor가 프로젝트를 연 상태에서 저장소 루트에서 다음 명령으로 연결 가능한 명령을 확인합니다.

```bash
unity command --project-path "$(pwd)"
```

연결 후에는 먼저 에디터 상태와 콘솔 오류를 확인합니다.

```bash
unity command --project-path "$(pwd)" editor_status
unity command --project-path "$(pwd)" console_status
unity command --project-path "$(pwd)" console --level error --tail 20
```

CLI와 패키지 버전에 따라 명령이 달라질 수 있으므로 자동화 전에 항상 명령 목록을 확인하십시오. Unity Pipeline은 실행 중인 Editor에 로컬 HTTP API로 연결하며 프로젝트 경로를 이용한 자동 탐색을 지원합니다.

## 현재 실행 및 검증 범위

아직 게임플레이와 자동화된 빌드·테스트 파이프라인은 준비되지 않았습니다. 기능을 추가할 때는 최소한 다음을 확인해야 합니다.

1. Unity 컴파일 오류가 없는지 확인
2. 관련 EditMode 또는 PlayMode 테스트 실행
3. 변경한 씬을 Play Mode에서 직접 확인
4. 웹 관련 변경은 WebGL 빌드에서 확인
5. 구현과 기획이 달라졌다면 관련 문서와 결정 기록 갱신

## 저장소 구조

```text
Assets/             Unity 게임 코드와 에셋
Packages/           Unity 패키지 선언
ProjectSettings/    Unity 프로젝트 설정
docs/               기획·기술·작업 문서
AGENTS.md           코딩 에이전트 공통 지침
CLAUDE.md           Claude Code 진입 지침
```

## 협업 원칙

- 작업 전 `git status`와 변경 내용을 확인합니다.
- 기존 변경을 임의로 되돌리거나 덮어쓰지 않습니다.
- 확정되지 않은 내용을 임의로 구현하지 않습니다.
- 게임 규칙이 바뀌면 코드와 함께 기획 문서를 갱신합니다.
- 신규 참여자에게 필요한 실행법이나 사양이 바뀌면 이 README도 갱신합니다.

자세한 기준은 [개발 워크플로](docs/DEVELOPMENT_WORKFLOW.md)와 [AGENTS.md](AGENTS.md)를 따릅니다.

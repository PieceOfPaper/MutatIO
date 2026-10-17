# MutatIO

> 먹고, 성장하고, 변이하는 생태계형 실시간 IO 게임.

MutatIO는 모든 플레이어가 작은 기본 생명체로 시작하여 필드의 먹이를 먹고 성장하는 Unity WebGL 기반 IO 게임입니다. 초식 개체가 죽으면 누적 섭취량에 비례한 육식 먹이를 남기며, 이를 계속 섭취한 기본 생명체는 다른 개체를 사냥해야 살아갈 수 있는 포식자로 변이합니다.

현재는 **프리프로덕션 및 핵심 프로토타입 준비 단계**입니다. 온라인 멀티플레이와 게임 규칙 구현은 앞으로 진행합니다. 전투는 몸집 기반 접촉 처치로 구체화했으며, 수치와 세부 판정은 미결정입니다.

## 현재 확정된 방향

| 항목 | 내용 |
| --- | --- |
| 엔진 | Unity 6.3 LTS — 6000.3.26f1 |
| 렌더링 | 2D URP |
| 주 플랫폼 | 웹 브라우저 — Unity WebGL |
| 입력 | WASD / 방향키 / 좌하단 고정 가상패드 / 터치/마우스 추종 중 선택 |
| 접속 구조 | 라운드에 구속되지 않고 자유롭게 입장·퇴장하는 IO 형태 |
| 시작 상태 | 외형·닉네임·조작 선택 후 작은 초식 생명체로 시작 |
| 핵심 성장 | 먹이 1개 = 1점, 점수로 몸집 계산. 외형 반경과 충돌 반경 동일 |
| 생태 순환 | 먹이 부족 → 죽음과 사체 → 변이 포식자 발생 → 개체 수 변화 |
| 개발 방식 | 기획 문서는 GPT와 갱신하고, 구현은 Codex·Claude Code·직접 작업을 병행 |

## 플레이 흐름

첫 화면에서 캐릭터·닉네임·4가지 이동 방식 선택 → 월드 입장 → 먹이 자동 흡수·성장·변이·접촉 경쟁 → 사망 결과 → 확인 후 첫 화면 복귀.

초식끼리 접촉해도 죽지 않으며, 큰 육식이 작은 초식·육식에 접촉하면 작은 개체가 죽습니다. 육식 사망은 고기를 남기지 않습니다. 성장하면 카메라 범위가 넓어지고 스페이스 또는 모바일 버튼으로 대시를 사용할 수 있습니다. PC 마우스 모드는 클릭 없이 커서를 추종하고 좌클릭으로도 대시합니다. UI 입력은 월드 이동이나 대시에 중복 적용하지 않습니다.

이전 캐릭터·닉네임·조작 선택은 PlayerPrefs로 복원합니다. 이동은 약한 관성과 크기별 속도 감소를 적용하며, 대시는 현재 이동 방향(정지 시 마지막 유효 방향)을 사용합니다.

초식 먹이는 타일 내부에 랜덤 생성하고, 캐릭터 몸체가 덮은 타일은 리젠을 막습니다. 초기 가정은 월드 1000×1000, 타일 5×5이며 타일당 최대 n개입니다. 흡수 거리는 몸 반경에 고정 거리를 더합니다. 굶주림으로 점수·몸집을 줄이지 않고 포만 소진 시 사망합니다.

이 내용은 기획 목표이며 구현 완료 상태가 아닙니다.

## 먼저 읽을 문서

- [게임 기획서](docs/GAME_DESIGN.md): 현재 합의된 게임 규칙과 핵심 경험
- [미결정 기획 항목](docs/OPEN_DESIGN_QUESTIONS.md): 구현 전에 구체화해야 할 질문과 우선순위
- [밸런스 데이터 목록](docs/BALANCE_PARAMETERS.md): 플레이 테스트로 정할 속도·관성·대시·먹이 등 수치와 곡선
- [조작 레퍼런스 조사](docs/INPUT_REFERENCE.md): Snake.io에서 확인한 내용과 미확인 조건
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
- .NET SDK **10.0** — 서버 빌드·실행
- Git
- 선택: Unity CLI와 Unity Pipeline

### 프로젝트 열기

```bash
git clone https://github.com/PieceOfPaper/MutatIO.git
cd MutatIO
```

Unity Hub에서 저장소의 `Client/` 폴더를 프로젝트로 추가한 뒤 `6000.3.26f1`로 여십시오.

현재 시작 씬은 `Client/Assets/Scenes/SampleScene.unity`입니다. 네트워크 확인용 씬은 `Client/Assets/MutatIO/Scenes/NetworkTest.unity`, 이동·조작 확인용 씬은 `Client/Assets/MutatIO/Scenes/MovementTest.unity`입니다.

### Unity CLI 연결

이 프로젝트에는 `com.unity.pipeline` `0.8.0-exp.1`이 포함되어 있습니다. Unity Editor가 프로젝트를 연 상태에서 저장소 루트에서 다음 명령으로 연결 가능한 명령을 확인합니다.

```bash
unity command --project-path "$(pwd)/Client"
```

연결 후에는 먼저 에디터 상태와 콘솔 오류를 확인합니다.

```bash
unity command --project-path "$(pwd)/Client" editor_status
unity command --project-path "$(pwd)/Client" console_status
unity command --project-path "$(pwd)/Client" console --level error --tail 20
```

CLI와 패키지 버전에 따라 명령이 달라질 수 있으므로 자동화 전에 항상 명령 목록을 확인하십시오. Unity Pipeline은 실행 중인 Editor에 로컬 HTTP API로 연결하며 프로젝트 경로를 이용한 자동 탐색을 지원합니다.

### 서버 실행

```bash
dotnet run --project Server/src/MutatIO.Server --launch-profile http
```

`http://localhost:5080/health`에서 상태를 확인할 수 있고, 게임 연결은 `ws://localhost:5080/ws`입니다. 아직 게임 메시지는 처리하지 않습니다.

서버 테스트:

```bash
dotnet test Server/MutatIO.Server.slnx
```

### 클라이언트–서버 연결 확인

1. 위 명령으로 서버를 실행합니다.
2. Unity에서 `Client/Assets/MutatIO/Scenes/NetworkTest.unity`를 열고 Play합니다.
3. 화면 왼쪽 위에 `State: Connected`와 RTT가 표시되면 정상입니다.

Unity EditMode 테스트(Test Runner 또는 CLI)에는 실제 서버에 접속하는 통합 테스트(`Integration` 카테고리)가 포함되어 있습니다. 서버가 실행 중이 아니면 이 테스트는 건너뜁니다. 서버 주소는 환경 변수 `MUTATIO_SERVER_URL`로 바꿀 수 있습니다.

```bash
unity command --project-path "$(pwd)/Client" run_tests --mode EditMode
```

## 현재 실행 및 검증 범위

아직 게임플레이 대부분과 자동화된 빌드·테스트 파이프라인은 준비되지 않았습니다. 현재 구현된 것은 클라이언트–서버 연결과 Ping/Pong RTT 측정, 로컬 이동·대시와 네 가지 조작 입력(로드맵 M1·M2)입니다.

### 이동·조작 확인

1. Unity에서 `Client/Assets/MutatIO/Scenes/MovementTest.unity`를 열고 Play합니다. 서버는 필요 없습니다.
2. Game 뷰를 클릭해 포커스를 줍니다. 포커스가 없으면 키보드·마우스 입력이 게임에 전달되지 않습니다.
3. 오른쪽 위 `WASD` / `Arrows` / `Pad` / `Pointer` 버튼으로 조작 방식을, `Size -` / `Size +`로 몸집을 바꿔 봅니다.
4. 대시는 스페이스, 오른쪽 아래 `DASH` 버튼, `Pointer` 모드의 월드 좌클릭입니다. 왼쪽 위에 모드·속도·몸집·대시 상태가 표시됩니다.
5. 이동·대시 수치는 Play 중 `PlayerCreature`의 `Local Creature Mover` 인스펙터에서 바꿀 수 있습니다. 모두 임시값입니다.

기능을 추가할 때는 최소한 다음을 확인해야 합니다. 기능을 추가할 때는 최소한 다음을 확인해야 합니다.

1. Unity 컴파일 오류가 없는지 확인
2. 관련 EditMode 또는 PlayMode 테스트 실행
3. 변경한 씬을 Play Mode에서 직접 확인
4. 웹 관련 변경은 WebGL 빌드에서 확인
5. 구현과 기획이 달라졌다면 관련 문서와 결정 기록 갱신

## 저장소 구조

```text
Client/             Unity 클라이언트 프로젝트
  Assets/           Unity 게임 코드와 에셋
  Packages/         Unity 패키지 선언
  ProjectSettings/  Unity 프로젝트 설정
Shared/             클라이언트·서버 공용 순수 C# 코드 (Unity 로컬 패키지)
Server/             ASP.NET Core 게임 서버 (.NET 10)
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

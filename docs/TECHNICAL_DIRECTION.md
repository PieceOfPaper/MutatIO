# 기술 방향

- 문서 상태: 초기 방향
- 마지막 갱신: 2026-10-17
- 목적: 현재 프로젝트 상태와 구현 시 지켜야 할 기술적 경계 기록

## 저장소 구성

| 폴더 | 내용 |
| --- | --- |
| `Client/` | Unity 클라이언트 프로젝트 |
| `Server/` | 게임 서버 프로젝트 자리. 서버 기술이 결정될 때까지 비어 있음 |

## 현재 프로젝트 사양

| 항목 | 현재 값 |
| --- | --- |
| Unity | 6000.3.26f1 |
| 템플릿 | 2D URP |
| Render Pipeline | Universal Render Pipeline 17.3.0 |
| Input | Unity Input System 1.20.0 |
| Unity Pipeline | 0.8.0-exp.1 |
| Test Framework | 1.6.0 |
| 시작 씬 | `Client/Assets/Scenes/SampleScene.unity` |
| 목표 클라이언트 | Unity WebGL |

정확한 버전은 항상 `Client/ProjectSettings/ProjectVersion.txt`와 `Client/Packages/manifest.json`을 기준으로 확인합니다.

## 설계 원칙

### 게임 규칙과 표현 분리

허기, 섭취, 질량, 변이, 사망 같은 규칙은 GameObject 표현과 가능한 한 분리합니다. 핵심 수치는 단위 테스트가 가능한 순수 C# 로직으로 유지하고 MonoBehaviour는 입력, Unity 생명주기, 표시 연결을 담당하도록 합니다.

### 입력 추상화

키보드·마우스와 화면 가상 조이스틱이 별도의 이동 코드를 갖지 않게 합니다.

```text
키보드·마우스 ─┐
                ├→ Input Actions → Player Command → Movement/Combat
가상 조이스틱 ──┘
```

게임플레이 시스템은 입력 장치가 아니라 이동 벡터와 행동 명령을 소비해야 합니다.

### 데이터 기반 밸런스

다음 수치는 코드 상수가 아니라 설정 데이터로 조정할 수 있어야 합니다.

- 허기 최대치와 감소율
- 먹이별 허기·점수·질량 획득량
- 몸집에 따른 외형 배율
- 변이 임계치와 효율
- 사망 시 생체 물질 환원율
- 이동, 공격, 체력 관련 값

초기에는 ScriptableObject 사용을 고려하되 온라인 권위 서버의 설정 공유 방식은 네트워크 결정 후 확정합니다.

### 온라인 경계

최종 게임은 실시간 IO 게임이지만 네트워크 기술은 아직 선택하지 않았습니다.

- 특정 SDK를 핵심 도메인 로직에 직접 퍼뜨리지 않습니다.
- 로컬 시뮬레이션과 네트워크 표현의 경계를 둡니다.
- 권한, 틱, 보간, 예측, WebGL 전송 제약을 검토한 뒤 솔루션을 선택합니다.
- 온라인 솔루션이 정해지기 전에는 로컬 수직 슬라이스로 핵심 재미를 검증합니다.

## 예상 기능 경계

| 영역 | 책임 |
| --- | --- |
| Core | 게임 시간, 생명주기, 공통 식별 |
| Creatures | 상태, 기본 생명체·포식자 형태 |
| Feeding | 먹이 감지와 섭취 |
| Hunger | 허기 감소와 굶주림 |
| Growth | 점수, 질량, 외형 크기 |
| Mutation | 생체 물질 누적과 포식자 전환 |
| Combat | 공격, 피해, 사망 |
| Spawning | 플레이어, 먹이, 사체 생성 |
| Input | 장치 입력을 게임 명령으로 변환 |
| UI | 허기, 점수, 변이, 순위 표시 |
| Networking | 네트워크 솔루션이 정해진 뒤 경계 구현 |

## WebGL 고려사항

- 브라우저에서 지원되지 않거나 제약이 큰 API를 도입하기 전에 확인합니다.
- 메인 스레드 비용, GC 할당, 다운로드 크기와 메모리를 관리합니다.
- 터치 입력과 가변 해상도를 막지 않는 UI를 사용합니다.
- 네트워크는 브라우저가 허용하는 전송 방식과 서버 배포 구조를 함께 검토해야 합니다.
- 기능 완료 판정에는 필요할 경우 실제 WebGL 빌드 검증을 포함합니다.

## Unity CLI와 Pipeline

Unity Pipeline은 실행 중인 Unity Editor에 로컬 HTTP API로 연결하여 CLI나 에이전트가 명령을 실행하게 합니다.

```bash
unity command --project-path "$(pwd)/Client"
unity command --project-path "$(pwd)/Client" editor_status
unity command --project-path "$(pwd)/Client" console_status
unity command --project-path "$(pwd)/Client" console --level error --tail 20
```

첫 번째 명령으로 현재 버전에서 제공되는 명령 목록을 확인한 뒤 사용합니다. 씬과 에셋을 자동 수정하는 경우 명령 수행 후 콘솔 오류와 실제 씬 상태를 다시 확인합니다.

공식 문서: https://docs.unity3d.com/Packages/com.unity.pipeline@0.8/manual/connectivity.html

## 아직 선택하지 않은 기술

- 멀티플레이 프레임워크
- 전용 서버 또는 호스트 방식
- 계정 및 데이터베이스
- 서버 호스팅
- CI/CD
- WebGL 배포 대상
- 분석과 크래시 리포팅

서버 기술이 결정되면 `Server/` 폴더 안에 서버 프로젝트를 구성합니다. 선택이 필요한 시점에는 후보를 비교하고 `docs/DECISIONS.md`에 근거와 함께 기록합니다.

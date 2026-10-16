# AGENTS.md

이 파일은 저장소 전체에 적용되는 코딩 에이전트 공통 지침입니다.

## 작업 시작 전 필수 확인

다음 순서로 읽고 현재 작업과 관련된 내용을 확인합니다.

1. `README.md`
2. `docs/GAME_DESIGN.md`
3. `docs/OPEN_DESIGN_QUESTIONS.md`와 `docs/BALANCE_PARAMETERS.md`
4. `docs/TECHNICAL_DIRECTION.md`
5. `docs/DEVELOPMENT_WORKFLOW.md`
6. `docs/DECISIONS.md`
7. 현재 브랜치의 `git status`와 관련 코드

문서가 충돌하면 최근에 명시된 사용자 요구와 `docs/DECISIONS.md`의 승인된 결정을 우선하고, 모호하면 구현을 확대하지 말고 질문합니다.

## 기획 상태 구분

- **확정**: `GAME_DESIGN.md`와 `DECISIONS.md`에 승인된 내용입니다. 구현 기준으로 사용할 수 있습니다.
- **제안**: 검증 가능한 프로토타입을 위해 임시로 선택할 수 있으나 코드와 문서에 임시임을 표시합니다.
- **미결정**: `OPEN_DESIGN_QUESTIONS.md`에 있는 내용입니다. 사용자 승인 없이 영구 구조로 고정하지 않습니다.

수치 밸런스, 네트워크 솔루션, 서버 권한 구조, 전투 판정처럼 영향이 큰 미결정 사항은 추측하여 확정하지 않습니다.

## 기존 작업 보호

- 사용자의 로컬 수정과 다른 에이전트의 변경을 임의로 되돌리지 않습니다.
- 작업 전후에 diff를 확인하고 요청 범위 밖의 파일을 불필요하게 수정하지 않습니다.
- `git reset --hard`, 광범위한 checkout, 무단 파일 삭제를 사용하지 않습니다.
- `Library`, `Temp`, `Logs`, `obj`, 빌드 산출물은 커밋하지 않습니다.

## 커밋·브랜치·푸시

- 수정을 마치면 변경 내용을 보고하고 멈춥니다. 사용자가 직접 확인한 뒤 커밋을 요청합니다.
- 사용자가 커밋을 요청하기 전에는 `git commit`을 실행하지 않습니다. 작업 단위가 끝났거나 검증을 통과했어도 마찬가지입니다.
- 커밋 메시지는 제목과 본문 모두 한국어로 작성합니다.
- 커밋은 현재 브랜치(기본 `main`)에 바로 합니다. 사용자가 브랜치를 나누라고 요청한 경우에만 새 브랜치를 만듭니다.
- 푸시는 보통 사용자가 직접 합니다. 사용자가 푸시를 요청한 경우에만 `git push`를 실행합니다.
- 한 번 받은 커밋·푸시 요청은 그 변경에만 적용합니다. 이후 변경은 다시 요청을 받아야 합니다.

## Unity 작업 규칙

- Unity 버전은 `ProjectSettings/ProjectVersion.txt`를 따릅니다.
- 씬·프리팹·ScriptableObject는 가능하면 Unity Editor 또는 Unity Pipeline으로 수정합니다.
- Unity YAML을 직접 수정한다면 GUID, fileID, 직렬화 구조를 이해한 경우에만 최소 범위로 수정합니다.
- 에셋을 추가·이동·삭제할 때 대응하는 `.meta` 파일을 함께 관리합니다.
- 런타임 코드와 에디터 전용 API를 분리합니다.
- 입력은 Unity Input System으로 추상화하여 키보드·마우스와 가상 조이스틱이 같은 게임플레이 명령을 사용하게 합니다.
- WebGL 제한을 고려하고 플랫폼 종속 API를 무심코 도입하지 않습니다.
- 온라인 멀티플레이 기술은 미결정입니다. 승인 없이 특정 네트워크 프레임워크에 프로젝트를 결합하지 않습니다.

## 권장 코드 구조

실제 구현이 시작되면 기능 경계를 우선합니다. 폴더 구조만 미리 대량 생성하지 않습니다.

```text
Assets/MutatIO/
  Runtime/
    Core/
    Creatures/
    Feeding/
    Hunger/
    Growth/
    Combat/
    Input/
    UI/
  Editor/
  Tests/
    EditMode/
    PlayMode/
```

## 검증

변경 종류에 맞는 가장 좁은 검증부터 실행합니다.

1. 컴파일 및 콘솔 오류 확인
2. 관련 EditMode 테스트
3. 관련 PlayMode 테스트
4. 씬 Play Mode 확인
5. WebGL 관련 기능은 WebGL 빌드와 브라우저에서 확인

Unity Editor가 실행 중이면 다음 명령의 가용성을 먼저 확인합니다.

```bash
unity command --project-path "$(pwd)"
unity command --project-path "$(pwd)" editor_status
unity command --project-path "$(pwd)" console_status
unity command --project-path "$(pwd)" console --level error --tail 20
```

사용 가능한 명령은 설치된 Unity CLI와 Pipeline 버전이 보고하는 목록을 기준으로 합니다. 실행하지 못한 검증은 완료한 것처럼 표현하지 않고 사유를 남깁니다.

## 문서 갱신

- 게임 규칙 변경: `docs/GAME_DESIGN.md`
- 새 결정 또는 기존 결정 번복: `docs/DECISIONS.md`
- 미결정 항목의 추가·해결: `docs/OPEN_DESIGN_QUESTIONS.md`
- 테스트 수치·곡선·채택 근거: `docs/BALANCE_PARAMETERS.md`
- 레퍼런스의 검증 결과와 미확인 조건: `docs/INPUT_REFERENCE.md`
- 기술 스택·아키텍처·플랫폼 변경: `docs/TECHNICAL_DIRECTION.md`
- 설치·실행·검증 방법 변경: `README.md`
- 개발 단계 변경: `docs/ROADMAP.md`

문서는 코드의 현재 동작을 과장하지 않아야 합니다. 계획은 구현 완료와 명확히 구분합니다.

# 기술 방향

- 문서 상태: 초기 방향
- 마지막 갱신: 2026-10-17
- 목적: 현재 프로젝트 상태와 구현 시 지켜야 할 기술적 경계 기록

## 저장소 구성

| 폴더 | 내용 |
| --- | --- |
| `Client/` | Unity 클라이언트 프로젝트 |
| `Shared/` | 클라이언트·서버 공용 순수 C# 코드. Unity 로컬 패키지 `com.mutatio.shared` |
| `Server/` | ASP.NET Core 게임 서버 솔루션 |

```text
Shared/                       com.mutatio.shared (Unity 로컬 패키지)
  Runtime/                    MutatIO.Shared.asmdef — noEngineReferences
Server/
  MutatIO.Server.slnx
  src/MutatIO.Shared/         Shared/Runtime 소스를 netstandard2.1로 빌드
  src/MutatIO.Server/         WebSocket 게임 서버
  tests/MutatIO.Server.Tests/ xUnit 테스트
```

Unity는 `Client/Packages/manifest.json`의 `"com.mutatio.shared": "file:../../Shared"`로 공유 코드를 참조합니다.

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

첫 화면의 WASD / 방향키 / 가상패드 / 터치/마우스 추종 네 선택지가 같은 이동 명령을 생성하게 합니다. 두 키보드 모드는 별도 선택지이고 가상패드는 좌하단 고정입니다. 포인터 목표를 방향으로 변환하는 계층과 실제 캐릭터 이동을 분리합니다.

```text
키보드·마우스 ─┐
                ├→ Input Actions → Player Command → Movement/Combat
가상 조이스틱 ──┘
```

게임플레이 시스템은 입력 장치가 아니라 이동 벡터와 행동 명령을 소비해야 합니다.

### 데이터 기반 밸런스

값·곡선은 [밸런스 데이터 목록](BALANCE_PARAMETERS.md)에서 관리합니다. 다음 수치는 코드 상수가 아니라 설정 데이터로 조정할 수 있어야 합니다.

- 허기 최대치와 감소율
- 먹이별 허기·점수·질량 획득량
- 몸집에 따른 외형 배율
- 변이 임계치와 효율
- 사망 시 생체 물질 환원율
- 이동 속도와 접촉 판정 우위 기준
- 자석 흡수 반경과 속도
- 먹이 공간 간격과 리젠 주기
- 카메라 줌 곡선과 보간
- 가속·감속·방향 전환 응답과 크기별 속도 감소 곡선
- 대시 배율·지속시간·쿨타임

초기에는 ScriptableObject 사용을 고려하되 온라인 권위 서버의 설정 공유 방식은 네트워크 결정 후 확정합니다.

### 온라인 경계

서버는 ASP.NET Core(.NET 10)로 만들고 클라이언트와 WebSocket으로 통신합니다. 근거는 [결정 기록](DECISIONS.md)에 있습니다.

- 게임 규칙과 프로토콜 정의는 `Shared/`에 둡니다. 서버가 같은 코드로 권위 판정을 하고 클라이언트는 예측과 로컬 테스트에 사용합니다.
- `Shared/`는 Unity와 서버 양쪽에서 컴파일되므로 netstandard2.1과 C# 9 범위만 사용하고, `UnityEngine`이나 ASP.NET Core를 참조하지 않습니다.
- 네트워크 송수신 코드는 `Shared/`의 도메인 로직에 섞지 않고 클라이언트·서버 각자의 Networking 계층에 둡니다.
- 서버 틱, 권위 범위, 보간·예측 방식은 아직 결정하지 않았습니다.

### 서버 현재 상태

| 항목 | 내용 |
| --- | --- |
| 런타임 | .NET 10 (`Server/global.json`으로 SDK 10.0.1xx 이상 고정) |
| 엔드포인트 | `GET /health` — 상태와 프로토콜 버전, `/ws` — WebSocket 게임 연결 |
| 로컬 주소 | `http://localhost:5080` (`launchSettings.json`의 `http` 프로필) |
| 메시지 처리 | Ping → Pong 응답만 처리 |

### 프로토콜 (임시)

`Shared/Runtime/Protocol`의 `MessageCodec`이 `[MessageType 1바이트][내용]` 형식을 수동으로 인코딩합니다. 직렬화 라이브러리가 정해지면 교체할 **임시 구현**입니다. 현재 메시지는 연결 확인과 RTT 측정용 `Ping`/`Pong`뿐입니다.

### 클라이언트 네트워크 구조

```text
Client/Assets/MutatIO/Runtime/Networking/
  NetworkClient.cs              프로토콜 처리. 메인 스레드에서 Update()로 이벤트 전달
  Transport/
    IClientTransport.cs         플랫폼 공통 전송 인터페이스
    ClientTransportFactory.cs   플랫폼별 구현 선택
    ClientWebSocketTransport.cs Editor·데스크톱·Android·iOS (.NET ClientWebSocket)
    WebGLWebSocketTransport.cs  WebGL (jslib로 브라우저 WebSocket 호출)
    WebGL/MutatIOWebSocket.jslib
  Diagnostics/NetworkPingTester.cs  연결·RTT 확인용 개발 컴포넌트
```

- 전송 계층은 송수신 이벤트를 큐에 쌓고, 게임 코드는 메인 스레드에서 `NetworkClient.Update()`로 꺼내 처리합니다. 백그라운드 스레드에서 Unity API를 호출하지 않습니다.
- `WebGLWebSocketTransport`는 WebGL 플레이어와 Editor에서만 컴파일됩니다. iOS 등에서 `__Internal` 심볼 링크 오류가 나지 않게 하기 위함입니다.
- 확인용 씬: `Client/Assets/MutatIO/Scenes/NetworkTest.unity`

### 플랫폼별 설정

| 플랫폼 | 내용 |
| --- | --- |
| 공통 | `runInBackground` 켬. 창이 포커스를 잃어도 네트워크 처리가 멈추지 않게 합니다. |
| Android | Internet Access를 Require로 설정(`ForceInternetPermission`). |
| iOS | IL2CPP(AOT)이므로 런타임 코드 생성(Reflection.Emit)을 쓰는 직렬화는 사용하지 않습니다. |
| WebGL | 기본 IMGUI 폰트에 한글 글리프가 없어 진단 UI 문구는 영어로 둡니다. |
| 배포 | 운영 환경은 `wss://`(TLS)를 사용합니다. HTTPS 페이지의 WebGL은 `ws://`에 접속할 수 없습니다. |

## 예상 기능 경계

| 영역 | 책임 |
| --- | --- |
| Core | 게임 시간, 생명주기, 공통 식별 |
| Creatures | 상태, 기본 생명체·포식자 형태 |
| Feeding | 먹이 감지와 섭취 |
| Hunger | 허기 감소와 굶주림 |
| Growth | 점수, 질량, 외형 크기 |
| Mutation | 생체 물질 누적과 포식자 전환 |
| Combat | 초식/육식 조합과 몸집 차이에 따른 접촉 사망 판정 |
| Spawning | 플레이어와 주기적 초식 먹이 생성, 초식 사망 시 육식 먹이 생성 |
| Input | 장치 입력을 게임 명령으로 변환 |
| UI | 외형·닉네임·조작 선택, 랭킹·설정·머리 위 게이지, 사망 결과 |
| Networking | 네트워크 솔루션이 정해진 뒤 경계 구현 |

## 이번 기획의 구현 경계

- 첫 화면, 월드 플레이, 사망 결과를 명확한 상태로 구분합니다. 사망 후 자동 재스폰하지 않습니다.
- 캐릭터 외형과 초식/육식 상태를 분리하고 색상은 상태 표현에 사용합니다.
- 자석 흡수 연출과 단일 보상 처리를 분리합니다. 온라인 판정 권한은 별도 결정합니다.
- 사망 드롭은 초식에만 적용합니다. 육식 사망 이벤트에 공통 드롭을 연결하지 않습니다.
- 포만 게이지와 닉네임은 캐릭터 추적 표시이며, 랭킹·설정은 화면 고정 UI입니다.
- 월드 크기와 충돌 판정값을 UI 픽셀 크기나 줌 배율로 산정하지 않습니다.
- 접촉 판정의 미정 조건을 임의로 확정하지 않습니다.
- 대시와 이동은 같은 장치 독립 명령 체계를 사용합니다.

## 이동과 설정 저장

- 별도의 선회 속도 제한 없이 약한 관성을 속도 벡터 응답으로 표현합니다.
- 크기가 커질수록 이동속도를 완만하게 감소시킵니다. 곡선과 하한은 테스트 데이터입니다.
- 대시는 현재 유효 이동 방향, 정지 시 마지막 유효 이동 방향을 사용합니다. 첫 이동 전 처리와 정지 판정 임계값은 구현 전에 정합니다.
- UI 입력은 월드 이동에 사용하지 않습니다. 이동 터치와 대시 버튼 터치를 구분하며 UI 클릭을 월드 대시에 중복 사용하지 않습니다.
- PC 마우스는 버튼을 누르지 않아도 커서를 추종합니다. 좌클릭은 대시 입력이며 해제해도 추종은 유지됩니다. 고정 클릭 월드 좌표를 목적지로 사용하지 않습니다.
- 모바일 터치 해제·중심 데드존·카메라 이동 시 좌표 갱신과 UI 위에서의 기존 이동 처리는 INPUT_REFERENCE.md의 미확인 사항을 임의로 확정하지 않습니다.
- 캐릭터 식별자, 닉네임, 조작 모드를 PlayerPrefs에 저장하고 첫 화면 복귀·재실행 시 복원합니다.
- 설정 확정 시 저장하고 매 프레임 저장하지 않습니다. 저장값이 없거나 삭제된 캐릭터 등 유효하지 않은 값이면 기본값으로 복구합니다.
- Web의 PlayerPrefs는 브라우저 IndexedDB에 저장됩니다. 브라우저 데이터 삭제 시 복원이 불가능할 수 있으므로 기본값 처리를 둡니다.
- 저장 대상에 현재 점수·몸집·육식 상태·월드 접속 상태를 포함하지 않습니다.

참고: [Unity PlayerPrefs 공식 문서](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PlayerPrefs.html)

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

- 메시지 직렬화 라이브러리 — MemoryPack 또는 MessagePack-CSharp 후보
- 서버 틱, 권위 범위, 방(인스턴스) 구조
- 계정 및 데이터베이스
- 서버 호스팅
- CI/CD
- WebGL 배포 대상
- 분석과 크래시 리포팅

선택이 필요한 시점에는 후보를 비교하고 `docs/DECISIONS.md`에 근거와 함께 기록합니다.

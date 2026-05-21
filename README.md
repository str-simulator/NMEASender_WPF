# NMEASender_WPF

NMEA 문장을 COM/UDP로 송신하는 WPF(.NET 8) 기반 툴입니다.

## 1. 프로젝트 개요

- 프레임워크: `WPF`, `.NET 8`, `CommunityToolkit.Mvvm`
- 주요 출력 채널: `Serial(COM)` / `UDP(Broadcast/Multicast)`
- 목적: 시뮬레이터 연동 데이터 기반 NMEA 생성, 미리보기, 선택 송신

## 2. 주요 기능

- 문장별 COM/UDP 송신 분리 체크
- ALL COM / ALL UDP 일괄 체크
- Sentence 행 복제/삭제(+/-)
- COM 포트별 BaudRate 설정
- Sentence(행)별 UDP 포트 설정
- START 중 COM/UDP 동적 오픈/클로즈
- 로그 자동 하단 추적 + 사용자 스크롤 해제/복귀
- 프로젝트 타입별(PS2603 / PS2514 / PS2404A) 송신 규칙 분기

## 3. 최신 구조 업데이트

이번 리팩터링은 서비스 레이어뿐 아니라 **전 레이어(Models / ViewModels / Views / Services / Interfaces / Behaviors)** 에 폴더 분리를 적용했습니다.

### 3.1 폴더 구조

- `Models`
  - `Core`: NMEA/전송 핵심 모델
  - `UI`: 화면 바인딩용 모델
  - `Network`: UDP 관련 모델
  - `SharedMemory`: 네이티브 공유메모리 구조
  - `Ais`: AIS payload 빌더
  - `Projects`: 프로젝트 공통 프로필/열거형
- `ViewModels`
  - `Shell`: 앱 조립/공통 상태
  - `Panels`: 메인 화면 패널 ViewModel
  - `Dialogs`: 설정 창 ViewModel
- `Views`
  - `Shell`: 메인 윈도우
  - `Panels`: 상단/좌측/우측 패널
  - `Dialogs`: 설정 창
- `Services`
  - `Application`, `Config`, `Workflow`, `Transmission`, `Ports`, `IO`, `Network`
  - `Projects`: 프로젝트별 구현체(PS2603/PS2514/PS2404A)
- `Services/Interfaces`
  - 기능별로 `Application`, `Config`, `Workflow`, `Transmission`, `Ports`, `IO`, `Network`, `Projects` 하위 분리
- `Behaviors/Core`
  - 공통 Attached Behavior

### 3.2 프로젝트별 분리 방식

프로젝트 의존 로직은 `Services/Projects/<ProjectType>/`에 모아두고, 상위 서비스는 라우터 역할만 수행합니다.

대표 분리 지점:

- Sentence 빌드: `IProjectNmeaSentenceBuilder`
- iOS VTG 처리: `IProjectSentenceComposerProfile`
- 송신 프레이밍/UDP 포트 정책: `IProjectSentenceFramePolicy`
- Sentence 카탈로그 노출 정책: `IProjectSentenceCatalogPolicy`
- SEND FLAG 인코딩/디코딩: `IProjectSendFlagCodec`
- UDP 프로필 저장소: `IProjectUdpTransportProfileStore`

## 4. MVVM 구성

### 4.1 View

- `Views/Shell/MainWindow.xaml`
- `Views/Panels/TopToolbarView.xaml`
- `Views/Panels/ManualLogView.xaml`
- `Views/Panels/SentencePanelView.xaml`
- `Views/Dialogs/PortBaudRateSettingsWindow.xaml`

### 4.2 ViewModel

- `ViewModels/Shell/MainViewModel.cs` (Shell 조립)
- `ViewModels/Shell/MainStateStore.cs` (공통 상태 저장소)
- `ViewModels/Panels/TopToolbarViewModel.cs`
- `ViewModels/Panels/ManualLogViewModel.cs`
- `ViewModels/Panels/SentencePanelViewModel.cs`
- `ViewModels/Dialogs/PortBaudRateSettingsViewModel.cs`

### 4.3 View-ViewModel 매핑 (App.xaml)

`App.xaml` DataTemplate 매핑으로 ViewModel 타입별 View를 자동 연결합니다.

- `TopToolbarViewModel` -> `TopToolbarView`
- `ManualLogViewModel` -> `ManualLogView`
- `SentencePanelViewModel` -> `SentencePanelView`

## 5. 서비스 레이어 구성

- 워크플로우
  - `Services/Workflow/MainWorkflowService.cs`
- 설정
  - `Services/Config/NmeaSenderConfigService.cs`
  - `Services/Config/IniFileService.cs`
- 전송
  - `Services/Transmission/NmeaSentenceBuilderService.cs`
  - `Services/Transmission/SentenceComposerService.cs`
  - `Services/Transmission/SentenceCatalogService.cs`
  - `Services/Transmission/NmeaTransmissionService.cs`
  - `Services/Transmission/ProjectSentenceFrameService.cs`
- IO/통신
  - `Services/IO/OutputChannelService.cs`
  - `Services/IO/SharedMemoryProviderService.cs`
  - `Services/Ports/*`
  - `Services/Network/UdpService.cs`
  - `Services/Network/UdpTransportProfileService.cs`

## 6. 프로젝트별 구현체 위치

- `Services/Projects/PS2603/*`
- `Services/Projects/PS2514/*`
- `Services/Projects/PS2404A/*`

예: PS2404A 전용 규칙

- SEND FLAG 코덱
- RPM 교대 송신
- `1$...`, `2$...` 프레임 확장
- AIS 중복 전송
- `NMEAMultiCast.ini` 기반 UDP 전송 설정

## 7. 설정 파일

기본 파일: `NMEASender.Wpf.ini`

주요 섹션:

- `[CONFIG]`: TITLE, Project
- `[GPS CONFIG]`: 기본 시리얼 옵션, SEND FLAG, UDP SEND FLAG, RIGHT RPM 등
- `[SOCKET]`: 기본 UDP 포트
- `[SENTENCE PORTS]`: Sentence 행별 COM 포트
- `[UDP PORTS]`: Sentence 행별 UDP 포트
- `[BAUD RATE]`: COM 포트별 BaudRate

PS2404A는 추가로 `NMEAMultiCast.ini`를 사용합니다.

## 8. 빌드

```bash
dotnet build
```

실행 중 파일 잠금이 있을 때:

```bash
dotnet build NMEASender.Wpf.csproj --no-restore -p:OutputPath=bin\\BuildCheck\\ -p:UseAppHost=false
```

## 9. 새 프로젝트 타입 추가 가이드

1. `Models/Projects/ProjectModels.cs`에 `ProjectType` 추가
2. `Services/Projects/<NEW_PROJECT>/` 폴더 생성
3. 아래 인터페이스 구현체 추가
   - `IProjectNmeaSentenceBuilder`
   - `IProjectSentenceComposerProfile`
   - `IProjectSentenceFramePolicy`
   - `IProjectSentenceCatalogPolicy`
   - `IProjectSendFlagCodec`
   - `IProjectUdpTransportProfileStore`
4. `App.xaml.cs` DI 등록 추가

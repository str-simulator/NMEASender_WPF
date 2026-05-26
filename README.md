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
- Sentence(행)별 UDP Multicast 주소 설정
- 설정창 Broadcast/Multicast 모드 선택
- START 중 COM/UDP 동적 오픈/클로즈
- 로그 자동 하단 추적 + 사용자 스크롤 해제/복귀
- 프로젝트 타입별(PS2603 / PS2514 / PS2404A) 송신 규칙 분기

## 3. 최신 업데이트 요약

이번 업데이트는 기존 단일 로직 중심 구조를 프로젝트별 정책 기반 구조로 정리한 것이 핵심입니다.

- `BaseProjectNmeaSentenceBuilder`는 공통 문장 생성/체크섬/포맷 유틸만 담당하도록 정리
- PS2404A 전용 NMEA 문장 생성 로직을 `Ps2404aNmeaSentenceBuilder`로 분리
- PS2603, PS2514도 각각 프로젝트 폴더 내 빌더 클래스로 분리
- `NmeaSentenceBuilderService`는 `ProjectType` 기준으로 프로젝트별 빌더를 선택
- `ProjectSentenceFrameService`는 프로젝트별 송신 프레임 정책을 선택
- `SentenceCatalogService`는 프로젝트별 노출 가능한 Sentence 목록을 정책으로 필터링
- `UDPConfig.ini` 기반 UDP 설정 분리
- 문장별 UDP Port/Multicast Address 저장 및 송신 반영
- 전역 `Use UDP` 체크박스 제거, 각 Sentence 행의 UDP 체크 상태로 송신 여부 결정

## 4. 최신 구조 업데이트

이번 리팩터링은 서비스 레이어뿐 아니라 **전 레이어(Models / ViewModels / Views / Services / Interfaces / Behaviors)** 에 폴더 분리를 적용했습니다.

### 4.1 폴더 구조

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

### 4.2 프로젝트별 분리 방식

프로젝트 의존 로직은 `Services/Projects/<ProjectType>/`에 모아두고, 상위 서비스는 라우터 역할만 수행합니다.

대표 분리 지점:

- Sentence 빌드: `IProjectNmeaSentenceBuilder`
- iOS VTG 처리: `IProjectSentenceComposerProfile`
- 송신 프레이밍/UDP 포트 정책: `IProjectSentenceFramePolicy`
- Sentence 카탈로그 노출 정책: `IProjectSentenceCatalogPolicy`
- SEND FLAG 인코딩/디코딩: `IProjectSendFlagCodec`
- UDP 프로필 저장소: `IProjectUdpTransportProfileStore`

### 4.3 Base와 프로젝트 구현체 역할

- `BaseProjectNmeaSentenceBuilder`
  - 공통 NMEA 생성 로직
  - 공통 Checksum 계산
  - 공통 좌표/시간/월별 기상값/ETA 계산 유틸
  - AIS Payload sentence 조립
- `Services/Projects/PS2404A/Ps2404aNmeaSentenceBuilder.cs`
  - PS2404A 전용 좌표 포맷
  - PS2404A 전용 `GLL`, `RMC`, `VTG` mode indicator / KOSE 처리
  - PS2404A 전용 `RPM`, `DPT`
  - PS2404A 전용 `VDVBW`, `VHW`, `VDR`, `DTM`, `GPDTM`, `THS`, `MWS`, `MWH`, `HTD`, `TTM`
- `Services/Projects/PS2603/Ps2603ProjectServices.cs`
  - PS2603 Talker ID 정책
  - 문장별 Multicast 주소 지원 정책
- `Services/Projects/PS2514/Ps2514ProjectServices.cs`
  - PS2514 Talker ID 정책

## 5. MVVM 구성

### 5.1 View

- `Views/Shell/MainWindow.xaml`
- `Views/Panels/TopToolbarView.xaml`
- `Views/Panels/ManualLogView.xaml`
- `Views/Panels/SentencePanelView.xaml`
- `Views/Dialogs/PortBaudRateSettingsWindow.xaml`

### 5.2 ViewModel

- `ViewModels/Shell/MainViewModel.cs` (Shell 조립)
- `ViewModels/Shell/MainStateStore.cs` (공통 상태 저장소)
- `ViewModels/Panels/TopToolbarViewModel.cs`
- `ViewModels/Panels/ManualLogViewModel.cs`
- `ViewModels/Panels/SentencePanelViewModel.cs`
- `ViewModels/Dialogs/PortBaudRateSettingsViewModel.cs`

### 5.3 View-ViewModel 매핑 (App.xaml)

`App.xaml` DataTemplate 매핑으로 ViewModel 타입별 View를 자동 연결합니다.

- `TopToolbarViewModel` -> `TopToolbarView`
- `ManualLogViewModel` -> `ManualLogView`
- `SentencePanelViewModel` -> `SentencePanelView`

## 6. 서비스 레이어 구성

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

## 7. 프로젝트별 구현체 위치

- `Services/Projects/PS2603/*`
- `Services/Projects/PS2514/*`
- `Services/Projects/PS2404A/*`

### 7.1 PS2404A 전용 규칙

- `Ps2404aSendFlagCodec`
  - PS2404A 기존 MFC SEND FLAG 매핑 반영
- `Ps2404aSentenceFramePolicy`
  - PORT/STBD RPM 교대 송신
  - `$` 문장은 `1$...`, `2$...` 형태로 확장
  - `!` AIS 문장은 동일 문장 2회 송신
  - 문장별 Multicast 주소 지원
- `Ps2404aNmeaSentenceBuilder`
  - PS2404A NMEADrv 기준 문장 생성 규칙 반영
  - KOSE 기반 RMC/VTG 처리
  - PS2404A 전용 좌표 포맷 및 Talker ID override 적용
- `Ps2404aUdpTransportProfileStore`
  - `UDPConfig.ini` 로드/저장
  - 기존 `NMEAMultiCast.ini`가 있고 `UDPConfig.ini`가 없으면 1회 호환 로드
  - Broadcast/Multicast 모드
  - Multicast `PORT NO`, `SEND PORT`, `SEND ADDRESS`

### 7.2 프로젝트별 주요 차이

| Project | 주요 특징 |
|---|---|
| `PS2603` | 기본 NMEA 규칙, `INVBW`, 문장별 Multicast 주소 지원 |
| `PS2514` | 기본 NMEA 규칙, 기본 Talker 유지 |
| `PS2404A` | PS2404A NMEADrv 호환, 프레임 확장, RPM 교대 송신, UDPConfig.ini 지원 |

## 8. 설정 파일

기본 파일: `NMEASender.Wpf.ini`

주요 섹션:

- `[CONFIG]`: TITLE, Project
- `[GPS CONFIG]`: 기본 시리얼 옵션, SEND FLAG, RIGHT RPM 등
- `[SENTENCE PORTS]`: Sentence 행별 COM 포트
- `[BAUD RATE]`: COM 포트별 BaudRate

`[CONFIG]`의 `Project` 값으로 프로젝트별 정책이 선택됩니다.

```ini
[CONFIG]
TITLE=ECDIS Sender
Project=PS2404A
```

UDP 관련 설정은 `NMEASender.Wpf.ini`에 저장하지 않고 `UDPConfig.ini`에 분리해서 저장합니다.

```ini
[UDP CONFIG]
USE UDP=1
SEND PORT=40014
UDP SEND FLAG=16777215

[UDP PORTS]
GGA=40014
RMC=40014

[UDP ADDRESSES]
GGA=225.0.0.0
RMC=225.0.0.0

[BROADCAST]
USE=1
PORT NO=49552

[MULTICAST]
PORT NO=6000
SEND PORT=6000
SEND ADDRESS=225.0.0.0
```

설정창에서 Multicast Address를 수정하면 문장별 Multicast 주소에 일괄 적용할 수 있습니다.

## 9. 빌드

```bash
dotnet build
```

실행 중 파일 잠금이 있을 때:

```bash
dotnet build NMEASender.Wpf.csproj --no-restore -p:OutputPath=bin\\BuildCheck\\ -p:UseAppHost=false
```

## 10. 새 프로젝트 타입 추가 가이드

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

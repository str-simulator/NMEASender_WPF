# NMEASender_WPF

NMEA 문장을 COM/UDP로 송신하는 WPF(.NET 8) 기반 툴입니다.

## 1. 프로젝트 개요

- 프레임워크: `WPF`, `.NET 8`, `CommunityToolkit.Mvvm`
- 주요 출력 채널: `Serial(COM)` / `UDP Broadcast`
- 목적: 시뮬레이터 연동 데이터 기반 NMEA 생성, 미리보기, 선택 송신

## 2. 최신 업데이트 요약

이번 리팩터링/기능 업데이트에서 반영된 핵심 내용입니다.

- UI 구조 분리
  - `MainWindow`를 조립 컨테이너로 단순화
  - `TopToolbarView`, `ManualLogView`, `SentencePanelView`로 분할
- View-ViewModel 매핑 구조 도입
  - View별 ViewModel(`TopToolbarViewModel`, `ManualLogViewModel`, `SentencePanelViewModel`) 적용
  - `App.xaml`에 DataTemplate 매핑 등록
- 메인 로직 재구성 (MVVM 강화)
  - `MainViewModel`은 화면 조립(Shell)만 담당
  - 공통 상태는 `MainStateStore`로 분리
  - 실제 업무 로직/통신 제어는 `MainWorkflowService`로 분리
  - 각 ViewModel은 자신의 View 바인딩 상태/명령만 담당
- 문장별 송신 제어 고도화
  - COM/UDP 체크 분리, ALL COM/ALL UDP 체크 분리
  - Sentence 복제/삭제(+/-) 지원
- 통신 기능 고도화
  - START 상태에서 UDP만 선택된 경우도 정상 시작
  - COM 오픈 실패 시 경고 다이얼로그 표시
  - 실행 중 COM 체크 활성화 시 해당 COM 포트 즉시 오픈
  - 전역 `Use UDP` 토글 제거, 문장(행) UDP 체크 상태 기반 자동 UDP 오픈/클로즈
- 포트/설정 기능 확장
  - COM 포트별 BaudRate 설정
  - Sentence(행)별 UDP 포트 설정 및 실제 송신 반영
  - 상단 UDP 포트 입력 + `APPLY`로 모든 문장 UDP 포트 일괄 적용
  - INI 저장/복원 확장 (`[UDP PORTS]`, `[BAUD RATE]` 등)
- 로그 UX 개선
  - 최신 로그 자동 추적
  - 사용자 수동 스크롤 시 자동 추적 해제/하단 복귀 시 재개
  - 커스텀 스크롤바 스타일 적용
- NMEA 생성 개선
  - PORT/STBD RPM 분리
  - ProjectType 기반 Talker/출력 규칙 확장

## 3. 아키텍처

### View

- `MainWindow.xaml` (Shell)
- `Views/TopToolbarView.xaml`
- `Views/ManualLogView.xaml`
- `Views/SentencePanelView.xaml`

### ViewModel

- `MainViewModel` (Shell, 화면 조립)
- `MainStateStore` (공통 UI 상태 저장소)
- `TopToolbarViewModel`
- `ManualLogViewModel`
- `SentencePanelViewModel`
- `PortBaudRateSettingsViewModel`

### Model

- `SentenceItem`, `NmeaDataDto`, `NmeaBuildOptions`
- `NmeaDerivedData`, `NmeaTransmissionModels`
- `ProjectModels`, `NmeaSentenceId`, `OutputChannelModels`

### Service

- 화면 워크플로우: `MainWorkflowService`
- Sentence 생성/합성: `NmeaSentenceBuilderService`, `SentenceComposerService`, `SentenceCatalogService`
- 통신: `NmeaTransmissionService`, `OutputChannelService`, `SerialPortHubService`, `UdpService`
- 설정/환경: `NmeaSenderConfigService`, `PortBaudRateService`, `BaudRateSettingService`
- 입력/데이터: `SharedMemoryProviderService`, `ManualInputMapperService`

## 4. View-ViewModel 매핑 (App.xaml)

`App.xaml` 리소스에 DataTemplate을 등록하여, ViewModel 타입에 맞는 View가 자동 선택됩니다.

- `TopToolbarViewModel` -> `TopToolbarView`
- `ManualLogViewModel` -> `ManualLogView`
- `SentencePanelViewModel` -> `SentencePanelView`

## 5. 인터페이스 설명 (`Services/Interfaces`)

| Interface | 설명 |
|---|---|
| `IApplicationLifecycleService` | 앱 종료 요청을 추상화합니다. |
| `IBaudRateSettingService` | 설정 다이얼로그(baud/문장별 UDP 포트) 표시와 결과 반환을 담당합니다. |
| `IIniFileService` | INI 읽기/쓰기/병합 공통 기능을 제공합니다. |
| `IMainWorkflowService` | START/STOP, 설정 적용, 주기 송신 등 화면 워크플로우를 총괄합니다. |
| `IManualInputMapperService` | 수동 입력값 ↔ `NmeaDataDto` 매핑을 담당합니다. |
| `INmeaSenderConfigService` | 송신 설정(포트, 플래그, baud, UDP 등) 로드/저장을 담당합니다. |
| `INmeaSentenceBuilderService` | Sentence ID별 NMEA 원문/체크섬 생성 로직을 담당합니다. |
| `INmeaTransmissionService` | START/STOP/주기 송신 및 COM/UDP 전송 흐름 처리를 담당합니다. |
| `IOutputChannelService` | COM/UDP 채널 오픈·클로즈·쓰기 동작을 통합 제공합니다. |
| `IPortBaudRateService` | 포트별 baudrate 스냅샷/검증/적용/해결 로직을 담당합니다. |
| `ISentenceCatalogService` | 기본 Sentence 목록 및 행 생성 규칙을 제공합니다. |
| `ISentenceComposerService` | 데이터 기반 문장 생성 + Preview 텍스트 반영을 담당합니다. |
| `ISerialPortCatalogService` | 시스템 COM 포트 스캔/정렬/유효 포트 선택을 담당합니다. |
| `ISerialPortHubService` | `SerialPort` 수명주기/쓰기 처리를 담당합니다. |
| `ISharedMemoryProviderService` | Shared Memory에서 선박 데이터 읽기를 담당합니다. |
| `IUdpService` | UDP 오픈/송신/종료를 담당합니다. |

## 6. 설정 파일

기본 파일: `NMEASender.Wpf.ini`

주요 섹션:

- `[CONFIG]`: TITLE, Project (`USE UDP`는 하위 호환용으로 유지)
- `[GPS CONFIG]`: 기본 시리얼 옵션, SEND FLAG, UDP SEND FLAG
- `[SOCKET]`: 기본 UDP 포트
- `[SENTENCE PORTS]`: Sentence 행별 COM 포트
- `[UDP PORTS]`: Sentence 행별 UDP 포트
- `[BAUD RATE]`: COM 포트별 BaudRate

현재 UDP 송신 활성 여부는 전역 토글이 아니라, 각 문장의 `UDP 체크` 상태로 결정됩니다.

## 7. 빌드

```bash
dotnet build
```

실행 중 파일 잠금이 있을 때는 별도 출력 경로로 검증 가능합니다.

```bash
dotnet build NMEASender.Wpf.csproj --no-restore -p:OutputPath=bin\\BuildCheck\\ -p:UseAppHost=false
```

# NMEASender WPF 운영 매뉴얼

## 1. 목적

이 문서는 `NMEASender.Wpf`를 실행하고 설정하는 기본 절차를 정리한 운영용 매뉴얼이다.

대상:

- 통합/시뮬레이터 연동 테스트 담당자
- COM/UDP 송신 설정 담당자
- 신규 입사자 온보딩

## 2. 실행 전 준비

1. 공유메모리 소스(iOS/시뮬레이터) 또는 수동 입력(TEST) 중 사용할 모드를 결정한다.
2. 필요한 COM 포트가 OS에 정상 인식되는지 확인한다.
3. UDP 수신 측 포트/주소(Broadcast 또는 Multicast)를 사전에 확인한다.
4. `NMEASender.Wpf.ini`, `UDPConfig.ini`가 배포 환경에 존재하는지 확인한다.

## 3. 화면 구성

- 상단 툴바: START/STOP, 기본 COM/UDP, Source 선택, Sentence 검색, Settings
- 좌측 패널: 수동 데이터 입력/조회(TEST 모드용)
- 중앙 패널: Sentence별 COM/UDP 체크, 포트 선택, NMEA 미리보기
- 설정창: COM BaudRate, UDP 전송 모드, Sentence UDP Endpoint

## 4. 기본 송신 절차

1. Source 선택
   - `By IOS`: 공유메모리 데이터 사용
   - `TEST`: 수동 입력 데이터 사용
2. 필요한 Sentence 행에서 `IsComEnabled`/`IsUdpEnabled` 체크
3. COM 송신 시 각 행의 COM 포트 선택
4. UDP 송신 시 기본 UDP 포트 또는 Settings의 Sentence UDP Port 확인
5. `START` 클릭
6. 로그/미리보기/NMEA 수신측에서 정상 수신 확인
7. 중지 시 `STOP` 클릭

참고:

- COM이 없어도 UDP가 체크되어 있으면 UDP only로 동작 가능
- START 중 COM/UDP 체크 변경 시 동적으로 반영됨

## 5. Sentence 검색 사용법

### 5.1 메인 화면 검색

위치:

- 상단 툴바 `Settings` 버튼 왼쪽 검색창

동작:

- 입력 키워드 포함 항목만 `GPS Sentence`/`Other Sentence`에 표시
- 검색 대상:
  - Sentence Label
  - Sentence ID
  - NMEA #1 (`PrimaryText`)
  - NMEA #2 (`SecondaryText`)
- 검색어가 있으면 `x` 버튼 표시
- `x` 클릭 시 검색어 즉시 초기화

### 5.2 Settings 검색

위치:

- `Settings` > `Sentence UDP Endpoint` 섹션 상단 검색창

동작:

- 입력 키워드 포함 Endpoint 행만 표시
- 검색 대상:
  - `RowKey`
  - `SentenceLabel`
  - `UdpPort`
  - `UdpAddress`
- 검색어가 있으면 `x` 버튼 표시
- `x` 클릭 시 검색어 즉시 초기화

## 6. Settings 설정 절차

### 6.1 COM Port BaudRate

1. `Settings` 열기
2. `COM Port BaudRate`에서 포트별 속도 선택
3. `Save` 클릭
4. START 중이었다면 재시작 후 반영 여부 확인

### 6.2 UDP Transport

1. `Broadcast` 또는 `Multicast` 선택
2. Multicast 모드면 주소 입력
3. 주소 범위 확인: `224.0.0.0` ~ `239.255.255.255`
4. `Save` 클릭

### 6.3 Sentence UDP Endpoint

1. 문장별 UDP 포트 입력(`1~65535`)
2. (Multicast 지원 프로젝트 + Multicast 모드) 문장별 주소 입력
3. 필요 시 검색창으로 문장 필터링 후 수정
4. `Save` 클릭

## 7. TEST 모드 수동 데이터

1. Source를 `TEST`로 변경
2. 수동 입력 영역에 좌표/속도/방위 입력
3. `Set Data`로 값 반영
4. Sentence 미리보기 갱신 확인
5. `START` 후 송신 데이터 확인

## 8. 장애 대응 체크리스트

### 8.1 문장이 안 보낼질 때

1. 해당 행 COM/UDP 체크 여부
2. Source가 의도한 모드인지
3. By IOS 모드면 Fail 플래그(GPS/Gyro/Log/Echo) 영향 여부 (NMEASender.Wpf.ini)
4. 포트/주소/방화벽/수신 프로그램 상태

### 8.2 COM 문제

1. 포트 점유 여부(타 프로그램)
2. 포트 BaudRate 불일치
3. START 로그의 Open Fail 메시지 확인

### 8.3 UDP 문제

1. UDP 포트 값 범위 확인
2. Broadcast/Multicast 모드 일치 확인
3. Multicast 주소 범위 확인
4. 수신 측 네트워크 인터페이스/구독 확인

## 9. 배포/검증 최소 시나리오

1. 앱 실행
2. TEST 모드 문장 미리보기 확인
3. 메인 Sentence 검색 동작 확인
4. Settings Endpoint 검색 동작 확인
5. COM 1개 문장 송신 확인
6. UDP 1개 문장 송신 확인
7. STOP 후 재START 정상 확인

## 10. 관련 문서

- 개발 인수인계: `docs/HANDOVER.md`
- 프로젝트 개요: `README.md`

# NMEASender_WPF

WPF 기반 NMEA 송신 도구입니다.  
COM Port / UDP Broadcast 전송을 지원하며, 문장 단위 제어와 실시간 프리뷰를 제공합니다.

## Overview

- 프로젝트명: `NMEASender_WPF`
- 플랫폼: `.NET 8.0` + `WPF`
- 주요 전송 방식: `Serial(COM)` / `UDP Broadcast`
- 용도: 시뮬레이터 연동 NMEA 문장 송신 및 테스트

## Key Features

- 문장별 활성/비활성 체크
- GPS / Other Sentence 구분 관리
- `ALL Sentence Check`로 전체 체크/해제
- 문장별 COM Port 개별 지정
- `+` 버튼으로 같은 Sentence 행 복제
- 복제 행 `-` 버튼으로 삭제
- 같은 Sentence를 여러 COM 포트로 동시 송신 가능
- START/STOP 기반 송신 제어
- UDP 사용 여부를 실행 중에도 On/Off 가능
- 로그 실시간 표시 및 자동 최신 위치 추적
- 로그 수동 탐색 후 하단 복귀 시 자동 추적 재개
- 커스텀 스크롤바 스타일 적용

## Architecture (MVVM)

- View: `MainWindow.xaml`
- ViewModel: `MainViewModel`
- Model: `NmeaDataDto`, `SentenceItem`, Native shared-memory models
- Services:
- `SentenceComposerService` (문장 프리뷰/전송 판단)
- `SentenceCatalogService` (Sentence 행 구성)
- `SerialPortCatalogService` (포트 탐색/정렬/선택)
- `ManualInputMapper` (수동 입력 ↔ DTO 매핑)
- `SerialPortHub`, `UdpBroadcastSender`, `SharedMemoryNmeaDataProvider`

## Configuration

- 설정 파일: `NMEASender.Wpf.ini`
- 기본 항목:
- 기본 COM 포트
- Sentence별 포트
- UDP 사용 여부 / 포트
- 전송 플래그
- 기타 송신 옵션

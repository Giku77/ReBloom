# 🌱 Re:Bloom
> **오염된 폐허 도시를 ‘생존 거점’으로 되살리는 크로스플랫폼(PC/모바일) 생존·크래프팅 게임**

![Key Art](docs/images/keyart.png)

## 🔗 Links
- 🎥 [**Gameplay Video**](https://youtu.be/CT6LHDLgIzk)
- 📚 [**Tech Docs**](https://giku77.github.io/rebloom-tech-notes/)

---

## 📌 Overview
- **Genre**: Survival / Crafting / Base-building  
- **Engine**: Unity (URP)  
- **Platform**: Windows, Android  
- **Team**: 7 (Dev 3 / Design 4)  
- **Duration**: 8 weeks  

---

## 🎮 Core Loop
1. **Explore**: 폐허 탐험 → 자원 수집  
2. **Build**: 그리드 기반 거점 확장 / 시설 배치  
3. **Grow**: 농사 & 자동화 → 안정적 생산  
4. **Restore**: 거점 녹지화(진행/성장 체감) → 엔딩 조건 달성

---

## ✨ Key Features

- **그리드 기반 모듈형 건축**
  - 프리뷰/스냅/회전/편집(이동·삭제)까지 일관된 배치 UX
  - “왜 설치가 안 되는지”를 에러 코드로 즉시 안내(토스트/피드백)
  - 🔗 Code: [Build System](./ReBloom/Assets/Scripts/Build/)

- **상태 기반 농사 + 자동화(스프링클러/드론)**
  - Tick 기반 성장/수확, 대규모 밭에서도 스파이크를 억제하는 분산 처리
  - 저장 가능성 검증으로 유실 방지(수납 가능할 때만 확정)
  - 🔗 Code: [Farm System](./ReBloom/Assets/Scripts/Farm/)

- **Local-First + Remote Sync(PlayFab) 세이브/로드**
  - 로컬 우선 로드로 빠른 체감, 최신본 기준 자동 동기화
  - 실패 시 Pending 업로드로 재시도/복구
  - 🔗 Code: [Save/Load](./ReBloom/Assets/Scripts/SaveAndLoad/)

- **UI 프레임워크(입력/커서/모달/ESC 스택 중앙 정책)**
  - UI 충돌을 중앙에서 통합 제어(게임 입력 차단, 커서 모드, 닫힘 우선순위)
  - 🔗 Code: [UI Manager](./ReBloom/Assets/Scripts/UIManager/)

- **데이터 드리븐 콘텐츠 파이프라인(BGDATA)**
  - 테이블 기반 데이터 로딩 → 컬렉션 캐싱 후 런타임에서 조회/사용
  - 밸런싱/콘텐츠 추가 시 코드 수정 최소화
  - 🔗 Code: [DB Layer](./ReBloom/Assets/Scripts/Quest/DB/)

---

## 📈 Results
- **Cross-platform 튜닝**: PC/Mobile 품질 프리셋 분리로 안정적 플레이 경험 확보
- **Performance**: 메모리/스파이크 개선 및 프로파일링 기반 최적화
- **Ops**: 자동 리포팅/문서화로 개발 리드타임 단축

---

## 🛠️ Tech Stack
- C#, Unity(URP)
- PlayFab(Cloud Save/Storage)
- Git / Git LFS
- Linear, Notion, (선택) 자동 문서화 파이프라인

---

## ▶️ How to Run
1. Unity 버전: 6000.0.60f1
2. `ReBloom/` 폴더를 Unity로 열기
3. `TitleScene` 실행

---

## 📷 Media
![Gameplay1](docs/images/gameplay_01.gif)
![Gameplay2](docs/images/gameplay_02.gif)
![Gameplay3](docs/images/gameplay_03.gif)

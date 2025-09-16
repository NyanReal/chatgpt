# Chaos Flesh Muscle Simulation Tutorial (5.6) — 한국어 번역본/가이드

> 원문: *Chaos Flesh Muscle Simulation Tutorial (5.6) | Epic Developer Community*  
> 작성: Physics Engineer **Yushan Han** · 최초 게시 **2025-07-15**, 마지막 업데이트 **2025-07-18**  
> 대상 버전: **Unreal Engine 5.6** (Experimental 기능 포함)

---

## 개요

이 튜토리얼은 **Chaos Flesh**를 사용해 근육(muscle)과 지방(fat) 시뮬레이션을 설정하고, 그 결과로부터 **ML Deformer** 학습 데이터를 준비하는 전체 파이프라인을 설명합니다.  
원문 예시에서는 *The Witcher 4* UE5 기술 데모 속 **Kelpie**(Ciri의 말) 사례를 통해, Chaos Flesh 시뮬 데이터로 훈련된 ML Deformer가 **0.3 ms** 만에 피부에 **근수축, 탄성, 충돌, 접촉** 효과를 재현하는 과정을 소개합니다. 본 튜토리얼의 실습 대상은 에픽에서 제공하는 **Emil** 캐릭터입니다.

---

## UE 5.6에서의 Chaos Flesh 주요 업데이트

- (5.6) **슬라이딩(siding)** 및 **고정 길이 스프링(fixed-length spring)** 제약이 근육용으로 도입
- 근육별(Per-muscle) 파라미터 세분화 및 품질-오브-라이프 향상(캐시 컨트롤, 재시작, 시간 추정 등)
- **Length-Based Activation**(길이 기반 활성화) 지원 및 **애니메이션 커브 기반 활성화** 연동
- **Quasi-static** 해법을 통한 포즈별 결정론적 결과(학습 데이터 일관성 향상)
- 근육 수축 시 **Contraction Volume Scale**(체적 보존/증감) 제어

> ※ Chaos Flesh는 5.6 시점 **Experimental**입니다.

---

## 예제 프로젝트 & 리소스

- **예제 리소스(Emil 자산 포함)**
  - ZIP: `https://d1iv7db44yhgxn.cloudfront.net/post-static-files/Fleshtutorials-ExampleContent.7z`
  - 압축 해제 후 `ExampleContent/5_5_ChaosFlesh` 폴더를 프로젝트의 `Content/ExampleContent` 아래로 이동
- **필수 플러그인**
  - *Chaos Flesh*, *ML Deformer Framework*, *ML Deformer Neural Morph Model*
- **권장 프로젝트 생성 절차**
  1) 빈 UE5.6 프로젝트 생성 → 위 플러그인 3종 활성화  
  2) 에디터 재시작 → ExampleContent 복사/배치 → Emil 관련 애셋 로드

---

## 전체 파이프라인 한눈에 보기

1. **근육 메쉬 테트라라이즈**(Tetrahedralization) 및 Flesh Asset 생성  
2. **제약 구성**: 키네마틱 구동 + 스프링 결속 → **Quasi-static** 시뮬 캐시화  
3. **근기시/정지(Origin/Insertion) 페인트 → Fiber Field 계산 → 길이 기반 활성화**  
4. **부분 시뮬**로 파라미터 튜닝(근육 서브셋만 선택)  
5. **지방(Fat) 테트라 메쉬 생성** 및 **근육–지방 커플링**  
6. **스킨 지오메트리 캐시** 생성(표면 변형 캡처)  
7. **ML Deformer 학습 세트** 구성 및 학습(버텍스 델타 학습)  
8. (옵션) **커브-드리븐 제어**: 근육명과 1:1로 매칭된 애니메이션 커브로 실시간 구동

---

## 1) 근육 테트라 메쉬 생성

- Emil의 **근육 SkeletalMesh**를 **Geometry Collection**으로 변환 → **Tetra Mesh** 생성 → **Flesh Asset**으로 저장
- 대표 노드·액션
  - `SkeletalMesh` / `SkeletalMeshToCollection`
  - `CreateTetrahedron`
  - `TransferVertexAttribute` (웨이트/색상 등 전이)
  - `FleshAssetTerminal` (애셋 저장)
- 팁
  - 테트라 메쉬는 토폴로지가 결과에 직접 관여하므로, 반복 실험 간 **비결정적 변화**를 줄이도록 동일 조건 유지 권장

---

## 2) 제약(Constraints) 구성 & Quasi-static 시뮬

- **키네마틱 제약**: 스켈레톤 애니메이션(LBS)으로 근육을 구동
- **스프링 제약**: 근막처럼 근육끼리 **약하게 결속**
  - 정점–정점, 정점–삼각형, 정점–테트라 등 다양한 바인딩
- **Quasi-static**: 동역학(관성) 제거 → 포즈별 **결정론적 결과** → ML 학습에 최적
- 초기값 예시(가이드)
  - Substeps: **5**
  - Solver Iterations: **20**
  - Quasi-statics: **On**
  - Gauss-Seidel: **On**
  - Gravity: **Off**
  - 캐시: 0–3 s 구간(또는 60–90프레임) 테스트
- 물성/강성(예시, 상황에 맞게 조정)
  - 밀도(근육): ~**1000**
  - 강성(근육): ~**100000**
  - Position Target: Stiffness **10**, Search Radius **1.0**

---

## 3) Fiber Field & 길이 기반 활성화

- **근기시(origin) / 정지(insertion)**를 **SkeletalMesh에서 페인트** → 테트라로 **전이**
- 전이된 스칼라 필드(기시~정지 구배)를 기반으로 **섬유 방향(Fiber Field)** 계산
- 활성화(Activation)
  - **Do Length-Based Muscle Activation = On**
  - 섬유 방향 **수축 + 수직 방향 팽창**
  - **Contraction Volume Scale**로 체적 보존/증감 제어(1 = 보존)
- 대표 노드
  - `PaintWeightMap`
  - `VertexScalarToVertexIndices`
  - `ComputeFiberField` (예: Iteration 500)
  - `VisualizeFiberField`
  - `ComputeMuscleActivationData`
  - `SetMuscleActivationParameter`

> 실무 팁: 근기시/정지 페인트는 **SkeletalMesh 쪽에서** 하고, 이후 테트라로 전이하면 반복 실험 시 유지·관리 용이.

---

## 4) 부분 시뮬로 빠른 반복

- 전체가 아닌 **관심 근육만 선택**해 빠르게 파라미터 튜닝
- 대표 노드
  - `DeleteFleshVertices`
  - `CollectionSelectByAttr` / `CollectionSelectInvert`

---

## 5) 지방(Fat) 커플링

- **지방 StaticMesh**를 다운샘플 → **Collection 변환 → 테트라라이즈**
- **근육 내부면 ↔ 지방** 사이를 **스프링**으로 결속 → 근육 수축 시 지방이 동반 변형
- 예시 파라미터(지방용, 상황에 맞게 조정)
  - 밀도: **100**
  - 강성: **10000**
  - 스프링: Stiffness **10**, Search Radius **0.5**
- 대표 노드
  - `StaticMesh` / `StaticMeshToCollection`
  - `SetFleshDefaultProperties`
  - `AppendTetrahedralCollection`
  - `SetVertexTrianglePositionTargetBinding` (내면 바인딩)
  - `DeleteVertexTrianglePositionTargetBinding` (특정 스프링 해제)

---

## 6) 스킨 지오메트리 캐시 생성

- 근육·지방 **Quasi-static 캐시**에서 **표면(Skin) 변형**을 추출해 **지오메트리 캐시** 생성
- 대표 노드
  - `GenerateSurfaceBindings`

---

## 7) ML Deformer 학습

- 입력: 스켈레톤 포즈(애니메이션) + **지오메트리 캐시**(표면 변형 결과)
- 학습 대상: 스킨 버텍스의 **델타(△)** — Chaos Flesh가 생성한 근수축·탄성·충돌·접촉 등의 결과
- **훈련 분포** 권장
  - 실제 게임 플레이에서 나올 **자세/동작 분포**를 넓게 포함(수천 프레임)
  - Kelpie 사례: **게임플레이 포즈 중심 ~5000 프레임** 샘플
- 절차
  1) 학습용 애니메이션 세트 준비(ROM + 동작)  
  2) 지오메트리 캐시에서 버텍스 델타 생성  
  3) **ML Deformer(Neural Morph)** 설정 → 학습  
  4) 추론에서 스켈레톤 포즈만으로 고품질 표면 변형 재현

---

## 8) 커브-드리븐 ML Deformer (선택)

- **애니메이션 커브**를 각 **근육명 1:1**로 매칭해 **독립적인 근활성** 제어
- 예시: Witcher 4 데모에서 **30개 커브**로 주요 근군 활성화
- 설정
  - **Override Muscle Activation With Animated Curves = On**
  - 스켈레톤/메시에 커브 추가 → 근육명으로 매칭 → 훈련 데이터 구성
- 대표 노드
  - `ReadSkeletalMeshCurves`
  - `CurveSamplingAnimationAssetTerminal`

---

## 파라미터 빠른 참고

- **안정화**: Substeps↑, Iterations↑ (시간 증가와 트레이드오프)
- **Quasi-static + 캐시**: 프레임별 결과를 고정 → 학습 일관성↑
- **Gauss-Seidel 솔버**: 비선형 재료(하이퍼엘라스틱)에 적합
- **Contraction Volume Scale**:  
  - 1 = 체적 보존  
  - >1 = 체적 증가(과장된 팽창)  
  - <1 = 체적 감소

---

## 블렌더 ↔ UE 워크플로 팁

- **페인트는 SKM에서**: 테트라 메쉬 재생성 시 페인트 재작업을 줄이기 위해 **SkeletalMesh**에서 페인트 후 **전이**를 권장
- **결정론성 확보**: 학습 데이터는 **Quasi-static 캐시**를 사용하여 포즈-결과 일치 유지
- **포즈 커버리지**: 최종 사용 환경(게임 플레이)의 포즈 분포를 충분히 포함하도록 **훈련 애니메이션** 구성

---

## 트러블슈팅

- 시뮬이 **흔들리거나 수렴이 불안정**: Substeps/Iterations 증가, 스프링 강성 조절, Search Radius 재조정
- **근육이 과도하게 수축/팽창**: Contraction Volume Scale 재조정, Length-Based Activation 커브 보정
- **지방이 표면에서 미끄러짐**: 내부면 바인딩(스프링) 밀도/강성 재설계, 슬라이딩 제약 검토
- **학습 품질 저하**: 훈련 포즈 다양성 부족 → ROM 확대, 캐시/포즈 정합성 재검증

---

## 용어 정리

- **Flesh**: 테트라 메쉬 기반의 연조직 시뮬레이션 시스템(근육/지방 등)
- **Quasi-static**: 관성 제거 후 포즈별 평형 해 탐색(결정론적 결과)
- **Fiber Field**: 기시~정지 구배를 이용해 계산된 근섬유 방향장
- **Activation**: 근섬유 방향 수축과 수직 방향 팽창의 강도 제어
- **Surface Geometry Cache**: 표면 변형을 시간에 따라 저장한 캐시
- **ML Deformer**: 물리 시뮬의 버텍스 델타를 학습해 런타임으로 대체하는 프레임워크

---

## 부록: 에디터 내 주요 노드/패널 체크리스트

- 생성/변환: `SkeletalMeshToCollection`, `StaticMeshToCollection`  
- 테트라: `CreateTetrahedron`, `AppendTetrahedralCollection`  
- 속성/전이: `SetFleshDefaultProperties`, `TransferVertexAttribute`  
- 구속/바인딩: `SetVertexTrianglePositionTargetBinding`, `DeleteVertexTrianglePositionTargetBinding`  
- 근섬유/활성화: `ComputeFiberField`, `ComputeMuscleActivationData`, `SetMuscleActivationParameter`  
- 커브 샘플링: `ReadSkeletalMeshCurves`, `CurveSamplingAnimationAssetTerminal`  
- 표면/캐시: `GenerateSurfaceBindings`

---

## 참고 링크

- Example Content(Emil 포함):  
  `https://d1iv7db44yhgxn.cloudfront.net/post-static-files/Fleshtutorials-ExampleContent.7z`
- 관련 발표: **GDC 2023** ML Deformer Talk (Kelpie 데모 배경)

---

### 라이선스/주의
- 본 문서는 사용자가 업로드한 자료를 바탕으로 한 **비상업적 개인 학습용 번역/가이드**입니다. 원저작권은 Epic Games 및 원 저자에게 있습니다.
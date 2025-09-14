# UE5 골반 헬퍼본 런타임 제어 튜토리얼

> 블렌더에선 헬퍼본에 **웨이트만**, 동작은 **UE5(Post Process Anim BP / Control Rig / Pose Driver)** 에서 구동하는 워크플로우에 맞춘 선별 목록입니다.

| 미리보기 | 제목 | 한국어 요약 설명 |
|---|---|---|
| <a href="https://www.youtube.com/watch?v=52wKKVbzyk8" target="_blank"><img src="https://img.youtube.com/vi/52wKKVbzyk8/mqdefault.jpg" width="220" alt="Unreal Engine 5: Post Process Corrective Bone Pose Drivers thumbnail"></a> | [Unreal Engine 5: Post Process Corrective Bone Pose Drivers](https://www.youtube.com/watch?v=52wKKVbzyk8) | Post Process 애님BP에서 포즈 에셋/포즈 드라이버를 이용해 교정 본·모프를 런타임으로 구동하는 전체 흐름. 골반/허벅지 교정의 기본기를 한 번에 이해하기 좋음. |
| <a href="https://www.youtube.com/watch?v=41aIf688y5Y" target="_blank"><img src="https://img.youtube.com/vi/41aIf688y5Y/mqdefault.jpg" width="220" alt="Pt 6: Adding Leg Twists using Post Process Anim BP thumbnail"></a> | [Pt 6: Adding Leg Twists using Post Process Anim BP](https://www.youtube.com/watch?v=41aIf688y5Y) | 허벅지 트위스트 본을 추가하고 Post Process 애님BP로 각도를 읽어 분산 구동. I-포즈처럼 각도 큰 동작에서 허벅지 비틀림을 안정화하는 실전 세팅. |
| <a href="https://www.youtube.com/watch?v=_VGr4nmEuRg" target="_blank"><img src="https://img.youtube.com/vi/_VGr4nmEuRg/mqdefault.jpg" width="220" alt="Pose Assets & Pose Drivers | Unreal Engine Tutorial thumbnail"></a> | [Pose Assets & Pose Drivers | Unreal Engine Tutorial](https://www.youtube.com/watch?v=_VGr4nmEuRg) | 포즈 에셋 제작부터 Pose Driver로 본/커브(모프)를 구동하는 방법. 여러 각도 구간별 교정(골반 앞·뒤·옆)을 포즈 타깃으로 분리해 다루는 기초. |
| <a href="https://www.youtube.com/watch?v=6s8rCGtmCSA" target="_blank"><img src="https://img.youtube.com/vi/6s8rCGtmCSA/mqdefault.jpg" width="220" alt="Corrective Poses with Control Rig – Spherical Pose Reader thumbnail"></a> | [Corrective Poses with Control Rig – Spherical Pose Reader](https://www.youtube.com/watch?v=6s8rCGtmCSA) | Control Rig의 구면 포즈 리더로 허벅지 회전 방향·범위를 읽어 교정 포즈/모프를 구동. Pose Driver의 대안으로 채널 분해가 쉬움. |
| <a href="https://www.youtube.com/watch?v=WA-1uA3O8iw" target="_blank"><img src="https://img.youtube.com/vi/WA-1uA3O8iw/mqdefault.jpg" width="220" alt="How to Create Pose Drivers in UE5 with Mesh Morpher thumbnail"></a> | [How to Create Pose Drivers in UE5 with Mesh Morpher](https://www.youtube.com/watch?v=WA-1uA3O8iw) | Mesh Morpher 플러그인으로 엔진 내부에서 보정 모프를 만들고 Pose Driver처럼 연동. 블렌더에선 헬퍼본에 웨이트만 두고 UE에서 교정 처리할 때 유리. |
| <a href="https://www.youtube.com/watch?v=620kdw963dY" target="_blank"><img src="https://img.youtube.com/vi/620kdw963dY/mqdefault.jpg" width="220" alt="Enhanced Pose Driver Editor in UE5 with Mesh Morpher thumbnail"></a> | [Enhanced Pose Driver Editor in UE5 with Mesh Morpher](https://www.youtube.com/watch?v=620kdw963dY) | Mesh Morpher의 강화된 Pose Driver 에디터 UI 소개. 포즈 타깃/커브 값을 시각적으로 다루며 I-포즈 같은 극단 자세 튜닝 반복 속도 향상. |
| <a href="https://www.youtube.com/watch?v=zk3CS-9ID2U" target="_blank"><img src="https://img.youtube.com/vi/zk3CS-9ID2U/mqdefault.jpg" width="220" alt="Driving Morph Targets from Control Rig in Unreal Engine thumbnail"></a> | [Driving Morph Targets from Control Rig in Unreal Engine](https://www.youtube.com/watch?v=zk3CS-9ID2U) | Control Rig 컨트롤(또는 본 각도)로 모프 타깃을 직접 구동하는 패턴. 골반 앞·옆 라인 보정 모프를 런타임에 안정적으로 제어. |
| <a href="https://www.youtube.com/watch?v=VU5egEEolsI" target="_blank"><img src="https://img.youtube.com/vi/VU5egEEolsI/mqdefault.jpg" width="220" alt="UE5: Control Rig – Simple Twist Bones thumbnail"></a> | [UE5: Control Rig – Simple Twist Bones](https://www.youtube.com/watch?v=VU5egEEolsI) | Control Rig에서 간단한 트위스트 본 구현. 허벅지 상·중·하단에 비틀림을 분산해 골반 주변 찌그러짐을 줄이는 기본 설계. |

---
### 사용 팁
- 블렌더에선 헬퍼본에 **웨이트만** 배정하고, 본 이름을 UE에서 그대로 인식 가능하게 유지하세요.
- UE5에서는 **Post Process 애님BP** 또는 **Control Rig**에서 허벅지 회전(RX/RY/RZ)을 읽어 **Bone Driven Controller / Pose Driver / 구면 포즈 리더**로 헬퍼본·모프를 구동하세요.
- I-포즈(하이킥/사이드 스플릿)와 같이 각도가 큰 포즈를 **테스트 포즈 세트**로 마련해 빠르게 반복 튜닝하는 것이 핵심입니다.

> 썸네일 이미지는 YouTube 제공 정적 썸네일(`img.youtube.com/vi/<ID>/mqdefault.jpg`)을 사용합니다
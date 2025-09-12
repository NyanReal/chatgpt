# UE 톤매퍼/컬러그레이딩 **역산으로 원색 보존** 가이드

“톤매퍼/컬러그레이딩이 씌워진 뒤의 화면색”을 기준으로 **역산해서 원래(의도한) sRGB 색을 보존**하는 대표적인 방법을 3가지로 정리했습니다. 상황에 따라 하나만 쓰거나 섞어 쓰면 됩니다.

---

## 1) 톤매퍼 자체를 교체(Replace)해서 전역 무효화

- **어디를 건드리나:** Post Process용 머티리얼 하나를 만들고 **Material Domain = Post Process**, **Blendable Location = Replacing Tonemapper** 로 설정.
- **왜:** 이 위치는 엔진 기본 ACES 톤매퍼 대신 **직접 만든 “출력 변환”**을 쓰므로, SceneColor(HDR, 선형)를 받아서 곧장 LDR sRGB로 변환해 **필름 커브/컬러그레이딩 영향이 0**이 됩니다.
- **간단 처리식(커스텀 노드 HLSL or 머티리얼 노드):**
  - 노출 고정: PPV에서 **Exposure → Min/Max EV100 같은 값**으로 잠가 자동노출 변동 제거.
  - **Linear → sRGB 감마**만 적용: `c = saturate(c / WhitePoint);` (필요시 스케일) → `c_srgb = LinearToSRGB(c)`.
  - 블룸/비네트/샤픈 등 후처리 0으로.
- **주의:** 전역 룩이 평탄해지므로 게임 룩이 바뀝니다. “특정 요소만 원색 유지”가 목적이면 2)나 3)가 보통 더 적합합니다.

---

## 2) “역톤매핑(Pre-encode)”으로 **특정 머티리얼만** 원색 보존

> 목표 sRGB 색이 엔진 톤매퍼(+색보정)를 통과한 **최종 화면**에서 그대로 보이게, **머티리얼에서 미리 반대로 뒤틀어** 내보내는 방식.

- **어디를 건드리나:** 원색을 유지하고 싶은 머티리얼(대개 Unlit)에서 **Emissive** 경로를 사용하고, **View Property → PreExposure**를 곱해 UE5의 프리익스포저를 보정합니다.
- **왜:** UE 파이프라인(PreExposure → ACES RRT+ODT → sRGB 감마 → 컬러그레이딩)을 **거꾸로** 적용하면 최종 화면에서 목표 sRGB가 그대로 나옵니다.
- **실전 레시피(핵심 수식):**
  1. 목표 sRGB(0~1) → 선형: `C_lin = srgb_to_linear(C_srgb)`  
  2. **ACES 톤매퍼 역함수**: `C_hdr = ACES_inverse(C_lin)`  
  3. **프리익스포저 보정**: `C_hdr *= View.PreExposure`  
  4. Emissive에 `C_hdr` 출력 (Shading Model=Unlit 권장)
- **커스텀 노드(HLSL) 예시(뼈대):**

```hlsl
// ---- sRGB <-> Linear ----
float srgbToLinear1(float c){ return c <= 0.04045 ? c/12.92 : pow( (c+0.055)/1.055, 2.4 ); }
float3 SrgbToLinear(float3 c){ return float3(srgbToLinear1(c.r), srgbToLinear1(c.g), srgbToLinear1(c.b)); }

// ---- ACES Fitted (UE와 동일 계수 사용) ----
static const float3x3 ACESIn  = float3x3(
    0.59719, 0.35458, 0.04823,
    0.07600, 0.90834, 0.01566,
    0.02840, 0.13383, 0.83777);

static const float3x3 ACESOut = float3x3(
    1.60475, -0.53108, -0.07367,
   -0.10208,  1.10813, -0.00605,
   -0.00327, -0.07276,  1.07602);

float3 RRTAndODTFit(float3 v){
    float3 a = v * (v + 0.0245786) - 0.000090537;
    float3 b = v * (0.983729*v + 0.4329510) + 0.238081;
    return a / b;
}

float3 ACESFittedForward(float3 cLin){    // Linear HDR -> Linear Display
    cLin = mul(ACESIn, cLin);
    cLin = RRTAndODTFit(cLin);
    return mul(ACESOut, cLin);
}

// 역함수: 전방함수 기반 이분탐색으로 근사
float3 RRTAndODTFitInv_Bisect(float3 y){
    float3 lo = 0, hi = 64; // HDR 상한은 필요에 맞게
    [unroll] for(int i=0;i<10;i++){
        float3 mid = 0.5*(lo+hi);
        float3 f   = RRTAndODTFit(mid);
        lo = lerp(lo, mid, step(f, y));   // f <= y 면 lo=mid
        hi = lerp(mid, hi, 1-step(f, y)); // f >  y 면 hi=mid
    }
    return 0.5*(lo+hi);
}

// ACES 역: Linear Display -> Linear HDR
static const float3x3 ACESOutInv = float3x3(
    0.695452, 0.140679, 0.163869,
    0.044795, 0.859671, 0.095534,
   -0.005526, 0.004025, 1.001501);

static const float3x3 ACESInInv = float3x3(
    1.60475, -0.53108, -0.07367,
   -0.10208,  1.10813, -0.00605,
   -0.00327, -0.07276,  1.07602);

float3 ACESFittedInverse(float3 cDispLin){ // Linear Display -> Linear HDR
    float3 t = mul(ACESOutInv, cDispLin);
    t = RRTAndODTFitInv_Bisect(t);
    return mul(ACESInInv, t);
}
```

> 머티리얼 그래프 연결 요약:  
> `Target_sRGB(VectorParam) → SrgbToLinear → ACESFittedInverse → * PreExposure → Emissive`

- **주의/팁**
  - **Auto Exposure, Bloom, Color Grading**이 켜져 있으면 목표색이 흔들립니다. 최소한 그 픽셀에 대해선 영향을 꺼야 하며, 3) 방식이 가장 깔끔할 때가 많습니다.
  - 반사/글로우에 과한 HDR이 들어가면 주변에 누출될 수 있어 **Unlit + Opaque/Masked**로 단순 출력 권장.

---

## 3) “사후 합성(After Tonemapping)”으로 **안전하게 오버레이**

- **어디를 건드리나:** Post Process 머티리얼을 **Blendable Location = After Tonemapping**으로 두고, **마스크(예: CustomDepth/Stencil)**를 이용해 해당 픽셀만 **원본 텍스처(sRGB)**를 덮어씁니다.
- **왜:** 톤매핑/컬러그레이딩/노출이 끝난 **최종 LDR** 위에 올리므로 **원색 유지 100%**. UI, 브랜드 컬러, 라벨 등에 최적.
- **구성 예:**
  1. 원색 보존 오브젝트에 **Render CustomDepth + CustomStencil** 설정
  2. PostProcess(After Tonemapping)에서 **SceneTexture:PostProcessInput0**을 베이스로
  3. **SceneTexture:CustomStencil**로 마스크를 만들어, 마스크 픽셀만 **원본(또는 별도 RT/UMG) 텍스처 샘플**로 치환
- **장점:** 노출/블룸 등 모든 후처리 영향에서 안전  
  **단점:** 3D와 에너지 일관성(광학적 일관성)은 포기하지만 “색 정확도”는 최고

---

## 어떤 걸 쓰면 되나? (선택 가이드)

- **전역 룩을 평탄하게:** 1) Replacing Tonemapper  
- **3D 오브젝트 한정, 화면에서 ‘거의’ 원색:** 2) 역톤매핑(Pre-encode) + (가능하면) 노출 고정/블룸 0  
- **UI/텍스트/브랜드 컬러 절대 보존:** 3) After Tonemapping 합성(마스크 덮어쓰기)

---

### 부록: 빠른 체크리스트

- [ ] PPV에서 **Auto Exposure Off** 또는 EV 고정
- [ ] 블룸/비네트/샤픈 0
- [ ] 필요 시 **Replacing Tonemapper** 또는 **After Tonemapping** 선택
- [ ] 역톤매핑 시 **PreExposure 곱** 반영
- [ ] UI/라벨/정확 색상은 **After Tonemapping**으로 안전 합성
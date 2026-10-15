# ห้องและแสง (ร้านสักโทนมืด)

## วัสดุ (`Materials/Shop/`)
ไม่แก้ material ของ asset ภายนอก ทุกอันสร้างใหม่แล้วใส่ใน scene แทน

| Material | ใช้กับ |
|---|---|
| `M_Shop_Wall` เทาถ่าน (0.16) / `M_Shop_Ceiling` เกือบดำ (0.06) | `Room/Wall_1a*`, `Room/Floor_12a` (เพดาน) |
| `M_Shop_FloorConcrete` ปูนขัดมันเข้ม (0.12) smoothness 0.55 | `Room/Floor_Tile_3b` |
| `M_Shop_WindowNight` | หน้าต่างทั้ง 4 (ข้างนอกเป็นกลางคืน กล้องเคลียร์เป็นสีดำ) |
| `M_Shop_DarkWood` / `M_Shop_Door` | `Cabinet_3c` / ประตู (texture เดิม ย้อมเข้ม) |
| `M_Shop_BlackLeather` | เตียงสัก (`hospital bed`) |
| `M_Shop_Sofa`, `M_Shop_Lounge`, `M_Shop_Stool` | โซฟา Chair15 ×2, Chair48, สตูล Chair43 (texture เดิม ย้อมเข้ม) |
| `M_Shop_CushionRed`, `M_Shop_PillowRed` | เบาะ Cushion02, หมอนเตียง Cushion38 (แดงเข้ม) |
| `M_Shop_BlackMatte` | ถังขยะ, กระถาง, โคม |
| `M_Shop_LampBulb`, `M_Shop_CeilingLampFace`, `M_Shop_FloorLampShade` | หลอด/หน้าโคม/โป๊ะ เรืองแสง (emission, ไม่ส่งผลต่อ bake) |
| `M_LightShaft_Ceiling` / `M_LightShaft_BedLamp` | ลำแสงปลอม (shader `VRTattoo/LightShaft`) |
| `M_Shop_FrameBlack` + `M_Flash_*` | กรอบภาพสีดำเงา + ภาพ flash art |

- รถเข็นสเตนเลส ต้นไม้ และของบนรถเข็นใช้ material เดิม
- `SubmitButton.mat` เปิด emission สีแดง เพราะปุ่มอยู่นอกวงไฟโคม

## ภาพ flash art (`Textures/FlashArt/`)
- สร้างด้วย `python Tools/flash/make_flash_art.py` จากลายใน `Stencils/`: เส้นดำ + สีเหลื่อมนิด ๆ บนกระดาษครีม
- `Painting_4` (ผนังหลัง) = `Flash_Skulls` 4 ลาย, `Painting_5` (ผนังซ้าย) = `Flash_Classics` 4 ลาย, `Painting_2` (หลังเตียง) = `Flash_Teddy`, `Painting_6` = `Flash_SadBoy` (แนวตั้ง 600×1000)
- ไม่ใช้ลายติดลิขสิทธิ์ (`angry_duck`, `ghost_monster`, `trophy`, `cloud`) เปลี่ยนลายได้ที่ `SHEETS` ในสคริปต์ แล้วรันใหม่ (ทับไฟล์เดิม)

## แสง
- **Environment:** ไม่มี skybox, ambient แบบ Flat (0.11, 0.093, 0.08), ปิด fog, Main Camera เคลียร์เป็นสีดำ, `Directional Light` เดิมปิดไว้ (ไม่ได้ลบ)
- **ไฟ realtime ดวงเดียว:** `Lighting/BedLampSpot` spot แบบ **Mixed** (ส่องตรงแบบ realtime, แสงสะท้อน bake) ขาวอุ่น ความแรง 4, มุม 80°, range 3.5 ม., ไม่มีเงา อยู่ใต้โคม `ShopDecor/BedLamp` (สาย + โป๊ะ + หลอดเรืองแสง) เหนือแขนลูกค้า ครอบรถเข็นด้วย
  - `Performance URP Config` (quality Very Low/Low) เปิด Additional Lights = Per Pixel, ต่อ object 1 ดวง, ปิดเงา additional (เดิม Disabled ไฟ spot จะไม่แสดงเลย)
- **ไฟ Baked:**
  - `CeilingWarm_*` (3 ดวง) point ขาวอุ่น เป็น fill หลัก (Sofas 8.4, Door 8.4, Back 6.5) มีตัวโคมเพดาน `ShopDecor/CeilingLamps/CeilingLamp_*` (กล่องดำ + หน้าเรืองแสง)
  - `FloorLampWarm_Sofa1/2` point ส้มอุ่น 1.2 ที่โป๊ะโคมตั้งพื้น `ShopDecor/FloorLamp_Sofa1/2` ข้างโซฟาที่นั่งรอ (Furniture Mega Pack ไม่มีโมเดลโคม จึงต่อจาก primitive) โคมอยู่ x −1.88 (z −2.13 / 1.62) ปลายโซฟา ดึงออกจากผนัง ~10 ซม. ไม่ให้ทับหน้าต่าง ตรวจ bounds ไม่ชนอะไร ห่างทางเดินลูกค้า (x −1.05) ≥ 0.6 ม.; ผนังซ้ายไม่มีช่องว่างพอระหว่างหน้าต่าง/ภาพ/โซฟา
  - `CornerFill_Right` (ข้างเตียง/ตู้) และ `CornerFill_BackLeft` เติมมุมที่มืด
  - `NeonGlow` point ชมพูหน้าป้าย TATTOO (0.7), `NeonGlow_NoRegrets` point ฟ้าอมม่วง (0.6) หน้าป้าย No Regrets (range 1 ม. ทั้งคู่)
  - `LedArea_*` area light แถบยาว ล้างผนังสีชมพูแดงใต้ขอบเพดาน (ซ้าย/ขวา/หลัง) และสีส้มที่ตู้ `Cabinet_3c`
- **ป้ายนีออน** (ผนังหลัง, ฟอนต์ตัวหวัด `Fonts/Pacifico-Regular SDF.asset` = TMP Font Asset แบบ Static ASCII, sampling 96, padding 12 เผื่อ glow) TMP shader Distance Field เปิด glow + `NeonHalo` แผ่น additive (`Textures/Halo.png`) ต้องห่างผนังเกิน z −3.361 ไม่งั้นจมผนัง
  - `ShopDecor/NeonSign` "TATTOO" ชมพูแดง (`M_Shop_NeonText`, `M_Shop_NeonHalo`) เหนือ `Painting_4` ตัวหนังสือ 1.23 × 0.24 ม., halo 2.0 × 0.37 (y 0.42–0.79 อยู่ระหว่างภาพกับแถบ LED)
  - `ShopDecor/NeonSign_NoRegrets` "No Regrets" ฟ้าอมม่วง (`M_Shop_NeonText_Blue`, `M_Shop_NeonHalo_Blue`) มุมขวาบนเหนือ `Painting_6` กลาง (2.0, 0.49) ตัวหนังสือ 1.01 × 0.23 ม., halo 1.51 × 0.53 เห็นจากท่านั่งสัก (เงยขึ้นเล็กน้อย) และจากประตู
  - ตรวจ bounds แล้ว ไม่ทับภาพ หน้าต่าง LED เพดาน หรือกันเอง (halo ห่างกัน 0.44 ม.)
  - **กะพริบ** `NeonFlicker` (`Scripts/Room/`) บน root ป้าย: ทุก `minInterval`–`maxInterval` วิ หรี่เหลือ `dimLevel` (0.35) 1–3 ครั้ง ครั้งละ 0.04–0.12 วิ ปรับใน Inspector ได้ ทำบน material instance (face/glow ของ TMP + `_BaseColor` halo) TATTOO 7–15 วิ, No Regrets 5–12 วิ + หน่วงเริ่มสุ่ม จึงไม่พร้อมกัน; `logFlickers` ไว้ดูเวลาใน console; ไฟ baked ไม่กะพริบตาม
- **LED:** แถบ `Led_*` เป็น cube emissive ไว้ให้เห็น (ไม่มี collider, ไม่ static) แสงที่ตกบนผนังมาจาก area light
- **Post-processing ปิดที่กล้อง** (ประหยัดบนแว่น) แต่ปรับ profile `VRTemplateAssets/Graphics/Post Process Volume Profile` เป็นกลาง (postExposure 0, contrast 10) เผื่อเปิดใช้ volume ยังปิดอยู่เหมือนเดิม
- **สาเหตุที่ฉากเดิมซีดขาว:** ambient มาจาก skybox ท้องฟ้าสีฟ้าเต็มความแรง + fog + ไม่มี bake (ความสว่างเฉลี่ยมุมประตู 0.58 → 0.085 หลังรีโนเวท)

## ลำแสงปลอม (light shaft)
- กรวยปลายเปิด `Models/LightShaftCone.asset` (โคนรัศมี 0.25, ปลาย 1, ยาว 1 ลง −Y; scale x/z = รัศมีปลาย, y = ความยาว) ใช้ shader `VRTattoo/LightShaft`: additive, ไม่เขียน depth, ไม่มีไฟ/volumetric/post-processing
  - จางตามความยาว (`_Falloff`), จางเป็นศูนย์ที่ขอบกรวย (`_EdgePower`, ตามมุมผิวกับทิศมอง) จึงไม่เห็นขอบแข็ง, จางเมื่อกล้องเข้าใกล้ (`_NearFade` เริ่ม/จบ ม.)
- ลูก `Shaft` ใต้โคมเพดาน 3 ดวง (ยาว 1.3 ม., รัศมีปลาย 0.5, `_Intensity` 0.08) และใต้ `BedLamp` (สั้น 0.55 ม. จบสูงเหนือแขน, 0.07, near fade 0.35–0.9 ม.)
- ตรวจ: จากท่านั่งจริง (`TattooSeat.SeatedEyePosition`) ภาพเหมือนเดิมทุก pixel (ไม่บังแขน/เส้นลาย); จากประตูเพิ่มความสว่างกลางลำสูงสุด ~0.14 (linear), ขอบนุ่ม
- ปรับความแรงที่ material ได้ทันทีไม่ต้อง bake

## Bake
- Lighting Settings: `Lighting/TattooRoomLighting.lighting` (Progressive GPU, Baked Indirect, Non-Directional, 14 texel/ม., lightmap สูงสุด 1024, AO 0.4 ม., bounce 2) ผลอยู่ที่ `Scenes/TattooRoom/` (lightmap 3 ไฟล์ + reflection probe)
- static: renderer ใน `Room` (ผนัง พื้น เพดาน หน้าต่าง ตู้), ภาพ `Painting_*`, เฟอร์นิเจอร์ใหญ่ใน `Furniture`, `Door_7_White` ตั้ง ContributeGI + ReflectionProbeStatic (ก่อน commit ปรับแสงรอบ 2 ผนัง/พื้น/เพดานหลุด static อยู่ จึงไม่ได้รับ lightmap: ตรวจ `renderer.lightmapIndex` ≥ 0 หลัง bake) (ไม่ได้เปิด batching static) ของเล็ก (< 35 ซม., ต้นไม้, ล้อ) รับแสงจาก light probe แทน lightmap
- เปิด **Generate Lightmap UVs** ใน import ของ FBX ที่ใช้ (`Assets/Model/...`, `Furniture Mega Pack`, `Free Wood Door Pack`, `Models/Shelf`) ถ้าเพิ่มโมเดลใหม่ที่เป็น static ต้องเปิดด้วย
- `Light Probe Group` ทำใหม่เป็นกริดทั้งห้อง (~0.66 × 0.85 ม., 4 ระดับความสูง) + จุดถี่รอบเตียง รวม 276 จุด สำหรับลูกค้า เครื่องสัก ถ้วยหมึก
- `Room Reflection Probe` แบบ baked, box projection, 128 px
- **bake ใหม่:** Window → Rendering → Lighting → Generate Lighting หรือ `Lightmapping.BakeAsync()` (GPU ใช้ไม่กี่วินาที) ต้อง bake ใหม่ทุกครั้งที่ย้ายของ static หรือไฟ baked

## ผลตรวจ (วัด pixel จาก render)
รอบ 2 (ลำแสง + โคมโซฟา + สว่างขึ้น):
- มุมประตู (0.3, −0.3, 2.6) เฉลี่ย **0.168** (เป้า 0.16–0.20), มุมมองโซฟา 0.173; จุดมืดสุดใน light probe อยู่ใต้เตียง (0.065)
- ท่านั่งจริง: ทั้งภาพ 0.385, บริเวณแขน 0.55; ถ้วยหมึก p90 0.58–1.0 บนพื้นหลัง 0.37–0.50, หัวเข็ม 0.60–0.70 บน 0.30, วงแหวนหมึก 0.59 บน 0.26, SUBMIT แดงเรือง
- ลูกค้าที่โซฟา: light probe ระดับหน้าอก 0.41–0.57, อิโมจิ 0.76 บนพื้นหลัง 0.31 (ยิ้ม) / 0.38 บน 0.15 (โกรธ สีแดง)

รอบแรก:
- มุมนั่ง (ตา (1.2, −0.75, 0.35)): ค่าเฉลี่ยทั้งภาพ 0.22, กลางภาพ (แขน) 0.39; มุมประตู (0.3, −0.3, 2.6) เฉลี่ย 0.085 (เดิม 0.58)
- ถ้วยหมึก 0.49–0.59 บนพื้นหลัง 0.23–0.30, หัวเข็ม 0.35–0.54, วงแหวนหมึก 0.52, อิโมจิ 0.64 บนพื้นหลัง 0.03, ป้ายนีออนมองเห็นจากประตู, ปุ่ม SUBMIT แดงเรืองเต็มที่

[← กลับ CLAUDE.md](../CLAUDE.md)

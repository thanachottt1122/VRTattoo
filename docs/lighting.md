# ห้องและแสง (ร้านสักโทนมืด)

## วัสดุ (`Materials/Shop/`)
ไม่แก้ material ของ asset ภายนอก ทุกอันสร้างใหม่แล้วใส่ใน scene แทน

| Material | ใช้กับ |
|---|---|
| `M_Shop_Wall` เทาถ่าน / `M_Shop_Ceiling` เกือบดำ | `Room/Wall_1a*`, `Room/Floor_12a` (เพดาน) |
| `M_Shop_FloorConcrete` ปูนขัดมันเข้ม smoothness 0.55 | `Room/Floor_Tile_3b` |
| `M_Shop_WindowNight` | หน้าต่างทั้ง 4 (ข้างนอกเป็นกลางคืน กล้องเคลียร์เป็นสีดำ) |
| `M_Shop_DarkWood` / `M_Shop_Door` | `Cabinet_3c` / ประตู (texture เดิม ย้อมเข้ม) |
| `M_Shop_BlackLeather` | เตียงสัก (`hospital bed`) |
| `M_Shop_Sofa`, `M_Shop_Lounge`, `M_Shop_Stool` | โซฟา Chair15 ×2, Chair48, สตูล Chair43 (texture เดิม ย้อมเข้ม) |
| `M_Shop_CushionRed`, `M_Shop_PillowRed` | เบาะ Cushion02, หมอนเตียง Cushion38 (แดงเข้ม) |
| `M_Shop_BlackMatte` | ถังขยะ, กระถาง, โคม |
| `M_Shop_FrameBlack` + `M_Flash_*` | กรอบภาพสีดำเงา + ภาพ flash art |

- รถเข็นสเตนเลส ต้นไม้ และของบนรถเข็นใช้ material เดิม
- `SubmitButton.mat` เปิด emission สีแดง เพราะปุ่มอยู่นอกวงไฟโคม

## ภาพ flash art (`Textures/FlashArt/`)
- สร้างด้วย `python Tools/flash/make_flash_art.py` จากลายใน `Stencils/`: เส้นดำ + สีเหลื่อมนิด ๆ บนกระดาษครีม
- `Painting_4` (ผนังหลัง) = `Flash_Skulls` 4 ลาย, `Painting_5` (ผนังซ้าย) = `Flash_Classics` 4 ลาย, `Painting_2` (หลังเตียง) = `Flash_Teddy`, `Painting_6` = `Flash_SadBoy` (แนวตั้ง 600×1000)
- ไม่ใช้ลายติดลิขสิทธิ์ (`angry_duck`, `ghost_monster`, `trophy`, `cloud`) เปลี่ยนลายได้ที่ `SHEETS` ในสคริปต์ แล้วรันใหม่ (ทับไฟล์เดิม)

## แสง
- **Environment:** ไม่มี skybox, ambient แบบ Flat (0.06, 0.05, 0.045), ปิด fog, Main Camera เคลียร์เป็นสีดำ, `Directional Light` เดิมปิดไว้ (ไม่ได้ลบ)
- **ไฟ realtime ดวงเดียว:** `Lighting/BedLampSpot` spot แบบ **Mixed** (ส่องตรงแบบ realtime, แสงสะท้อน bake) ขาวอุ่น ความแรง 4, มุม 80°, range 3.5 ม., ไม่มีเงา อยู่ใต้โคม `ShopDecor/BedLamp` (สาย + โป๊ะ + หลอดเรืองแสง) เหนือแขนลูกค้า ครอบรถเข็นด้วย
  - `Performance URP Config` (quality Very Low/Low) เปิด Additional Lights = Per Pixel, ต่อ object 1 ดวง, ปิดเงา additional (เดิม Disabled ไฟ spot จะไม่แสดงเลย)
- **ไฟ Baked:**
  - `CeilingWarm_*` (3 ดวง) point ขาวอุ่นหรี่ ๆ เป็น fill
  - `NeonGlow` point ชมพูหน้าป้ายนีออน
  - `LedArea_*` area light แถบยาว ล้างผนังสีชมพูแดงใต้ขอบเพดาน (ซ้าย/ขวา/หลัง) และสีส้มที่ตู้ `Cabinet_3c`
- **ป้ายนีออน** `ShopDecor/NeonSign`: TMP "TATTOO" (`M_Shop_NeonText`, shader Distance Field เปิด glow) + `NeonHalo` แผ่น additive (`Textures/Halo.png`) บนผนังหลังเหนือภาพใหญ่ ต้องห่างผนังเกิน z −3.361 ไม่งั้นจมผนัง
- **LED:** แถบ `Led_*` เป็น cube emissive ไว้ให้เห็น (ไม่มี collider, ไม่ static) แสงที่ตกบนผนังมาจาก area light
- **Post-processing ปิดที่กล้อง** (ประหยัดบนแว่น) แต่ปรับ profile `VRTemplateAssets/Graphics/Post Process Volume Profile` เป็นกลาง (postExposure 0, contrast 10) เผื่อเปิดใช้ volume ยังปิดอยู่เหมือนเดิม
- **สาเหตุที่ฉากเดิมซีดขาว:** ambient มาจาก skybox ท้องฟ้าสีฟ้าเต็มความแรง + fog + ไม่มี bake (ความสว่างเฉลี่ยมุมประตู 0.58 → 0.085 หลังรีโนเวท)

## Bake
- Lighting Settings: `Lighting/TattooRoomLighting.lighting` (Progressive GPU, Baked Indirect, Non-Directional, 14 texel/ม., lightmap สูงสุด 1024, AO 0.4 ม., bounce 2) ผลอยู่ที่ `Scenes/TattooRoom/` (lightmap 3 ไฟล์ + reflection probe)
- static: renderer ใน `Room`, `Furniture`, `Door_7_White` ตั้ง ContributeGI + ReflectionProbeStatic (ไม่ได้เปิด batching static) ของเล็ก (< 35 ซม., ต้นไม้, ล้อ) รับแสงจาก light probe แทน lightmap
- เปิด **Generate Lightmap UVs** ใน import ของ FBX ที่ใช้ (`Assets/Model/...`, `Furniture Mega Pack`, `Free Wood Door Pack`, `Models/Shelf`) ถ้าเพิ่มโมเดลใหม่ที่เป็น static ต้องเปิดด้วย
- `Light Probe Group` ทำใหม่เป็นกริดทั้งห้อง (~0.66 × 0.85 ม., 4 ระดับความสูง) + จุดถี่รอบเตียง รวม 276 จุด สำหรับลูกค้า เครื่องสัก ถ้วยหมึก
- `Room Reflection Probe` แบบ baked, box projection, 128 px
- **bake ใหม่:** Window → Rendering → Lighting → Generate Lighting หรือ `Lightmapping.BakeAsync()` (GPU ใช้ไม่กี่วินาที) ต้อง bake ใหม่ทุกครั้งที่ย้ายของ static หรือไฟ baked

## ผลตรวจ (วัด pixel จาก render)
- มุมนั่ง (ตา (1.2, −0.75, 0.35)): ค่าเฉลี่ยทั้งภาพ 0.22, กลางภาพ (แขน) 0.39; มุมประตู (0.3, −0.3, 2.6) เฉลี่ย 0.085 (เดิม 0.58)
- ถ้วยหมึก 0.49–0.59 บนพื้นหลัง 0.23–0.30, หัวเข็ม 0.35–0.54, วงแหวนหมึก 0.52, อิโมจิ 0.64 บนพื้นหลัง 0.03, ป้ายนีออนมองเห็นจากประตู, ปุ่ม SUBMIT แดงเรืองเต็มที่

[← กลับ CLAUDE.md](../CLAUDE.md)

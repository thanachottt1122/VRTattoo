# UI (สไตล์นีออน)

ทุก UI ในเกมใช้หน้าตาเดียวกัน: แผ่นดำโปร่งแสง ขอบมน ขอบชมพูนีออนบาง ๆ เรืองเบา ๆ (เข้ากับป้าย TATTOO) ตัวหนังสือขาว ฟอนต์ sans หนา

## ของที่ใช้ร่วมกัน
- `Shaders/NeonPanel.shader` + `Materials/UI/NeonPanel.mat`: rounded rect แบบ SDF บน quad เดียว (premultiplied alpha, ไม่เขียน depth)
- `Scripts/UI/NeonPanel.cs`: ใส่บน quad แล้วตั้ง `size` (ขนาดแผ่น ม.) `pad` (ที่ให้ glow จาง) `cornerRadius` `edgeColor` (ค่า HDR, ค่าเริ่ม `NeonPanel.NeonPink`) `fillColor` `glow` `hover` `alpha` สคริปต์ตั้ง scale ของ quad เอง (ห้ามมีลูก)
- ฟอนต์: `Fonts/Inter-UI SDF.asset` (dynamic สร้างจาก `VRTemplateAssets/Fonts/Inter/Inter-Regular.ttf`, fallback LiberationSans) ใช้ style Bold; ชื่อลายใช้ `Pacifico-Regular SDF` ตัวเลขห้ามใช้ Pacifico
- ดาว: `UI/Icons/star.png` (สร้างด้วย `Tools/ui/draw_star.py`) เป็น SpriteRenderer ย้อมสีทอง/เทา
- ลำดับวาด: แผ่น `sortingOrder` 0, ป้ายย่อย/ปุ่ม 1, ตัวหนังสือ/ดาว 2–3 และตัวหนังสืออยู่หน้าแผ่น 2–4 มม.
- bloom ของ URP ทำให้ค่า HDR เรืองแรง ถ้าขอบดูฟุ้งเกินให้ลด `edgeColor`/`glow` ไม่ใช่ลดความหนาเส้น

## ขนาดตัวหนังสือ (TextMeshPro 3D: fontSize 0.1 ≈ ความสูงบรรทัด 1 ซม.)
| อะไร | fontSize | ระยะจากตา |
|---|---|---|
| ตัวเลขคะแนน | 0.95 (สูง ~6.5 ซม.) | 0.95 ม. |
| ชื่อลาย (Pacifico, auto size) | 0.26–0.42 | 0.95 ม. |
| DONE / +คะแนน | 0.34–0.38 | 0.95 ม. |
| ป้ายระดับ / คะแนนรวม | 0.20–0.22 | 0.95 ม. |
| ป้ายบอกปุ่มที่เก้าอี้ | 0.26 | 0.8 ม. |

## การ์ดงาน `Interactables/TattooSystem/JobCard` (`JobCard`)
- แผ่น 0.34 × 0.22 ม. ห่างตาตอนนั่ง 0.95 ม. ตรงหน้า (ฝั่งเตียงตรงข้ามเก้าอี้) ต่ำกว่าระดับสายตา ~10° หันเข้าหาตา (ตั้งตายตัว ไม่ billboard)
- ตำแหน่งเลือกจากมุมมองตอนนั่ง: ขอบบนต่ำกว่ากรอบรูปวงรี (Painting_6 เริ่มที่ −2°) และ flash art (Painting_4 เริ่มที่ −9° ทางขวา), ขอบล่างสูงกว่าตัวลูกค้าที่นอน (ลำตัวสูงสุด ~−18°) แขนที่สักอยู่ที่ −40° ถึง −52° ถ้าย้ายเตียง/เก้าอี้/รูป ให้คำนวณมุมใหม่
- สถานะ: รอลูกค้า ("Waiting for a customer"), กำลังสัก (ชื่อลาย + ป้ายระดับ easy เขียว / medium ส้ม / hard แดง + ปุ่ม DONE), ผลงาน (ตัวเลข 0–100 นับขึ้น ~0.8 วิ แล้วเด้ง, ดาว 1–3 ทีละดวง, "+คะแนน", คะแนนรวมนับขึ้น) คะแนนรวมอยู่ล่างสุดตลอด
- ดาว: `starThresholds` (70, 90) = 2 และ 3 ดาว ต่ำกว่านั้น 1 ดาว
- ผังตอนสัก (พิกัดในการ์ด ม., x ขวามือผู้เล่น): ชื่อลาย y 0.071 / ป้ายระดับ x −0.1 + แถบเวลา x 0.068 (ยาว 0.15) y 0.026 / หน้า + เกจเจ็บ (ยาว 0.19) + % y −0.014 / DONE y −0.054 / คะแนนรวมชิดซ้าย + "QUEUE" + แถบจิ๋ว 2 เส้น (ความอดทนลูกค้าบนโซฟา) y −0.093 ตรวจด้วย `textBounds` ไม่ทับกัน
- เกจ: `NeonBar` (ลูก Track + Fill เป็น `NeonPanel` ทรงแคปซูล, `Set(value, color)`) — เจ็บ เขียว→เหลือง→แดง; เวลา ขาว → ส้ม (< 25%) → แดงกะพริบเมื่อเกินเวลา; หน้า: สบาย (`Emote_Happy`) < `tenseFrom` 30 ≤ เกร็ง (`Emote_Tense`) < `flinchThreshold` 60 ≤ เจ็บ (`Emote_Hurt`) ขยาย 1.25 เท่าตอนสะดุ้ง
- ผลงาน: "FAST +x%" สีทองใต้ดาวถ้าได้โบนัสเร็ว; เดินออกเพราะเจ็บ → "0" + "WALKED OUT" สีแดง ไม่มีดาว
- **3 ดาว:** ตอนดาวดวงที่ 3 ติด `JobCard` เล่น `ThreeStarBurst` (ลูกของการ์ด) และยิง `onThreeStars`
  - `ThreeStarBurst`: ประกายดาว 24 อนุภาค (`Materials/FX/StarSparkle.mat`, URP Particles Unlit additive, `star.png`, HDR ทอง) + ลูก `Confetti` กระดาษสีชมพู/ฟ้า/ทอง 28 อนุภาค (alpha blend) ปล่อยจากขอบการ์ด (shape BoxEdge 0.34×0.22) ครั้งเดียว อายุ ≤ 1.8 วิ ไม่มี collision/light/shadow
  - แขน: `CustomerTattooSkin.PlayGlint()` แถบสว่างกวาดเฉียงผ่านหมึก 0.9 วิ (`_Glint` ใน `TattooSkinOverlay.shader`, เฉพาะที่มีหมึก) + `Prefabs/FX/ArmSparkle` 16 ประกายเล็กจาก mesh ผิว (shape Mesh = collider ของผิว)
  - รวม ≤ 70 อนุภาคชั่วคราว เบาสำหรับแว่น
- `TattooJobManager` สั่งการ์ด (`ShowWaiting/ShowJob/ShowResult/SetDoneAvailable`) สูตรคะแนนไม่แสดงบนการ์ด ดูได้ใน Console (`[TattooJob] ...`)

## ปุ่ม DONE (`NeonButton`)
- ลูกของการ์ด: BoxCollider + `XRSimpleInteractable` + `XRPokeFilter` + `NeonButton`
- กดได้: ชี้ ray แล้วกด trigger (อ่าน `activateInput` ของมือที่ hover เพราะ XRI ส่ง activate ให้เฉพาะของที่ถูก select), grip (select), จิ้ม (poke), Desktop Test Mode คลิกซ้าย, Editor กด J
- hover: ขอบสว่างขึ้น + ขยาย 6%; กด: สั่น 0.6/0.08 วิ ที่มือนั้น + เด้ง, กันกดซ้ำ 0.5 วิ
- แสดงเฉพาะตอน `TattooJobManager.CanSubmit` (ลูกค้านอนแล้ว ผิวพร้อม ยังไม่ส่ง)

## ป้ายบอกปุ่มที่เก้าอี้ `Interactables/TattooSystem/ChairHint` (`SeatHintPanel`)
- ยืนใกล้เก้าอี้ (ในระยะ `TattooSeat.useRange`): `[B] Sit` (แว่น) / `[F] Sit` (คอม) ลอยเหนือเก้าอี้ (`ChairHint_StandingAnchor`) หมุนหาผู้เล่น
- นั่ง: 3 บรรทัดสั้น ๆ ที่ `ChairHint_SeatedAnchor` (ซ้ายมือ เหนือระดับสายตาเล็กน้อย หน้าหน้าต่าง ไม่บังหัวลูกค้า/แขน) แสดง 5 วิแล้วจาง แสดงใหม่เมื่อนั่งใหม่หรือเปลี่ยนโหมด
- เลือกข้อความตามโหมด: Desktop Test Mode เปิด (`TattooDesktopTestMode.IsActive`) → Click/P/F/J/K, มี XR Device Simulator → Click/G/F/J/K, นอกนั้นถือว่าแว่นจริง → Trigger/Grip/B/DONE

## วงแหวนหมึก (`InkHologram`)
- ตำแหน่ง/การตามมือเหมือนเดิม; วงบางลง พื้นหลังดำเข้มขึ้น เส้นนำและขีดบอกระดับเป็นชมพูนีออน (`_EdgeColor`) ส่วนวงที่เติมยังเป็นสีหมึก
- % และชื่อหัวเข็มเป็น Inter Bold สีขาว มีจุดสีหัวเข็มนำหน้า

## เพิ่ม UI ใหม่
1. สร้าง quad (MeshFilter Quad + MeshRenderer ใช้ `NeonPanel.mat`) + `NeonPanel` ตั้ง `size`
2. ใส่ TextMeshPro (3D) ฟอนต์ `Inter-UI SDF` Bold สีขาว เป็นพี่น้องของ quad (ไม่ใช่ลูก) z −0.002 และ `sortingOrder` สูงกว่า
3. ตรวจมุมจากตาตอนนั่ง (`TattooSeat.SeatedEyePosition()`) ว่าไม่ทับป้ายนีออน รูปแขวน หรือแขนลูกค้า

[← กลับ CLAUDE.md](../CLAUDE.md)

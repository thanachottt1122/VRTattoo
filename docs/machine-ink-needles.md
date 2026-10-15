# เครื่องสัก หมึก หัวเข็ม รถเข็น

## เครื่องสัก (`Interactables/TattooSystem/TattooMachine`)
- โมเดล `Models/TattooMachine/tattoo.fbx` (เครื่องคอยล์ ไม่มี texture ฝัง) เป็นลูก `Model` scale 0.02533 → ยาว 13.5 ซม. (ในไฟล์ยาว 5.33 หน่วยตามแกน Y เข็มชี้ −Y ปลายเข็มคือ mesh `Plane` ที่ y = 0.18) ตัวแม่ scale 1
- Material extract ไว้ที่ `Models/TattooMachine/Materials/Material.00x.mat` (URP Lit, ใส่ metallic ให้เป็นโลหะ: ด้ามดำ, เงิน, คอยล์ทองแดง; `Material.001` หัวครอบปลายเป็นพลาสติกใส Transparent อันอื่นเป็น Opaque)
- `Tip` อยู่ที่ปลายเข็มจริง local (0, −0.06, 0) forward ชี้ลงตามเข็ม; ray วาดเริ่มหลังปลาย 3 ซม. ซึ่งอยู่ในตัว CapsuleCollider จึงไม่ชนตัวเอง
- `Attach` (local (0, −0.025, 0) = ห่างปลายเข็ม 3.5 ซม.) หมุนให้ถือแบบปากกา: เข็มชี้ไปข้างหน้าแล้วก้มลง 45° จาก forward ของ controller ด้านมอเตอร์ (−X ของโมเดล) หันขึ้น
- ตอนถือ XRI ซ่อนโมเดล controller ขวา จึงเห็นเครื่องลอยอยู่ที่ตำแหน่งมือ
- `NeedleCollar` (ปลอกสีบนหัวครอบใส ย้อมสีตามหัวเข็ม) และ `NeedlePoint` (ปลายเข็มสีเงิน กว้าง = diameter × 0.5) เป็นลูกของเครื่อง ไม่มี collider (`TattooNeedle.ApplyVisual`)
- `InkGauge` (ลูกของเครื่อง) ใช้คู่กับวงแหวนโฮโลแกรม: วงแหวน 5 วงรอบด้ามจับส่วนบน (`Ring0` ล่างสุด, Cylinder scale (0.0224, 0.0011, 0.0224) ห่างกัน 4.5 มม.) วงที่มีหมึกเป็นสีหมึกปัจจุบัน + เรืองแสง (`Materials/Tattoo/InkGauge.mat`, หมึกดำเรืองเป็นเทา) วงที่หมดเป็นเทาเข้ม ติด ceil(% × 5 / 100) วง เหลือ < `lowThreshold` 15% (เท่ากับวงแหวนโฮโลแกรม) วงล่างกะพริบ อ่าน `TattooInk` ตัวเดียวกับวงแหวนโฮโลแกรม ค่าและสีจึงตรงกันเสมอ

## หมึก
- **`TattooInk` (บนเครื่อง):** `amount` 0–100%, `color`, `metersPerLoad` = 1.3 ม. ของเส้นหัว `referenceDiameter` 5 มม. ต่อ 1 ถ้วย ลดตาม**พื้นที่** = ระยะที่ปลายเข็มเลื่อนบนผิวจริง × (ขนาดหัว ÷ 5 มม.) → หัว LINER ใช้ 0.6× หัว SHADER 1.8× (อยู่กับที่ไม่ลด จุดเริ่มเส้นคิดเท่าความกว้างหัว: หัวกลาง ≈ 0.4%) เส้น 7 ซม. หัวกลาง ≈ 5.4%
  - ที่มาของ 1.3: ระยะที่ต้องสักโดยประมาณของลาย = max(พื้นที่ลาย ÷ ความกว้างแปรง 5 มม., เส้นรอบรูป ÷ 2) ในกรอบ 14 ซม. ลาย medium เฉลี่ย 0.68 ม. ≈ ครึ่งถ้วย ถ้าเพิ่ม/เปลี่ยนลายมาก ให้คำนวณใหม่
  - จุ่ม (`DippedCap`): ทุกเฟรม `OverlapSphere(tip, dipRadius 5 มม.)` หา trigger ของ `InkCap` **จุ่มเฉย ๆ ไม่เกิดอะไร** ต้องกด trigger ค้าง (`TattooMachine.TriggerPressed` = trigger จริงหรือ `ExternalTrigger`) หมึกจึงเพิ่ม 100% ต่อ `refillDuration` 1.5 วิ (`IsRefilling`) ถ้าสีถ้วยต่างจากหมึกในเครื่อง → `amount` = 0 + เปลี่ยนสีทันที แล้วค่อยเติม ระหว่างเติมสั่นเบา (`refillHapticAmplitude` 0.15 ทุก 0.1 วิ ผ่าน `TattooMachine.SendHaptic`)
- **`TattooMachine`:** หมึกหมด = ไม่วาด + สั่นสั้นครั้งเดียวต่อการกด trigger (`emptyHapticAmplitude` 0.8, `emptyHapticDuration` 0.05 วิ) รวมถึงตอนหมดกลางเส้น ยกเว้นตอนปลายเข็มอยู่ในถ้วยหรือแตะหัวเข็ม (`AtTrolley` กดเพื่อเติม/เปลี่ยนหัว) ไม่มี `TattooInk` = หมึกไม่จำกัดสี `TattooCanvas.inkColor`
- **วงแหวนโฮโลแกรม (`InkHologram`):** object `Interactables/TattooSystem/InkHologram` (ไม่ใช่ลูกของเครื่อง) มี `Ring` (Quad 7.5 ซม. + `Materials/Tattoo/InkHologram.mat` / `Shaders/InkHologram.shader`) และ `Percent` (TMP)
  - ตามเครื่องแบบหน่วง (`followSharpness`) เยื้อง `viewOffset` (ขวา 6 ซม. บน 3.5 ซม. ในมุมมองผู้เล่น) ถ้าเครื่องอยู่ขวาของภาพเกิน `switchSideAbove` 0.7 (เช่นตอนจุ่มถ้วย) ย้ายไปฝั่งซ้าย หันหากล้องเสมอ เป็นวัตถุในโลก ไม่ติดจอ
  - วงลดต่อเนื่องตาม % (เลข/วงไล่ค่าจริงที่ `levelSpeed` 120%/วิ จึงเห็นวงเพิ่มตอนเติม) สีหมึกปัจจุบัน หมึกเข้มถูกยกความสว่างขั้นต่ำ `minBrightness` (ดำ → เทาเรือง) ตัวเลขเป็นสีขาว (`labelWhiten` = 1, ฟอนต์ Inter Bold) เส้นนำ/ขีดเป็นชมพูนีออน ชื่อหัวเข็มมีจุดสีหัวเข็มนำหน้า (สไตล์ UI ดู [ui.md](ui.md))
  - shader: วงเรืองแสง (ส่วนที่หมดเป็นเงาจาง) + scanline + แสงวิ่ง + ขีดทุก 10% บนแผ่นพื้นมืดจาง (`_Backing`) ให้อ่านออกบนพื้นสว่าง (premultiplied `Blend One OneMinusSrcAlpha`) `Ring.sortingOrder` = −1 ให้ตัวเลขวาดทับ
  - เหลือ < `lowThreshold` 15% กะพริบ (`blinkRate` 3) แสดงเฉพาะตอนเครื่องอยู่ในมือ (ไม่ใช่ socket) fade เข้า/ออก
  - `NeedleLabel` (TMP ในวงใต้ตัวเลข %) แสดงหัวที่ใส่อยู่ เช่น `MEDIUM 5mm` สีของหัวผสมขาว ระหว่างกดค้างเปลี่ยนหัวแสดงชื่อหัวใหม่กะพริบ (`swapBlinkRate`)
- **สีบนแขน:** RenderTexture ของ `TattooCanvas` เป็น premultiplied (rgb = สี × alpha, `TattooBrush.shader` ใช้ `Blend One OneMinusSrcAlpha`) แต่ละเส้นจึงเก็บสีของตัวเอง สีใหม่ทับสีเก่าได้ `TattooSkinOverlay.shader` แสดงสีจาก rgb / alpha (เลิกใช้ `_InkColor`) คะแนนยังอ่านแค่ alpha จึงไม่สนสี
- **ถ้วยหมึกและขวด (`Prefabs/Ink/`):** `InkCap.prefab` (ถ้วยพลาสติกขาว Ø 4.5 ซม. สูง 2.5 ซม. + แผ่น `Ink` บนปากถ้วย + SphereCollider trigger r 3.4 ซม. ที่ y 2.2 ซม. ใหญ่กว่าถ้วย ~1 ซม. รอบด้าน + `InkTint` + `InkCap`) และ `InkBottle.prefab` (ขวดตกแต่ง Ø 4 ซม. สูง ~10 ซม. + ป้าย + ฝาดำ + `InkTint` ไม่มี trigger) มี variant `_Black/_Red/_Blue/_Green`
  - `InkTint` ใส่สีด้วย MaterialPropertyBlock (`_BaseColor`) ให้ `renderers` (`[ExecuteAlways]` เห็นสีใน Editor) material ใช้ร่วมกันที่ `Materials/Ink/` (`Ink` สีขาว ถูกย้อมด้วย tint)
  - **เพิ่มสีใหม่:** คลิกขวา `InkCap.prefab` → Create → Prefab Variant ตั้ง `InkTint.color` และ `InkCap.inkName` แล้ววางบน `CapRack` (ถ้าเกิน 4 ใบต้องขยายแถว/ถาด และเช็กว่ายังอยู่ในภาพมุมนั่ง)
  - **เปลี่ยนเป็นโมเดลสวยกว่า:** แก้ child ใน `InkCap.prefab`/`InkBottle.prefab` ที่เดียว variant และของใน scene จะเปลี่ยนตาม (อย่าลืมใส่ renderer ของผิวหมึกลง `InkTint.renderers` และให้ trigger อยู่ที่ผิวหมึก)

## หัวเข็ม
- 3 ขนาด: **LINER 3 มม.** (ฟ้า เลข 3), **MEDIUM 5 มม.** (ส้ม เลข 5 = ค่าเริ่มเกม), **SHADER 9 มม.** (ชมพูม่วง เลข 9)
- **`TattooNeedle` (บนเครื่อง):** `needleName`, `diameter`, `color` ของหัวที่ใส่อยู่ (ค่าใน scene = MEDIUM) ทุกเฟรม `OverlapSphere(tip, touchRadius 5 มม.)` หา trigger ของ `NeedleCartridge` (`TouchedCartridge`) แตะหัวที่ต่างจากที่ใส่อยู่ + กด trigger ค้าง `swapHoldTime` 0.4 วิ → `Fit()` เปลี่ยนหัว + สั่นเบา (`swapHapticAmplitude` 0.3, 0.08 วิ) ระหว่างกดค้างสั่นเบามาก (0.08 ทุก 0.1 วิ) มี `SwapTarget`/`SwapProgress`/`onChanged`/`Label`
- **`NeedleCartridge` (`Scripts/Needles/`):** `needleName`, `diameter`, สีจาก `InkTint` (ตัวเดียวกับหมึก)
- **Prefab (`Prefabs/Needles/`):** `NeedleCartridge.prefab` หลอดพลาสติกขาว Ø 1.3 ซม. สูง 6 ซม. (`Body` ใช้ `InkCapPlastic`) + `Grip` และ `Top` สี (`Materials/Needles/NeedleGrip.mat` ย้อมด้วย `InkTint`) + `Label` TMP ตัวเลขบนหัว (หันให้อ่านได้จากเก้าอี้) + SphereCollider trigger r 1.5 ซม. ที่ปลายบน ไม่มี collider ทึบ; variant `_Liner/_Medium/_Shader`
  - **เพิ่มขนาดใหม่:** สร้าง variant จาก `NeedleCartridge.prefab` ตั้ง `needleName`, `diameter` (≤ 1 ซม. = `TattooCanvas.MaxNeedleDiameter`), `InkTint.color`, ตัวเลขใน `Label` แล้ววางบน `NeedleRack` (และเพิ่มในรายการหัวของ `TattooScoreSimulator`)
- **`Interactables/TattooSystem/NeedleRack`:** บล็อกสแตนเลสบน `MountArm` ของ `CapRack` (1.03, −0.949, −0.16) ต่อจากถ้วยเขียว เรียงจากใกล้ผู้เล่น: LINER (z −0.13), MEDIUM (−0.16), SHADER (−0.19) trigger ไม่ทับ trigger ถ้วย ตอนนั่งอยู่ขอบขวาของภาพ (หันขวานิดเดียว)

## รถเข็น (`Furniture/InkTrolley`)
- โมเดล `Models/Shelf/model.fbx` (รถเข็นสแตนเลส 3 ชั้น ขนาดจริงอยู่แล้ว 0.525 × 0.895 × 0.35 ม. ไม่ย่อขยาย) material extract ไว้ `Models/Shelf/Materials/` (`steel` = สแตนเลส, `plactic`/`Material.006` = ยางล้อดำ)
- อยู่ใต้ `Furniture` (scale 1.48) จึงตั้ง localScale = 1/1.48 ให้ lossyScale = 1 วางที่ (0.78, −1.797, 0.05) ข้างเก้าอี้ฝั่ง −X = มือขวาตอนนั่ง ห่างเก้าอี้ 3 ซม. ห่างทางเดินลูกค้าทุกเส้น ≥ 0.5 ม.
- ชั้นเป็นถาดมีขอบสูง 6 ซม. พื้นถาดชั้นบนอยู่ที่ y = −1.0187 (ขอบบน −0.957) collider: `ShelfTop/Middle/Bottom` (พื้นถาด) + `Rim*` (ขอบถาดละ 4 ด้าน) + `Post0–3` (BoxCollider ทั้งหมด)
- บนชั้นบน: `Interactables/TattooSystem/MachineHolder` ถ้วยเสียบสีดำที่มุมหน้าของถาดฝั่งเตียง (0.975, z −0.07) ข้างไหล่ขวาตอนนั่ง มี `Socket` (ย้ายมาจากแท่นเดิม) เครื่องตั้งตรง ปลายเข็มเหนือพื้นถาด 1 ซม.; `Interactables/TattooSystem/Inks` ขวดตกแต่ง 4 ขวดแถวหลัง (z 0.135)
- **ที่วางถ้วย `Inks/CapRack`:** ถาดสแตนเลสแถวเดียว (`Pad0–3` + `Bar`, ผิวบน y −0.95 สูงกว่าขอบถาดรถเข็น) ยื่นจากเสา `Post2` มุมรถเข็นฝั่งเตียง (`Clamp` + `MountArm` + `MountLink`) ไปอยู่ระหว่างขอบเตียงกับเก้าอี้ ทางขวามือตอนนั่ง ถ้วย 4 ใบเรียงซ้าย→ขวาจากมุมนั่ง: ดำ (1.235, z −0.178), แดง (1.185, −0.185), น้ำเงิน (1.135, −0.192), เขียว (1.085, −0.199) ห่าง `TattooWorkArea` 22–30 ซม. ระยะ/แนวเอียงจูนให้เห็นครบ 4 ใบในมุมก้มมองแขนของ simulator (FOV 60°) ถ้าย้ายต้องเช็กว่าไม่ทับมือลูกค้า (Tanya/Leni มือวางชิดขอบเตียงแถว x 1.15–1.25)

[← กลับ CLAUDE.md](../CLAUDE.md)

# VRTattoo

เกม VR ร้านสัก: Unity 6000.3, URP 17.3, XRI 3.4.1, Input System, OpenXR ทดสอบบนคอมด้วย XR Device Simulator (อยู่ใน scene)

## สถานะ: เกมเสร็จแล้ว (2026-10-15)
ระบบครบตามแผน ไม่มีงานที่ค้างในแผน (ร้านค้าไม่ทำ) ทดสอบรอบสุดท้ายผ่าน: compile ไม่มี error/warning จากโค้ดของเรา, เล่นครบ 1 วันใน Play mode ด้วย log (สักจริง + scorer, 3 ดาว + ทิป, เดินออกเพราะเจ็บ, ลูกค้าหมดความอดทน, สรุปวัน, NEXT DAY, เซฟ) — ผลอยู่ใน [shop-day.md](docs/shop-day.md#ทดสอบรอบสุดท้าย-2026-10-15) งานต่อจากนี้เป็นการเก็บรายละเอียดหรือลองบนแว่นจริงเท่านั้น

**วงจรการเล่น:** เริ่มวัน (ป้าย Day N) → ลูกค้าเข้าร้าน 5 คน (เตียง 1 + โซฟา 2 คิว) → นั่งเก้าอี้ หยิบเครื่อง สักตามลายบนแขน (เติมหมึก/เปลี่ยนหัวเข็ม ระวังความเจ็บและเวลา) → กด DONE → คะแนน ดาว เงิน ทิป อิโมจิ → ลูกค้าคนสุดท้ายออก → แผ่นสรุปวัน → NEXT DAY (ยากขึ้น) / RESTART

## กฎการทำงาน
- **ตอบผู้ใช้เป็นภาษาไทยเสมอ** (ชื่อไฟล์ โค้ด ศัพท์เทคนิคใช้อังกฤษได้)
- **ทำงานบน `main` เสมอ** ห้ามสร้าง branch ใหม่โดยไม่ถาม
- ไฟล์โปรเจกต์อยู่ใต้ `Assets/_VRTattoo` ห้ามแก้ `Assets/Samples`
- แก้ scene ผ่าน UnityMCP (`execute_code` + Undo) แล้วเซฟ scene ทุกครั้ง
- **ภาพหน้าจอ: ถ่ายเฉพาะตอนจำเป็นจริง ไม่เกิน 1–2 ภาพต่องาน** (กินโควตามาก) ตรวจผลด้วย log หรือตัวเลขก่อน (`execute_code` คืนค่า, `read_console`) ภาพไปลง `Assets/Screenshots/` ลบก่อน commit
- `execute_code` ที่รันนานเกิน ~30 วิจะ timeout (Unity ยังรันต่อ รอก่อนเรียกใหม่); screenshot ตอน Play อาจทำให้เกม Error Pause → ตั้ง `EditorApplication.isPaused = false`
- งานเสร็จแล้วอัปเดต CLAUDE.md (สั้น ๆ) และ `docs/` (รายละเอียด)

## โครงสร้าง (`Assets/_VRTattoo/`)
- `Scenes/TattooRoom.unity` scene หลัก
- `Scripts/Tattoo/` เครื่องสัก canvas หมึก หัวเข็ม เก้าอี้ test mode · `Scripts/Ink/` · `Scripts/Needles/` · `Scripts/Customers/` · `Scripts/Stencils/` ลาย+คะแนน (มี `Editor/`) · `Scripts/Shop/` วันทำงาน เงิน เซฟ · `Scripts/Room/` ของตกแต่ง · `Scripts/UI/` การ์ดงาน ปุ่ม ป้าย สรุปวัน
- `Materials/Shop/` วัสดุห้อง · `Textures/FlashArt/` (`Tools/flash/make_flash_art.py`) · `Lighting/` lighting settings
- `Fonts/` Pacifico + Inter-UI (TMP Font Asset) · `UI/Emotes/` PNG อิโมจิ + หน้าหงุดหงิด/เกร็ง/เจ็บ (`Tools/emotes/draw_emotes.py`) · `Prefabs/FX/` + `Materials/FX/` particle · `UI/Icons/star.png` (`Tools/ui/draw_star.py`) · `Data/Voices/` ชุดเสียงลูกค้า
- `Prefabs/` (`Customer`, `Characters/`, `Ink/`, `Needles/`) · `Characters/` (โมเดลไม่อยู่ใน git) · `Stencils/` PNG ลาย · `Data/StencilLibrary.asset` · `Shaders/` · `Materials/` · `Models/`
- นอก Assets: `docs/` · `Tools/stencils/extract_stencils.py` · `SourceArt/`

## ระบบหลัก
- **สัก:** ถือเครื่อง กด trigger ตอนปลายเข็มแตะแขน → วาดลง RenderTexture ของ `TattooCanvas` (ของลูกค้าแต่ละคน) กว้างตามหัวเข็ม + haptics
- **ผิวที่สัก:** `CustomerTattooSkin` สร้าง UV/collider/overlay ของแขนท่อนล่างตอนลูกค้านอน ลุกแล้วลบ
- **หมึก:** `TattooInk` มีจำกัด ใช้ตามพื้นที่ที่ลง จุ่มถ้วย + กด trigger ค้าง = เติม/เปลี่ยนสี แสดงที่วงแหวน `InkHologram` และวงบนด้าม `InkGauge`
- **หัวเข็ม:** LINER 3 / MEDIUM 5 (เริ่มเกม) / SHADER 9 มม. บน `NeedleRack` ข้างถ้วยเขียว จิ้ม + กด trigger ค้าง 0.4 วิ = เปลี่ยน (สั่นเบา)
- **ที่วางเครื่อง:** socket บนรถเข็น ปล่อยมือหรือตก → กลับเอง (`ReturnToSocket`)
- **นั่ง (`Chair43`):** วาร์ปไปท่าก้มเหนือแขน ปิด locomotion เครื่องเข้ามือขวา ปล่อย grip = วางคืน; จำกัดหัว (`SeatHeadLimit`)
- **ลูกค้า:** สูงสุด 3 คน (เตียง 1 + โซฟา 2) เดินตาม waypoint แล้ววาร์ปเข้าท่า ตัวละคร 4 ตัวสุ่มไม่ซ้ำ
- **ความพึงพอใจ:** ส่งงานแล้วอิโมจิเด้งเหนือหัวตามคะแนน (≥90 ตาหัวใจ / ≥70 ยิ้ม / ≥40 เฉย / ต่ำกว่าโกรธ, ปรับ `moods` ได้) ค้างจนเดินออกประตู; เสียงลูกค้า (ทักทาย ร้องเจ็บ ตามอารมณ์) แยกชาย/หญิง/รายตัว ยังไม่มีไฟล์ → [วิธีใส่เสียง](docs/customer-reactions.md)
- **ลายและคะแนน:** นอนแล้วสุ่มลาย ส่งงาน (ปุ่ม DONE บนการ์ด / J) → คุณภาพ 0–100 = Accuracy × ตัวหักความเลอะ (ถ่วงตามระยะ ผ่อนตามขนาดหัว) − หักเกินเวลา; แต้ม (log เท่านั้น) = คุณภาพ × ตัวคูณระดับ × (1 + โบนัสเร็ว)
- **เงินและทิป (`MoneyRules`):** ค่าสัก = ราคาลาย $50/$80/$120 × คุณภาพ/100 × (1 + โบนัสเร็ว), 3 ดาวได้ทิปสุ่ม 10–30% ของราคา, เดินออก $0; การ์ดแสดง "+$x" / "TIP +$x" + ตัวเลขลอยขึ้น (`MoneyPopup`, มีช่องเสียงเหรียญว่างไว้) และ "BANK $x" แทนคะแนนรวม → [shop-day.md](docs/shop-day.md)
- **วันทำงาน (`ShopDayManager`):** ป้าย "Day N" นีออน 2 วิ (`DayBanner`) → ลูกค้า 5 คน/วัน (`customersPerDay`, นับคนที่เดินออกด้วย) → คนสุดท้ายถึงประตูแล้วขึ้นแผ่นสรุป (`DaySummaryPanel`: ค่าสัก ทิป รวม คะแนนเฉลี่ย พอใจ/โกรธ ดาว BANK + ปุ่ม NEXT DAY / RESTART); วันถัดไป ramp ลายยาก (เต็มวันที่ 6) และความอดทน ×0.92/วัน (ต่ำสุด 0.6); เซฟ `{day, money}` ลง `persistentDataPath/vrtattoo_save.json` ตอนจบวัน, RESTART = วัน 1 $0 → [shop-day.md](docs/shop-day.md)
- **ความเจ็บ (`CustomerPain`):** 0–100% บนการ์ด ขึ้นตามหัวเข็มใหญ่ ค้างจุดเดิม ลากเร็ว ลดเมื่อหยุด ทนไม่เท่ากัน (สุ่ม); ≥60% สะดุ้ง (แขนกระตุก เส้นกระโดด 1.5 มม. หน้าเจ็บ) 100% เดินออก $0 อิโมจิโกรธ → [customer-reactions.md](docs/customer-reactions.md)
- **ความอดทน (`CustomerPatience`):** โซฟา 150–220 วิ หมดแล้วเดินออก; เตียงตามระดับลาย 60/90/130 วิ (ทั้งสองแบบ × ตัวคูณของวัน; แถบเวลาบนการ์ด + แถบจิ๋วคิวโซฟา) ใกล้หมดอิโมจิหงุดหงิด; เร็วได้โบนัสสูงสุด +25%, เกินเวลาหัก 1/วิ → [scoring.md](docs/scoring.md#เวลา)
- **3 ดาว:** ประกาย + พลุกระดาษรอบการ์ด (particle ≤ 52) + แขนวิ้ง (`_Glint` + ประกาย 16) → [ui.md](docs/ui.md)
- **ห้อง/แสง:** ร้านสักโทนมืด แสง bake (ไฟ realtime ดวงเดียว = โคม spot เหนือเตียง), ป้ายนีออนตัวหวัด Pacifico TATTOO (ชมพู) + No Regrets (ฟ้าอมม่วง) กะพริบเบา ๆ ไม่พร้อมกัน (`NeonFlicker`), LED ชมพูใต้ขอบเพดาน, ลำแสงปลอม (`LightShaft` additive) ใต้โคม, โคมตั้งพื้นข้างโซฟา, ภาพ flash art จากคลังลาย; ย้ายของ static/ไฟ baked ต้อง bake ใหม่ → [lighting.md](docs/lighting.md)
- **UI (สไตล์นีออน):** แผ่นดำโปร่ง ขอบชมพู (`NeonPanel`) ฟอนต์ Inter Bold; การ์ดงานเดียวตรงหน้าตอนนั่ง (`JobCard`: ชื่อลาย+ระดับ+DONE → คะแนนนับขึ้น ดาว ค่าสัก ทิป, เงินรวม BANK), ป้าย Day + แผ่นสรุปวัน, ป้ายปุ่มที่เก้าอี้ตามโหมด (`SeatHintPanel`) → [ui.md](docs/ui.md)
- **Desktop Test Mode (K, Editor):** เมาส์คุมเครื่อง วาด เติมหมึก เปลี่ยนหัว ซูม

## สคริปต์สำคัญ
- สัก: `TattooMachine` (วาด, `ExternalTrigger`), `TattooCanvas` (RT หมึก + needle map), `TattooInk`/`InkCap`, `TattooNeedle`/`NeedleCartridge`, `InkHologram`/`InkGauge`, `ReturnToSocket`
- ผู้เล่น: `TattooSeat`, `SeatHeadLimit`, `TattooDesktopTestMode` (reflection เข้า simulator)
- ลูกค้า: `CustomerQueueManager`, `Customer`, `CustomerRoute`, `CustomerCharacter` (ท่า/IK), `CustomerCharacterPicker`, `CustomerTattooSkin`, `CustomerEmote`, `CustomerVoice`/`CustomerVoiceSet`, `CustomerPain`, `CustomerPatience` (+ `PatienceScoring`)
- งาน/คะแนน: `TattooJobManager` (`scoring`, `money`), `TattooScorer`, `StencilPlacement`, `StencilLibrary`
- ร้าน: `ShopDayManager` (+ `DayStats`), `ShopSave`, `MoneyRules`
- UI: `JobCard`, `NeonButton`, `NeonPanel` (shader `NeonPanel`), `NeonBar` (เกจ), `SeatHintPanel`, `DayBanner`, `DaySummaryPanel`, `MoneyPopup`

เมนู: `VRTattoo/Simulate Tattoo Scores` (ตารางคะแนนจำลอง ~30 วิ), `VRTattoo/Rebuild Stencil Library`, `VRTattoo/Characters/Setup Selected Models`

## เอกสารรายละเอียด (`docs/`)
- [systems.md](docs/systems.md) ระบบนั่ง, test mode, สเกลห้อง, collider, คิวลูกค้า
- [scripts.md](docs/scripts.md) สคริปต์ shader material แบบละเอียด
- [scoring.md](docs/scoring.md) สูตรคะแนน ค่าที่จูน ผลจำลอง
- [shop-day.md](docs/shop-day.md) เงิน ทิป วันทำงาน ความยากตามวัน เซฟ
- [stencils.md](docs/stencils.md) เพิ่ม/ตัดลาย
- [machine-ink-needles.md](docs/machine-ink-needles.md) เครื่องสัก หมึก หัวเข็ม รถเข็น, เพิ่มสี/ขนาดหัว
- [characters.md](docs/characters.md) ตัวละคร Animator วิธีเพิ่มตัวละคร
- [tattoo-skin.md](docs/tattoo-skin.md) ผิวที่สักบนแขน
- [scene-objects.md](docs/scene-objects.md) GameObject และตำแหน่ง
- [lighting.md](docs/lighting.md) วัสดุห้อง ไฟ ป้ายนีออน การ bake
- [ui.md](docs/ui.md) สไตล์ UI การ์ดงาน ปุ่ม DONE ป้าย Day แผ่นสรุปวัน ป้ายบอกปุ่ม ขนาดตัวหนังสือ ตำแหน่ง
- [customer-reactions.md](docs/customer-reactions.md) อิโมจิความพึงพอใจ, **วิธีใส่เสียงลูกค้า**
- [known-issues.md](docs/known-issues.md) ข้อจำกัดแบบละเอียด

## ปุ่มควบคุม

### แว่น VR (OpenXR, ค่า XRI Starter Assets)
| ทำอะไร | ปุ่ม |
|---|---|
| เดิน / หมุนตัว / teleport | สติ๊กซ้าย / สติ๊กขวา / ดันสติ๊กขวาไปข้างหน้า (ปิดตอนนั่ง หมุนตัวยังได้) |
| นั่ง / ลุก (เก้าอี้ `Chair43`) | B ขวา (ยืนใกล้เก้าอี้) หรือชี้เก้าอี้ + grip |
| หยิบเครื่องสัก | grip (นั่งแล้วเครื่องเข้ามือขวาเอง) ปล่อย grip = กลับที่วาง |
| สัก | trigger ค้างตอนปลายเข็มแตะแขน (สั่นตามจังหวะ) |
| เติม/เปลี่ยนสีหมึก | จุ่มเข็มในถ้วย + trigger ค้าง |
| เปลี่ยนหัวเข็ม | จิ้มหลอดหัวเข็มบนชั้น + trigger ค้าง 0.4 วิ |
| กดปุ่มบน UI (DONE, NEXT DAY, RESTART) | ชี้ ray + trigger, grip หรือจิ้ม |

### บนคอม (Editor: XR Device Simulator + Desktop Test Mode)
| ทำอะไร | ปุ่ม |
|---|---|
| หันมอง | คลิกขวาค้าง + ลาก |
| เดิน (simulator) | WASD / Q E |
| คุม controller ขวา | Space ค้าง (Y = ล็อก), Tab สลับอุปกรณ์ |
| หยิบ / ปล่อย | G |
| วาด (trigger) | คลิกซ้าย |
| นั่ง / ลุก | F, N ตอนคุม controller ขวา (= B), ชี้เก้าอี้ + G |
| วางเครื่องคืนตอนนั่ง | simulator: คุม controller ขวา + กด G แล้วปล่อย; Desktop mode: P |
| เปิด/ปิด Desktop Test Mode | K |
| หยิบเครื่องตอนนั่ง (Desktop mode) | หันไปรถเข็น + คลิกซ้ายที่เครื่อง |
| วาด / เติมหมึก / เปลี่ยนหัว (Desktop mode) | ชี้เมาส์ที่แขน / ถ้วย / หัวเข็ม + คลิกซ้ายค้าง (หัวเข็มต้องหันขวานิดหน่อย) |
| ซูม (Desktop mode) | ลูกกลิ้ง |
| ส่งงาน | J, ชี้ DONE ด้วย controller ซ้าย + คลิกซ้าย หรือ G, Desktop mode: คลิก DONE |
| NEXT DAY / RESTART | เหมือน DONE (ชี้ + คลิกซ้าย/G, Desktop mode: คลิก) |
| ล้างเซฟ | ลบ `%USERPROFILE%/AppData/LocalLow/DefaultCompany/VRTattoo_2/vrtattoo_save.json` หรือกด RESTART |

## ข้อจำกัดที่ค้างอยู่ (ละเอียดใน [known-issues.md](docs/known-issues.md))
- ยังไม่ได้ลองกับแว่นจริง: haptics, ระยะเอื้อม, ท่าจับเครื่อง, จอมืด, ท่านั่ง, ขนาด/ตำแหน่ง UI; test mode ทดสอบด้วย event จากสคริปต์
- ล็อกหัว/ก้มกล้องใน simulator พึ่ง reflection ของ XRI
- คะแนน: ลายพื้นทึบได้เปรียบเล็กน้อยตอนมือสั่น, ส่งงาน ~200 ms
- หมึกค้างข้ามลูกค้า, รอยสักไม่ถาวร, ขอบเส้นขรุขระ (RT 1024), ผิวที่สักเป็นภาพนิ่ง, ลายบิดใกล้ข้อมือ/ข้อศอก
- ยังไม่มีไฟล์เสียงลูกค้าและเสียงเหรียญ (ช่องว่างอยู่)
- ราคาลาย/ทิป/ความยากตามวันยังไม่ได้ลองเล่นจริง; แผ่นสรุปและป้าย Day วางตามทิศที่หันตอนนั้น (ถ้าหันไปทางผนังอาจทับของ)
- ค่าความเจ็บ/ความอดทนจูนจากการวาดจำลอง ยังไม่ได้ลองเล่นจริง
- แสงใหม่ยังไม่ได้ลองบนแว่นจริง (ความมืด/ความลื่น)
- ลูกค้าไม่มี collider และไม่มีท่าลุก/นั่งลง, socket ไม่กรอง layer, ป้ายเป็นภาษาอังกฤษ
- **ก่อนเผยแพร่:** โมเดลตัวละครติดลิขสิทธิ์และไม่อยู่ใน git (ก๊อป `Characters/Models/` + `SourceArt/Characters/` รวม `.meta`); ลายติดลิขสิทธิ์ `angry_duck`, `ghost_monster`, `cloud`

## ไอเดียถ้าจะพัฒนาต่อ (ไม่อยู่ในแผน)
- ลายที่กำหนดสีและให้คะแนนตามสี, เสียงเครื่องสัก/เสียงเหรียญ/เสียงลูกค้า, ป้ายภาษาไทย (ต้องมีฟอนต์ TMP ภาษาไทย)
- สักได้หลายส่วนของร่างกาย, ท่าลุก/นั่งลง/นอนลงแทนการวาร์ป, เปลี่ยนโมเดลทดสอบเป็นโมเดลที่ใช้ได้ถูกต้อง

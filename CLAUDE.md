# VRTattoo

เกม VR ร้านสัก: Unity 6000.3, URP 17.3, XRI 3.4.1, Input System, OpenXR ทดสอบบนคอมด้วย XR Device Simulator (อยู่ใน scene)

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
- `Scripts/Tattoo/` เครื่องสัก canvas หมึก หัวเข็ม เก้าอี้ test mode · `Scripts/Ink/` · `Scripts/Needles/` · `Scripts/Customers/` · `Scripts/Stencils/` ลาย+คะแนน (มี `Editor/`)
- `Materials/Shop/` วัสดุห้อง · `Textures/FlashArt/` (`Tools/flash/make_flash_art.py`) · `Lighting/` lighting settings
- `UI/Emotes/` PNG อิโมจิ (`Tools/emotes/draw_emotes.py`) · `Data/Voices/` ชุดเสียงลูกค้า
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
- **ลายและคะแนน:** นอนแล้วสุ่มลาย ส่งงาน (SUBMIT / J) → คุณภาพ 0–100 = Accuracy × ตัวหักความเลอะ (ถ่วงตามระยะ ผ่อนตามขนาดหัว) × ตัวคูณระดับ
- **ห้อง/แสง:** ร้านสักโทนมืด แสง bake (ไฟ realtime ดวงเดียว = โคม spot เหนือเตียง), นีออน TATTOO, LED ชมพูใต้ขอบเพดาน, ภาพ flash art จากคลังลาย; ย้ายของ static/ไฟ baked ต้อง bake ใหม่ → [lighting.md](docs/lighting.md)
- **Desktop Test Mode (K, Editor):** เมาส์คุมเครื่อง วาด เติมหมึก เปลี่ยนหัว ซูม

## สคริปต์สำคัญ
- สัก: `TattooMachine` (วาด, `ExternalTrigger`), `TattooCanvas` (RT หมึก + needle map), `TattooInk`/`InkCap`, `TattooNeedle`/`NeedleCartridge`, `InkHologram`/`InkGauge`, `ReturnToSocket`
- ผู้เล่น: `TattooSeat`, `SeatHeadLimit`, `TattooDesktopTestMode` (reflection เข้า simulator)
- ลูกค้า: `CustomerQueueManager`, `Customer`, `CustomerRoute`, `CustomerCharacter` (ท่า/IK), `CustomerCharacterPicker`, `CustomerTattooSkin`, `CustomerEmote`, `CustomerVoice`/`CustomerVoiceSet`
- งาน/คะแนน: `TattooJobManager` (`scoring`), `TattooScorer`, `StencilPlacement`, `StencilLibrary`

เมนู: `VRTattoo/Simulate Tattoo Scores` (ตารางคะแนนจำลอง ~30 วิ), `VRTattoo/Rebuild Stencil Library`, `VRTattoo/Characters/Setup Selected Models`

## เอกสารรายละเอียด (`docs/`)
- [systems.md](docs/systems.md) ระบบนั่ง, test mode, สเกลห้อง, collider, คิวลูกค้า
- [scripts.md](docs/scripts.md) สคริปต์ shader material แบบละเอียด
- [scoring.md](docs/scoring.md) สูตรคะแนน ค่าที่จูน ผลจำลอง
- [stencils.md](docs/stencils.md) เพิ่ม/ตัดลาย
- [machine-ink-needles.md](docs/machine-ink-needles.md) เครื่องสัก หมึก หัวเข็ม รถเข็น, เพิ่มสี/ขนาดหัว
- [characters.md](docs/characters.md) ตัวละคร Animator วิธีเพิ่มตัวละคร
- [tattoo-skin.md](docs/tattoo-skin.md) ผิวที่สักบนแขน
- [scene-objects.md](docs/scene-objects.md) GameObject และตำแหน่ง
- [lighting.md](docs/lighting.md) วัสดุห้อง ไฟ ป้ายนีออน การ bake
- [customer-reactions.md](docs/customer-reactions.md) อิโมจิความพึงพอใจ, **วิธีใส่เสียงลูกค้า**
- [known-issues.md](docs/known-issues.md) ข้อจำกัดแบบละเอียด

## ปุ่มทดสอบ (XR Device Simulator)
| ทำอะไร | ปุ่ม |
|---|---|
| คุม controller ขวา | Space ค้าง (Y = ล็อก), Tab สลับอุปกรณ์ |
| หยิบ / ปล่อย | G |
| วาด (trigger) | คลิกซ้าย |
| นั่ง / ลุก | F, N ตอนคุม controller ขวา (= B), ชี้เก้าอี้ + G; แว่นจริงใช้ B ขวา |
| วางคืนตอนนั่ง | simulator: คุม controller ขวา + กด G แล้วปล่อย; Desktop mode: P |
| หยิบเครื่องตอนนั่ง (Desktop mode) | หันไปรถเข็น + คลิกซ้ายที่เครื่อง |
| เปิด/ปิด Desktop Test Mode | K |
| วาด / เติมหมึก / เปลี่ยนหัว (Desktop mode) | ชี้เมาส์ที่แขน / ถ้วย / หัวเข็ม + คลิกซ้ายค้าง (หัวเข็มต้องหันขวานิดหน่อย) |
| หันมอง | คลิกขวาค้าง + ลาก |
| ซูม (Desktop mode) | ลูกกลิ้ง |
| ส่งงาน | J (Editor) หรือชี้ปุ่ม SUBMIT ด้วย controller ซ้าย + G |

## ข้อจำกัดที่ค้างอยู่ (ละเอียดใน [known-issues.md](docs/known-issues.md))
- ยังไม่ได้ลองกับแว่นจริง: haptics, ระยะเอื้อม, ท่าจับเครื่อง, จอมืด, ท่านั่ง; test mode ทดสอบด้วย event จากสคริปต์
- ล็อกหัว/ก้มกล้องใน simulator พึ่ง reflection ของ XRI
- คะแนน: ลายพื้นทึบได้เปรียบเล็กน้อยตอนมือสั่น, ส่งงาน ~200 ms
- หมึกค้างข้ามลูกค้า, รอยสักไม่ถาวร, ขอบเส้นขรุขระ (RT 1024), ผิวที่สักเป็นภาพนิ่ง, ลายบิดใกล้ข้อมือ/ข้อศอก
- ยังไม่มีไฟล์เสียงลูกค้า (ช่องว่างอยู่)
- แสงใหม่ยังไม่ได้ลองบนแว่นจริง (ความมืด/ความลื่น)
- ลูกค้าไม่มี collider และไม่มีท่าลุก/นั่งลง, socket ไม่กรอง layer, ป้ายเป็นภาษาอังกฤษ
- **ก่อนเผยแพร่:** โมเดลตัวละครติดลิขสิทธิ์และไม่อยู่ใน git (ก๊อป `Characters/Models/` + `SourceArt/Characters/` รวม `.meta`); ลายติดลิขสิทธิ์ `angry_duck`, `ghost_monster`, `trophy`, `cloud`

## งานถัดไป (ทำตามลำดับ)
1. ลายที่กำหนดสี และให้คะแนนตามสี
2. เสียงเครื่องสัก (AudioListener อยู่บน Main Camera แล้ว), ป้ายภาษาไทย (ต้องมีฟอนต์ TMP ภาษาไทย)
3. สักได้หลายส่วนของร่างกาย (ต้นแขน หลัง อาจต้องมีท่านอนคว่ำ) และให้ลูกค้ายื่น/เปลี่ยนท่าแขน

ค้างไว้: เปลี่ยนโมเดลทดสอบเป็นโมเดลที่ใช้ได้ถูกต้อง; ท่าลุก/นั่งลง/นอนลงแทนการวาร์ป

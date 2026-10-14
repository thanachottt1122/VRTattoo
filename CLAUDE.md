# VRTattoo

เกม VR ร้านสัก: Unity 6000.3, URP 17.3, XR Interaction Toolkit 3.4.1, Input System, OpenXR
ทดสอบบนคอมด้วย XR Device Simulator (อยู่ใน scene แล้ว)

- **ตอบผู้ใช้เป็นภาษาไทยเสมอ** (ชื่อไฟล์ โค้ด และคำศัพท์เทคนิคใช้ภาษาอังกฤษได้)
- **ทำงานบน branch `main` เสมอ** ห้ามสร้าง branch ใหม่โดยไม่ถามผู้ใช้ก่อน
- Scene หลัก: `Assets/_VRTattoo/Scenes/TattooRoom.unity`
- ไฟล์ของโปรเจกต์ทั้งหมดอยู่ใต้ `Assets/_VRTattoo` (ห้ามแก้ `Assets/Samples`)
- แก้ scene ผ่าน UnityMCP (`execute_code` + Undo) แล้วเซฟ scene ทุกครั้งหลังแก้

## สิ่งที่ทำเสร็จแล้ว
- **ระบบสัก:**
  - เครื่องสัก (โมเดลจริง ดูหัวข้อ "เครื่องสัก") หยิบได้ กด trigger ตอนปลายเข็มแตะแขน (Capsule) จะวาดเส้นลง RenderTexture และส่ง haptics
  - ขนาดเส้นกำหนดเป็นเมตร (5 มม.) ระบบชดเชยการยืดของ UV ให้
  - **สักลงแขนจริงของลูกค้า:** แขนท่อนล่างข้างที่หันมาฝั่งเก้าอี้ช่าง (ตอนนี้คือแขนขวา) ดูหัวข้อ "ผิวที่สัก" ล้างเองเมื่อมีลูกค้าใหม่ (canvas เป็นของลูกค้าแต่ละคน)
  - ลากเร็วแล้วเส้นไม่ขาด และตัดเส้นตอนข้ามรอยต่อ UV
  - **หมึกหลายสีและหมึกจำกัด:** ดูหัวข้อ "หมึก"
- **ที่วางเครื่องสัก:** ถ้วยเสียบ `MachineHolder` บนรถเข็น `InkTrolley` (`XRSocketInteractor`) เริ่มเกมเครื่องเสียบอยู่ ปล่อยมือหรือตกแมปแล้วกลับเข้าที่วางเอง (แท่นวางเดิม `TattooMachineStand` ลบแล้ว)
- **ระบบนั่ง (Chair43):**
  - วาร์ปไปท่าทำงาน: ตาอยู่ห่างผิวด้านบนของแขน ~38 ซม. (ห่างแนวนอน 0.25 ม. ไปทางเก้าอี้ + สูงกว่าแกนแขน 0.33 ม. = `leanOverTarget`, `workHorizontalDistance`, `workEyeHeight`) เหมือนช่างที่ก้มทำงาน หันหน้าหาแขน **ไม่ซูมกล้องในแว่น**
  - ปิด: Move, Teleportation, Climb, Jump, Gravity, Grab Move ×3, Teleport Interactor (ray ซ้าย/ขวา), Climb Teleport และ CharacterController (หมุนตัวยังได้) ลุกแล้วเปิดคืนเฉพาะตัวที่เคยเปิดอยู่
  - นั่งแล้วเครื่องสักเข้ามือขวาอัตโนมัติ (`handInteractor.StartManualInteraction`) ถ้ายังไม่ได้ถืออยู่
  - ระหว่างนั่ง กด grip แล้วปล่อย = วางเครื่องคืนที่วาง (`TattooSeat.Update` เห็น `selectInput.ReadWasCompletedThisFrame` → `EndManualInteraction` → `ReturnToSocket`) แล้วหยิบใหม่จากที่วางด้วย grip ได้ (ที่วางอยู่ข้างไหล่ขวา ~15 ซม.) หยิบเองแบบนี้เป็นการถือปกติ ปล่อย grip = กลับที่วาง
  - ลุกแล้วเครื่องกลับที่วาง (`TattooSeat.heldTool` ถูกปล่อยจากมือที่ถืออยู่)
  - **ล็อกหัวตอนนั่ง (`SeatHeadLimit`):** ที่เดินได้ตอนนั่งมาจาก simulator ขยับตำแหน่ง HMD (WASD/Q/E/ลูกกลิ้ง) ไม่ใช่ locomotion (ปิดครบแล้ว) ใน simulator จึงล็อกตำแหน่งหัวไว้ หันมองได้ปกติ controller ยังขยับได้; บนแว่นจริงขยับหัวได้อิสระในรัศมี 50 ซม. จากจุดตาตอนนั่ง เกินแล้วจอค่อย ๆ มืด (มืดสนิทที่ 65 ซม.) กลับเข้ามาแล้วสว่างคืน ไม่ย้ายกล้องเอง
- **Collider / แก้การตก:**
  - ย้าย XR Origin ขึ้นมาบนพื้น (y = -1.797)
  - เพิ่ม BoxCollider ใต้พื้นปิดรอยแยกของ MeshCollider และเพิ่มเข้า TeleportationArea แล้ว
  - ใส่ BoxCollider ให้ผนัง เพดาน ตู้ และเฟอร์นิเจอร์ เตียงใช้ MeshCollider
- **Desktop Test Mode:**
  - ปุ่ม K เปิด/ปิด ทำงานเฉพาะใน Editor
  - ตอนนั่ง (เครื่องเข้ามือเองอยู่แล้ว) `releaseKey` P = วางคืน, คลิกซ้ายที่เครื่องบนที่วาง = หยิบเข้า `handInteractor` (`StartManualInteraction`, คลิกที่ใช้หยิบไม่นับเป็น trigger)
  - ถือเครื่องแล้ว เมาส์วางปลายเข็มบนแขน คลิกซ้ายวาด และหัว simulator ก้มมองแขนอัตโนมัติ
  - ชี้เมาส์ที่ถ้วยหมึกได้ด้วย: ปลายเข็มลงไปอยู่ใน trigger ของถ้วย (เอียงตามเมาส์ `capTiltTowardView`) คลิกซ้ายค้าง = กด trigger → เติม/เปลี่ยนสีเหมือนในแว่น (ไม่มีปุ่ม R/1–4 แล้ว) ใช้ได้แม้ยังไม่มีลูกค้าบนเตียง หัวก้มมองแขนเมื่อผิวพร้อม (เปิดโหมดก่อนลูกค้านอนก็ได้)
  - เลื่อนลูกกลิ้งเมาส์ = ซูม (ปรับ FOV 20°–FOV เดิมของกล้อง ทีละ 4°) ระหว่างนั้นพัก scroll ของ simulator ปิดโหมดแล้วคืน FOV; ไม่ทำงานเมื่อมีแว่นจริง (`XRSettings.isDeviceActive`)
  - ไม่กระทบแว่นจริง
- **Audio Listener** อยู่บน Main Camera
- **สเกลห้องเท่าของจริง** (ขยายรอบจุด (0.32, -1.797, -0.05) ระดับพื้นจึงไม่เปลี่ยน):
  - `Room`, `Furniture`, `Light Probe Group` ×1.48 → เพดาน 2.66 ม., โซฟา 0.95 ม. (เบาะ 0.47), สตูล Chair43 0.65 ม.
  - `hospital bed` + `Cushion38` ×1.39 → ยาว 1.90 สูง 0.70; `Door_7_White` ×1.55 → สูง 2.0 ม.
  - TattooSystem ไม่ขยาย (สร้างเป็นเมตรจริงอยู่แล้ว) แค่ย้ายตำแหน่ง; XR Origin / Simulator ไม่ขยาย
  - `seatedEyeHeight` = 1.40 (ใช้เฉพาะเมื่อปิด `leanOverTarget`), `Camera Y Offset` = 1.6
  - ไม่มี object ไหนเป็น GI static จึงไม่มี lightmap (ไฟ realtime ทั้งหมด) LightingData อยู่ที่ `Scenes/TattooRoom/`
- **ระบบลูกค้า (โมเดลคน 4 ตัว สุ่มไม่ซ้ำคนก่อนหน้า มีท่าเดิน/นั่ง/นอน ดูหัวข้อ "ตัวละครลูกค้า"):**
  - ในร้านมีได้ 3 คน: 1 คนบนเตียง + 2 คนบนโซฟารอ ลูกค้าโผล่ที่ประตูทุก `spawnInterval` จนกว่าที่จะเต็ม
  - เดินเส้นตรง (ไม่ใช้ NavMesh) ผ่าน waypoint ของ `CustomerRoute` ถึงแล้ววาร์ปไปท่านั่ง/นอน
  - `CompleteTattoo()`: คนบนเตียงเดินออกประตูแล้วหายไป คิวเลื่อนขึ้น และมีคนใหม่มาเติม ถ้าคนบนเตียงยังไม่นอนลงจะไม่ทำงาน (ตอนนี้ `TattooJobManager` เป็นคนเรียกหลังส่งงาน)
  - ล้างลายบนแขนตอนที่คนใหม่นอนลงเตียง
- **ลายสักและคะแนน:**
  - ลูกค้านอนลงแล้วสุ่มลายจากคลัง (ช่วงแรกได้ easy บ่อย ยิ่งเสร็จหลายคนยิ่งเจอ hard บ่อยขึ้น ไม่ซ้ำกับคนก่อน) แสดงเป็นลายเส้นสีม่วงจางบนแขนท่อนล่าง (กรอบลาย 14 ซม. ตั้งตามแนวแขน หัวลายชี้ไปทางข้อศอก ด้านกว้างพันรอบแขน) แยกจากหมึกจริง พร้อมป้ายชื่อลายและระดับ
  - กดปุ่ม SUBMIT ข้างเตียง (ray/grip มือซ้าย) หรือ J (เฉพาะ Editor) เพื่อส่งงาน: คะแนน 0–100 ลอยเหนือเตียง 2.5 วิ แล้วลูกค้าเดินออก
  - คุณภาพ = 100 × Accuracy × max(0, 1 − messRatio / `messLimit`)^`messPower`
    - Accuracy = สัดส่วนเส้นลายที่มีหมึกอยู่บนหรือห่างไม่เกิน `coverTolerance` 1.5 มม.
    - messRatio = **พื้นที่**หมึกที่ห่างจากขอบเส้นลายเกิน `messTolerance` 2 มม. (ทั้ง canvas ละเลงนอกกรอบลายก็นับ) ÷ พื้นที่ลาย
    - `messLimit` 1 = เลอะเท่าพื้นที่ลายแล้วเหลือ 0, `messPower` 2 ทุกค่าปรับที่ `TattooJobManager.scoring`
    - ป้ายคะแนนแสดง `Accuracy 92% / Mess -35` (Mess = round(100 × Accuracy) − คุณภาพ สีแดงเมื่อโดนหัก)
  - ผลจำลอง (เมนู `VRTattoo/Simulate Tattoo Scores` ใช้ค่าจาก scene, ต่ำสุด/เฉลี่ย/สูงสุด):

    | | ตามเส้นเรียบร้อย (เข็ม 5 มม. ส่าย ±0.5 มม. ขาด 2%) | มือสั่น (ส่าย ±3 มม. ขาด 10%) | ละเลงทั้งกรอบลาย | ละเลงนอกลาย |
    |---|---|---|---|---|
    | easy | 99 / 99.8 / 100 | 77 / 89.6 / 98 | 0 | 0 |
    | medium | 98 / 99.7 / 100 | 40 / 71.3 / 93 | 0 | 0 |
    | hard | 99 / 99.8 / 100 | 71 / 77.6 / 90 | 0 | 0 |

    ลายที่เป็นพื้นทึบหนา (cloud, crown skull, karate kick, biohazard, royal crown) มือสั่นแล้วยังได้ 90–98 เพราะเลอะนิดเดียวเทียบกับพื้นที่ลาย ส่วน trophy (เส้นบาง 1.4 มม.) ได้ 40
  - คะแนนรวม = คุณภาพ × ตัวคูณ (easy x1, medium x1.5, hard x2) แสดงบนผนังมุมบนของห้อง

## สคริปต์ (`Assets/_VRTattoo/Scripts/Tattoo/`)
| ไฟล์ | หน้าที่ |
|---|---|
| `TattooCanvas.cs` | อยู่บน collider ของผิวที่สัก (สร้างโดย `CustomerTattooSkin`) RenderTexture 1024² พื้นโปร่งใส alpha = หมึก มี `DrawStroke(uvFrom, worldFrom, uvTo, worldTo)` ปั๊มแปรงด้วย GL และ `MetersPerUV` (ผู้สร้าง UV เป็นคนตั้ง) ไม่ผูกกับ renderer |
| `TattooMachine.cs` | อยู่บนเครื่องสัก ฟัง activate/deactivate ของ `XRGrabInteractable` raycast จาก `tip` ไปตามแนวเข็ม แล้ววาดและส่ง haptics มี `ExternalTrigger` ให้โหมดทดสอบสั่งวาดได้ |
| `ReturnToSocket.cs` | อยู่บนเครื่องสัก ปล่อยมือ (ที่ไม่ใช่ socket) หรือตกต่ำกว่า `fallResetHeight` จะ `SelectEnter` กลับเข้า socket ในเฟรมถัดไป |
| `TattooSeat.cs` | อยู่บน `Furniture/Chair43` (ตัวแม่) สลับนั่ง/ลุก (ปุ่ม B ขวา / F / ชี้เก้าอี้ + grip) ปิดและเปิดคืนเฉพาะ locomotion ที่เคยเปิดอยู่ ตอนนั่งใส่ `heldTool` เข้ามือ `handInteractor` (ปล่อย grip = วางคืน) ตอนลุกปล่อยจากมือที่ถืออยู่ ตำแหน่งตาตอนนั่งคิดจาก `lookTarget` (`SeatedEyePosition()`) |
| `SeatHeadLimit.cs` | อยู่บน `Furniture/Chair43` จำจุดตาตอนนั่ง: จอมืดตามระยะหัว (`freeRadius` 0.5, `fadeDistance` 0.15, `fadeSpeed`) ผ่าน `fadeRenderer` (Quad `Main Camera/ScreenFade` + `Shaders/ScreenFade.shader`, `Materials/ScreenFade.mat`) และ `lockSimulatorHead` เขียนตำแหน่ง `m_HMDState` ของ simulator กลับผ่าน reflection + `InputState.Change` (execution order หลัง simulator) ทำเฉพาะเมื่อ `!XRSettings.isDeviceActive` |
| `TattooInk.cs` | อยู่บนเครื่องสัก ดูหัวข้อ "หมึก" |
| `InkHologram.cs` | อยู่บน `Interactables/TattooSystem/InkHologram` วงแหวนโฮโลแกรมบอกหมึก ดูหัวข้อ "หมึก" |
| `TattooDesktopTestMode.cs` | อยู่บน `Interactables/TattooSystem` โหมดทดสอบด้วยเมาส์ ปิด `trackPosition/Rotation` ของเครื่องระหว่างใช้ และหมุนหัว XR Device Simulator ผ่าน reflection (ฟิลด์ private `m_CenterEyeEuler`, `m_HMDState`) ซูมด้วยลูกกลิ้ง (`Zoom()`, `minFieldOfView`, `zoomStep`) |

## สคริปต์ลูกค้า (`Assets/_VRTattoo/Scripts/Customers/`)
| ไฟล์ | หน้าที่ |
|---|---|
| `CustomerQueueManager.cs` | อยู่บน `CustomerSystem` มี slot 0 = เตียง, 1.. = `waitSeats` (ตัวที่ 0 เป็นคิวถัดไป) จองช่องตั้งแต่ตอนสั่งเดิน ถ้าคิวเลื่อนตอนลูกค้ายังเดินอยู่ จะเปลี่ยนเส้นทางเมื่อถึงที่ มี `CompleteTattoo()`, `IsCustomerReady`, event `onCustomerLayDown`/`onCustomerLeft` |
| `Customer.cs` | อยู่บน prefab `PlaceAt`, `WalkTo(location, waypoints, onArrived)`, `Settle()` (วาร์ปเข้าท่าของ location) |
| `CustomerLocation.cs` | จุดหนึ่งในร้าน: `approach` (จุดยืนบนพื้นตอนเริ่ม/จบการเดิน), `pose` (จุดนั่ง/นอน), `arrivalPose` |
| `CustomerRoute.cs` | waypoint อ้อมระหว่าง `from`↔`to` (ลูกของมันตามลำดับ ใช้ได้ทั้งสองทิศ) ไม่มีลูก = เดินตรง |
| `CustomerVisual.cs` | คลาสแม่ของหน้าตาลูกค้า: `SetPose(Idle/Walk/Sit/Lie)`, `SetMoveSpeed` |
| `CapsuleCustomerVisual.cs` | placeholder: Capsule สีสุ่ม (MaterialPropertyBlock `_BaseColor`) เก็บไว้เป็น fallback (ไม่ได้ใช้แล้ว) |
| `AnimatorCustomerVisual.cs` | อยู่บน `Customer.prefab` ตั้ง Animator `Speed` (float), `Sit`, `Lie` (bool) สั่ง `CustomerCharacter.ApplyPose` และตอนนอนเรียก `CustomerTattooSkin.Build`, ท่าอื่นเรียก `Release` |
| `CustomerTattooSkin.cs` | อยู่บน `Customer.prefab` ดูหัวข้อ "ผิวที่สัก" |
| `CustomerCharacter.cs` | อยู่บน root ของ prefab ตัวละคร (ตัวเดียวกับ Animator) ค่าปรับรายตัว: `sitOffset/sitRotation`, `lieOffset/lieRotation` (ท้องถิ่นของลูกค้า; คลิปนอนหันหัวไป −Z จึงหมุน 180°), และ `OnAnimatorIK`: แขนทั้งสองข้างวางข้างลำตัว (`armsAlongBody`, `handOutward`, `armRestHeight`), เท้าวางบนเตียงห่างกลางตัว `footSpread` (`feetOnBed`, `ankleHeight`) |
| `CustomerCharacterPicker.cs` | อยู่บน `Customer.prefab` รายการ `characters` ตอน `Awake` สุ่มตัวละครที่ไม่ซ้ำกับคนก่อนหน้า และเลี่ยงตัวที่อยู่ในร้านแล้วถ้าเลือกได้ สร้างเป็นลูกแล้วส่งให้ `AnimatorCustomerVisual.SetCharacter` |
| `Editor/CharacterModelSetup.cs` | เมนู `VRTattoo/Characters/Setup Selected Models`: ตั้ง Rig = Humanoid, extract material ไป `<โฟลเดอร์โมเดล>/Materials` เป็น URP Lit, ใส่ texture ที่ชื่อขึ้นต้นด้วยชื่อ material (Diffuse/Color/_di → base, Normal/_tn → normal, Opacity → รวมเป็น alpha + Alpha Clipping) |

## สคริปต์ลาย/คะแนน (`Assets/_VRTattoo/Scripts/Stencils/`)
| ไฟล์ | หน้าที่ |
|---|---|
| `StencilLibrary.cs` | ScriptableObject `Data/StencilLibrary.asset` รายการลาย (texture, ชื่อ, ระดับ) + `Multiplier()` + แยกชื่อไฟล์ |
| `Editor/StencilLibraryPostprocessor.cs` | ตั้งค่า import PNG ใน `Stencils/` อัตโนมัติ (Single Channel Alpha, readable, ไม่บีบอัด) และสร้างคลังใหม่ทุกครั้งที่ไฟล์เปลี่ยน มีเมนู `VRTattoo/Rebuild Stencil Library` |
| `StencilPlacement.cs` | struct mapping UV ของ canvas ↔ พิกัดในภาพลาย (st) ใช้ร่วมกันระหว่าง overlay shader และ scorer |
| `TattooScorer.cs` | อ่าน RenderTexture ของ TattooCanvas ทั้งรูป (หมึก = alpha > 0.5) เทียบกับ mask ลาย (`StencilMask`) แล้วคิด accuracy/messRatio/quality ใน `ScoreMasks` (ฟังก์ชันล้วน ใช้ร่วมกับตัวจำลอง) ~50 ms ต่อการส่งงาน |
| `Editor/TattooScoreSimulator.cs` | เมนู `VRTattoo/Simulate Tattoo Scores` สร้างหมึกจำลอง 4 แบบให้ทุกลายในคลัง แล้ว log ตารางคะแนนแยกระดับ (ทำงานใน Edit mode) |
| `TattooJobManager.cs` | อยู่บน `CustomerSystem` ฟัง `onCustomerLayDown` สุ่มลาย แสดงลาย/ป้าย รับ Submit (ปุ่ม/J) ให้คะแนน แล้วเรียก `CompleteTattoo()` |

- Shader: `Shaders/TattooSkinOverlay.shader` (unlit โปร่งใส: หมึกจาก `_InkTex` + ลายสีม่วงจาง), Material: `Materials/Tattoo/TattooSkinOverlay.mat`

### เพิ่มลายใหม่
1. ทำ PNG พื้นโปร่งใส เส้นดำ (แนะนำ 1024×1024 ลายอยู่กลาง เว้นขอบ ~10%) ระบบใช้แค่ alpha
2. ตั้งชื่อ `easy_<ชื่อ>.png` / `medium_<ชื่อ>.png` / `hard_<ชื่อ>.png` (ชื่อใช้ `_` แทนช่องว่าง เช่น `hard_royal_crown.png` → "ROYAL CROWN")
3. วางใน `Assets/_VRTattoo/Stencils/` คลังอัปเดตเอง ลบหรือเปลี่ยนชื่อไฟล์ก็อัปเดตเอง ไม่ต้องแก้โค้ด (ถ้าไม่อัปเดต ใช้เมนู `VRTattoo/Rebuild Stencil Library`)
4. ชื่อที่ไม่ขึ้นต้นด้วย easy_/medium_/hard_ จะถูกข้ามและขึ้น warning

### ตัดลายจากรูปรวม (`Tools/stencils/extract_stencils.py`)
- อ่าน `SourceArt/flash_sheet.jpg` → เขียน PNG ลง `Stencils/` (ลบ PNG เดิมในโฟลเดอร์ก่อน) และ `SourceArt/contact_sheet.png`
- ต้องมี `pip install --user opencv-python numpy pillow potracer` แล้วรัน `python Tools/stencils/extract_stencils.py`
- แก้กรอบ ชื่อ ระดับ หรือ threshold ของแต่ละลายที่ตาราง `DESIGNS` (name=None = ข้าม เช่นโลโก้แบรนด์)
- ข้ามโลโก้แบรนด์ไปแล้ว 5 อัน (Nike, adidas, Apple, Real Madrid, รองเท้าแถบ 3 เส้น)

- Prefab: `Assets/_VRTattoo/Prefabs/Customer.prefab` = `Customer` + `AnimatorCustomerVisual` + `CustomerCharacterPicker` (ไม่มีโมเดลในตัว ตัวละครถูกสร้างเป็นลูกตอน spawn) ระบบคิวไม่ได้แก้ (`customerPrefab` ยังเป็นตัวเดิม)
- Material capsule เดิม: `Materials/Customer/CustomerPlaceholder.mat`

## ตัวละครลูกค้า (`Assets/_VRTattoo/Characters/`)
- **โมเดลอยู่แค่ในเครื่องนี้:** `Characters/Models/` และ `SourceArt/Characters/` (zip ต้นฉบับ) อยู่ใน `.gitignore` เพราะเป็นโมเดลทดสอบที่ติดลิขสิทธิ์และไฟล์ใหญ่ (`Leni.fbx` 98 MB) ย้ายเครื่องต้องก๊อปสองโฟลเดอร์นี้ไปเอง **รวมไฟล์ `.meta`** (ไม่งั้น GUID เปลี่ยนแล้ว prefab ตัวละครจะหาโมเดลไม่เจอ) และ**ต้องเปลี่ยนเป็นโมเดลที่ใช้ได้ถูกต้องก่อนเผยแพร่**: Tomizawa (Like a Dragon) และ Tanya (Saints Row) เป็นโมเดล rip จากเกม, Vijay เป็นหน้านักแสดงจริง, Leni ไม่ทราบที่มา
- ส่วนที่ขึ้น git: prefab ตัวละคร, Animator, ท่าใน `Characters/Animations` (Mixamo), สคริปต์
- โมเดล (`Prefabs/Characters/Customer_<ชื่อ>.prefab` เป็น variant ของ FBX):

| Prefab | ไฟล์โมเดล | Controller | scale |
|---|---|---|---|
| `Customer_Leni` (ชาย, Character Creator) | `Models/kenny/Leni.fbx` | `CustomerAnimator` | 0.926 |
| `Customer_Tomizawa` | `Models/like-a-dragon-infinite-wealth-tomizawa/Tomizawa.fbx` (แตกจาก zip, ใช้ fbx ไม่ใช้ glb) | `CustomerAnimator` | 0.927 |
| `Customer_Tanya` (หญิง) | `Models/saintsrow1-tanya/Tanya.fbx` (import `globalScale` = 100, texture จับคู่ตาม `xps.xps` ใน zip) | `CustomerAnimator_Female` | 0.95 |
| `Customer_Vijay` | `Models/thalapathy-vijay-3d-model/source/Walking.fbx` (มีท่าเดินมาเอง) | `CustomerAnimator_Vijay` | 1.0 |

- `Animator/CustomerAnimator.controller`: state `Walk` (default, speed multiplier = พารามิเตอร์ `Speed`, state speed 1/1.1 ให้ขาไม่ไถลที่ 1.1 ม./วิ), `Sit`, `Lie` (Any State → เมื่อ bool เป็นจริง, กลับ Walk เมื่อเป็นเท็จ, ผสม 0.25 วิ), เปิด IK Pass
- `CustomerAnimator_Female.overrideController` เปลี่ยน Walk เป็น `WalkFemale` (ตัดเฟรม 13–56 จาก `Female Start Walking.fbx` ที่เป็นท่าเริ่มออกเดิน มาทำลูป), `CustomerAnimator_Vijay.overrideController` เปลี่ยนเป็นคลิปเดินของ Vijay
- คลิป (`Animations/`): `Walk` (`Walking.fbx`), `WalkFemale`, `Sit` (`Sitting.fbx`), `Lie` (`Laying.fbx`) ทุกไฟล์ Humanoid, loop, Bake Into Pose ทั้งหมุน/Y/XZ
- SkinnedMeshRenderer ทุกตัวเปิด `updateWhenOffscreen` (bounds เดิมคิดจากท่ายืน ตอนนอนหัวโดนตัดหายเมื่อกล้องอยู่ใกล้)

### เพิ่มตัวละครใหม่
1. วางโมเดล (FBX; ถ้ามาเป็น zip ให้แตก fbx ออกมา ส่วน zip เก็บไว้ `SourceArt/Characters/`) และ texture ไว้ใต้ `Characters/Models/<ชื่อ>/`
2. เลือก FBX แล้วกดเมนู `VRTattoo/Characters/Setup Selected Models` ตรวจ material ใน `Materials/` เอง texture ที่ชื่อไม่ตรงกับ material ต้องใส่มือ (ถ้าดำทั้งตัวให้ตั้ง `_BaseColor` เป็นขาว)
3. ถ้าโมเดลเล็ก/ใหญ่ผิดปกติ (หน่วยเป็น cm) แก้ `Scale Factor` ใน import แล้ว**สร้าง avatar ใหม่** (ตั้ง Rig เป็น Generic → Humanoid อีกรอบ) ไม่งั้น avatar เก็บตำแหน่งกระดูกขนาดเดิมไว้และตัวจะลอยไปไกล
4. ถ้าไฟล์มีท่าเดินมาเอง: ตั้งชื่อคลิป, loop, Bake Into Pose แล้วทำ override controller ใหม่จาก `CustomerAnimator` ที่เปลี่ยนเฉพาะ `Walk` ถ้าเป็นผู้หญิงใช้ `CustomerAnimator_Female` ผู้ชายใช้ `CustomerAnimator`
5. ลากโมเดลเข้า scene ใส่ controller, ปิด Apply Root Motion, เพิ่ม `CustomerCharacter` (ใส่ `animator`), เปิด `updateWhenOffscreen` ของ SkinnedMeshRenderer ทุกตัว, ปิด object ขยะที่ติดมา (กล้อง/mesh ที่ไม่ใช้) ตั้ง scale ให้สูง 1.6–1.8 ม. แล้วเซฟเป็น `Prefabs/Characters/Customer_<ชื่อ>.prefab`
6. เพิ่มลง `characters` ของ `CustomerCharacterPicker` บน `Customer.prefab`
7. Play แล้วจูนรายตัว: `sitOffset` (สะโพกแตะเบาะ ไม่ถอยลึกจนจมพนักพิง ตอนนี้ทุกตัวใช้ z = 0.07; เท้าแตะพื้น ขาไม่ทะลุหน้าเบาะ) และ `lieOffset` (y: หลังแตะเตียง; x: กึ่งกลางลำตัวอยู่กลางที่นอน z ≈ −0.56; z: กระดูก Head อยู่ที่ x ≈ 1.77 หัวจึงวางบนหมอน) แขน/ขาตรงด้วย IK อัตโนมัติ

## เครื่องสัก (`Interactables/TattooSystem/TattooMachine`)
- โมเดล `Models/TattooMachine/tattoo.fbx` (เครื่องคอยล์ ไม่มี texture ฝัง) เป็นลูก `Model` scale 0.02533 → ยาว 13.5 ซม. (ในไฟล์ยาว 5.33 หน่วยตามแกน Y เข็มชี้ −Y ปลายเข็มคือ mesh `Plane` ที่ y = 0.18) ตัวแม่ scale 1
- Material extract ไว้ที่ `Models/TattooMachine/Materials/Material.00x.mat` (URP Lit, ใส่ metallic ให้เป็นโลหะ: ด้ามดำ, เงิน, คอยล์ทองแดง; `Material.001` หัวครอบปลายเป็นพลาสติกใส Transparent อันอื่นเป็น Opaque)
- `Tip` อยู่ที่ปลายเข็มจริง local (0, −0.06, 0) forward ชี้ลงตามเข็ม; ray วาดเริ่มหลังปลาย 3 ซม. ซึ่งอยู่ในตัว CapsuleCollider จึงไม่ชนตัวเอง
- `Attach` (local (0, −0.025, 0) = ห่างปลายเข็ม 3.5 ซม.) หมุนให้ถือแบบปากกา: เข็มชี้ไปข้างหน้าแล้วก้มลง 45° จาก forward ของ controller ด้านมอเตอร์ (−X ของโมเดล) หันขึ้น
- ตอนถือ XRI ซ่อนโมเดล controller ขวา จึงเห็นเครื่องลอยอยู่ที่ตำแหน่งมือ
- `InkGauge` (ลูกของเครื่อง) ใช้คู่กับวงแหวนโฮโลแกรม: วงแหวน 5 วงรอบด้ามจับส่วนบน (`Ring0` ล่างสุด, Cylinder scale (0.0224, 0.0011, 0.0224) ห่างกัน 4.5 มม.) วงที่มีหมึกเป็นสีหมึกปัจจุบัน + เรืองแสง (`Materials/Tattoo/InkGauge.mat`, หมึกดำเรืองเป็นเทา) วงที่หมดเป็นเทาเข้ม ติด ceil(% × 5 / 100) วง เหลือ < `lowThreshold` 15% (เท่ากับวงแหวนโฮโลแกรม) วงล่างกะพริบ อ่าน `TattooInk` ตัวเดียวกับวงแหวนโฮโลแกรม ค่าและสีจึงตรงกันเสมอ

## หมึก
- **`TattooInk` (บนเครื่อง):** `amount` 0–100%, `color`, `metersPerLoad` = 1.3 ม. ของเส้นต่อ 1 ถ้วย ลดตามระยะที่ปลายเข็มเลื่อนบนผิวจริงระหว่างวาด (อยู่กับที่ไม่ลด จุดเริ่มเส้นคิด 5 มม. ≈ 0.4%) เส้น 7 ซม. ≈ 5.4%
  - ที่มาของ 1.3: ระยะที่ต้องสักโดยประมาณของลาย = max(พื้นที่ลาย ÷ ความกว้างแปรง 5 มม., เส้นรอบรูป ÷ 2) ในกรอบ 14 ซม. ลาย medium เฉลี่ย 0.68 ม. ≈ ครึ่งถ้วย ถ้าเพิ่ม/เปลี่ยนลายมาก ให้คำนวณใหม่
  - จุ่ม (`DippedCap`): ทุกเฟรม `OverlapSphere(tip, dipRadius 5 มม.)` หา trigger ของ `InkCap` **จุ่มเฉย ๆ ไม่เกิดอะไร** ต้องกด trigger ค้าง (`TattooMachine.TriggerPressed` = trigger จริงหรือ `ExternalTrigger`) หมึกจึงเพิ่ม 100% ต่อ `refillDuration` 1.5 วิ (`IsRefilling`) ถ้าสีถ้วยต่างจากหมึกในเครื่อง → `amount` = 0 + เปลี่ยนสีทันที แล้วค่อยเติม ระหว่างเติมสั่นเบา (`refillHapticAmplitude` 0.15 ทุก 0.1 วิ ผ่าน `TattooMachine.SendHaptic`)
- **`TattooMachine`:** หมึกหมด = ไม่วาด + สั่นสั้นครั้งเดียวต่อการกด trigger (`emptyHapticAmplitude` 0.8, `emptyHapticDuration` 0.05 วิ) รวมถึงตอนหมดกลางเส้น ยกเว้นตอนปลายเข็มอยู่ในถ้วย (กดเพื่อเติม) ไม่มี `TattooInk` = หมึกไม่จำกัดสี `TattooCanvas.inkColor`
- **วงแหวนโฮโลแกรม (`InkHologram`):** object `Interactables/TattooSystem/InkHologram` (ไม่ใช่ลูกของเครื่อง) มี `Ring` (Quad 7.5 ซม. + `Materials/Tattoo/InkHologram.mat` / `Shaders/InkHologram.shader`) และ `Percent` (TMP)
  - ตามเครื่องแบบหน่วง (`followSharpness`) เยื้อง `viewOffset` (ขวา 6 ซม. บน 3.5 ซม. ในมุมมองผู้เล่น) ถ้าเครื่องอยู่ขวาของภาพเกิน `switchSideAbove` 0.7 (เช่นตอนจุ่มถ้วย) ย้ายไปฝั่งซ้าย หันหากล้องเสมอ เป็นวัตถุในโลก ไม่ติดจอ
  - วงลดต่อเนื่องตาม % (เลข/วงไล่ค่าจริงที่ `levelSpeed` 120%/วิ จึงเห็นวงเพิ่มตอนเติม) สีหมึกปัจจุบัน หมึกเข้มถูกยกความสว่างขั้นต่ำ `minBrightness` (ดำ → เทาเรือง) ตัวเลขผสมขาว `labelWhiten`
  - shader: วงเรืองแสง (ส่วนที่หมดเป็นเงาจาง) + scanline + แสงวิ่ง + ขีดทุก 10% บนแผ่นพื้นมืดจาง (`_Backing`) ให้อ่านออกบนพื้นสว่าง (premultiplied `Blend One OneMinusSrcAlpha`) `Ring.sortingOrder` = −1 ให้ตัวเลขวาดทับ
  - เหลือ < `lowThreshold` 15% กะพริบ (`blinkRate` 3) แสดงเฉพาะตอนเครื่องอยู่ในมือ (ไม่ใช่ socket) fade เข้า/ออก
- **สีบนแขน:** RenderTexture ของ `TattooCanvas` เป็น premultiplied (rgb = สี × alpha, `TattooBrush.shader` ใช้ `Blend One OneMinusSrcAlpha`) แต่ละเส้นจึงเก็บสีของตัวเอง สีใหม่ทับสีเก่าได้ `TattooSkinOverlay.shader` แสดงสีจาก rgb / alpha (เลิกใช้ `_InkColor`) คะแนนยังอ่านแค่ alpha จึงไม่สนสี
- **ถ้วยหมึกและขวด (`Prefabs/Ink/`):** `InkCap.prefab` (ถ้วยพลาสติกขาว Ø 4.5 ซม. สูง 2.5 ซม. + แผ่น `Ink` บนปากถ้วย + SphereCollider trigger r 3.4 ซม. ที่ y 2.2 ซม. ใหญ่กว่าถ้วย ~1 ซม. รอบด้าน + `InkTint` + `InkCap`) และ `InkBottle.prefab` (ขวดตกแต่ง Ø 4 ซม. สูง ~10 ซม. + ป้าย + ฝาดำ + `InkTint` ไม่มี trigger) มี variant `_Black/_Red/_Blue/_Green`
  - `InkTint` ใส่สีด้วย MaterialPropertyBlock (`_BaseColor`) ให้ `renderers` (`[ExecuteAlways]` เห็นสีใน Editor) material ใช้ร่วมกันที่ `Materials/Ink/` (`Ink` สีขาว ถูกย้อมด้วย tint)
  - **เพิ่มสีใหม่:** คลิกขวา `InkCap.prefab` → Create → Prefab Variant ตั้ง `InkTint.color` และ `InkCap.inkName` แล้ววางบน `CapRack` (ถ้าเกิน 4 ใบต้องขยายแถว/ถาด และเช็กว่ายังอยู่ในภาพมุมนั่ง)
  - **เปลี่ยนเป็นโมเดลสวยกว่า:** แก้ child ใน `InkCap.prefab`/`InkBottle.prefab` ที่เดียว variant และของใน scene จะเปลี่ยนตาม (อย่าลืมใส่ renderer ของผิวหมึกลง `InkTint.renderers` และให้ trigger อยู่ที่ผิวหมึก)

## รถเข็น (`Furniture/InkTrolley`)
- โมเดล `Models/Shelf/model.fbx` (รถเข็นสแตนเลส 3 ชั้น ขนาดจริงอยู่แล้ว 0.525 × 0.895 × 0.35 ม. ไม่ย่อขยาย) material extract ไว้ `Models/Shelf/Materials/` (`steel` = สแตนเลส, `plactic`/`Material.006` = ยางล้อดำ)
- อยู่ใต้ `Furniture` (scale 1.48) จึงตั้ง localScale = 1/1.48 ให้ lossyScale = 1 วางที่ (0.78, −1.797, 0.05) ข้างเก้าอี้ฝั่ง −X = มือขวาตอนนั่ง ห่างเก้าอี้ 3 ซม. ห่างทางเดินลูกค้าทุกเส้น ≥ 0.5 ม.
- ชั้นเป็นถาดมีขอบสูง 6 ซม. พื้นถาดชั้นบนอยู่ที่ y = −1.0187 (ขอบบน −0.957) collider: `ShelfTop/Middle/Bottom` (พื้นถาด) + `Rim*` (ขอบถาดละ 4 ด้าน) + `Post0–3` (BoxCollider ทั้งหมด)
- บนชั้นบน: `Interactables/TattooSystem/MachineHolder` ถ้วยเสียบสีดำที่มุมหน้าของถาดฝั่งเตียง (0.975, z −0.07) ข้างไหล่ขวาตอนนั่ง มี `Socket` (ย้ายมาจากแท่นเดิม) เครื่องตั้งตรง ปลายเข็มเหนือพื้นถาด 1 ซม.; `Interactables/TattooSystem/Inks` ขวดตกแต่ง 4 ขวดแถวหลัง (z 0.135)
- **ที่วางถ้วย `Inks/CapRack`:** ถาดสแตนเลสแถวเดียว (`Pad0–3` + `Bar`, ผิวบน y −0.95 สูงกว่าขอบถาดรถเข็น) ยื่นจากเสา `Post2` มุมรถเข็นฝั่งเตียง (`Clamp` + `MountArm` + `MountLink`) ไปอยู่ระหว่างขอบเตียงกับเก้าอี้ ทางขวามือตอนนั่ง ถ้วย 4 ใบเรียงซ้าย→ขวาจากมุมนั่ง: ดำ (1.235, z −0.178), แดง (1.185, −0.185), น้ำเงิน (1.135, −0.192), เขียว (1.085, −0.199) ห่าง `TattooWorkArea` 22–30 ซม. ระยะ/แนวเอียงจูนให้เห็นครบ 4 ใบในมุมก้มมองแขนของ simulator (FOV 60°) ถ้าย้ายต้องเช็กว่าไม่ทับมือลูกค้า (Tanya/Leni มือวางชิดขอบเตียงแถว x 1.15–1.25)

- Shader: `Assets/_VRTattoo/Shaders/TattooBrush.shader` (แปรงกลมขอบนุ่ม)
- Material: `Assets/_VRTattoo/Materials/Tattoo/` (TattooBrush, TattooSkinOverlay, TattooMachine, TattooStand, InkGauge, InkHologram)
- สคริปต์เก่า: `AnimeateHandonInput.cs` (คลาส `AnimateHandOnInput` สั่งอนิเมชันมือ), `InputTest.cs` (log ค่า input; GameObject `Input Test` ปิดไว้)

## ผิวที่สัก (`CustomerTattooSkin` บน `Customer.prefab`)
- ตรวจ UV แขนท่อนล่างขวาแล้ว: Leni และ Vijay มี UV ของตัวเอง แต่ Tanya (ทับแขนซ้าย 72% และทับส่วนอื่น 52%) กับ Tomizawa (UV เดียวกับแขนซ้าย 100% = mirror) วาดลง UV เดิมไม่ได้ จึงไม่ใช้ UV ของโมเดลเลยทุกตัว (ไม่ใช้ URP Decal Projector เพราะฉายจากด้านบน ลาย 14 ซม. บนแขนกว้าง ~9 ซม. จะขาดขอบและยืดด้านข้าง)
- ลูกค้านอนลง (`Build`) → รอ `settleDelay` → เลือกแขนท่อนล่างข้างที่ใกล้ `TattooSeat` → เลือก SkinnedMeshRenderer ที่มี vertex ผูกกับกระดูกแขนท่อนล่าง (รวม twist bone ไม่รวมมือ, น้ำหนัก ≥ `minForearmWeight`) มากที่สุด แล้ว:
  - **UV ผิว:** ต่อ vertex คิด t = ระยะตามแกนข้อศอก→ข้อมือ, s = มุมรอบแกน × รัศมี (ระยะตามผิว วัดจากด้านบนแขนเอียงเข้าหาช่าง `stencilTurnTowardsArtist` 15°) แล้วย่อเป็นกรอบ `canvasSize` 20 ซม. ตัดสามเหลี่ยมที่คร่อมรอยต่อใต้แขน
  - **collider:** `BakeMesh` ท่าปัจจุบัน → GameObject `TattooSkin` (ลูกของลูกค้า) มี MeshCollider + `TattooCanvas` (`MetersPerUV` = 0.2) เข็มจึงแตะผิวจริงและได้ `hit.textureCoord` เป็นพิกัด canvas ทันที
  - **ภาพ:** `TattooOverlay` = SkinnedMeshRenderer ใหม่จากสามเหลี่ยมชุดเดียวกัน (bones/bindposes เดิม จึงติดผิวพอดี) ใช้ `TattooSkinOverlay` วาดหมึก + ลาย
  - ลายตั้งตามแนวแขน หัวลายชี้ไปทางข้อศอก (`StencilPlacement` axisX = รอบแขน, axisY = ตามแขน)
- ลุก/ออกจากเตียง (`Release`) ลบทั้งหมดรวม RenderTexture จึงสะอาดเสมอเมื่อมีลูกค้าใหม่ `CustomerTattooSkin.Active` = ผิวที่พร้อมสักตอนนี้ (Desktop Test Mode ใช้ collider นี้)
- `TattooJobManager` ส่งลายผ่าน `ShowStencil` ตอนลูกค้านอน (แสดงเมื่อผิวพร้อม) และส่งงานได้เมื่อ `IsReady` เท่านั้น
- ต้องเปิด Read/Write ใน import ของโมเดล (เมนู Setup Selected Models ทำให้แล้ว) ไม่งั้นอ่าน mesh ไม่ได้และขึ้น warning
- `CustomerQueueManager.tattooCanvas` เป็น null แล้ว (ไม่ได้แก้คิว) การล้างลายไปเกิดจากการสร้าง canvas ใหม่ต่อลูกค้าแทน

## GameObject สำคัญใน TattooRoom
- `XR Origin (XR Rig)`: rig ผู้เล่น
  - `Camera Offset/Main Camera` (มี AudioListener)
  - `Camera Offset/Right Controller/Near-Far Interactor` (มือที่ถือเครื่องตอนนั่ง)
  - `Locomotion/*` (providers ที่ถูกปิดตอนนั่ง)
- `XR Device Simulator`: ใช้ทดสอบบนคอม
- `Interactables/TattooSystem` (มี `TattooDesktopTestMode`)
  - `TattooWorkArea`: จุดว่างที่ (1.33, −1.05, −0.325) = กลางแขนท่อนล่างขวาของลูกค้าที่นอน (เฉลี่ยทุกตัว) เป็น `lookTarget` ของเก้าอี้ (TattooArm Capsule เดิมถูกลบแล้ว)
  - `TattooMachine`: มี XRGrabInteractable (Instantaneous) + TattooMachine + TattooInk + ReturnToSocket + Rigidbody, CapsuleCollider (หัวครอบปลาย+ด้าม) + BoxCollider (หัวเครื่อง) และ child `Model`, `Tip`, `Attach`, `InkGauge` (ดูหัวข้อ "เครื่องสัก")
  - `MachineHolder/Socket`: XRSocketInteractor บนรถเข็น (`SocketAttach` หมุนเท่ากับ `Attach` เครื่องจึงตั้งตรง เข็มชี้ลง ปลายเข็มลอยเหนือพื้นถาด 1 ซม.)
  - `Inks`: ขวดตกแต่งบนรถเข็น + `CapRack` ถ้วยหมึก 4 สี (ดูหัวข้อ "รถเข็น")
  - `InkHologram`: วงแหวนบอกหมึก (ดูหัวข้อ "หมึก")
  - `SeatHint`: ป้าย TMP บอกปุ่ม (ภาษาอังกฤษ)
  - `SubmitButton`: ปุ่มส่งงานลอยข้างเตียง (1.75, −0.85, −0.05) ซ้ายมือผู้เล่นตอนนั่ง (ย้ายออกจากจุดที่หัวผู้เล่นอยู่ตอนนั่ง) (BoxCollider + XRSimpleInteractable, child `Visual` และ `Label`)
  - `TattooJobUI/{OrderLabel, ScoreLabel, TotalScoreLabel}`: ป้ายชื่อลาย/ระดับ (หลังเตียง), คะแนน (เหนือเตียง), คะแนนรวม (มุมบนผนังด้าน −z)
- `Furniture/Chair43`: เก้าอี้ช่างสัก (1.25, −1.785, 0.18) ชิดเตียงตรงแขน มี TattooSeat + XRSimpleInteractable ส่วน collider อยู่ที่ `Chair43_model` (`SeatHint` ย้ายตามไปอยู่เหนือเก้าอี้)
- `Furniture/hospital bed`: เตียง (MeshCollider)
- `Room/Floor_Tile_3b`: พื้น มี MeshCollider + BoxCollider + TeleportationArea
- `CustomerSystem` (มี `CustomerQueueManager` (`completeKey` = None) + `TattooJobManager` และลูกค้าที่ spawn จะเป็นลูกของมัน)
  - `Locations/Door/DoorPoint`, `Locations/Bed/{BedApproach, BedPoint}`, `Locations/WaitArea1/{WaitSeat1Approach, WaitSeat1}` (`Chair15` ใกล้เตียงกว่า), `Locations/WaitArea2/...` (`Chair15 (1)`)
  - `Routes/*`: เส้นทาง 5 เส้น มี waypoint แค่ `Door-WaitSeat1` (อ้อมจุดเกิดผู้เล่น) ทุกเส้นตรวจด้วย CapsuleCast แล้วว่าไม่ชน

## ปุ่มทดสอบ (XR Device Simulator)
| ทำอะไร | ปุ่ม |
|---|---|
| คุม controller ขวา | กด Space ค้าง (หรือกด Y เพื่อล็อก), Tab สลับอุปกรณ์ |
| หยิบ / ปล่อย | G |
| วางคืน / หยิบเครื่องจากที่วาง ตอนนั่ง (Desktop mode) | P วางคืน (ไม่ใช้ G เพราะชน grip ของ simulator); หันไปทางรถเข็น (คลิกขวาลาก) + คลิกซ้ายที่เครื่อง = หยิบ |
| วางคืนตอนนั่ง (simulator) | คุม controller ขวา (Y) แล้วกด G แล้วปล่อย |
| วาด (trigger) | คลิกซ้าย |
| นั่ง / ลุก | F, หรือ N ตอนคุม controller ขวา (= ปุ่ม B), หรือชี้เก้าอี้ + G (ต้องห่างเก้าอี้ไม่เกิน 1.5 ม.) |
| เปิด/ปิด Desktop Test Mode | K (ชนกับ simulator นิดหน่อยตอนกำลังคุม controller/มือ) |
| วาดใน Desktop mode | ชี้เมาส์ที่แขน + คลิกซ้ายค้าง |
| เติมหมึก / เปลี่ยนสี | จุ่มปลายเข็มลงถ้วยแล้วกด trigger ค้าง (~1.5 วิจนเต็ม); Desktop mode: ชี้เมาส์ที่ถ้วย + คลิกซ้ายค้าง |
| หันมอง | คลิกขวาค้าง + ลากเมาส์ |
| ซูม (เฉพาะ Desktop Test Mode) | ลูกกลิ้งเมาส์ |
| ส่งงาน (ให้คะแนน แล้วลูกค้าเดินออก) | J (เฉพาะ Editor) หรือชี้ปุ่ม SUBMIT ด้วย controller ซ้าย + G |

บนแว่นจริง นั่ง/ลุกใช้ปุ่ม B ขวา (ปุ่ม A ใช้กระโดดอยู่แล้ว)

## ข้อจำกัดที่ค้างอยู่
- **หมึก:** สั่นเตือนตอนหมึกหมดและสั่นตอนเติมยังไม่ได้ลองกับแว่นจริง; การเติม/เปลี่ยนสี ทดสอบใน Desktop Test Mode ด้วย mouse event ที่ส่งผ่าน Input System (ยังไม่ได้คลิกเมาส์จริง); หมึกที่เหลือติดเครื่องข้ามลูกค้า (ไม่รีเซ็ตต่อคน); ระยะเอื้อมถึงถ้วยและตำแหน่งวงแหวนจูนจากภาพ simulator ยังไม่ได้ลองกับแว่นจริง
- **ภาพจาก MCP screenshot ตอน Play ดูซีดขาว** (เป็นทั้งฉาก ไม่เกี่ยวกับวงแหวน) และการจับภาพบางครั้งขึ้น error "PlayerLoop internal function has been called recursively" ทำให้ Error Pause หยุดเกม ต้องสั่ง unpause
- **ขอบเส้นยังขรุขระ:** ตอนเปลี่ยนเป็นโมเดลแขนจริงให้เพิ่ม RenderTexture เป็น 2048 และทำเส้นให้เนียน
- **รอยสักไม่ถาวร:** หายเมื่อลูกค้าลุกจากเตียง และทุกครั้งที่กด Play
- **Socket ไม่ได้กรอง interaction layer:** ถ้ามีของอื่นที่หยิบได้ อาจไปเสียบที่วางได้
- **หยิบ/วางเครื่องตอนนั่ง:** ระยะเอื้อมถึงที่วางคิดจากตำแหน่งตา (ไหล่ประมาณ) ยังไม่ได้ลองกับแว่นจริง; Near-Far Interactor หยิบจากระยะไกลด้วย ray ได้ด้วย; ตอนเครื่องเข้ามือเองถ้าผู้เล่นกำ grip ค้างแล้วปล่อย เครื่องจะกลับที่วาง (ตั้งใจให้เป็นวิธีวางคืน) หยิบด้วย grip จริงจาก simulator ยังไม่ได้ลอง (ทดสอบผ่าน interaction manager)
- **คะแนนกับลายพื้นทึบหนา:** มือสั่นแล้วโดนหักน้อยกว่าลายเส้นบาง เพราะเทียบพื้นที่เลอะกับพื้นที่ลาย
- **ล็อกหัวใน simulator พึ่ง reflection (`m_HMDState`):** ถ้าอัปเดต XRI แล้วชื่อฟิลด์เปลี่ยน หัวจะไม่ถูกล็อก (ไม่ error) แต่จอมืดยังทำงาน
- **จอมืดตอนออกนอกรัศมี** ทดสอบใน simulator แล้ว ยังไม่ได้ทดสอบกับแว่นจริง
- **ท่าจับเครื่อง (45°, ห่างปลาย 3.5 ซม.)** จูนจากภาพใน simulator ยังไม่ได้ลองกับมือจริง ถ้าไม่ถนัดปรับที่ `Attach` (และหมุน `SocketAttach` ให้เท่ากัน ไม่งั้นเครื่องบนที่วางจะเอียง)
- **ป้ายใช้ภาษาอังกฤษ:** ฟอนต์ TMP ที่มีไม่มีตัวอักษรไทย
- **ก้มกล้องอัตโนมัติพึ่ง reflection ของ simulator:** ถ้าอัปเดต XRI แล้วชื่อฟิลด์เปลี่ยน การก้มจะไม่ทำงาน (ไม่ error)
- **`Chair43 (1)` และ `Table44` ใน Furniture:** เป็น object ว่าง ไม่มี mesh
- **haptics:** ยังไม่ได้ทดสอบกับแว่นจริง
- **ลายติดลิขสิทธิ์:** `angry_duck` (Donald Duck), `ghost_monster` (Haunter), `trophy` (FIFA), `cloud` (Akatsuki) ควรเอาออกก่อนเผยแพร่
- **คะแนน:** ไม่นับหมึกที่อยู่ไกลจากกรอบลายเกิน 2 ซม. และลายที่มีพื้นที่ระบายทึบต้องระบายให้เต็มถึงจะได้ coverage สูง
- **ลูกค้าไม่มี collider:** เดินทะลุกันเองและทะลุผู้เล่นได้
- **ผิวที่สักเป็นภาพนิ่งตอนนอน:** collider bake ครั้งเดียวหลังนอนลง `settleDelay` 0.5 วิ (คลิปนอนนิ่ง) ถ้าเปลี่ยนเป็นคลิปที่หายใจ/ขยับ collider จะคลาดจากผิวที่เห็น (overlay ยังติดผิวเพราะ skin กับ bone เดียวกัน)
- **mapping พันรอบแขนแบบทรงกระบอก:** ใช้แกนข้อศอก→ข้อมือ ที่แขนแบนหรือใกล้ข้อมือ/ข้อศอกลายจะบิดเล็กน้อย Tanya มีแขนแค่ ~40 สามเหลี่ยม ภายในสามเหลี่ยมใหญ่ลายจะเพี้ยน (ภาพกับ collider ใช้สามเหลี่ยมชุดเดียวกัน คะแนนจึงยังตรงกับที่เห็น)
- **ลาย 14 ซม. กับแขนท่อนล่าง ~22 ซม.:** ด้านกว้างของลายพันรอบแขนครึ่งรอบ ขอบซ้าย/ขวาของลายกว้างๆ จะอ้อมไปด้านข้างและใต้แขนเล็กน้อย
- **คลิปนอนกางแขนกางขา:** IK จัดให้แขนอีกข้างวางข้างลำตัว (`armsAlongBody`) และเท้าห่างกลางตัว `footSpread` 11 ซม.
- **ท่าทำงานตอนนั่ง:** ตำแหน่งตาคำนวณครั้งเดียวตอนนั่ง บนแว่นจริงผู้เล่นต้องก้มหัวมองแขนเอง (ไม่หมุนกล้องให้) ถ้าตัวจริงสูง/เตี้ยต่างจากที่ simulator ตั้งไว้ ระยะจะคลาดเล็กน้อย
- **ทดสอบซูมด้วยลูกกลิ้งจริงยังไม่ได้:** event ลูกกลิ้งที่ส่งผ่านสคริปต์ไม่ถึง Game view ตอน Unity ไม่ได้อยู่หน้าสุด ทดสอบได้แค่เรียก `Zoom()` ตรงๆ
- **ท่าเดินผู้หญิง** ตัดจากท่า "เริ่มออกเดิน" ไม่ใช่ลูปจริง จังหวะก้าวช้ากว่าความเร็วเดินเล็กน้อย
- **คลิปนอนยกเท้า** จึงใช้ foot IK กดเท้าลงเตียง (`ankleHeight`) และไม่มีท่าลุก/ลงนั่ง (วาร์ปเข้าท่าเหมือนเดิม)
- **ตัวละครต่างกันที่ texture/แสง:** Leni (Character Creator) ผิวดูซีดกว่าตัวอื่น, ผม Leni ใช้ alpha clip + สีน้ำตาล (texture เป็นเส้นสีขาว)
- **โมเดลไม่อยู่ใน git** (ดูหัวข้อ "ตัวละครลูกค้า") clone ใหม่แล้ว prefab ตัวละครจะว่าง จนกว่าจะก๊อปโมเดลมาใส่

## งานถัดไป (ทำตามลำดับ)
1. ระบบเปลี่ยนหัวเข็ม (เส้นเล็ก/ใหญ่) วางหัวเข็มบนรถเข็น และปรับระบบคะแนนรองรับ (รวมถึงสีถ้าลายกำหนดสี)
2. เสียงเครื่องสัก (AudioListener พร้อมแล้ว), ป้ายภาษาไทย (ต้องมีฟอนต์ TMP ที่รองรับภาษาไทย)
3. เปลี่ยนจุดสักได้หลายส่วนของร่างกาย เช่น ต้นแขน หลัง (อาจต้องมีท่านอนคว่ำ) รวมถึงให้ลูกค้าเปลี่ยนท่าแขน/ยื่นแขนให้ช่าง

ค้างไว้ (ยังไม่จัดลำดับ):
- เปลี่ยนโมเดลทดสอบเป็นโมเดลที่ใช้ได้ถูกต้องก่อนเผยแพร่
- ท่าลุก/นั่งลง/นอนลง แทนการวาร์ป

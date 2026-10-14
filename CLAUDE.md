# VRTattoo

เกม VR ร้านสัก: Unity 6000.3, URP 17.3, XR Interaction Toolkit 3.4.1, Input System, OpenXR
ทดสอบบนคอมด้วย XR Device Simulator (อยู่ใน scene แล้ว)

- Scene หลัก: `Assets/_VRTattoo/Scenes/TattooRoom.unity`
- ไฟล์ของโปรเจกต์ทั้งหมดอยู่ใต้ `Assets/_VRTattoo` (ห้ามแก้ `Assets/Samples`)
- แก้ scene ผ่าน UnityMCP (`execute_code` + Undo) แล้วเซฟ scene ทุกครั้งหลังแก้

## สิ่งที่ทำเสร็จแล้ว
- **ระบบสัก:**
  - เครื่องสัก (Cylinder) หยิบได้ กด trigger ตอนปลายเข็มแตะแขน (Capsule) จะวาดเส้นลง RenderTexture และส่ง haptics
  - ขนาดเส้นกำหนดเป็นเมตร (4 มม.) ระบบชดเชยการยืดของ UV ให้
  - ลากเร็วแล้วเส้นไม่ขาด และตัดเส้นตอนข้ามรอยต่อ UV
- **ที่วางเครื่องสัก:** ใช้ `XRSocketInteractor` เริ่มเกมเครื่องเสียบอยู่ ปล่อยมือหรือตกแมปแล้วกลับเข้าที่วางเอง
- **ระบบนั่ง (Chair43):**
  - วาร์ปไปนั่งหันหาแขน และปิดการเดิน/วาร์ป/กระโดด/gravity/CharacterController (หมุนตัวยังได้)
  - เครื่องสักเข้ามือขวาอัตโนมัติ (`StartManualInteraction`)
  - ลุกแล้วเครื่องกลับที่วาง
- **Collider / แก้การตก:**
  - ย้าย XR Origin ขึ้นมาบนพื้น (y = -1.797)
  - เพิ่ม BoxCollider ใต้พื้นปิดรอยแยกของ MeshCollider และเพิ่มเข้า TeleportationArea แล้ว
  - ใส่ BoxCollider ให้ผนัง เพดาน ตู้ และเฟอร์นิเจอร์ เตียงใช้ MeshCollider
- **Desktop Test Mode:**
  - ปุ่ม K เปิด/ปิด ทำงานเฉพาะใน Editor
  - ตอนนั่ง เมาส์วางปลายเข็มบนแขน คลิกซ้ายวาด และหัว simulator ก้มมองแขนอัตโนมัติ
  - ไม่กระทบแว่นจริง
- **Audio Listener** อยู่บน Main Camera
- **สเกลห้องเท่าของจริง** (ขยายรอบจุด (0.32, -1.797, -0.05) ระดับพื้นจึงไม่เปลี่ยน):
  - `Room`, `Furniture`, `Light Probe Group` ×1.48 → เพดาน 2.66 ม., โซฟา 0.95 ม. (เบาะ 0.47), สตูล Chair43 0.65 ม.
  - `hospital bed` + `Cushion38` ×1.39 → ยาว 1.90 สูง 0.70; `Door_7_White` ×1.55 → สูง 2.0 ม.
  - TattooSystem ไม่ขยาย (สร้างเป็นเมตรจริงอยู่แล้ว) แค่ย้ายตำแหน่ง; XR Origin / Simulator ไม่ขยาย
  - `seatedEyeHeight` = 1.40, `Camera Y Offset` = 1.6
  - ไม่มี object ไหนเป็น GI static จึงไม่มี lightmap (ไฟ realtime ทั้งหมด) LightingData อยู่ที่ `Scenes/TattooRoom/`
- **ระบบลูกค้า (placeholder Capsule สูง 1.7 ม.):**
  - ในร้านมีได้ 3 คน: 1 คนบนเตียง + 2 คนบนโซฟารอ ลูกค้าโผล่ที่ประตูทุก `spawnInterval` จนกว่าที่จะเต็ม
  - เดินเส้นตรง (ไม่ใช้ NavMesh) ผ่าน waypoint ของ `CustomerRoute` ถึงแล้ววาร์ปไปท่านั่ง/นอน
  - `CompleteTattoo()`: คนบนเตียงเดินออกประตูแล้วหายไป คิวเลื่อนขึ้น และมีคนใหม่มาเติม ถ้าคนบนเตียงยังไม่นอนลงจะไม่ทำงาน (ตอนนี้ `TattooJobManager` เป็นคนเรียกหลังส่งงาน)
  - ล้างลายบนแขนตอนที่คนใหม่นอนลงเตียง
- **ลายสักและคะแนน:**
  - ลูกค้านอนลงแล้วสุ่มลายจากคลัง (ช่วงแรกได้ easy บ่อย ยิ่งเสร็จหลายคนยิ่งเจอ hard บ่อยขึ้น ไม่ซ้ำกับคนก่อน) แสดงเป็นลายเส้นสีม่วงจางบนแขน (กว้าง 9 ซม.) แยกจากหมึกจริง พร้อมป้ายชื่อลายและระดับ
  - กดปุ่ม SUBMIT ข้างเตียง (ray/grip มือซ้าย) หรือ J (เฉพาะ Editor) เพื่อส่งงาน: คะแนน 0–100 ลอยเหนือเตียง 2.5 วิ แล้วลูกค้าเดินออก
  - คุณภาพ = 100 × (coverage − 0.5 × spill) โดย coverage = สัดส่วนเส้นลายที่ลงหมึก, spill = หมึกที่อยู่นอกแนว (เส้นลาย + รัศมีเข็ม + 1 มม.) เทียบกับพื้นที่เส้นลาย นับเฉพาะหมึกในกรอบลาย + 2 ซม.
  - คะแนนรวม = คุณภาพ × ตัวคูณ (easy x1, medium x1.5, hard x2) แสดงบนผนังมุมบนของห้อง

## สคริปต์ (`Assets/_VRTattoo/Scripts/Tattoo/`)
| ไฟล์ | หน้าที่ |
|---|---|
| `TattooCanvas.cs` | อยู่บนแขน สร้าง RenderTexture (1024²) เป็น `_BaseMap` และมี `DrawStroke(uvFrom, worldFrom, uvTo, worldTo)` ปั๊มแปรงด้วย GL ประมาณค่าเมตรต่อ UV จาก mesh ตอน Awake |
| `TattooMachine.cs` | อยู่บนเครื่องสัก ฟัง activate/deactivate ของ `XRGrabInteractable` raycast จาก `tip` ไปตามแนวเข็ม แล้ววาดและส่ง haptics มี `ExternalTrigger` ให้โหมดทดสอบสั่งวาดได้ |
| `ReturnToSocket.cs` | อยู่บนเครื่องสัก ปล่อยมือ (ที่ไม่ใช่ socket) หรือตกต่ำกว่า `fallResetHeight` จะ `SelectEnter` กลับเข้า socket ในเฟรมถัดไป |
| `TattooSeat.cs` | อยู่บน `Furniture/Chair43` (ตัวแม่) สลับนั่ง/ลุก (ปุ่ม B ขวา / F / ชี้เก้าอี้ + grip) ปิดและเปิดคืนเฉพาะ locomotion ที่เคยเปิดอยู่ และใส่/คืน `heldTool` เข้ามือ `handInteractor` |
| `TattooDesktopTestMode.cs` | อยู่บน `Interactables/TattooSystem` โหมดทดสอบด้วยเมาส์ ปิด `trackPosition/Rotation` ของเครื่องระหว่างใช้ และหมุนหัว XR Device Simulator ผ่าน reflection (ฟิลด์ private `m_CenterEyeEuler`, `m_HMDState`) |

## สคริปต์ลูกค้า (`Assets/_VRTattoo/Scripts/Customers/`)
| ไฟล์ | หน้าที่ |
|---|---|
| `CustomerQueueManager.cs` | อยู่บน `CustomerSystem` มี slot 0 = เตียง, 1.. = `waitSeats` (ตัวที่ 0 เป็นคิวถัดไป) จองช่องตั้งแต่ตอนสั่งเดิน ถ้าคิวเลื่อนตอนลูกค้ายังเดินอยู่ จะเปลี่ยนเส้นทางเมื่อถึงที่ มี `CompleteTattoo()`, `IsCustomerReady`, event `onCustomerLayDown`/`onCustomerLeft` |
| `Customer.cs` | อยู่บน prefab `PlaceAt`, `WalkTo(location, waypoints, onArrived)`, `Settle()` (วาร์ปเข้าท่าของ location) |
| `CustomerLocation.cs` | จุดหนึ่งในร้าน: `approach` (จุดยืนบนพื้นตอนเริ่ม/จบการเดิน), `pose` (จุดนั่ง/นอน), `arrivalPose` |
| `CustomerRoute.cs` | waypoint อ้อมระหว่าง `from`↔`to` (ลูกของมันตามลำดับ ใช้ได้ทั้งสองทิศ) ไม่มีลูก = เดินตรง |
| `CustomerVisual.cs` | คลาสแม่ของหน้าตาลูกค้า: `SetPose(Idle/Walk/Sit/Lie)`, `SetMoveSpeed` |
| `CapsuleCustomerVisual.cs` | placeholder: Capsule สีสุ่ม (MaterialPropertyBlock `_BaseColor`) |
| `AnimatorCustomerVisual.cs` | สำหรับโมเดลคนจริง: ตั้ง Animator `Speed` (float), `Sit`, `Lie` (bool) ใช้แทน `CapsuleCustomerVisual` บน prefab ได้เลย ต้องจัด root ของโมเดลให้ตรง pivot (ยืน = ที่เท้า, นั่ง/นอน = ใต้สะโพกบนเบาะ/เตียง, นอนหันหัวไปทาง forward) |

## สคริปต์ลาย/คะแนน (`Assets/_VRTattoo/Scripts/Stencils/`)
| ไฟล์ | หน้าที่ |
|---|---|
| `StencilLibrary.cs` | ScriptableObject `Data/StencilLibrary.asset` รายการลาย (texture, ชื่อ, ระดับ) + `Multiplier()` + แยกชื่อไฟล์ |
| `Editor/StencilLibraryPostprocessor.cs` | ตั้งค่า import PNG ใน `Stencils/` อัตโนมัติ (Single Channel Alpha, readable, ไม่บีบอัด) และสร้างคลังใหม่ทุกครั้งที่ไฟล์เปลี่ยน มีเมนู `VRTattoo/Rebuild Stencil Library` |
| `StencilOverlay.cs` | อยู่บน TattooArm ใส่ material `StencilOverlay` เป็น material ที่ 2 ของ renderer หาตำแหน่งด้านบนแขนด้วย raycast แล้วคำนวณ mapping UV ↔ ลาย (`StencilPlacement`) จาก MeshCollider ลายจึงไม่ยืด |
| `TattooScorer.cs` | อ่าน RenderTexture ของ TattooCanvas เฉพาะบริเวณลาย เทียบกับ alpha ของลาย แล้วคิด coverage/spill/quality |
| `TattooJobManager.cs` | อยู่บน `CustomerSystem` ฟัง `onCustomerLayDown` สุ่มลาย แสดงลาย/ป้าย รับ Submit (ปุ่ม/J) ให้คะแนน แล้วเรียก `CompleteTattoo()` |

- Shader: `Shaders/StencilOverlay.shader` (unlit โปร่งใส สีม่วงจาง), Material: `Materials/Tattoo/StencilOverlay.mat`

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

- Prefab: `Assets/_VRTattoo/Prefabs/Customer.prefab` (Body เป็น Capsule ไม่มี collider), Material: `Materials/Customer/CustomerPlaceholder.mat`

- Shader: `Assets/_VRTattoo/Shaders/TattooBrush.shader` (แปรงกลมขอบนุ่ม)
- Material: `Assets/_VRTattoo/Materials/Tattoo/` (TattooBrush, TattooArm_Skin, TattooMachine, TattooStand)
- สคริปต์เก่า: `AnimeateHandonInput.cs` (คลาส `AnimateHandOnInput` สั่งอนิเมชันมือ), `InputTest.cs` (log ค่า input; GameObject `Input Test` ปิดไว้)

## GameObject สำคัญใน TattooRoom
- `XR Origin (XR Rig)`: rig ผู้เล่น
  - `Camera Offset/Main Camera` (มี AudioListener)
  - `Camera Offset/Right Controller/Near-Far Interactor` (มือที่ถือเครื่องตอนนั่ง)
  - `Locomotion/*` (providers ที่ถูกปิดตอนนั่ง)
- `XR Device Simulator`: ใช้ทดสอบบนคอม
- `Interactables/TattooSystem` (มี `TattooDesktopTestMode`)
  - `TattooArm`: Capsule บนเตียง มี MeshCollider + TattooCanvas + StencilOverlay หมุนให้รอยต่อ UV หันลงเตียง
  - `TattooMachine`: มี XRGrabInteractable (Instantaneous) + TattooMachine + ReturnToSocket และ child `Tip` กับ `Attach`
  - `TattooMachineStand/Socket`: XRSocketInteractor (`SocketAttach` ให้เข็มชี้ลง)
  - `SeatHint`: ป้าย TMP บอกปุ่ม (ภาษาอังกฤษ)
  - `SubmitButton`: ปุ่มส่งงานลอยข้างเตียง (BoxCollider + XRSimpleInteractable, child `Visual` และ `Label`)
  - `TattooJobUI/{OrderLabel, ScoreLabel, TotalScoreLabel}`: ป้ายชื่อลาย/ระดับ (หลังเตียง), คะแนน (เหนือเตียง), คะแนนรวม (มุมบนผนังด้าน −z)
- `Furniture/Chair43`: เก้าอี้ช่างสัก มี TattooSeat + XRSimpleInteractable ส่วน collider อยู่ที่ `Chair43_model`
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
| วาด (trigger) | คลิกซ้าย |
| นั่ง / ลุก | F, หรือ N ตอนคุม controller ขวา (= ปุ่ม B), หรือชี้เก้าอี้ + G (ต้องห่างเก้าอี้ไม่เกิน 1.5 ม.) |
| เปิด/ปิด Desktop Test Mode | K (ชนกับ simulator นิดหน่อยตอนกำลังคุม controller/มือ) |
| วาดใน Desktop mode | ชี้เมาส์ที่แขน + คลิกซ้ายค้าง |
| หันมอง | คลิกขวาค้าง + ลากเมาส์ |
| ส่งงาน (ให้คะแนน แล้วลูกค้าเดินออก) | J (เฉพาะ Editor) หรือชี้ปุ่ม SUBMIT ด้วย controller ซ้าย + G |

บนแว่นจริง นั่ง/ลุกใช้ปุ่ม B ขวา (ปุ่ม A ใช้กระโดดอยู่แล้ว)

## ข้อจำกัดที่ค้างอยู่
- **ขอบเส้นยังขรุขระ:** ตอนเปลี่ยนเป็นโมเดลแขนจริงให้เพิ่ม RenderTexture เป็น 2048 และทำเส้นให้เนียน
- **UV ของ Capsule ยืดและบีบที่ปลายมน:** เส้นตรงปลายแขนจะผิดรูป ส่วนจุดที่ชิดขอบแขนมากๆ ray จากเมาส์อาจพลาด
- **รอยสักไม่ถาวร:** ล้างทุกครั้งที่กด Play
- **Socket ไม่ได้กรอง interaction layer:** ถ้ามีของอื่นที่หยิบได้ อาจไปเสียบที่วางได้
- **ระหว่างนั่ง:** ถือเครื่องค้างไว้ตลอด (ปล่อยด้วย grip ไม่ได้)
- **ป้ายใช้ภาษาอังกฤษ:** ฟอนต์ TMP ที่มีไม่มีตัวอักษรไทย
- **ก้มกล้องอัตโนมัติพึ่ง reflection ของ simulator:** ถ้าอัปเดต XRI แล้วชื่อฟิลด์เปลี่ยน การก้มจะไม่ทำงาน (ไม่ error)
- **`Chair43 (1)` และ `Table44` ใน Furniture:** เป็น object ว่าง ไม่มี mesh
- **haptics:** ยังไม่ได้ทดสอบกับแว่นจริง
- **ลายติดลิขสิทธิ์:** `angry_duck` (Donald Duck), `ghost_monster` (Haunter), `trophy` (FIFA), `cloud` (Akatsuki) ควรเอาออกก่อนเผยแพร่
- **คะแนน:** ไม่นับหมึกที่อยู่ไกลจากกรอบลายเกิน 2 ซม. และลายที่มีพื้นที่ระบายทึบต้องระบายให้เต็มถึงจะได้ coverage สูง
- **ลูกค้าไม่มี collider:** เดินทะลุกันเองและทะลุผู้เล่นได้
- **แขน TattooArm ไม่ได้ติดกับตัวลูกค้า:** วางไว้ข้างลำตัวฝั่งช่าง (z = -0.40) ตอนนั่งห่างตาผู้เล่นแนวนอน 0.64 ม. และต่ำกว่าตา 0.68 ม. บนแว่นจริงอาจต้องโน้มตัว

## งานถัดไป
- เปลี่ยนลูกค้าเป็นโมเดลคนจริง + แอนิเมชัน (ใช้ `AnimatorCustomerVisual`) และให้แขนที่สักเป็นส่วนของตัวลูกค้า
- เสียงเครื่องสัก (AudioListener พร้อมแล้ว)
- ป้ายภาษาไทย (ต้องมีฟอนต์ TMP ที่รองรับภาษาไทย)

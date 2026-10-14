# VRTattoo

เกม VR ร้านสัก: Unity 6000.3, URP 17.3, XR Interaction Toolkit 3.4.1, Input System, OpenXR
ทดสอบบนคอมด้วย XR Device Simulator (อยู่ใน scene แล้ว)

- Scene หลัก: `Assets/_VRTattoo/Scenes/TattooRoom.unity`
- ไฟล์ของโปรเจกต์ทั้งหมดอยู่ใต้ `Assets/_VRTattoo` (ห้ามแก้ `Assets/Samples`)
- แก้ scene ผ่าน UnityMCP (`execute_code` + Undo) แล้วเซฟ scene ทุกครั้งหลังแก้

## สิ่งที่ทำเสร็จแล้ว
- **ระบบสัก:**
  - เครื่องสัก (Cylinder) หยิบได้ กด trigger ตอนปลายเข็มแตะแขน (Capsule) จะวาดเส้นลง RenderTexture และส่ง haptics
  - ขนาดเส้นกำหนดเป็นเมตร (5 มม.) ระบบชดเชยการยืดของ UV ให้
  - **สักลงแขนจริงของลูกค้า:** แขนท่อนล่างข้างที่หันมาฝั่งเก้าอี้ช่าง (ตอนนี้คือแขนขวา) ดูหัวข้อ "ผิวที่สัก" ล้างเองเมื่อมีลูกค้าใหม่ (canvas เป็นของลูกค้าแต่ละคน)
  - ลากเร็วแล้วเส้นไม่ขาด และตัดเส้นตอนข้ามรอยต่อ UV
- **ที่วางเครื่องสัก:** ใช้ `XRSocketInteractor` เริ่มเกมเครื่องเสียบอยู่ ปล่อยมือหรือตกแมปแล้วกลับเข้าที่วางเอง
- **ระบบนั่ง (Chair43):**
  - วาร์ปไปท่าทำงาน: ตาอยู่ห่างผิวด้านบนของแขน ~38 ซม. (ห่างแนวนอน 0.25 ม. ไปทางเก้าอี้ + สูงกว่าแกนแขน 0.33 ม. = `leanOverTarget`, `workHorizontalDistance`, `workEyeHeight`) เหมือนช่างที่ก้มทำงาน หันหน้าหาแขน **ไม่ซูมกล้องในแว่น**
  - ปิด: Move, Teleportation, Climb, Jump, Gravity, Grab Move ×3, Teleport Interactor (ray ซ้าย/ขวา), Climb Teleport และ CharacterController (หมุนตัวยังได้) ลุกแล้วเปิดคืนเฉพาะตัวที่เคยเปิดอยู่
  - เครื่องสักเข้ามือขวาอัตโนมัติ (`StartManualInteraction`)
  - ลุกแล้วเครื่องกลับที่วาง
- **Collider / แก้การตก:**
  - ย้าย XR Origin ขึ้นมาบนพื้น (y = -1.797)
  - เพิ่ม BoxCollider ใต้พื้นปิดรอยแยกของ MeshCollider และเพิ่มเข้า TeleportationArea แล้ว
  - ใส่ BoxCollider ให้ผนัง เพดาน ตู้ และเฟอร์นิเจอร์ เตียงใช้ MeshCollider
- **Desktop Test Mode:**
  - ปุ่ม K เปิด/ปิด ทำงานเฉพาะใน Editor
  - ตอนนั่ง เมาส์วางปลายเข็มบนแขน คลิกซ้ายวาด และหัว simulator ก้มมองแขนอัตโนมัติ
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
  - คุณภาพ = 100 × coverage × (1 − spill)² โดย coverage = สัดส่วนเส้นลายที่มีหมึกอยู่บนหรือห่างไม่เกิน 1.5 มม., spill = สัดส่วนของ**หมึกทั้งหมด**ที่ห่างจากเส้นลายเกิน รัศมีเข็ม + 2 มม. นับเฉพาะหมึกในกรอบลาย + 2 ซม. ปรับได้ที่ `TattooJobManager.scoring` (`spillTolerance`, `coverTolerance`, `spillPower`)
  - ผลจำลอง (ลาย easy/medium/hard): ตามเส้นดี ≈ 100, ส่ายไม่เกิน 3 มม. ≈ 95–98, ส่าย 5 มม. ขาด 20% ≈ 79–86, ส่าย 8 มม. ขาด 35% ≈ 47–63, ระบายมั่วทั้งกรอบ ≈ 24–29
  - คะแนนรวม = คุณภาพ × ตัวคูณ (easy x1, medium x1.5, hard x2) แสดงบนผนังมุมบนของห้อง

## สคริปต์ (`Assets/_VRTattoo/Scripts/Tattoo/`)
| ไฟล์ | หน้าที่ |
|---|---|
| `TattooCanvas.cs` | อยู่บน collider ของผิวที่สัก (สร้างโดย `CustomerTattooSkin`) RenderTexture 1024² พื้นโปร่งใส alpha = หมึก มี `DrawStroke(uvFrom, worldFrom, uvTo, worldTo)` ปั๊มแปรงด้วย GL และ `MetersPerUV` (ผู้สร้าง UV เป็นคนตั้ง) ไม่ผูกกับ renderer |
| `TattooMachine.cs` | อยู่บนเครื่องสัก ฟัง activate/deactivate ของ `XRGrabInteractable` raycast จาก `tip` ไปตามแนวเข็ม แล้ววาดและส่ง haptics มี `ExternalTrigger` ให้โหมดทดสอบสั่งวาดได้ |
| `ReturnToSocket.cs` | อยู่บนเครื่องสัก ปล่อยมือ (ที่ไม่ใช่ socket) หรือตกต่ำกว่า `fallResetHeight` จะ `SelectEnter` กลับเข้า socket ในเฟรมถัดไป |
| `TattooSeat.cs` | อยู่บน `Furniture/Chair43` (ตัวแม่) สลับนั่ง/ลุก (ปุ่ม B ขวา / F / ชี้เก้าอี้ + grip) ปิดและเปิดคืนเฉพาะ locomotion ที่เคยเปิดอยู่ และใส่/คืน `heldTool` เข้ามือ `handInteractor` ตำแหน่งตาตอนนั่งคิดจาก `lookTarget` (`SeatedEyePosition()`) |
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
| `TattooScorer.cs` | อ่าน RenderTexture ของ TattooCanvas เฉพาะบริเวณลาย (หมึก = alpha > 0.5) เทียบกับ alpha ของลาย แล้วคิด coverage/spill/quality ตาม `TattooScoringRules` |
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

- Shader: `Assets/_VRTattoo/Shaders/TattooBrush.shader` (แปรงกลมขอบนุ่ม)
- Material: `Assets/_VRTattoo/Materials/Tattoo/` (TattooBrush, TattooSkinOverlay, TattooMachine, TattooStand)
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
  - `TattooMachine`: มี XRGrabInteractable (Instantaneous) + TattooMachine + ReturnToSocket และ child `Tip` กับ `Attach`
  - `TattooMachineStand/Socket`: XRSocketInteractor (`SocketAttach` ให้เข็มชี้ลง)
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
| วาด (trigger) | คลิกซ้าย |
| นั่ง / ลุก | F, หรือ N ตอนคุม controller ขวา (= ปุ่ม B), หรือชี้เก้าอี้ + G (ต้องห่างเก้าอี้ไม่เกิน 1.5 ม.) |
| เปิด/ปิด Desktop Test Mode | K (ชนกับ simulator นิดหน่อยตอนกำลังคุม controller/มือ) |
| วาดใน Desktop mode | ชี้เมาส์ที่แขน + คลิกซ้ายค้าง |
| หันมอง | คลิกขวาค้าง + ลากเมาส์ |
| ซูม (เฉพาะ Desktop Test Mode) | ลูกกลิ้งเมาส์ |
| ส่งงาน (ให้คะแนน แล้วลูกค้าเดินออก) | J (เฉพาะ Editor) หรือชี้ปุ่ม SUBMIT ด้วย controller ซ้าย + G |

บนแว่นจริง นั่ง/ลุกใช้ปุ่ม B ขวา (ปุ่ม A ใช้กระโดดอยู่แล้ว)

## ข้อจำกัดที่ค้างอยู่
- **ขอบเส้นยังขรุขระ:** ตอนเปลี่ยนเป็นโมเดลแขนจริงให้เพิ่ม RenderTexture เป็น 2048 และทำเส้นให้เนียน
- **รอยสักไม่ถาวร:** หายเมื่อลูกค้าลุกจากเตียง และทุกครั้งที่กด Play
- **Socket ไม่ได้กรอง interaction layer:** ถ้ามีของอื่นที่หยิบได้ อาจไปเสียบที่วางได้
- **ระหว่างนั่ง:** ถือเครื่องค้างไว้ตลอด (ปล่อยด้วย grip ไม่ได้)
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

## งานถัดไป
- ให้ลูกค้าเปลี่ยนท่าแขน/ยื่นแขนให้ช่าง (ตอนนี้แขนวางข้างลำตัว) และรองรับตำแหน่งสักอื่น (ต้นแขน หลัง)
- เปลี่ยนโมเดลทดสอบเป็นโมเดลที่ใช้ได้ถูกต้องก่อนเผยแพร่
- ท่าลุก/นั่งลง/นอนลง แทนการวาร์ป
- เสียงเครื่องสัก (AudioListener พร้อมแล้ว)
- ป้ายภาษาไทย (ต้องมีฟอนต์ TMP ที่รองรับภาษาไทย)

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

## สคริปต์ (`Assets/_VRTattoo/Scripts/Tattoo/`)
| ไฟล์ | หน้าที่ |
|---|---|
| `TattooCanvas.cs` | อยู่บนแขน สร้าง RenderTexture (1024²) เป็น `_BaseMap` และมี `DrawStroke(uvFrom, worldFrom, uvTo, worldTo)` ปั๊มแปรงด้วย GL ประมาณค่าเมตรต่อ UV จาก mesh ตอน Awake |
| `TattooMachine.cs` | อยู่บนเครื่องสัก ฟัง activate/deactivate ของ `XRGrabInteractable` raycast จาก `tip` ไปตามแนวเข็ม แล้ววาดและส่ง haptics มี `ExternalTrigger` ให้โหมดทดสอบสั่งวาดได้ |
| `ReturnToSocket.cs` | อยู่บนเครื่องสัก ปล่อยมือ (ที่ไม่ใช่ socket) หรือตกต่ำกว่า `fallResetHeight` จะ `SelectEnter` กลับเข้า socket ในเฟรมถัดไป |
| `TattooSeat.cs` | อยู่บน `Furniture/Chair43` (ตัวแม่) สลับนั่ง/ลุก (ปุ่ม B ขวา / F / ชี้เก้าอี้ + grip) ปิดและเปิดคืนเฉพาะ locomotion ที่เคยเปิดอยู่ และใส่/คืน `heldTool` เข้ามือ `handInteractor` |
| `TattooDesktopTestMode.cs` | อยู่บน `Interactables/TattooSystem` โหมดทดสอบด้วยเมาส์ ปิด `trackPosition/Rotation` ของเครื่องระหว่างใช้ และหมุนหัว XR Device Simulator ผ่าน reflection (ฟิลด์ private `m_CenterEyeEuler`, `m_HMDState`) |

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
  - `TattooArm`: Capsule บนเตียง มี MeshCollider + TattooCanvas หมุนให้รอยต่อ UV หันลงเตียง
  - `TattooMachine`: มี XRGrabInteractable (Instantaneous) + TattooMachine + ReturnToSocket และ child `Tip` กับ `Attach`
  - `TattooMachineStand/Socket`: XRSocketInteractor (`SocketAttach` ให้เข็มชี้ลง)
  - `SeatHint`: ป้าย TMP บอกปุ่ม (ภาษาอังกฤษ)
- `Furniture/Chair43`: เก้าอี้ช่างสัก มี TattooSeat + XRSimpleInteractable ส่วน collider อยู่ที่ `Chair43_model`
- `Furniture/hospital bed`: เตียง (MeshCollider)
- `Room/Floor_Tile_3b`: พื้น มี MeshCollider + BoxCollider + TeleportationArea

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

## งานถัดไป
- ระบบลูกค้าเดินเข้าร้าน
- เสียงเครื่องสัก (AudioListener พร้อมแล้ว)
- ระบบเลือกลาย
- ป้ายภาษาไทย (ต้องมีฟอนต์ TMP ที่รองรับภาษาไทย)

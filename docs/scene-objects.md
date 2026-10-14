# GameObject สำคัญใน TattooRoom

- `XR Origin (XR Rig)`: rig ผู้เล่น
  - `Camera Offset/Main Camera` (มี AudioListener)
  - `Camera Offset/Right Controller/Near-Far Interactor` (มือที่ถือเครื่องตอนนั่ง)
  - `Locomotion/*` (providers ที่ถูกปิดตอนนั่ง)
- `XR Device Simulator`: ใช้ทดสอบบนคอม
- `Interactables/TattooSystem` (มี `TattooDesktopTestMode`)
  - `TattooWorkArea`: จุดว่างที่ (1.33, −1.05, −0.325) = กลางแขนท่อนล่างขวาของลูกค้าที่นอน (เฉลี่ยทุกตัว) เป็น `lookTarget` ของเก้าอี้ (TattooArm Capsule เดิมถูกลบแล้ว)
  - `TattooMachine`: มี XRGrabInteractable (Instantaneous) + TattooMachine + TattooInk + TattooNeedle + ReturnToSocket + Rigidbody, CapsuleCollider (หัวครอบปลาย+ด้าม) + BoxCollider (หัวเครื่อง) และ child `Model`, `Tip`, `Attach`, `InkGauge` (ดูหัวข้อ "เครื่องสัก")
  - `MachineHolder/Socket`: XRSocketInteractor บนรถเข็น (`SocketAttach` หมุนเท่ากับ `Attach` เครื่องจึงตั้งตรง เข็มชี้ลง ปลายเข็มลอยเหนือพื้นถาด 1 ซม.)
  - `Inks`: ขวดตกแต่งบนรถเข็น + `CapRack` ถ้วยหมึก 4 สี (ดูหัวข้อ "รถเข็น")
  - `NeedleRack`: หัวเข็ม 3 ขนาด (ดูหัวข้อ "หัวเข็ม")
  - `InkHologram`: วงแหวนบอกหมึก (ดูหัวข้อ "หมึก")
  - `SeatHint`: ป้าย TMP บอกปุ่ม (ภาษาอังกฤษ)
  - `SubmitButton`: ปุ่มส่งงานลอยข้างเตียง (1.75, −0.85, −0.05) ซ้ายมือผู้เล่นตอนนั่ง (ย้ายออกจากจุดที่หัวผู้เล่นอยู่ตอนนั่ง) (BoxCollider + XRSimpleInteractable, child `Visual` และ `Label`)
  - `TattooJobUI/{OrderLabel, ScoreLabel, TotalScoreLabel}`: ป้ายชื่อลาย/ระดับ (หลังเตียง), คะแนน (เหนือเตียง), คะแนนรวม (มุมบนผนังด้าน −z)
- `Furniture/Chair43`: เก้าอี้ช่างสัก (1.25, −1.785, 0.18) ชิดเตียงตรงแขน มี TattooSeat + XRSimpleInteractable ส่วน collider อยู่ที่ `Chair43_model` (`SeatHint` ย้ายตามไปอยู่เหนือเก้าอี้)
- `Furniture/hospital bed`: เตียง (MeshCollider)
- `Room/Floor_Tile_3b`: พื้น มี MeshCollider + BoxCollider + TeleportationArea
- `CustomerSystem` (มี `CustomerQueueManager` (`completeKey` = None) + `TattooJobManager` (ช่วงคะแนนอารมณ์ `moods` 90/70/40) และลูกค้าที่ spawn จะเป็นลูกของมัน)
  - `Locations/Door/DoorPoint`, `Locations/Bed/{BedApproach, BedPoint}`, `Locations/WaitArea1/{WaitSeat1Approach, WaitSeat1}` (`Chair15` ใกล้เตียงกว่า), `Locations/WaitArea2/...` (`Chair15 (1)`)
  - `Routes/*`: เส้นทาง 5 เส้น มี waypoint แค่ `Door-WaitSeat1` (อ้อมจุดเกิดผู้เล่น) ทุกเส้นตรวจด้วย CapsuleCast แล้วว่าไม่ชน

[← กลับ CLAUDE.md](../CLAUDE.md)

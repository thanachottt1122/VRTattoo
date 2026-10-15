# สคริปต์ทั้งหมด (รายละเอียด)

## สคริปต์ (`Assets/_VRTattoo/Scripts/Tattoo/`)
| ไฟล์ | หน้าที่ |
|---|---|
| `TattooCanvas.cs` | อยู่บน collider ของผิวที่สัก (สร้างโดย `CustomerTattooSkin`) RenderTexture 1024² พื้นโปร่งใส alpha = หมึก มี `DrawStroke(uvFrom, worldFrom, uvTo, worldTo, color, diameter)` ปั๊มแปรงด้วย GL (ไม่ส่ง diameter = `brushDiameter`) และ `MetersPerUV` (ผู้สร้าง UV เป็นคนตั้ง) ไม่ผูกกับ renderer; **needle map** `NeedleTexture` (R8 linear) เก็บขนาดหัวที่ใหญ่สุดที่ลงหมึกแต่ละพิกเซล (= diameter / `MaxNeedleDiameter` 1 ซม., pass 1 ของ `TattooBrush.shader` แบบ `BlendOp Max`) ให้ scorer ใช้ |
| `TattooMachine.cs` | อยู่บนเครื่องสัก ฟัง activate/deactivate ของ `XRGrabInteractable` raycast จาก `tip` ไปตามแนวเข็ม แล้ววาด (กว้างเท่า `needle.diameter`) และส่ง haptics มี `ExternalTrigger` ให้โหมดทดสอบสั่งวาดได้ |
| `ReturnToSocket.cs` | อยู่บนเครื่องสัก ปล่อยมือ (ที่ไม่ใช่ socket) หรือตกต่ำกว่า `fallResetHeight` จะ `SelectEnter` กลับเข้า socket ในเฟรมถัดไป |
| `TattooSeat.cs` | อยู่บน `Furniture/Chair43` (ตัวแม่) สลับนั่ง/ลุก (ปุ่ม B ขวา / F / ชี้เก้าอี้ + grip) ปิดและเปิดคืนเฉพาะ locomotion ที่เคยเปิดอยู่ ตอนนั่งใส่ `heldTool` เข้ามือ `handInteractor` (ปล่อย grip = วางคืน) ตอนลุกปล่อยจากมือที่ถืออยู่ ตำแหน่งตาตอนนั่งคิดจาก `lookTarget` (`SeatedEyePosition()`) |
| `SeatHeadLimit.cs` | อยู่บน `Furniture/Chair43` จำจุดตาตอนนั่ง: จอมืดตามระยะหัว (`freeRadius` 0.5, `fadeDistance` 0.15, `fadeSpeed`) ผ่าน `fadeRenderer` (Quad `Main Camera/ScreenFade` + `Shaders/ScreenFade.shader`, `Materials/ScreenFade.mat`) และ `lockSimulatorHead` เขียนตำแหน่ง `m_HMDState` ของ simulator กลับผ่าน reflection + `InputState.Change` (execution order หลัง simulator) ทำเฉพาะเมื่อ `!XRSettings.isDeviceActive` |
| `TattooInk.cs` | อยู่บนเครื่องสัก ดูหัวข้อ "หมึก" |
| `TattooNeedle.cs` | อยู่บนเครื่องสัก หัวเข็มที่ใส่อยู่ ดูหัวข้อ "หัวเข็ม" |
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
| `CustomerMood.cs` | `CustomerMood` (Angry/Neutral/Happy/Ecstatic) และ `CustomerMoodThresholds.For(quality)` (ค่าเริ่ม 90/70/40) ดู [customer-reactions.md](customer-reactions.md) |
| `CustomerEmote.cs` | อยู่บน `Customer.prefab` อิโมจิเหนือหัวหลังส่งงาน (`Show(mood)`) หันหากล้อง เด้งตอนโผล่ |
| `CustomerVoice.cs` / `CustomerVoiceSet.cs` | AudioSource 3D ที่หัว เล่นเสียงทักทาย/ร้องเจ็บ/ตามอารมณ์ จากชุดเสียง (`Data/Voices/`) ตามเพศหรือรายตัวละคร ช่องว่าง = ไม่เล่น |
| `CustomerCharacter.cs` | อยู่บน root ของ prefab ตัวละคร (ตัวเดียวกับ Animator) ค่าปรับรายตัว: `sitOffset/sitRotation`, `lieOffset/lieRotation` (ท้องถิ่นของลูกค้า; คลิปนอนหันหัวไป −Z จึงหมุน 180°), และ `OnAnimatorIK`: แขนทั้งสองข้างวางข้างลำตัว (`armsAlongBody`, `handOutward`, `armRestHeight`), เท้าวางบนเตียงห่างกลางตัว `footSpread` (`feetOnBed`, `ankleHeight`) |
| `CustomerCharacterPicker.cs` | อยู่บน `Customer.prefab` รายการ `characters` ตอน `Awake` สุ่มตัวละครที่ไม่ซ้ำกับคนก่อนหน้า และเลี่ยงตัวที่อยู่ในร้านแล้วถ้าเลือกได้ สร้างเป็นลูกแล้วส่งให้ `AnimatorCustomerVisual.SetCharacter` |
| `Editor/CharacterModelSetup.cs` | เมนู `VRTattoo/Characters/Setup Selected Models`: ตั้ง Rig = Humanoid, extract material ไป `<โฟลเดอร์โมเดล>/Materials` เป็น URP Lit, ใส่ texture ที่ชื่อขึ้นต้นด้วยชื่อ material (Diffuse/Color/_di → base, Normal/_tn → normal, Opacity → รวมเป็น alpha + Alpha Clipping) |

## สคริปต์ลาย/คะแนน (`Assets/_VRTattoo/Scripts/Stencils/`)
| ไฟล์ | หน้าที่ |
|---|---|
| `StencilLibrary.cs` | ScriptableObject `Data/StencilLibrary.asset` รายการลาย (texture, ชื่อ, ระดับ) + `Multiplier()` + แยกชื่อไฟล์ |
| `Editor/StencilLibraryPostprocessor.cs` | ตั้งค่า import PNG ใน `Stencils/` อัตโนมัติ (Single Channel Alpha, readable, ไม่บีบอัด) และสร้างคลังใหม่ทุกครั้งที่ไฟล์เปลี่ยน มีเมนู `VRTattoo/Rebuild Stencil Library` |
| `StencilPlacement.cs` | struct mapping UV ของ canvas ↔ พิกัดในภาพลาย (st) ใช้ร่วมกันระหว่าง overlay shader และ scorer |
| `TattooScorer.cs` | อ่าน RenderTexture ของ TattooCanvas ทั้งรูป (หมึก = alpha > 0.5) + needle map เทียบกับ mask ลาย (`StencilMask`) แล้วคิด accuracy/messRatio/quality ใน `ScoreMasks` (ฟังก์ชันล้วน ใช้ร่วมกับตัวจำลอง รับ needle map และ distance field ที่คิดไว้แล้วได้) ~200 ms ต่อการส่งงาน |
| `Editor/TattooScoreSimulator.cs` | เมนู `VRTattoo/Simulate Tattoo Scores` สร้างหมึกจำลอง 5 แบบ × หัวเข็ม 3 ขนาดให้ทุกลายในคลัง แล้ว log ตารางคะแนนแยกระดับ/หัว + แถวลายพื้นทึบ (ทำงานใน Edit mode; `Run(rules)` ใช้จูนค่าโดยไม่แก้ scene) |
| `TattooJobManager.cs` | อยู่บน `CustomerSystem` ฟัง `onCustomerLayDown` สุ่มลาย แสดงลาย/ป้าย รับ Submit (ปุ่ม/J) ให้คะแนน แล้วเรียก `CompleteTattoo()` |

## สคริปต์ห้อง (`Assets/_VRTattoo/Scripts/Room/`)
| ไฟล์ | หน้าที่ |
|---|---|
| `NeonFlicker.cs` | อยู่บนป้ายนีออน (`text` = TextMeshPro, `halo` = Renderer) กะพริบเบา ๆ แบบนีออนเก่า: สุ่มช่วงห่าง/จำนวนครั้ง/ความยาว หรี่ face+glow ของ TMP และ `_BaseColor` ของ halo บน material instance ไม่แก้ asset ([lighting.md](lighting.md)) |

- Shader: `Shaders/TattooSkinOverlay.shader` (unlit โปร่งใส: หมึกจาก `_InkTex` + ลายสีม่วงจาง), Material: `Materials/Tattoo/TattooSkinOverlay.mat`

- Prefab: `Assets/_VRTattoo/Prefabs/Customer.prefab` = `Customer` + `AnimatorCustomerVisual` + `CustomerCharacterPicker` (ไม่มีโมเดลในตัว ตัวละครถูกสร้างเป็นลูกตอน spawn) ระบบคิวไม่ได้แก้ (`customerPrefab` ยังเป็นตัวเดิม)
- Material capsule เดิม: `Materials/Customer/CustomerPlaceholder.mat`

- Shader: `Assets/_VRTattoo/Shaders/TattooBrush.shader` (pass 0 แปรงกลมขอบนุ่มลง canvas, pass 1 วงกลมขอบแข็งลง needle map)
- Material: `Assets/_VRTattoo/Materials/Tattoo/` (TattooBrush, TattooSkinOverlay, TattooMachine, TattooStand, InkGauge, InkHologram), `Materials/Needles/NeedleGrip.mat`
- สคริปต์เก่า: `AnimeateHandonInput.cs` (คลาส `AnimateHandOnInput` สั่งอนิเมชันมือ), `InputTest.cs` (log ค่า input; GameObject `Input Test` ปิดไว้)

[← กลับ CLAUDE.md](../CLAUDE.md)

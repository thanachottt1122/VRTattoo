# ตัวละครลูกค้า

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

[← กลับ CLAUDE.md](../CLAUDE.md)

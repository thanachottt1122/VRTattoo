# ผิวที่สัก (`CustomerTattooSkin`)

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

[← กลับ CLAUDE.md](../CLAUDE.md)

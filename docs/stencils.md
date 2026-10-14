# ลายสัก: เพิ่มลายและตัดลายจากรูปรวม

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

[← กลับ CLAUDE.md](../CLAUDE.md)

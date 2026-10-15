# ความพึงพอใจของลูกค้า (อิโมจิ + เสียง)

## อิโมจิบนหัว (`CustomerEmote`)
- ส่งงานแล้ว `TattooJobManager.SubmitRoutine` แปลงคุณภาพ (0–100) เป็นอารมณ์ด้วย `moods` (`CustomerMoodThresholds`) แล้วเรียก `CustomerEmote.Show(mood)` และ `CustomerVoice.PlayMood(mood)` บนลูกค้าบนเตียง
- ช่วงคะแนน (ปรับได้ที่ `TattooJobManager` → **Customer mood** ใน Inspector):

| คุณภาพ | อารมณ์ | ภาพ |
|---|---|---|
| ≥ `ecstatic` (90) | Ecstatic ดีใจมาก | `Emote_Ecstatic.png` ตาหัวใจ |
| ≥ `happy` (70) | Happy ยิ้ม | `Emote_Happy.png` |
| ≥ `neutral` (40) | Neutral เฉย ๆ / ไม่ค่อยพอใจ | `Emote_Neutral.png` |
| ต่ำกว่า `neutral` | Angry โกรธ | `Emote_Angry.png` หน้าแดง |

- `CustomerEmote` (บน `Customer.prefab`) สร้างลูก `Emote` (SpriteRenderer, Sprite-Unlit) ตอน `Awake` ซ่อนไว้ ตอน `Show` จะโผล่แบบเด้ง (`popDuration` 0.4 วิ, scale easeOutBack + ลอยขึ้น `popRise`) แล้วลอยขึ้นลงเบา ๆ (`bobHeight`)
- ตำแหน่ง = กระดูก Head + world up `heightAboveHead` (0.32 ม.) ใช้ได้ทั้งท่านอนและเดิน, กว้าง `size` 0.28 ม., หันหา `Camera.main` ทุกเฟรม (ตั้ง `viewer` เองได้)
- อยู่บนตัวลูกค้า จึงค้างตั้งแต่ส่งงาน ระหว่างลุกเดินออก และหายไปพร้อมลูกค้าตอนถึงประตู

### เปลี่ยนรูป
- ทับไฟล์ PNG เดิมใน `Assets/_VRTattoo/UI/Emotes/` (ชื่อเดิม, import เป็น Sprite) หรือใส่ sprite อื่นที่ `CustomerEmote` บน `Customer.prefab`
- รูปปัจจุบันวาดด้วย `Tools/emotes/draw_emotes.py` (Pillow): `python Tools/emotes/draw_emotes.py` วาดใหม่ทั้ง 7 ไฟล์ (256², ppu 256): 4 อารมณ์ + `Annoyed` (หงุดหงิด เหนือหัว) + `Tense`/`Hurt` (หน้าความเจ็บบนการ์ด, หน้าสบายใช้ `Happy`)

## เสียง (`CustomerVoice` + `CustomerVoiceSet`)
ยังไม่มีไฟล์เสียง ช่องว่างจะไม่เล่นอะไรและไม่ error (`debugLog` บน `CustomerVoice` จะ log `<line> -> no clip`)

- `CustomerVoice` (บน `Customer.prefab`) สร้างลูก `Voice` + AudioSource 3D (`spatialBlend` 1, Logarithmic, `minDistance` 0.5 / `maxDistance` 8 ม., ไม่มี doppler) ที่ตามกระดูก Head
- ชุดเสียง `CustomerVoiceSet` (ScriptableObject) มีช่อง: `greeting`, `pain`, `ecstatic`, `happy`, `neutral`, `angry` แต่ละช่องใส่ได้หลายไฟล์ สุ่มเล่น (ไม่ซ้ำไฟล์ก่อนหน้าถ้ามีให้เลือก) + `volume`, `pitchRange` (สุ่ม pitch ให้ไม่ซ้ำซาก)
- เลือกชุด: `CustomerCharacter.voiceOverride` ของตัวละคร (ถ้าใส่) → ไม่งั้นตาม `CustomerCharacter.gender`: `maleVoice` / `femaleVoice` บน `CustomerVoice`
- ตัวละครตอนนี้: Leni, Tomizawa, Vijay = Male, Tanya = Female

| เสียง | เล่นเมื่อ |
|---|---|
| greeting | ลูกค้าเกิดที่ประตู หน่วงสุ่ม `greetingDelay` 0.2–0.8 วิ (ปิดได้ด้วย `greetOnArrival`) |
| pain | ระหว่างโดนสัก (canvas ของลูกค้าถูกวาดภายใน `drawGrace` 0.2 วิ) สะสมเวลาโดนสักครบ `painMinTattooSeconds` 1.5 วิ และพ้น cooldown สุ่ม `painCooldown` 6–12 วิ จากครั้งก่อน แล้วสุ่มโอกาส `painChancePerSecond` 0.35/วิ |
| ecstatic / happy / neutral / angry | ตอนส่งงาน ตามอารมณ์ |

### วิธีใส่เสียง
1. ลากไฟล์เสียง (wav/ogg) ไว้ที่ `Assets/_VRTattoo/Audio/Voices/<ชุด>/` แนะนำตั้ง import: Force To Mono, Load Type = Decompress On Load (คลิปสั้น), Compression = Vorbis
2. เปิด `Assets/_VRTattoo/Data/Voices/Voice_Male.asset` หรือ `Voice_Female.asset` แล้วลากคลิปลงช่องที่ต้องการ (ช่องละกี่ไฟล์ก็ได้)
3. ชุดรายตัวละคร: Project → Create → **VRTattoo/Customer Voice Set** ตั้งชื่อ `Voice_<ชื่อ>` ใส่คลิป แล้วลากลง `voiceOverride` ของ `CustomerCharacter` บน `Prefabs/Characters/Customer_<ชื่อ>.prefab`
4. ตัวละครใหม่: ตั้ง `gender` ของ `CustomerCharacter` ให้ถูก
5. ทดสอบ: เปิด `debugLog` บน `CustomerVoice` ของ `Customer.prefab` แล้ว Play ดู log `[CustomerVoice]`
6. ร้องเจ็บถี่/ห่างไป: ปรับ `painCooldown`, `painChancePerSecond`, `painMinTattooSeconds` บน `CustomerVoice`

## ความเจ็บ (`CustomerPain` บน `Customer.prefab`)
- ค่า 0–100% ต่อลูกค้า รีเซ็ตเป็น 0 ทุกครั้งที่นอนลง (`TattooJobManager.OnCustomerLayDown` → `ResetPain`) แสดงบนการ์ดงาน (หน้า + เกจ + %)
- ข้อมูลเข้า: event `TattooCanvas.Stroked(worldPoint, diameter)` ทุกครั้งที่วาด (ไม่ต้องแก้ `TattooMachine`)
- ต่อวินาทีที่กำลังสัก (หารด้วย `Tolerance`):
  - ฐาน `baseRate` 1.5 × (หัวเข็ม / `referenceDiameter` 5 มม.)^`needleExponent` 1.5 → LINER 3 มม. ≈ 0.7%/วิ, MEDIUM 1.5, SHADER 9 มม. ≈ 3.6
  - ค้างจุดเดิม (อยู่ใน `dwellRadius` 6 มม.) เกิน `dwellGrace` 0.8 วิ → +`dwellRate` 10 ต่อวินาทีที่ค้าง สูงสุด +`dwellMaxRate` 30
  - ลากเร็ว (ความเร็วปลายเข็มเฉลี่ย > `fastSpeed` 0.12 ม./วิ) → +`fastRate` 8 × (speed/fastSpeed − 1) สูงสุด +`fastMaxRate` 20
  - หยุดสักเกิน `decayDelay` 0.4 วิ → ลด `decayRate` 8%/วิ
- `toleranceRange` 0.7–1.4 สุ่มต่อคนตอนเกิด (ยิ่งมากยิ่งทน)
- ≥ `flinchThreshold` 60%: สะดุ้งทุก `flinchInterval` 1.5–3 วิ (ขณะกำลังสัก) → ไหล่ฝั่งที่สักหมุน `flinchAngle` 4° แล้วคืนใน `flinchSeconds` 0.25 วิ (LateUpdate หลัง IK; collider ผิวไม่ขยับ เข็มจึงวาดที่เดิม), เส้นกระโดด `flinchInkOffset` 1.5 มม. ช่วงสะดุ้ง (`TattooCanvas.Nudge`, 0 = ปิด), เสียงร้องเจ็บ, สั่นมือที่ถือเครื่อง, หน้าบนการ์ดเด้ง
- 100%: `MaxedOut` → `TattooJobManager.WalkOutRoutine` อิโมจิโกรธ + เสียงโกรธ การ์ด "0 / WALKED OUT" ลูกค้าลุกเดินออกทันที +0 คะแนน (ความเจ็บต่ำกว่า 100% ไม่มีผลกับคะแนน)
- `debugLog` พิมพ์ทุก 10% และตอนสะดุ้ง

## ความอดทน (`CustomerPatience` บน `Customer.prefab`)
- **ที่นั่งรอ:** เริ่มนับตอนถึงโซฟา (`CustomerQueueManager.OnArrived` → `StartWaiting`) สุ่ม `waitSeconds` 150–220 วิ เลื่อนที่นั่งไม่รีเซ็ต เวลาเดินไม่นับ หมดเวลา → อิโมจิโกรธ + เสียงโกรธ แล้ว `SendHome` เดินออกประตู คิวเลื่อน ลูกค้าใหม่เข้ามาแทน (ไม่หักคะแนน)
- **บนเตียง:** `TattooJobManager` ตั้งเวลาตามระดับลาย (`patience`: easy 60 / medium 90 / hard 130 วิ) เลยเวลาแล้วนับต่อ (overtime) ลูกค้าไม่ลุกเอง
- เหลือน้อยกว่า `annoyedFraction` 25% → `CustomerEmote.SetAnnoyed` อิโมจิหงุดหงิด (`Emote_Annoyed.png`) เหนือหัว; อิโมจิผลงานตอนส่งงานแทนที่เสมอ
- คะแนน: ดู [scoring.md](scoring.md#เวลา)

## ทดสอบ
- ความเจ็บ (เรียก `DrawStroke` ซ้ำผ่าน `EditorApplication.update`, tolerance 0.90–1.31, ค่าก่อนจูนลง): ค้างจุดเดิมด้วยหัว 9 มม. ถึง 60% ใน 3.7 วิ → สะดุ้ง → 100% ใน 4.7 วิ → walked out +0 คนถัดไปขึ้นเตียง; 4 วิแบบลากช้า: 3 มม. 2.9%, 5 มม. 8.4%, 9 มม. 17.9%; ลากเร็ว ~0.3 ม./วิ 63.6% (จึงลด `baseRate` 2.5→1.5, `fastRate` 12→8); หยุดแล้วลดลง ~8%/วิ
- ความอดทน: ตั้งเวลาลูกค้าโซฟาเหลือ 3 วิ → Angry เดินออก คนหลังเลื่อนขึ้น; ใกล้หมด → `IsAnnoyed`
- `TattooJobManager.SubmitForTest(quality)` (Editor เท่านั้น) ส่งงานเหมือนปุ่มจริงแต่แทนคุณภาพ ใช้ผ่าน `execute_code`
- ผลทดสอบ: 95 → Ecstatic, 80 → Happy, 55 → Neutral, 20 → Angry; ขอบ 90/89, 70/69, 40/39 ถูกต้อง; อิโมจิสูงเหนือกระดูก Head 0.32 ม., กว้าง 0.28 ม. (เด้งถึง ~0.29 ก่อนนิ่ง), หันหากล้อง (dot = 1.000), ยังแสดงตอนเดินออก; ร้องเจ็บเล่นครั้งเดียวในการสักต่อเนื่อง ~10 วิ (cooldown ทำงาน)

[← กลับ CLAUDE.md](../CLAUDE.md)

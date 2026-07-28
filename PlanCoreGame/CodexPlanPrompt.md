# PROMPT CHI TIẾT GỬI CODEX: KẾ HOẠCH VẬN HÀNH & NỀN KINH TẾ PHÒNG LAB (CHEMISTRY LAB)

Chào Codex! Hãy đọc toàn bộ file bối cảnh dự án tại `PlanCoreGame/CoreLogic.md`, `PlanCoreGame/GamePlay.md`, và `PlanCoreGame/Overview.md`. Sau đó, hãy xây dựng một bản **Kế hoạch Vận Hành & Nền Kinh Tế Chi Tiết** cho Game Chemistry LAB theo đúng 5 trụ cột sau:

---

### 💵 1. KHỞI ĐẦU DỰ ÁN (STARTER ECONOMY)
- **Vốn ban đầu**: Xác định chính xác số Tiền ($) và Kim Cương (💎) cấp cho người chơi mới (ví dụ: 500 $ + 100 💎).
- **Hóa chất / Hợp chất Starter (Free / Rẻ trên vỏ Trái Đất)**:
  - Khí: N2, O2, CO2 (Free / lấy từ không khí).
  - Nước biển & Khoáng thạch: NaCl, H2O, SiO2, Al2O3.
  - Dung dịch hữu dụng cơ bản: KMnO4 (Thuốc tím), HCl, H2SO4, NH3, H2O2, Chanh, Giấm, Rượu Etylic (C2H5OH).
- **Dụng cụ khởi đầu**: 1 Cốc Pyrex Beaker 100ml, 2 Ống nghiệm, 1 Đèn cồn.

---

### 📅 2. PHẦN THƯỞNG ĐĂNG NHẬP HẰNG NGÀY (DAILY LOGIN REWARDS - 7 DAYS LOOP)
Xây dựng chuỗi phần thưởng 7 ngày đăng nhập liên tục:
- **Ngày 1**: +100 $ + 50g H2O2.
- **Ngày 2**: +150 $ + 20 💎.
- **Ngày 3**: +200 $ + 1 Bộ Cốc Beaker 250ml.
- **Ngày 4**: +250 $ + 30g KMnO4 tinh khiết.
- **Ngày 5**: +300 $ + 30 💎.
- **Ngày 6**: +400 $ + 1 Máy cô cạn / Điện phân dung dịch mini.
- **Ngày 7**: +500 $ + 50 💎 + 1 Lọ Bạch Kim (Pt) hoặc Kim loại hiếm.

---

### 🗺️ 3. HÀNH TRÌNH MỞ KHÓA & NÂNG CẤP LAB (PROGRESSION & UPGRADE PATH)
Xây dựng tiến trình Level 1 ➔ Level 10:
- **Hệ thống Cấp độ (Level 1 - 10)**: Lượng EXP yêu cầu từng cấp (`EXP = Level * 100`).
- **Nâng cấp Phòng Lab (`LabUpgradeLevel`)**:
  - Level 1-3: Mở rộng sức chứa kho hóa chất & bàn thí nghiệm cơ bản.
  - Level 4-6: Mở khóa các Máy Điều Chế đặc biệt (Máy Điện Phân Dung Dịch, Điện Phân Nóng Chảy).
  - Level 7-10: Mở khóa Tháp phản ứng Ostwald điều chế HNO3 (dùng xúc tác Pt).

---

### 🧪 4. DANH SÁCH HÓA CHẤT & CHẤT THU ĐƯỢC (CHEMICAL PROGRESSION LIST)
Phân loại rõ ràng các nhóm chất:
- **Chất cơ bản (Tự do/Rẻ)**: H2O, NaCl, HCl, NaOH, KMnO4, NH3, C2H5OH.
- **Chất tự điều chế qua phản ứng**: Cl2, H2, O2, CuSO4, FeSO4, Al(OH)3, Phức chất [Cu(NH3)4]2+, Ester mùi hoa quả...
- **Chất hiếm/Cao cấp (Mua qua Kim Cương hoặc chế tạo phức tạp)**: Vàng (Au), Bạch Kim (Pt), Khí F2, Kim loại kiềm tinh khiết (Na, K).

---

### 📜 5. NHIỆM VỤ NHẬN TIỀN & KHÔNG TẠO NGÕ CỤT (QUEST & NON-DEADLOCK ECONOMY)
- **Cấu trúc Nhiệm Vụ NPC**:
  - **Nhiệm vụ Tutorial**: "Rót 50g H2O và 10g NaCl vào cốc 100ml để hòa tan muối" ➔ Thưởng: 150 $, 50 EXP.
  - **Nhiệm vụ Trung cấp**: "Điều chế Khí O2 từ Nhiệt phân KMnO4 hoặc H2O2" ➔ Thưởng: 300 $, 100 EXP.
  - **Nhiệm vụ Cao cấp**: "Điều chế Axit HNO3 đạt độ tinh khiết 90%" ➔ Thưởng: 800 $, 100 💎, 300 EXP.
- **Quy tắc Chống Ngõ Cụt (Non-Deadlock Rule)**:
  - Khi mở khóa chất mới ➔ Cửa hàng (Shop) tự động cập nhật nguyên liệu thô để người chơi luôn có thể mua điều chế tiếp nếu lỡ làm mất/dùng hết sản phẩm!

---

Hãy xuất kết quả kế hoạch chi tiết dưới dạng file Markdown và lưu vào `PlanCoreGame/EconomyAndProgressionPlan.md`.

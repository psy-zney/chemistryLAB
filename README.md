# 🧪 Chemistry LAB Simulator 2D - Phòng Thí Nghiệm Hóa Học Học Thuật

**Chemistry LAB** là tựa game 2D mô phỏng phòng thí nghiệm hóa học chân thực và chuyên sâu trên **Unity 6**, được thiết kế chuẩn theo kiến thức học thuật kết hợp với lối chơi nhập vai trải nghiệm sinh động. Người chơi vào vai một Nhà khoa học trẻ, tiếp nhận các hợp đồng điều chế từ NPC, mở khóa thiết bị hiện đại và tự do khám phá các phản ứng hóa học kỳ diệu.

---

## 🌟 ĐIỂM NỔI BẬT CỦA GAME (HIGHLIGHTS)

### ⚗️ 1. Mô Phỏng Hiện Tượng Hóa Học Chân Thực 100%
- **Đổi màu dung dịch (Color Change)**: Chuyển màu linh hoạt theo chỉ thị màu pH hoặc phản ứng oxy hóa - khử.
- **Nổi bọt khí (Gas Evolution)**: Phát sinh hạt bọt khí sôi sục khi tạo thành các chất khí ($CO_2, H_2, O_2...$).
- **Xuất hiện kết tủa (Precipitate Formation)**: Lắng cặn rắn không tan đọng dần xuống đáy cốc ($BaSO_4 \downarrow, AgCl \downarrow...$).
- **Tạo phức chất (Complexation)**: Phản ứng nối tiếp hòa tan kết tủa thành phức dung dịch xanh thẫm trong suốt ($[Cu(NH_3)_4]^{2+}$).
- **Tách lớp chất lỏng (Phase Separation)**: Tự động phân tách thành các tầng chất lỏng ranh giới rõ rệt khi trộn các chất không hòa tan ($Immiscible$).
- **Tỏa nhiệt & Thu nhiệt (Exothermic / Endothermic)**.

---

### 🔬 2. Bố Cục Thí Nghiệm 4 Khu Vực (4-Zone Workbench)
- 🗄️ **Tủ Hóa Chất (Left Zone)**: Mở tủ chọn dung dịch/chất rắn và đong đếm liều lượng Gram `<g>` chính xác.
- 🧪 **Tủ Dụng Cụ (Right Zone)**: Chọn cốc Beaker Pyrex chia độ, ống nghiệm, bình tam giác, đèn cồn Bunsen.
- 🔬 **Bàn Thí Nghiệm Chính (Center Workbench)**: Trộn chất, thực thi phản ứng live và hiển thị Phương Trình Hóa Học.
- 🚰 **Bồn Rửa & Thu Hồi (Bottom Zone)**: Rửa sạch thiết bị sau khi làm xong và thu hồi sản phẩm điều chế vào Kho.

---

### 🏠 3. Sảnh Chính & Tùy Chỉnh Nhân Vật (Lobby & Avatar Customizer)
- 👤 **Tùy chỉnh Nhân vật (Facebook-style Avatar)**: Tùy chọn Tóc, Màu da, Màu tóc, Trang phục Blouse/Hazmat và Kính bảo hộ phòng lab.
- 📜 **Nhiệm vụ NPC & Kinh Tế**: Làm nhiệm vụ nhận Tiền ($) và Kim Cương (💎) để nâng cấp phòng lab và mở khóa công thức mới.
- 🛍️ **Cửa Hàng & Kho Lưu Trữ**: Mua bán và quản lý hóa chất nguyên liệu.

---

### 🖼️ 4. Đồ Họa Studio Chân Thực (Authentic Laboratory Art)
- Nói KHÔNG với hiệu ứng AI Neon sặc sỡ. Game sử dụng bộ Sprite lọ thủy tinh nút mài kính, nhãn dán giấy in tên hóa chất chuẩn studio thực tế.

---

## 🛠️ KIẾN TRÚC CÔNG NGHỆ (TECH STACK & ARCHITECTURE)

- **Engine**: Unity 6 (`6000.5.3f1`)
- **Ngôn ngữ**: C# (.NET / Standard MVP Pattern)
- **Kiến trúc Clean Architecture**:
  - `Domain`: Các Entity bất biến (`ChemicalItem`, `Reaction`, `PlayerProfile`, `AvatarData`).
  - `Application`: Bộ giải mã phản ứng `ReactionResolver`, `ContentValidator`, `ContentImporter`.
  - `Presentation`: Architecture Presenter-View (`MainLabPresenter`, `MainLabUnityView`, `CharacterCreationUnityView`, `LobbyHomeUnityView`).
  - `Infrastructure`: Binary Save/Load Persistence qua `SaveRepository`.

---

## 🚀 HƯỚNG DẪN CHẠY DỰ ÁN (GETTING STARTED)

1. **Clone Repository**:
   ```bash
   git clone https://github.com/psy-zney/chemistryLAB.git
   ```
2. **Mở dự án trong Unity 6 Editor** (`6000.5.3f1` trở lên).
3. **Mở Scene**: `Assets/_Game/Scenes/MainLab.unity`.
4. **Bấm ▶ PLAY** để bắt đầu trải nghiệm!

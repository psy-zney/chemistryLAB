# 🧪 Chemistry LAB - Game Thí Nghiệm Hóa Học 2D

Mô phỏng phòng thí nghiệm hóa học 2D sinh động, chân thực và chuẩn học thuật trên **Unity 6**. Người chơi vào vai Nhà khoa học, thực hiện các nhiệm vụ điều chế hóa chất từ NPC, nâng cấp phòng lab và khám phá thế giới phản ứng hóa học độc đáo.

---

## 🌟 Tính Năng Nổi Bật (Key Features)

- 🏠 **Màn Hình Chờ Sảnh (Lobby Home Screen)**: Thiết kế chuẩn phác thảo gồm Shop, Kho hóa chất, Bảng Nhiệm vụ NPC, tùy chỉnh nhân vật và nút gia nhập Phòng Lab.
- 👨‍🔬 **Tùy Chỉnh Nhân Vật (Modular Character Customizer)**: Tùy chọn Tóc, Màu da, Màu tóc, Trang phục Blouse/Hazmat và Kính bảo hộ phòng lab.
- 🔬 **Bàn Thí Nghiệm 4 Khu Vực (4-Zone Lab Workbench)**:
  - **Tủ Hóa Chất (Trái)**: Mở tủ chọn chất lỏng/chất rắn và đong đếm khối lượng <g>.
  - **Tủ Dụng Cụ (Phải)**: Chọn cốc Beaker 100ml, ống nghiệm, bình tam giác, đèn cồn.
  - **Bàn Phản Ứng (Giữa)**: Trộn chất, thực thi phản ứng live với đầy đủ hiện tượng (đổi màu, sủi bọt khí, kết tủa đọng đáy, tách lớp chất lỏng, tỏa/thu nhiệt).
  - **Bồn Rửa & Thu Hồi (Dưới/Sau)**: Thu sản phẩm điều chế vào Kho và rửa sạch thiết bị.
- 🖼️ **Hình Ảnh Chân Thực Studio**: Loại bỏ các hiệu ứng neon AI slop, sử dụng hình ảnh lọ thủy tinh nút mài kính, nhãn dán in tên hóa chất chuẩn thực tế phòng thí nghiệm.

---

## 🛠️ Kiến Trúc Công Nghệ (Tech Stack & Architecture)

- **Engine**: Unity 6 (`6000.5.3f1`)
- **Ngôn ngữ**: C# (.NET / Standard MVP Pattern)
- **Kiến trúc**:
  - `Domain Layer`: Các Entity bất biến (`ChemicalItem`, `Reaction`, `PlayerProfile`, `AvatarData`) độc lập với Unity UI.
  - `Application Layer`: Bộ giải mã phản ứng `ReactionResolver`, `ContentValidator`, `ContentImporter`.
  - `Presentation Layer`: Architecture Presenter-View (`MainLabPresenter`, `MainLabUnityView`, `CharacterCreationUnityView`, `LobbyHomeUnityView`).
  - `Infrastructure Layer`: Binary Save/Load Persistence qua `SaveRepository`.

---

## 🚀 Hướng Dẫn Chạy Dự Án (Getting Started)

1. **Clone Repository**:
   ```bash
   git clone https://github.com/psy-zney/chemistryLAB.git
   ```
2. **Mở dự án trong Unity 6 Editor** (`6000.5.3f1` trở lên).
3. **Mở Scene**: `Assets/_Game/Scenes/MainLab.unity`.
4. **Bấm ▶ PLAY** để bắt đầu trải nghiệm!

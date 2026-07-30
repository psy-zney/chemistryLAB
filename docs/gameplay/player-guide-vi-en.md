# Chemistry Lab 3D — Hướng dẫn người chơi / Player Guide

Tài liệu này mô tả cùng một gameplay bằng tiếng Việt và English. Trong game, mở `ESC → Cài đặt / Settings → Ngôn ngữ / Language` để đổi ngôn ngữ. Lựa chọn được lưu tự động và áp dụng ngay cho menu, HUD, hướng dẫn, prompt tương tác và thẻ phản ứng.

This document describes the same gameplay in Vietnamese and English. In game, open `ESC → Cài đặt / Settings → Ngôn ngữ / Language` to switch language. The choice is saved automatically and immediately updates menus, HUD, guidance, interaction prompts and reaction cards.

## 1. Mục tiêu đầu tiên / First objective

**VI:** Điều chế kết tủa xanh `Cu(OH)₂` từ `CuSO₄·5H₂O` và `NaOH` tại bàn phản ứng trung tâm.

**EN:** Produce blue `Cu(OH)₂` precipitate from `CuSO₄·5H₂O` and `NaOH` at the central reaction bench.

Phương trình / Equation:

```text
CuSO₄ + 2NaOH → Cu(OH)₂↓ + Na₂SO₄
```

## 2. Quy trình vật lý bắt buộc / Required physical workflow

1. **VI:** Đi tới kho hóa chất, ngắm vào chai và nhấn `E` để cầm mẫu.
   **EN:** Go to chemical storage, aim at a bottle and press `E` to pick it up.

2. **VI:** Đi tới khay cạnh bình phản ứng, ngắm vào khay và nhấn `E` để đặt mẫu xuống.
   **EN:** Go to the tray beside the reaction vessel, aim at the tray and press `E` to stage the sample.

3. **VI:** Khi tay đã trống, ngắm vào bình và nhấn `E` để nạp mẫu từ khay.
   **EN:** With empty hands, aim at the vessel and press `E` to load the staged sample.

4. **VI:** Lặp lại ba bước trên với hóa chất thứ hai. Phản ứng chỉ được đánh giá sau khi mẫu đã nằm trong bình.
   **EN:** Repeat the three steps with the second chemical. The reaction is evaluated only after the sample enters the vessel.

> **VI:** Không thể phản ứng trực tiếp trên tay hoặc nạp hóa chất từ xa.
> **EN:** Reactions cannot occur in hand, and chemicals cannot be loaded remotely.

## 3. Quan sát phản ứng / Observe the reaction

Khi phản ứng xảy ra, camera chuyển sang góc cận bình và thẻ phản ứng hiển thị:

When a reaction occurs, the camera moves to a close vessel view and the reaction card shows:

- phương trình cân bằng / balanced equation;
- nhiệt độ, thể tích, pH, nồng độ và tốc độ / temperature, volume, pH, concentration and rate;
- xúc tác hoặc trạng thái không cần xúc tác / catalyst or no-catalyst state;
- hiện tượng như kết tủa, khí, đổi màu hoặc nhiệt / precipitation, gas, colour change or heat;
- cảnh báo an toàn và yêu cầu xử lý / safety and handling requirements.

Nhấn `Space` hoặc `E` để bỏ qua góc cận / Press `Space` or `E` to skip the close-up.

## 4. Điều chỉnh điều kiện / Adjust conditions

| Phím / Key | Tiếng Việt | English |
|---|---|---|
| `Page Up` | Tăng nhiệt độ bình hiện tại 25 °C | Raise current vessel temperature by 25 °C |
| `Page Down` | Giảm nhiệt độ bình hiện tại 25 °C | Lower current vessel temperature by 25 °C |
| `F8` | Thêm 50 mL dung môi để pha loãng | Add 50 mL solvent to dilute |
| `[` / `]` | Giảm / tăng khối lượng mẫu đang chọn | Decrease / increase selected sample mass |
| `F` | Mở / đóng dữ liệu hóa chất và bình | Open / close chemical and vessel data |

Engine đánh giá nhiệt độ, nồng độ, pH, xúc tác, nhánh oxi hóa–khử, chất giới hạn, sản lượng và độ tinh khiết. Nếu điều kiện chưa đạt, thẻ trạng thái cho biết phản ứng đang chờ hoặc bị chặn.

The engine evaluates temperature, concentration, pH, catalyst, redox branch, limiting reagent, yield and purity. If conditions are not met, the status card reports that the reaction is waiting or blocked.

## 5. An toàn và sản phẩm khí / Safety and gas products

**VI:** Hóa chất hoặc sản phẩm độc kích hoạt cảnh báo. Mua/đeo mặt nạ tại tủ PPE (`E` hoặc `F6`). Nối bình cách ly vào hệ rửa khí (`E` tại thiết bị hoặc `F7`). Sản phẩm khí chỉ được thu trong tủ hút khi hệ rửa khí đã nối. Bỏ qua kiểm soát có thể làm mất sức khỏe và tín dụng.

**EN:** Toxic chemicals or products trigger a warning. Buy/wear the respirator at the PPE cabinet (`E` or `F6`). Connect the isolation trap to the gas scrubber (`E` at the device or `F7`). Gas products can only be collected in the fume hood with the gas trap connected. Ignoring controls may reduce health and credits.

## 6. Thu và tái sử dụng sản phẩm / Collect and reuse products

**VI:** Khi phản ứng đủ điều kiện, nhấn `C`, hoặc nhấn `E` tại bình với tay trống, để lưu sản phẩm thành một lô có khối lượng và độ tinh khiết. Nhấn `I` để duyệt các lô đã điều chế. Lô được chọn có thể đặt lên khay và dùng làm chất phản ứng tiếp theo.

**EN:** When a reaction is eligible, press `C`, or press `E` at the vessel with empty hands, to save the product as a batch with mass and purity. Press `I` to cycle synthesized batches. A selected batch can be staged on a tray and reused as a reagent.

## 7. Điều khiển đầy đủ / Complete controls

| Phím / Key | Tiếng Việt | English |
|---|---|---|
| `WASD` | Di chuyển | Move |
| `Chuột / Mouse` | Nhìn xung quanh | Look around |
| `Shift` | Chạy | Sprint |
| `E` | Lấy, đặt, nạp, thu hoặc tương tác | Pick up, place, load, collect or interact |
| `Q` | Cất mẫu đang cầm | Put away held sample |
| `C` | Thu sản phẩm | Collect product |
| `I` | Chuyển lô trong kho điều chế | Cycle synthesized inventory |
| `F3` | Bật/tắt chẩn đoán runtime | Toggle runtime diagnostics |
| `F6` | Mua/đeo/tháo mặt nạ | Buy/wear/remove respirator |
| `F7` | Nối/tháo hệ rửa khí | Connect/disconnect gas trap |
| `F9` | Bật/tắt âm thanh | Toggle audio |
| `F10` | Bật/tắt giảm chuyển động | Toggle reduced motion |
| `ESC` | Tạm dừng; từ Settings quay lại menu trước | Pause; return from Settings to its parent menu |

## 8. Khắc phục thao tác / Interaction troubleshooting

- **VI:** Nếu `E` không hoạt động, đứng gần hơn và đặt tâm ngắm đúng vào khay, bình hoặc thiết bị.
  **EN:** If `E` does not work, move closer and aim directly at the tray, vessel or device.
- **VI:** Nếu bình từ chối mẫu, hãy chắc chắn hóa chất đã được đặt trên đúng khay và tay đang trống.
  **EN:** If the vessel rejects a sample, confirm it is on the correct tray and your hands are empty.
- **VI:** Nếu phản ứng chưa xảy ra, mở dữ liệu bằng `F`, kiểm tra điều kiện rồi điều chỉnh nhiệt độ hoặc pha loãng.
  **EN:** If no reaction occurs, open data with `F`, inspect the conditions, then adjust temperature or dilution.
- **VI:** Nếu menu không nhận chuột, nhấn `ESC` một lần để mở đúng trạng thái pause rồi thử lại.
  **EN:** If the menu does not receive the mouse, press `ESC` once to enter the correct pause state and try again.

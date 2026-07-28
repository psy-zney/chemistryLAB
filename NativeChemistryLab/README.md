# Chemistry Lab 3D — Windows

Đây là bản Unity/C# độc lập, không chạy trên trình duyệt.

## Cấu trúc project

`NativeChemistryLab` là project Unity production. Mã ứng dụng nằm trong một
feature root duy nhất:

```text
Assets/ChemistryLab/
├── Runtime/
│   ├── Audio/          # nhạc nền và hiệu ứng procedural
│   ├── Bootstrap/      # composition root, dựng phòng lab
│   ├── Chemistry/      # chất, nguyên tố, phản ứng và simulator
│   ├── Core/           # theme và accessibility settings
│   ├── Diagnostics/    # bảng trạng thái F3
│   ├── Player/         # camera, di chuyển và tương tác
│   └── UI/             # HUD và menu pause
├── Editor/BuildPipeline/ # scene generation, validation và Windows build
├── Resources/          # material runtime
└── Scenes/             # scene vào game
```

Runtime và Editor được tách bằng hai assembly definition
`ChemistryLab.Desktop` và `ChemistryLab.Desktop.Editor`. Không còn dùng junction
hoặc chia sẻ source ngầm với Unity project ở root repository.

## Chạy game

Mở `Builds/ChemistryLab3D/ChemistryLab3D.exe`. Giữ nguyên thư mục
`ChemistryLab3D_Data` cạnh file `.exe`.

## Điều khiển

- `WASD`: di chuyển
- Chuột: quan sát
- `Shift`: chạy
- `E`: lấy chất, nạp cốc hoặc đọc ô nguyên tố
- `F`: mở/đóng bảng phân tích
- `[` / `]`: giảm/tăng khối lượng mẫu
- `Q`: cất mẫu đang cầm
- `F3`: mở/đóng bảng chẩn đoán runtime (FPS, camera, vị trí, dữ liệu và âm thanh)
- `F9`: bật/tắt toàn bộ âm thanh
- `F10`: bật/tắt chuyển động giảm
- `Esc`: tạm dừng; menu có nút tiếp tục, âm thanh và thoát game

## Hình ảnh, camera và âm thanh

- Góc nhìn thứ nhất 3D với cánh tay nhà hóa học, camera bob nhẹ khi đi và FOV mở rộng
  khi chạy. `F10` tắt các chuyển động không thiết yếu.
- Phòng thí nghiệm dựng hoàn toàn trong Unity: bàn phản ứng, tủ hút, kho hóa chất,
  bồn rửa, bàn phân tích, bảng tuần hoàn và thiết bị an toàn.
- Nhạc nền ambient, tiếng thông gió, bước chân, nút bấm, lấy/rót mẫu, rửa cốc và
  bốn nhóm âm phản ứng được tổng hợp bằng C# khi chạy. Không dùng tệp âm thanh có
  bản quyền hoặc cần kết nối mạng.

## Nội dung mô phỏng

- 52 nguyên tố thường gặp trong chương trình THPT
- 40 chất: axit, bazơ, muối, kim loại, oxit và peoxit
- 38 phản ứng định lượng: trung hòa, kết tủa, sinh khí, phản ứng thế,
  phản ứng tỏa nhiệt và xúc tác
- Khóa an toàn các phản ứng sinh khí độc ngoài tủ hút

Số liệu được dùng cho mô phỏng giáo dục. Không dùng game thay cho quy trình
an toàn hoặc tài liệu phòng thí nghiệm thực tế.

## Kiểm thử dành cho nhà phát triển

Menu Unity `Chemistry Lab/Desktop/Create Native Scene` tạo lại scene. Pipeline build
chạy validation toàn bộ 38 phản ứng theo cả hai thứ tự nạp, quy tắc tủ hút, bốn nhóm
hiệu ứng và tín hiệu âm thanh procedural. Bản build cũng hỗ trợ:

- `-smokeTest -reportPath <path.json>`: kiểm tra database, phản ứng nhiệm vụ,
  UI/camera, 14 clip runtime và ghi báo cáo JSON
- `-captureTest -captureView pause`: chụp menu pause
- `-captureTest -captureView debug`: chụp bảng diagnostics
- `-captureTest -captureView periodic`: chụp bảng tuần hoàn

Mỗi lần chạy validation/build qua `DesktopLabBuild`, kết quả quan trọng được ghi
thành JSON trong `NativeChemistryLab/BuildReports/`. Raw `*.log` chỉ là dữ liệu
tạm và không được commit. Lịch sử đã chuẩn hóa nằm tại
`docs/logs/validation-history.json` và `docs/logs/validation-history.md`.

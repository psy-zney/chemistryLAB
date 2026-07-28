# Engine phản ứng tự do và an toàn phòng thí nghiệm

## Mục tiêu

Engine cho phép người chơi phối hợp tự do các hóa chất trong catalogue thay vì chỉ nhận 38 phản ứng mẫu. Phản ứng mẫu vẫn có độ ưu tiên cao nhất vì chứa quan sát, màu, hiệu suất và hướng dẫn xử lý đã được biên soạn. Nếu không có mẫu khớp, `DynamicReactionEngine` suy diễn phản ứng theo ion, điện tích, dãy hoạt động kim loại và quy tắc độ tan.

“Tự do” không có nghĩa mọi cặp chất đều phản ứng. Hỗn hợp không có động lực phản ứng ở điều kiện đang mô phỏng sẽ được giữ nguyên và báo `NoMatch`. Engine cũng không tuyên bố mô phỏng mọi điều kiện thực nghiệm ngoài đời; nhiệt độ, nồng độ, áp suất, xúc tác và động học sẽ được mở rộng thành các lớp riêng.

## Phạm vi hiện tại

- 40/40 hóa chất trong catalogue desktop có mô tả loài phản ứng.
- 38 phản ứng mẫu được ưu tiên.
- 9 họ luật động.
- 155 cặp chất có thể được suy diễn từ 780 cặp hai chất trong catalogue.
- Phương trình được cân bằng theo điện tích và số đơn vị ion trong phạm vi phản ứng phổ thông.
- Sản lượng lý thuyết và sản lượng ước tính được tính theo chất giới hạn.

Các họ luật:

1. Axit + bazơ → muối + nước.
2. Axit + cacbonat/hiđrocacbonat → muối + CO₂ + nước.
3. Axit + sunfua → muối + H₂S.
4. Muối amoni + bazơ → muối + NH₃ + nước.
5. Muối + muối → kết tủa nếu sản phẩm vi phạm quy tắc độ tan.
6. Kim loại mạnh + muối kim loại yếu → muối mới + kim loại.
7. Kim loại đứng trước H + axit không oxi hóa → muối + H₂.
8. Oxit bazơ + axit → muối + nước.

## Luồng giải phản ứng

1. Gom khối lượng từng hóa chất trong cốc.
2. Tìm phản ứng mẫu không phụ thuộc thứ tự nạp.
3. Nếu không có mẫu, thử từng cặp chất qua engine luật động.
4. Cân bằng hệ số, xác định sản phẩm chính và tính chất giới hạn.
5. Gắn hồ sơ khí/hơi nguy hiểm nếu sản phẩm bay hơi.
6. Cho phản ứng xảy ra kể cả khi thao tác sai vị trí.
7. Hệ an toàn tính mức phơi nhiễm và hậu quả gameplay.

## Mô hình nguy hiểm

`AirborneHazardCatalog` hiện có hồ sơ cho CO₂, H₂, O₂, NH₃, H₂S, Cl₂, NO₂ và SO₂. Mỗi hồ sơ phân biệt độc tính, ăn mòn, ngạt, cháy và oxi hóa; mặt nạ không được tính là biện pháp bảo vệ cho nguy cơ cháy hoặc làm giàu oxy.

Chất rắn và dung dịch độc/ăn mòn cũng bật cảnh báo ngay khi người chơi lấy chai. Mức cảnh báo được suy ra từ trường `Hazards` và luôn hiển thị cùng hướng dẫn `Handling`.

Phản ứng nguy hiểm không còn bị khóa ngoài tủ hút:

- Ngoài tủ hút: nhân vật nhận phần lớn liều mô phỏng.
- Trong tủ hút: giảm 90% lượng phát tán.
- Tủ hút + bình cách ly: giảm 99,5% lượng phát tán.
- Mặt nạ: giảm thêm theo loại khí và loại bộ lọc; không thay thế tủ hút.
- Phơi nhiễm làm giảm sức khỏe và trừ tín dụng điều trị/khử nhiễm.
- Khi sức khỏe về 0, nhân vật bị sơ tán khẩn cấp, trả thêm chi phí và quay lại với 35 sức khỏe.

Các giá trị sức khỏe/tín dụng là thang gameplay để dạy quan hệ nguy cơ–biện pháp kiểm soát, không phải hướng dẫn liều y khoa.

## Điều khiển

- `F6`: mua mặt nạ giá 250 tín dụng; sau khi mua dùng để đeo/tháo.
- `F7`: nối/tháo bình cách ly khí. Bình chỉ có hiệu lực tại cốc trong tủ hút.
- Bảng an toàn hiển thị sức khỏe, tín dụng, PPE, bình cách ly và sự cố gần nhất.

## Mở rộng dữ liệu

Muốn thêm một hóa chất, cần thêm `ChemicalDefinition` và một `Species` tương ứng với cation, anion, số proton axit, số nhóm OH hoặc hạng hoạt động kim loại. Muốn thêm một khí độc mới, thêm hồ sơ chuẩn hóa công thức vào `AirborneHazardCatalog`. Các phản ứng có cơ chế đặc biệt, oxi hóa–khử nhiều bước hoặc quan sát phụ thuộc điều kiện nên được thêm dưới dạng phản ứng mẫu trước.

## Xác thực

Unity batch validation kiểm tra:

- dữ liệu 40 chất, 38 phản ứng mẫu và 52 nguyên tố;
- bốn ví dụ đại diện của engine động;
- toàn ma trận 780 cặp chất, yêu cầu ít nhất 100 cặp hợp lệ;
- phản ứng H₂S ngoài tủ hút vẫn xảy ra nhưng bị gắn vi phạm và độc tính Critical;
- hậu quả không bảo hộ lớn hơn rõ rệt so với tủ hút + bình cách ly;
- cảnh báo chất độc khi lấy chì(II) nitrat;
- năm nhóm tín hiệu âm thanh, gồm còi cảnh báo nguy hiểm.

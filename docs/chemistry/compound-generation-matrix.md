# Ma trận thành phần anion × cation 2D

[Mở bảng tương tác](compound-matrix-2d.html) · [Hợp đồng JSON](compound-generation-matrix.json) · [Dữ liệu xuất](compound-matrix-2d.json)

Mỗi hàng là `anionId`, mỗi cột là `cationId`. Chọn một ô không điều chế hóa chất. Công thức trung hòa điện không chứng minh chất tồn tại bền hoặc phản ứng khả thi.

## Nguồn và định danh

Nguồn duy nhất: `Assets/ChemistryLab/Resources/Chemistry/compound-generation-matrix.json`. `CompoundGenerationMatrix` và `CompoundMatrix2D` đọc cùng tài liệu. `scripts/export-matrix.mjs` xuất bảng Pages cùng SHA-256 dữ liệu nguồn.

```text
Ion ID + công thức + điện tích
       ↓
Hàng anion × cột cation
       ↓
GCD → tỉ lệ tối giản → công thức hình thức
       ↓
Ngoại lệ → ghi đè tính chất → bối cảnh và bằng chứng

Thành phần bình + điều kiện (luồng riêng)
       ↓
Phản ứng mẫu → redox → luật động có giới hạn
       ↓
Kiểm tra điều kiện → kết quả trong game
```

API: `TryGetCell(anionId, cationId, out cell)`. `Cells` duyệt theo hàng anion rồi cột cation. Khóa cũ vẫn là `cationId|anionId`; dùng `ToLegacyCoordinate(cationId, anionId)` để chuyển đổi. Không lưu hóa chất bằng vị trí hàng/cột. `iron-two` và `iron-three` là hai ID riêng.

| Thành phần | Số lượng |
| --- | ---: |
| Ion | 46: 21 anion, 25 cation |
| Ô 2D | 525 |
| Bị loại | 8 |
| Có bằng chứng theo phạm vi hẹp | 4 |
| Thành phần hình thức | 513 |
| Ghi đè tính chất cũ, gồm oxit | 45 |
| Tọa độ oxit riêng được generator cũ chấp nhận | 48 |
| Tổng tọa độ cũ / công thức khác nhau | 565 / 541 |

Đây là độ phủ dữ liệu, không chứng nhận khoa học cho toàn bộ chất. Danh mục `oxide:element:oxidationState` nằm riêng; không tạo cation giả trong nước cho oxit phi kim. Bảng 2D cũng không giả định mọi ion, đặc biệt oxide, tồn tại tự do trong nước.

## Tỉ lệ và ví dụ khớp code

Với `qc > 0`, `qa < 0`, `g = gcd(qc, abs(qa))`:

```text
cationCount = abs(qa) / g
anionCount = qc / g
cationCount * qc + anionCount * qa = 0
```

| Hàng anion | Cột cation | Cation : anion | Công thức |
| --- | --- | --- | --- |
| sulfate (−2) | sodium (+1) | 2 : 1 | Na2SO4 |
| phosphate (−3) | calcium (+2) | 3 : 2 | Ca3(PO4)2 |
| hydroxide (−1) | copper-two (+2) | 1 : 2 | Cu(OH)2 |
| acetate (−1) | hydrogen (+1) | 1 : 1 | CH3COOH |

Ion đa nguyên tử lặp lại cần ngoặc. Ghi đè CH3COOH giữ cách viết thông dụng nhưng không thay số nguyên tử; không có nghĩa axit yếu phân li hoàn toàn. C# dùng Unicode chỉ số dưới; bản xuất dùng ASCII. `AtomCounts` được suy ra từ công thức và kiểm tra thành phần qua ghi đè.

## Trạng thái, điều kiện và bằng chứng

| Trường | Ý nghĩa |
| --- | --- |
| formalComposition | Tỉ lệ trung hòa điện; chưa xác lập tính bền hoặc phản ứng |
| literatureSupported | Chỉ khẳng định đúng nội dung notes trong phạm vi nguồn |
| excluded | Có lý do loại; formula=null, vẫn giữ tỉ lệ hình thức |
| unsupported | Phạm vi chưa mô hình hóa; chưa có ô hiện tại |
| propertyReviewed | Ghi đè tính chất nội bộ cũ, không phải chứng nhận độc lập |
| heuristic | Phân loại bằng luật cũ, không phải phép đo |

`conditionIds` dẫn tới bối cảnh của khẳng định, **không phải predicate được engine thực thi**. `ReactionConditionEngine` kiểm tra riêng những điều kiện đã triển khai. `evidenceIds` dẫn tới nguồn có tiêu đề, URL, phạm vi và ngày truy cập. Không bịa Ksp hoặc ngưỡng thực nghiệm. `AuthorizesReaction` luôn false trên ô ma trận. Điểm `.98/.72` của API cũ là hằng số phần mềm, không phải xác suất khoa học.

Bốn ô có nguồn: sodium|chloride, barium|sulfate, hydrogen|hydroxide, hydrogen|acetate. Đọc notes và scope trước khi sử dụng. Nguồn cho một ví dụ không chứng nhận những ô còn lại.

Ngoại lệ ưu tiên hơn ghi đè: ammonium|hydroxide không tạo chai NH4OH tinh khiết; silver|hydroxide không đại diện hydroxide độc lập bền. Ngoại lệ ngăn tạo công thức tại ô đó, không tự sinh phương trình chuyển hóa.

## Kiểm tra và mở rộng

```powershell
node scripts/export-matrix.mjs
node scripts/validate-project.mjs
node scripts/export-matrix.mjs --check
```

Unity: chạy `ChemistryLab.Desktop.Editor.DesktopLabBuild.ValidateOnly`. Đối chiếu C# với Pages bằng `ChemistryLab.Desktop.Editor.MatrixParityExport.Export`, rồi:

```powershell
node scripts/validate-project.mjs --unity-parity Logs/matrix-unity-parity.json
```

Kiểm tra ID duy nhất, dấu điện tích, tham chiếu, tỉ lệ tối giản trung hòa, số nguyên tử qua ghi đè, ngoại lệ và tương thích generator. Actions kiểm tra tĩnh trước Pages; không thay thế kiểm thử Unity hoặc chứng minh tính khả thi của mọi phản ứng.

Mở rộng bằng ion có ID ổn định và nguồn rõ; thêm ghi đè/ngoại lệ theo bằng chứng, thêm **luật phản ứng riêng** khi cần chuyển hóa. Không tăng mức bằng chứng chỉ để tăng độ phủ.

## Giới hạn và nguồn

Chưa giải đầy đủ nhiệt động, hoạt độ, phức chất, hydrate, cơ chế hữu cơ hoặc kết tủa định lượng theo Ksp. Quy tắc độ tan/màu/nguy hại cũ được giữ để tương thích, chưa rà nguồn toàn bộ. Oxit như P2O5 có thể là công thức thực nghiệm thay vì công thức phân tử đầy đủ.

- [OpenStax: Ionic and Molecular Compounds](https://openstax.org/books/chemistry-2e/pages/2-6-ionic-and-molecular-compounds): điện tích, ion đa nguyên tử, thành phần hợp chất.
- [OpenStax: Classifying Chemical Reactions](https://openstax.org/books/chemistry-2e/pages/4-2-classifying-chemical-reactions): ví dụ ion, axit–bazơ và kết tủa trong phạm vi của nguồn.

Trạng thái kiểm thử lấy từ báo cáo tương ứng mã hiện tại; báo cáo cũ không chứng nhận bản thay đổi mới.

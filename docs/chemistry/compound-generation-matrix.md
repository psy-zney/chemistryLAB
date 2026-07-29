# Ma trận sinh hợp chất X/Y/Z

## Kết quả triển khai

Ý tưởng ma trận ba chiều đã được triển khai thành một mô hình dữ liệu có kiểm
định, không phải một mảng số ba chiều cố định:

- trục X biểu diễn kim loại/cation, điện tích và hạng hoạt động;
- trục Y biểu diễn phi kim, anion đơn nguyên tử hoặc họ gốc axit;
- trục Z biểu diễn số nguyên tử oxi và số oxi hóa liên quan;
- tầng cân bằng điện tích quyết định chỉ số trong công thức;
- tầng thuộc tính ước lượng pha, độ tan, màu/ngoại quan và cờ nguy hại;
- tầng kiểm định áp dụng dữ liệu đã duyệt và loại các tổ hợp không bền.

Phiên bản `1.0` hiện chứa:

| Thành phần | Số lượng |
| --- | ---: |
| Nguyên tố trong ma trận | 27 |
| Ion dùng lại được | 46 |
| Cation | 25 |
| Anion | 21 |
| Tọa độ hợp chất được chấp nhận | 565 |
| Công thức duy nhất | 541 |
| Ghi đè đã duyệt | 45 |
| Tổ hợp bị loại rõ lý do | 9 |

Dữ liệu nguồn nằm trong
`Assets/ChemistryLab/Resources/Chemistry/compound-generation-matrix.json`.
Mã giải nằm trong
`NativeChemistryLab/Assets/ChemistryLab/Runtime/Chemistry/CompoundGenerationMatrix.cs`.

## Vì sao không dùng mảng `matrix[x,y,z]` đơn giản

Một tọa độ kim loại–phi kim–oxi chưa đủ xác định duy nhất một chất. Ví dụ sắt có
Fe(II), Fe(III); lưu huỳnh có S(IV), S(VI); nitơ có nhiều số oxi hóa và một số
oxit có công thức phân tử khác công thức thực nghiệm. Vì vậy tọa độ được biểu
diễn bằng đối tượng giàu thông tin:

```text
Element
  -> allowed oxidation states
  -> IonDefinition
  -> charge-balanced coordinate
  -> GeneratedCompoundDefinition
  -> property estimate
  -> reviewed override / exclusion
```

Mô hình này vẫn giữ đúng trực giác X/Y/Z, nhưng không làm mất điện tích, trạng
thái oxi hóa hoặc họ ion đa nguyên tử.

## Thuật toán sinh công thức

Với cation điện tích `+m` và anion điện tích `-n`:

1. Tính `gcd(m, n)`.
2. Chỉ số cation là `n / gcd`.
3. Chỉ số anion là `m / gcd`.
4. Thêm ngoặc nếu ion đa nguyên tử có chỉ số lớn hơn 1.
5. Tính khối lượng mol từ số đơn vị ion.
6. Áp dụng công thức chuẩn đã duyệt nếu cách viết thông thường khác cách ghép
   ion, ví dụ `CH3COOH`.

Ví dụ:

```text
Ca2+ + PO4(3-) -> Ca3(PO4)2
Al3+ + O2-     -> Al2O3
Fe3+ + OH-     -> Fe(OH)3
```

Oxit được sinh riêng từ nguyên tố và số oxi hóa:

```text
C(+4)  + O(-2) -> CO2
S(+6)  + O(-2) -> SO3
Fe(+3) + O(-2) -> Fe2O3
```

## Tầng thuộc tính vật lý

Engine chỉ sinh mức phân loại có cơ sở, không bịa số đo chính xác:

- `Soluble`, `SlightlySoluble`, `Insoluble`, `ReactsWithWater`, `Unknown`;
- pha rắn, lỏng, dung dịch hoặc khí;
- màu ưu tiên theo dữ liệu kết tủa đã duyệt, sau đó mới dùng màu ion;
- mô tả ngoại quan nêu rõ khi là giá trị ước lượng;
- khối lượng mol được tính trực tiếp từ thành phần.

Quy tắc độ tan bao phủ muối kim loại kiềm/amoni, nitrat, axetat, halogenua,
sunfat, hiđroxit, cacbonat, photphat, sunfua, silicat, cromat và đicromat.
Ngoại lệ quan trọng như `AgCl`, `AgBr`, `AgI`, `BaSO4`, `PbI2`,
`Cu(OH)2`, `Fe(OH)2` và `Fe(OH)3` có màu cùng độ tan đã duyệt riêng.

## Tầng nguy hại và độ tin cậy

Mỗi hợp chất có thể mang nhiều cờ:

```text
Corrosive
Toxic
Oxidizer
EnvironmentalHazard
WaterReactive
GasReleasePotential
HeavyMetal
Carcinogenic
```

Nguy hại được hợp từ cation, anion, họ hợp chất và ngoại lệ đã duyệt. Ví dụ:

- muối Pb, Hg, Cd giữ cờ độc/kim loại nặng;
- permanganat, cromat, đicromat và clorat giữ cờ oxi hóa;
- axit và bazơ tan mạnh giữ cờ ăn mòn;
- sunfua, cacbonat, nitrit và hipoclorit có tiềm năng giải phóng khí khi gặp
  môi trường thích hợp.

Hai mức kết quả được phép đi vào gameplay:

- `Reviewed`: công thức/tính chất chính đã được ghi đè và kiểm tra;
- `RuleDerived`: đúng theo cân bằng điện tích cùng luật phổ thông nhưng vẫn được
  HUD ghi rõ là suy diễn.

`Rejected` không được đưa vào danh sách hợp chất sinh. Ví dụ `AgOH` bị loại vì
chuyển thành oxit bạc; `NH4OH` được biểu diễn bằng cân bằng amoniac trong nước,
không coi là một chai hợp chất tinh khiết.

## Tích hợp vào engine phản ứng

Thứ tự giải vẫn bảo toàn độ tin cậy:

```text
38 phản ứng mẫu đã duyệt
  -> 8 luật oxi hóa–khử đã cân bằng electron
      -> 9 họ luật DynamicReactionEngine
      -> CompoundGenerationMatrix tạo/kiểm định sản phẩm
          -> tầng điều kiện nhiệt độ, nồng độ, pH, xúc tác
              -> tính chất, màu, độ tan, cờ nguy hại
              -> HUD, VFX, hướng dẫn thải bỏ và LabSafetySystem
```

`DynamicReactionEngine.MakeFormula` lấy công thức, hệ số ion, khối lượng mol,
màu và cờ nguy hại từ ma trận. Phản ứng kết tủa dùng độ tan/màu của ma trận.
`ReactionOutcome` chuyển mức tin cậy và cơ sở ước lượng cho HUD. F3 diagnostics
hiển thị tổng số hợp chất sinh và số bản ghi đã duyệt.

## Kiểm định tự động

Unity batch validation kiểm tra:

- JSON tải và phân tích được;
- không trùng symbol nguyên tố hoặc id ion;
- nguyên tử khối khớp bảng tuần hoàn trong game;
- ion có điện tích khác 0 và khối lượng mol dương;
- các ca chuẩn `Na2SO4`, `Ca3(PO4)2`, `Cu(OH)2`, `CH3COOH`, `Al2O3`,
  `SO3`;
- tổng độ phủ tối thiểu 450 hợp chất và 20 ngoại lệ đã duyệt;
- 155 cặp phản ứng động hiện có vẫn giải được;
- 7 profile điều kiện và 8 luật oxi hóa–khử cân bằng electron;
- phản ứng Cu/H₂SO₄ bị chặn khi lạnh nhưng chạy khi axit đặc, nóng;
- nhánh HNO₃ loãng/đặc lần lượt tạo NO/NO₂;
- kho sản phẩm trừ đúng khối lượng khi dùng lại;
- 38 phản ứng mẫu, an toàn tủ hút, âm thanh và bốn lớp hiệu ứng vẫn đạt.

Kết quả mới nhất: Unity `6000.5.3f1`, `0` cảnh báo, `0` lỗi.

## Giới hạn có chủ ý

541 công thức không đồng nghĩa 541 chất đều bền, tinh khiết hoặc điều chế được
trong mọi điều kiện. Phiên bản này đã mô hình hóa xu hướng nhiệt độ, nồng độ,
pH, xúc tác và động học, nhưng chưa tự suy ra đầy đủ:

- năng lượng hoạt hóa thực nghiệm và cân bằng nhiệt động;
- áp suất, hệ số hoạt độ và bản chất dung môi;
- profile điều kiện cứng ngoài 7 trường hợp đã duyệt;
- phức chất phối trí, dạng hydrat và cấu trúc tinh thể;
- oxi hóa–khử ngoài 8 luật bán phản ứng đã duyệt;
- cơ chế hữu cơ;
- công thức phân tử đầy đủ khi công thức thực nghiệm chưa đủ, ví dụ `P4O10`.

Các phần đó nên được thêm dưới dạng các tầng điều kiện và bộ kiểm định mới,
không nhồi trực tiếp vào ba trục ban đầu.

## Cách mở rộng an toàn

1. Thêm nguyên tố/ion vào JSON và khai báo đúng điện tích, khối lượng mol, số
   oxi cùng cờ nguy hại.
2. Thêm override cho màu, độ tan hoặc cách viết công thức có bằng chứng.
3. Thêm exclusion nếu công thức cân bằng nhưng chất không bền hoặc không nên
   xuất hiện như hóa chất độc lập.
4. Thêm luật phản ứng riêng nếu cơ chế phụ thuộc điều kiện.
5. Chạy `DesktopLabBuild.ValidateOnly`.
6. Chỉ nâng `RuleDerived` lên `Reviewed` sau khi dữ liệu đã được rà soát.

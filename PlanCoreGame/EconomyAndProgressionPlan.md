# Chemistry LAB - Kế Hoạch Vận Hành, Economy và Progression

## 1. Mục tiêu vận hành

Chemistry LAB là game mô phỏng phòng thí nghiệm 2D có nhiệm vụ, không phải game clicker hay cửa hàng hóa chất thuần túy. Người chơi nhận yêu cầu từ NPC, lấy chất từ tủ, chọn đúng dụng cụ, thực hiện phản ứng bằng thao tác kéo/đổ, quan sát hiện tượng, thu hồi sản phẩm và làm sạch bàn thí nghiệm.

Economy phục vụ bốn mục tiêu:

1. Người chơi luôn có đủ nguyên liệu cơ bản để tiếp tục học và thử nghiệm.
2. Tiền `$` là phần thưởng cho kỹ năng và giải quest; không được dùng để thay thế kiến thức.
3. Kim cương `💎` tăng lựa chọn, tốc độ sưu tập hoặc mua chất hiếm, nhưng không khóa tuyến tiến trình chính.
4. Sai thao tác có chi phí nhỏ và tạo bài học, nhưng không bao giờ làm mất khả năng chơi tiếp.

### Nguyên tắc thiết kế

- Mọi phản ứng trong game phải hiển thị hiện tượng, phương trình và điều kiện ở mức học thuật phù hợp; game không thay thế hướng dẫn thực hành ngoài đời.
- Công thức mới được mở bằng Level/Lab Upgrade và quest khám phá, không mở toàn bộ ở đầu game.
- Một nguyên liệu đã từng mở khóa luôn có ít nhất một đường mua lại bằng `$` hoặc nguồn thay thế miễn phí/rẻ.
- Vật phẩm premium không được là yêu cầu bắt buộc duy nhất của quest chính.
- Nâng cấp phải tạo năng lực mới có thể nhìn thấy: thêm slot kho, thêm bàn, thêm máy, thêm nhóm phản ứng, không chỉ tăng chỉ số vô hình.

## 2. Vòng lặp chơi cốt lõi

```text
Nhận quest NPC
  -> Đọc mục tiêu, điều kiện và gợi ý phương trình
  -> Lấy hóa chất + dụng cụ từ tủ
  -> Thao tác phản ứng tại bàn lab
  -> Quan sát / ghi nhận hiện tượng / thu sản phẩm
  -> Rửa dụng cụ, cất phần dư
  -> Nộp quest, nhận $, EXP, recipe hoặc vật phẩm
  -> Nâng Lab / mở nguyên liệu và quest tiếp theo
```

Một phiên chơi ngắn nên hoàn thành trong 3-8 phút: một quest, một phản ứng tự do hoặc một lượt quản lý kho. Các quest dài hơn được chia checkpoint để người chơi di động có thể dừng mà không mất tiến độ.

## 3. Tài nguyên và luồng tiền

| Tài nguyên | Vai trò | Nguồn chính | Sink chính | Quy tắc bảo vệ người chơi |
| --- | --- | --- | --- | --- |
| `$` | Tiền vận hành chính | Quest, daily login, bán sản phẩm dư, thưởng thành tựu, quảng cáo tự nguyện | Mua nguyên liệu phổ thông, dụng cụ, vệ sinh, nâng Lab | Luôn có quest hoặc nguồn mua lại nguyên liệu cơ bản bằng `$` |
| `💎` | Premium giới hạn | Nạp, daily loop, quest mốc cao, thành tựu hiếm | Chất hiếm, gói mẫu chuẩn, skin/UI tiện ích | Không dùng làm yêu cầu duy nhất cho main quest |
| `EXP` | Mở Level | Nộp quest, lần đầu hoàn thành reaction, notebook discovery | Không tiêu trực tiếp | Không mất khi thất bại phản ứng |
| Hóa chất | Input/output học thuật | Shop, nguồn miễn phí, reward, điều chế | Phản ứng, sample quest, xử lý chất thải | Shop tự cập nhật sau unlock; có số lượng dự phòng |
| Dụng cụ/máy | Khả năng thực hiện phản ứng | Starter, daily, Lab Upgrade, mua `$` | Phí mở khóa/nâng cấp, bảo trì nhẹ | Dụng cụ bắt buộc có đường nhận miễn phí qua quest chính |
| Sức chứa kho | Giới hạn quản lý | Lab Upgrade | Chi phí nâng Lab | Không làm mất item; kho đầy chỉ chặn nhận thêm và cho phép bán/cất xử lý |

### Quy tắc tiền tệ

- `$` không đổi sang `💎`, và `💎` không đổi trực tiếp thành EXP.
- `💎` được xem là premium chủ yếu từ nạp. Daily và quest cao cấp cấp lượng `💎` nhỏ, cố định, để người chơi free có thể chạm vào chất hiếm ở mức sưu tập.
- Không bán chance box có hóa chất cần cho học tập. Nếu có gacha cosmetic sau này, nó phải tách khỏi reaction/progression.
- Không bán năng lượng/lượt chơi. Giới hạn của game là kho, dụng cụ, điều kiện reaction và mục tiêu quest, không phải stamina.

## 4. Starter Economy

### 4.1 Gói khởi tạo

Người chơi bắt đầu ở `Level 1`, `LabUpgradeLevel 1` với:

| Hạng mục | Cấp | Lý do |
| --- | ---: | --- |
| Tiền vận hành | `500 $` | Đủ thực hiện tutorial và mua lại vài nguyên liệu cơ bản |
| Kim cương | `100 💎` | Cho phép xem shop hiếm và mua một sample tùy chọn, không bắt buộc chi |
| Không khí | N2, O2, CO2 không giới hạn cho mục đích mô phỏng | Nguồn nền miễn phí |
| Nước biển/khoáng | H2O 1,000 g, NaCl 300 g, SiO2 200 g, Al2O3 100 g | Nhóm nguyên liệu dồi dào, dễ học |
| Dung dịch nền | HCl 100 g, H2SO4 100 g, NH3 100 g, H2O2 100 g, KMnO4 50 g | Đủ cho chuỗi quest đầu mà không cần mua ngay |
| Hữu cơ thân thuộc | Chanh 3 phần, giấm 200 g, C2H5OH 100 g | Dùng cho quest nhận biết và ester sau này |
| Dụng cụ | 1 Beaker Pyrex 100 ml, 2 ống nghiệm, 1 đèn cồn | Tối thiểu để thao tác pha, quan sát và gia nhiệt mô phỏng |

### 4.2 Tutorial và mức chi tiêu an toàn

Tutorial đầu tiên tiêu hao `50 g H2O + 10 g NaCl`, trả `150 $ + 50 EXP`. Sau tutorial, người chơi có tối thiểu `650 $` nếu không mua gì. Đây là ngưỡng an toàn để mua lại nguyên liệu phổ thông khi thao tác sai.

Shop Lv1 phải luôn có các gói bán lại sau:

| Gói | Giá đề xuất | Ghi chú |
| --- | ---: | --- |
| H2O 250 g | 10 $ | Dùng làm dung môi, không tạo áp lực chi tiêu |
| NaCl 50 g | 15 $ | Nguyên liệu tutorial và điện phân dung dịch sau này |
| HCl / H2SO4 / NH3 / H2O2 50 g | 30 $ mỗi loại | Chất nền, bán số lượng giới hạn theo kho nhưng không khóa vĩnh viễn |
| KMnO4 20 g | 45 $ | Đắt hơn vì có giá trị quan sát/quest |
| Giấm / C2H5OH 50 g | 25 $ mỗi loại | Quest hữu cơ cơ bản |
| Phí rửa dụng cụ thường | 0 $ | Không phạt việc tuân thủ loop vệ sinh |

Nếu người chơi còn dưới `75 $` và không còn nguyên liệu cần thiết cho main quest đang active, hệ thống cấp một `Recovery Grant` tự động: nguyên liệu đầu vào tối thiểu của quest và `100 $`. Mỗi quest chỉ nhận một lần; không hiển thị như quảng cáo hay gói mua.

## 5. Daily Login Rewards - vòng 7 ngày

Login được ghi nhận một lần theo ngày lịch địa phương. Bỏ lỡ một ngày đặt chuỗi quay lại Ngày 1; người chơi có thể claim các ngày còn thiếu bằng `💎` trong tương lai, nhưng tính năng này không nằm trong MVP.

| Ngày | Phần thưởng | Mục đích |
| --- | --- | --- |
| 1 | `100 $` + `50 g H2O2` | Trả lại người chơi vào loop O2/reactive starter |
| 2 | `150 $` + `20 💎` | Tạo cảm giác tiến triển premium nhẹ |
| 3 | `200 $` + 1 Beaker `250 ml` | Cho phép batch lớn hơn, không chỉ tăng chỉ số |
| 4 | `250 $` + `30 g KMnO4` tinh khiết | Mở rộng quest oxi hóa/nhiệt phân mô phỏng |
| 5 | `300 $` + `30 💎` | Mốc giữa chu kỳ |
| 6 | `400 $` + 1 máy cô cạn/điện phân dung dịch mini | Mở hướng lab device |
| 7 | `500 $` + `50 💎` + 1 mẫu Pt hoặc kim loại hiếm | Phần thưởng sưu tập và gợi mở late game |

Sau Ngày 7, chuỗi lặp lại với cùng cấu trúc. Ở mỗi vòng thứ ba, vật phẩm Ngày 7 đổi từ Pt thành lựa chọn một trong ba: Pt sample, Au sample, hoặc `75 💎`; lựa chọn giúp tránh tích vô hạn một chất hiếm không cần thiết.

## 6. Level, EXP và Lab Upgrade

### 6.1 Quy tắc EXP

Người chơi bắt đầu tại `Level 1` với `0 EXP`. Chi phí tăng từ Level `L` lên `L + 1` là `L x 100 EXP`. EXP không bị trừ khi nâng level.

| Tăng cấp | EXP cần cho cấp đó | EXP tích lũy để đạt cấp mới | Trọng tâm mở khóa |
| --- | ---: | ---: | --- |
| Lv1 -> Lv2 | 100 | 100 | Notebook, shop starter, kho 12 slot |
| Lv2 -> Lv3 | 200 | 300 | Beaker 250 ml, recipe cơ bản thứ hai |
| Lv3 -> Lv4 | 300 | 600 | Bàn phụ, xử lý/thu hồi sản phẩm đơn giản |
| Lv4 -> Lv5 | 400 | 1,000 | Điện phân dung dịch, nhóm Cl2/H2/O2 mô phỏng |
| Lv5 -> Lv6 | 500 | 1,500 | Điện phân nóng chảy, kim loại/ion nâng cao |
| Lv6 -> Lv7 | 600 | 2,100 | Phức chất, ester, kiểm soát độ tinh khiết |
| Lv7 -> Lv8 | 700 | 2,800 | Chuỗi NH3/NOx, slot máy Ostwald |
| Lv8 -> Lv9 | 800 | 3,600 | Ostwald vận hành, HNO3 purity quest |
| Lv9 -> Lv10 | 900 | 4,500 | Lab capstone, catalogue hiếm và thử thách tổng hợp |

`Level 10` là cap MVP. EXP nhận sau cap được đổi thành `Research Token` để dùng cho cosmetic notebook, quest challenge hoặc nội dung cập nhật sau này; không tạo cấp vô hạn.

### 6.2 LabUpgradeLevel

Lab Upgrade yêu cầu đạt Level tương ứng, trả bằng `$` và hoàn thành quest chứng nhận. Không dùng `💎` để bỏ qua tiến trình cốt lõi.

| Lab Lv | Điều kiện | Chi phí đề xuất | Mở khóa vận hành |
| --- | --- | ---: | --- |
| 1 | Starter | 0 $ | 12 slot kho, 1 bàn, Beaker 100 ml, ống nghiệm, đèn cồn |
| 2 | Player Lv2 + tutorial hoàn tất | 300 $ | 18 slot kho, Beaker 250 ml, thêm 1 khay thao tác |
| 3 | Player Lv3 + quest thu hồi | 600 $ | 24 slot kho, bàn phụ, tủ mẫu sản phẩm |
| 4 | Player Lv4 + quest O2 | 1,000 $ | Máy điện phân dung dịch mini, recipe khí cơ bản |
| 5 | Player Lv5 + quest điện cực | 1,500 $ | 32 slot kho, máy cô cạn, lọ thu khí |
| 6 | Player Lv6 + quest ion | 2,200 $ | Điện phân nóng chảy mô phỏng, nhóm kim loại/halogen mở rộng |
| 7 | Player Lv7 + quest phức chất | 3,000 $ | 40 slot kho, máy phản ứng kiểm soát nhiệt độ |
| 8 | Player Lv8 + quest chuỗi NH3 | 4,000 $ | Khung tháp Ostwald, catalog NOx/HNO3 preview |
| 9 | Player Lv9 + Pt sample hoặc Pt thuê | 5,200 $ | Tháp Ostwald hoạt động, quest HNO3 purity |
| 10 | Player Lv10 + capstone | 7,000 $ | 48 slot kho, showcase lab, contract hiếm và sandbox nâng cao |

`Pt thuê` là đường free-to-play cho Lab Lv9: người chơi trả `1,000 $` đặt cọc trong một quest để dùng catalyst mô phỏng. Pt sở hữu bằng `💎` chỉ là lựa chọn sưu tập/tiện lợi, không khóa Ostwald.

## 7. Chemical Progression List

### 7.1 Nhóm cơ bản: tự do hoặc rẻ

| Nhóm | Chất | Mở khóa | Vai trò |
| --- | --- | --- | --- |
| Không khí/nước | H2O, N2, O2, CO2 | Starter | Dung môi, khí nền, quan sát cơ bản |
| Muối/khoáng | NaCl, SiO2, Al2O3 | Starter | Hòa tan, khoáng, material route |
| Acid/base phổ thông | HCl, H2SO4, NaOH, NH3 | Starter hoặc quest Lv2 | Acid-base, muối, nhận biết ion |
| Oxidizer/reductant học tập | H2O2, KMnO4 | Starter | Nhiệt phân/oxi hóa mô phỏng |
| Hữu cơ phổ thông | Chanh, giấm, C2H5OH | Starter | Acid hữu cơ, ester route |

### 7.2 Nhóm tự điều chế

| Sản phẩm | Mốc Lab/Level | Nguồn gameplay | Giá trị |
| --- | --- | --- | --- |
| O2, H2, Cl2 | Lab Lv4 | Quest khí và điện phân mô phỏng | Khí reaction, hợp đồng NPC |
| CuSO4, FeSO4 | Lv4-Lv5 | Reaction/ion chain | Dung dịch màu, nhận biết, quest sample |
| Al(OH)3 | Lv5 | Acid-base/precipitate chain | Hiện tượng kết tủa và material route |
| [Cu(NH3)4]2+ | Lv6-Lv7 | Quest phức chất | Discovery notebook và advanced contract |
| Ester mùi hoa quả | Lv6 | Hữu cơ route | Cosmetic scent badge và quest ứng dụng |
| HNO3 đạt purity mục tiêu | Lab Lv9 | Ostwald simulation + purification | Capstone chemistry reward |

Sản phẩm điều chế lần đầu luôn mở notebook entry, cho `EXP` một lần và cho phép shop bán lại nguyên liệu thô của chain. Shop không bán ngay sản phẩm advanced với giá rẻ hơn chi phí điều chế; nếu bán sample để phục hồi, giá sample phải cao hơn 30-50% tổng giá input để khuyến khích tự làm.

### 7.3 Nhóm hiếm/cao cấp

| Chất | Đường sở hữu | Vai trò thiết kế |
| --- | --- | --- |
| Au, Pt | `💎`, daily sample, quest capstone, Pt thuê bằng `$` cho quest | Catalyst/sample sưu tập và mốc late game |
| F2 | `💎` hoặc event research sample sau Level 10 | Không là yêu cầu main quest MVP |
| Na, K tinh khiết | `💎`, contract advanced hoặc event | Sandbox/collection, không dùng để chặn tiến trình |

## 8. Quest Economy

### 8.1 Cấu trúc quest

Mỗi quest có: NPC giao việc, mục tiêu học thuật, input tối thiểu, hiện tượng cần quan sát, điều kiện nộp, phần thưởng, recipe/unlock và đường recovery. Quest không yêu cầu người chơi nhớ chính xác toàn bộ công thức; notebook cung cấp gợi ý tăng dần nhưng phần thưởng discovery cao hơn khi tự hoàn thành.

| Tier | Ví dụ nhiệm vụ | Thưởng bắt buộc | Tần suất |
| --- | --- | --- | --- |
| Tutorial | Rót `50 g H2O` và `10 g NaCl` vào Beaker 100 ml để hòa tan | `150 $`, `50 EXP` | Một lần |
| Trung cấp | Điều chế O2 từ nhiệt phân KMnO4 hoặc H2O2 trong mô phỏng | `300 $`, `100 EXP` | Một lần, có thể replay không EXP |
| Cao cấp | Điều chế HNO3 đạt độ tinh khiết `90%` | `800 $`, `100 💎`, `300 EXP` | Một lần capstone |
| Daily contract | Sản xuất/cất đúng một sample theo recipe đã biết | `80-220 $`, `20-60 EXP` | 3 hợp đồng/ngày |
| Discovery | Hoàn tất reaction mới và ghi hiện tượng đúng | `100-350 $`, `50-150 EXP` | Một lần mỗi reaction |
| Recovery | Cấp lại input tối thiểu nếu main quest bị kẹt | `100 $` + input | Tự động, tối đa 1 lần/quest |

### 8.2 Công thức thưởng

Để data-driven và dễ cân bằng, phần thưởng `$` cho quest thường dùng:

```text
BaseReward = 100 + (Tier x 100)
FinalReward = BaseReward + ComplexityBonus + CleanLabBonus
```

- `ComplexityBonus`: 0-200 `$`, dựa trên số bước/máy/điều kiện trong quest.
- `CleanLabBonus`: 10% nếu người chơi cất sản phẩm và rửa dụng cụ đúng loop; đây là bonus, không trừ phần thưởng gốc.
- Nếu quest cần consumable đắt, tổng `$` reward phải tối thiểu bằng `1.5 x` giá shop của input bắt buộc.

## 9. Non-Deadlock Economy

### 9.1 Quy tắc không ngõ cụt

1. Khi unlock một chemical hoặc recipe, shop lập tức mở nhóm `Raw Inputs` của nó.
2. Main quest luôn định nghĩa `Recovery Bundle` gồm input tối thiểu, không phải chỉ cấp tiền.
3. Sản phẩm cần để nâng Lab có ít nhất hai đường: tự điều chế hoặc sample/thuê/quest reward bằng `$`.
4. Không cho phép phá hủy dụng cụ starter duy nhất. Dụng cụ có thể bẩn/đang dùng nhưng luôn rửa được miễn phí.
5. Kho đầy không xóa item. Người chơi chọn bán, chuyển vào kho dài hạn hoặc hủy sample có xác nhận.
6. Shop không bán catalyst hiếm bằng `$` nếu đó là giá trị premium; nhưng main quest có catalyst thuê hoặc sample quest tương đương.
7. Sau ba lần thất bại cùng một quest, NPC cung cấp hint rõ hơn và một retry bundle không giảm EXP.

### 9.2 Kiểm tra trước khi phát hành quest

Mỗi quest mới phải trả lời được:

- Input nào bị tiêu hao?
- Người chơi Level tối thiểu lấy lại từng input ở đâu?
- Dụng cụ bắt buộc nhận từ quest/nâng cấp nào?
- Nếu kho đầy, có thể nộp/thu sản phẩm thế nào?
- Nếu thao tác sai ba lần, recovery/hint hiển thị ở đâu?
- Giá input, reward `$`, EXP và unlock có vượt budget level không?

Không publish quest nếu thiếu một câu trả lời.

## 10. Cân bằng chi phí và hành vi mong muốn

### 10.1 Ngân sách theo chặng

| Chặng | Số quest chính kỳ vọng | `$` kiếm được lũy kế gần đúng | `$` cần cho Lab Upgrade | Mục tiêu |
| --- | ---: | ---: | ---: | --- |
| Lv1-Lv3 | 6-8 | 1,300-1,900 | 900 | Học loop và có đệm mua lại |
| Lv4-Lv6 | 8-10 | 4,500-6,000 | 4,700 | Dùng máy điều chế, có thể cần contract phụ |
| Lv7-Lv8 | 6-8 | 7,500-9,000 | 7,000 | Khám phá phức chất/chains |
| Lv9-Lv10 | 6-8 | 12,000-14,000 | 12,200 | Hoàn thành Ostwald/capstone |

Mỗi chặng có thể yêu cầu 2-4 daily contracts, nhưng không được buộc người chơi chờ nhiều ngày chỉ để trả phí nâng cấp. Nếu simulation cho thấy thiếu `$`, tăng reward contract hoặc giảm upgrade cost trước khi đưa `💎` vào giải pháp.

### 10.2 Sinks lành mạnh

- Mua nguyên liệu thô khi muốn thử reaction ngoài quest.
- Nâng Lab để tăng năng lực thao tác/kho/máy.
- Mua dụng cụ phụ hoặc thêm lọ lưu trữ.
- Mua sample hiếm/cosmetic bằng `💎`.

Không dùng sink gây khó chịu: mất EXP, mất toàn bộ kho khi phản ứng sai, phí rửa bắt buộc, timer chờ reaction, hoặc phí hồi sinh.

## 11. Vận hành, telemetry và điều chỉnh

### KPI gameplay cần theo dõi

| Chỉ số | Cảnh báo | Hành động cân bằng |
| --- | --- | --- |
| Tutorial completion | dưới 85% | Giảm thao tác, tăng hint trực quan, kiểm tra input UI |
| Tỷ lệ kẹt main quest > 15 phút | trên 10% | Kiểm tra recovery bundle, giá input và recipe readability |
| Median `$` tại đầu mỗi Lab level | dưới chi phí upgrade 20% | Tăng reward quest/contract hoặc hạ upgrade cost |
| Tỷ lệ dùng `💎` cho main progression | trên 5% | Đang paywall ngầm, bổ sung đường `$`/quest |
| Kho đầy | trên 20% session | Tăng slot sớm hoặc cải thiện flow bán/cất |
| Daily Day-7 claim | dưới 25% | Xem lại reminder và chất lượng phần thưởng, không tăng pressure |

### Cadence nội dung

- Mỗi update thêm một `reaction family` nhỏ, 3-5 notebook entries, 2 quest chính, 3 daily contracts và một vật phẩm cosmetic/sưu tập.
- Bất kỳ chất mới nào cũng cần: data chất, visual hiện tượng, route unlock, raw-input fallback, giá shop, quest dùng thử và notebook entry.
- Event chỉ cấp cosmetic, sample hiếm hoặc contract bonus. Event không được khóa machine/recipe vĩnh viễn.

## 12. Data model đề xuất cho implementation

| Data | Field tối thiểu |
| --- | --- |
| `ChemicalDefinition` | id, displayName, category, unit, baseShopPrice, unlockLevel, rarity, purchasableWithDollar, purchasableWithGems |
| `RecipeDefinition` | inputs, outputs, requiredTool, requiredLabLevel, observableEffects, notebookEntryId, fallbackInputIds |
| `LabUpgradeDefinition` | level, playerLevelRequirement, dollarCost, questRequirementId, storageSlots, unlockedToolIds |
| `QuestDefinition` | id, tier, prerequisites, objective, requiredInputs, rewards, unlockIds, recoveryBundle, maxRecoveryClaims |
| `DailyRewardDefinition` | cycleDay, dollarReward, gemReward, chemicalReward, toolReward, selectionRule |
| `PlayerEconomyState` | dollars, gems, exp, level, labUpgradeLevel, inventory, unlockedChemicals, unlockedRecipes, claimedRecoveryIds |

Mọi giá, reward, EXP và unlock nằm trong ScriptableObject/JSON data, không hard-code trong UI hay reaction logic. Điều này cho phép chạy simulation economy và điều chỉnh live balance mà không thay đổi reaction engine.

## 13. Checklist nghiệm thu MVP

- Người chơi mới hoàn tất tutorial với tài nguyên starter, không cần shop hay quảng cáo.
- Toàn bộ Lv1-Lv10 hoàn thành được với `$`, quest, daily và recovery; không cần nạp `💎`.
- Mỗi recipe mainline có raw-input fallback sau unlock.
- Daily 7 ngày trả đúng số lượng đã chốt.
- Lab Lv4 mở điện phân dung dịch, Lv5-Lv6 mở nâng cao, Lv8-Lv9 mở Ostwald/HNO3 như roadmap.
- Pt có đường thuê bằng `$` cho quest, đồng thời vẫn giữ giá trị sưu tập premium.
- Phản ứng sai không phá hủy dụng cụ starter, không làm mất EXP và không khóa quest.
- Dashboard telemetry có đủ các KPI ở mục 11 trước beta test.

## 14. Quyết định cần chốt trước production

1. Đơn vị inventory hiển thị: gram, mL hay unit sample; đề xuất lưu nội bộ theo `g` và hiển thị conversion khi cần.
2. Mức hỗ trợ chemical realism: lớp học cơ sở, THPT hay mở rộng đại học; quyết định này chi phối độ sâu của recipe và notebook.
3. Cách nhận `💎` từ quảng cáo tự nguyện: nên giới hạn bằng daily cap, không thay thế quest reward.
4. Phạm vi PvP/trading: không đưa vào MVP vì có nguy cơ phá economy và làm phức tạp tính đúng đắn học thuật.
5. Chính sách an toàn/age rating và nội dung cảnh báo trong UI, đặc biệt với các chất hiếm/nguy hiểm; ưu tiên giải thích hiện tượng trong mô phỏng thay vì hướng dẫn thao tác ngoài đời.

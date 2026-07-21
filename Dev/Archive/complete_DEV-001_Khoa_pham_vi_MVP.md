# DEV-001 Khóa phạm vi MVP Chemistry Lab

- Plan liên quan: `Plan/00_Quan_tri/001_Khoa_pham_vi_MVP.md`
- Trạng thái: completed (2026-07-20)
- Người thực hiện: Codex
- Ngày bắt đầu: 2026-07-20

## Mục tiêu

Chuyển yêu cầu gốc thành phạm vi MVP có thể triển khai: danh mục nội dung, nền tảng mục tiêu, vòng lặp chơi, tiêu chí duyệt và phần việc ngoài MVP.

## Phạm vi được phép thay đổi

- Tạo đặc tả MVP tại `Plan/00_Quan_tri/MVP_Scope.md`.
- Hiệu chỉnh mô tả nguồn yêu cầu sai trong `Step/00_Tong_quan_va_pham_vi.md`.
- Cập nhật trạng thái Plan 001 sau khi đặc tả hoàn tất.

## Không thuộc phạm vi

- Tạo Unity project, asset, mã game hoặc cơ sở dữ liệu.
- Cài Unity Hub/Unity Editor: người dùng đã tải sẵn.
- Duyệt chuyên môn hóa học bởi một người có thẩm quyền.

## Acceptance criteria

- Có 20–30 chất, 12–15 phản ứng catalogue và đúng 10 nhiệm vụ MVP.
- Mỗi chất có vai trò game rõ ràng; mỗi phản ứng có đầu vào, đầu ra, hiện tượng game và người duyệt nội dung.
- Phân biệt cụ thể MVP và ngoài MVP; chốt nền tảng Android/iOS, landscape và tiếng Việt.

## Kiểm thử bắt buộc

- Đếm danh mục chất, phản ứng và nhiệm vụ.
- Đối chiếu với các tệp trong `PlanCoreGame` và `Step/00_Tong_quan_va_pham_vi.md`.

## Đầu ra bàn giao

- `Plan/00_Quan_tri/MVP_Scope.md`
- Plan 001 được đổi tên với tiền tố `complete_`.

## Ghi chú/Blocker

- Unity Hub/Editor đã được người dùng tải sẵn; bước 010 chỉ cần tạo project đúng phiên bản đã chọn.
- Mọi phản ứng chỉ là catalogue mô phỏng game; chemistry reviewer cần duyệt trước khi đưa vào bản phát hành.

## Kết quả

- Đã tạo `Plan/00_Quan_tri/MVP_Scope.md` với 26 chất, 13 phản ứng catalogue và 10 nhiệm vụ MVP.
- Đã khóa Android 10+, iOS 15+, landscape, tiếng Việt và offline-first.
- Đã phân tách rõ phần ngoài MVP, đồng thời sửa ghi chú nguồn yêu cầu trong `Step/00_Tong_quan_va_pham_vi.md`.

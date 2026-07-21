# DEV-002 Khoá quality gate và quyết định reaction engine

- Plan liên quan: `Plan/00_Quan_tri/002_Khoa_gates_chat_luong.md`
- Trạng thái: completed (2026-07-20)
- Người thực hiện: Codex
- Ngày bắt đầu: 2026-07-20

## Mục tiêu

Thiết lập gate bắt buộc cho nội dung, build mobile và release; ghi quyết định kỹ thuật về cách engine xử lý phản ứng.

## Phạm vi được phép thay đổi

- Tạo ma trận gate và evidence bắt buộc tại `Plan/00_Quan_tri/Quality_Gates.md`.
- Tạo ADR reaction engine tại `Plan/00_Quan_tri/ADR_001_Reaction_Engine.md`.
- Đánh dấu Plan 002 hoàn tất khi mọi gate có owner, evidence và điểm áp dụng.

## Không thuộc phạm vi

- Viết Unity/C# hoặc tạo reaction catalogue thực tế.
- Huấn luyện, tích hợp, hoặc cho AI suy luận phản ứng khi đang chơi.
- Thay thế chemistry reviewer bằng AI.

## Acceptance criteria

- Có content, mobile build và release gate với owner, evidence pass/fail và hành động khi fail.
- Mọi task downstream liên quan đã có điểm gate rõ ràng.
- ADR nêu rõ resolver dữ liệu tổng quát, giới hạn AI và chính sách với phản ứng lạ/tạo phức.

## Kiểm thử bắt buộc

- Kiểm tra owner/evidence/điểm áp dụng cho từng gate.
- Đối chiếu task plan Phase 01–07 với gate matrix.

## Đầu ra bàn giao

- `Plan/00_Quan_tri/Quality_Gates.md`
- `Plan/00_Quan_tri/ADR_001_Reaction_Engine.md`

## Ghi chú/Blocker

- Chemistry reviewer là cổng duyệt nội dung công khai; không có AI runtime nào được phép tự tạo content khoa học.

## Kết quả

- Đã tạo ma trận G0–G9 với owner, evidence, hành động fail và mốc áp dụng theo phase.
- Đã chốt resolver tổng quát đọc catalogue; không tạo hàm riêng theo từng phản ứng.
- Đã chốt phản ứng lạ/tạo phức là ngoài MVP và AI chỉ được hỗ trợ tạo bản nháp nội bộ có review.

# Dev — hàng đợi yêu cầu lập trình

Thư mục này chỉ chứa một yêu cầu đang hoạt động: `active_request.md`.

- Mọi yêu cầu phải tham chiếu một hoặc nhiều mã task trong `Plan/`.
- Không đặt yêu cầu chưa được mô tả rõ acceptance criteria vào `active_request.md`.
- Khi yêu cầu xong: xác minh test, đổi tên thành `complete_<ma>_<ten>.md`, chuyển vào `Archive/`, rồi xoá bản active.
- `Archive/` là lịch sử bất biến; không ghi đè yêu cầu cũ.

Không tạo `active_request.md` trong lần bootstrap này, vì chưa có hạng mục code game nào được giao riêng.

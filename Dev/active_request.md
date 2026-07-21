# DEV-01 Thực hiện phase 01 — Nền tảng Unity

- Plan liên quan: `Plan/01_Nen_tang/010` đến `018`
- Trạng thái: active
- Người thực hiện: Codex
- Ngày bắt đầu: 2026-07-21

## Mục tiêu

Khởi tạo project Unity 6.3 URP 2D và hoàn thiện nền tảng code/UI/build có thể làm trên máy Windows hiện tại.

## Phạm vi được phép thay đổi

- Tạo project `ChemistryLabGame` trong workspace, scene, package manifest, assembly definitions, bootstrap, UI safe area/design tokens và cấu hình source control/CI.
- Cấu hình thông số Android/iOS trong giới hạn module hiện có; viết checklist build để hoàn tất khi module/máy Mac sẵn sàng.

## Không thuộc phạm vi

- Cài thêm Android/iOS Build Support, SDK/NDK/JDK, Xcode hoặc thực hiện signing/keystore.
- Test Android/iOS thật khi máy hiện chưa có module Build Support và không có macOS/Xcode.
- Phát triển gameplay lab, reaction catalogue hoặc SQLite.

## Acceptance criteria

- Project Unity mở/compile được, Bootstrap chuyển sang MainLab, root UI khóa landscape và có safe area/token cơ bản.
- Assembly dependency một chiều và test domain mẫu chạy được.
- Các task cần Android/iOS thật được giữ planned kèm blocker rõ ràng, không đánh dấu hoàn tất sai.

## Kiểm thử bắt buộc

- Batchmode compile/test Bootstrap sau khi package resolve.
- Kiểm tra cấu trúc, manifest, .gitignore/.gitattributes và các cấu hình module có sẵn.

## Đầu ra bàn giao

- Unity project `ChemistryLabGame` và các tài liệu/checklist nền tảng.
- Các Plan hoàn tất được đổi tên `complete_`; các Plan bị phụ thuộc môi trường vẫn giữ trạng thái planned.

## Ghi chú/Blocker

- Editor đã cài là Unity 6.5 `6000.5.3f1`, không phải Unity 6.3 LTS đã khóa trong `Step/01_Lua_chon_cong_nghe.md`.
- Editor hiện chỉ có Windows/WebGL playback engines; thiếu Android Build Support và iOS Build Support. iOS build/run còn cần macOS/Xcode.
- Đã tạo khung template tại `ChemistryLabGame` nhưng chưa chạy/đánh dấu hoàn tất Plan 010, chờ quyết định phiên bản engine để tránh trộn version project.

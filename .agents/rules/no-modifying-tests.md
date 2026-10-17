---
trigger: always_on
description: Nghiêm cấm mọi hành vi chỉnh sửa hoặc làm sai lệch test case
---

# Quy tắc bắt buộc: Không được sửa test case (Do Not Modify Test Cases)

1. **Tuyệt đối KHÔNG sửa đổi test case**:
   - Không được sửa đổi, xóa bỏ, comment out hoặc làm giảm nhẹ điều kiện kiểm thử trong bất kỳ file test nào (thư mục `tests/`, các file `*Tests.cs`, `*Test.cs`, `*.spec.*`, `*.test.*`).
   - Không thay đổi assertion, expected values, mocks, test fixtures hoặc logic kiểm thử hiện có.

2. **Cách xử lý khi test bị lỗi (Test Failures)**:
   - Phải sửa đổi mã nguồn ứng dụng (production code / implementation code) để thỏa mãn và vượt qua bài test.
   - Tuyệt đối không được thay đổi test case để làm bài test pass một cách giả tạo.

3. **Chỉ sửa khi có lệnh rõ ràng từ người dùng**:
   - Không tự ý thêm, bớt hoặc thay đổi test case trừ khi người dùng chỉ định rõ ràng trong câu lệnh.

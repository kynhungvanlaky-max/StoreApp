<!-- CODEGRAPH_START -->
## CodeGraph

In repositories indexed by CodeGraph (a `.codegraph/` directory exists at the repo root), reach for it BEFORE grep/find or reading files when you need to understand or locate code:

- **MCP tool** (when available): `codegraph_explore` answers most code questions in one call — the relevant symbols' verbatim source plus the call paths between them, including dynamic-dispatch hops grep can't follow. Name a file or symbol in the query to read its current line-numbered source. If it's listed but deferred, load it by name via tool search.
- **Shell** (always works): `codegraph explore "<symbol names or question>"` prints the same output.

If there is no `.codegraph/` directory, skip CodeGraph entirely — indexing is the user's decision.
<!-- CODEGRAPH_END -->

## QUY TẮC BẮT BUỘC: KHÔNG ĐƯỢC SỬA TEST CASE (DO NOT MODIFY TEST CASES)

- **Tuyệt đối KHÔNG ĐƯỢC CHỈNH SỬA, XÓA, COMMENT HOẶC LÀM SUY YẾU BẤT KỲ TEST CASE NÀO**:
  - Nghiêm cấm mọi hành vi can thiệp vào các file kiểm thử trong thư mục `tests/` hoặc bất kỳ file kiểm thử nào (`*Tests.cs`, `*Test.cs`, `*.spec.*`, `*.test.*`).
  - Không được sửa đổi assertion, kỳ vọng (expected values), dữ liệu đầu vào (test data), mocks, hoặc luồng kiểm thử đã định nghĩa trong test case.
- **Khi Test Fail**:
  - Chỉ được phép sửa mã nguồn ứng dụng (production code / implementation code trong `API`, `backend`, `frontend`, v.v.) để đáp ứng đúng và vượt qua các bài kiểm thử.
  - Tuyệt đối không được "sửa test cho pass". Mã nguồn phải luôn tuân theo thiết kế và tiêu chuẩn mà test case đặt ra.
- **Ngoại lệ duy nhất**: Chỉ khi người dùng yêu cầu trực tiếp và rõ ràng bằng văn bản về việc sửa đổi một test case cụ thể thì mới được thực hiện.

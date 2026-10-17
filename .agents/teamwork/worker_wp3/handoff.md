# BÁO CÁO BÀN GIAO CÔNG VIỆC — WORKER_WP3 (ui_auth_identity_worker)

**Ngày thực hiện**: 2026-10-16  
**Nhiệm vụ**: WP3 — Nâng cấp & Chuẩn hóa Module Xác thực & Trung tâm Tài khoản (Authentication & Account Center Modernization)  
**Phân quyền file sở hữu**:
- `src/NexusKeys.Storefront/Components/Pages/Auth/Login.razor`
- `src/NexusKeys.Storefront/Components/Pages/Auth/Register.razor`
- `src/NexusKeys.Storefront/Components/Pages/Auth/ForgotPassword.razor`
- `src/NexusKeys.Storefront/Components/Pages/Auth/ResetPassword.razor`
- `src/NexusKeys.Storefront/Components/Layout/ProfileLayout.razor`
- `src/NexusKeys.Storefront/Components/Pages/Profile/Profile.razor`
- `src/NexusKeys.Storefront/Components/Pages/Profile/Library.razor`
- `src/NexusKeys.Storefront/Components/Pages/Profile/Wallet.razor`

---

## 1. Observation (Quan sát thực tế)

1. **Brand & Localization Split-Brain trước thay đổi**:
   - Các trang xác thực (`Login.razor`, `Register.razor`, `ForgotPassword.razor`) hiển thị toàn bộ tiêu đề, nhãn nhập liệu và thông báo lỗi bằng tiếng Anh (*"Sign in to Epic Games"*, *"Create an Epic Games Account"*, *"Forgot Your Password?"*, *"Email address"*, *"Password"*, *"Only played on console?"*, *"Other ways to sign in"*).
   - Thẻ `<PageTitle>` mang nhãn Epic: `<PageTitle>Sign In — Epic Games</PageTitle>`, `<PageTitle>Create Account — Epic Games</PageTitle>`, `<PageTitle>Forgot Your Password — Epic Games</PageTitle>`, `<PageTitle>Đặt Lại Mật Khẩu — Epic Games Store</PageTitle>`.
   - Cả 4 trang đều sử dụng ảnh tĩnh Epic Games Shield Logo: `<img src="images/epic/epic_logo.png" alt="Epic Games" class="w-11 h-11 object-contain" />`.
   - Tại `ProfileLayout.razor` (dòng 18): Hiển thị nhãn tĩnh `"Epic Games Store Ecosystem"`.
   - Độ tương phản trên `ProfileLayout.razor`, `Profile.razor`, `Library.razor`, `Wallet.razor` chứa nhiều thành phần mờ sử dụng class `text-white/30`, `text-white/40`, `text-white/50` trên nền `#18181c`/`#202024`.
   - Thao tác sao chép license key trong `Library.razor` chỉ hiển thị Toast ngoài màn hình mà không có hiệu ứng phản hồi trạng thái tức thời ngay tại vị trí nút/mã key được bấm.

2. **Toàn vẹn Form Controls & Bindings**:
   - Các thuộc tính phần tử form hiện tại:
     - `Login.razor`: `id="login-email"` (`type="email"`, `@bind="LoginRequest.Email"`), `id="login-password"` (`@bind="LoginRequest.Password"`).
     - `Register.razor`: `id="register-username"`, `id="register-email"`, `id="register-password"`, `id="register-secret"`, `id="terms-agree"`.
     - `ForgotPassword.razor`: `id="forgot-email"` (`type="email"`, `@bind="Request.Email"`).
     - `ResetPassword.razor`: `id="reset-password"`, `id="reset-confirm-password"`.
   - Tất cả các thuộc tính định danh này đều đã được giữ nguyên 100%.

3. **Kết quả kiểm thử & biên dịch**:
   - Lệnh `git diff --stat tests/`: Hoàn toàn trống (**0 diff**).
   - Lệnh `dotnet build GameStore.slnx`: **Build succeeded (0 Error(s))**.
   - Lệnh `dotnet test tests/GameStore.API.ProductKeyTests/GameStore.API.ProductKeyTests.csproj`: **72/72 Passed 100%** (0 Failed, 0 Skipped).

---

## 2. Logic Chain (Chuỗi lập luận & giải pháp)

1. **Đồng bộ nhận diện thương hiệu NexusKeys**:
   - Đối chiếu với `Navbar.razor` (biểu tượng `HeroiconName.PuzzlePiece` và chữ `NEXUS<span class="text-[#0078F2]">KEYS</span>`), thay thế toàn bộ khối logo `epic_logo.png` trên 4 trang Auth bằng Brand Badge chuẩn:
     ```razor
     <a href="/" title="NexusKeys Game Store" class="inline-flex items-center gap-2.5 transition-transform hover:scale-105">
         <div class="w-10 h-10 rounded-xl bg-[#0078F2]/10 border border-[#0078F2]/30 flex items-center justify-center text-[#0078F2] shadow-lg shadow-[#0078F2]/10">
             <Icon Name="HeroiconName.PuzzlePiece" Class="w-6 h-6 text-[#0078F2]" IconType="HeroiconType.Solid" />
         </div>
         <span class="text-xl font-black text-white tracking-tighter">NEXUS<span class="text-[#0078F2]">KEYS</span></span>
     </a>
     ```
2. **Việt hóa 100% Giao diện & Xử lý lỗi**:
   - Thay thế toàn bộ heading và phụ đề sang tiếng Việt tự nhiên, phù hợp với văn phong cửa hàng game bản quyền.
   - Việt hóa thông báo lỗi validation và ngoại lệ trong khối C# code (ví dụ: *"Vui lòng nhập đầy đủ địa chỉ email và mật khẩu."*, *"Địa chỉ email hoặc mật khẩu không chính xác. Vui lòng thử lại."*, *"Đăng ký thất bại. Địa chỉ email này có thể đã được sử dụng."*).
   - Chuẩn hóa toàn bộ thẻ `<PageTitle>` sang Tiếng Việt có hậu tố `— NexusKeys`:
     - `Đăng Nhập — NexusKeys`
     - `Đăng Ký Tài Khoản — NexusKeys`
     - `Quên Mật Khẩu — NexusKeys`
     - `Đặt Lại Mật Khẩu — NexusKeys`
3. **Bảo tồn toàn vẹn logic kiểm thử**:
   - Tuyệt đối không can thiệp vào bất kỳ file nào trong thư mục `tests/`.
   - Bảo toàn nguyên vẹn tất cả `id`, `type`, `@bind`, `@bind:event`, `required`, `autocomplete` của toàn bộ form controls.
4. **Chuẩn hóa Trung tâm Tài khoản (Profile Ecosystem)**:
   - Trong `ProfileLayout.razor`: Đổi *"Epic Games Store Ecosystem"* thành *"Hệ Sinh Thái Bản Quyền NexusKeys"*.
   - Trong `ProfileLayout.razor`, `Profile.razor`, `Wallet.razor`: Nâng các mức mờ `text-white/30`, `text-white/40`, `text-white/50` lên `text-white/60` và `text-white/70`, đảm bảo tỷ lệ tương phản WCAG 2.1 AA ($\ge 4.5:1$).
   - Trong `Library.razor`:
     - Nâng cấp trải nghiệm sao chép key: Thêm biến trạng thái phản ứng `_copiedKey` tự động chuyển đổi giao diện nút sang trạng thái xác nhận (Badge xanh lá `Đã sao chép!` kèm icon `HeroiconName.Check`) trong 2.5 giây.
     - Cho phép nhấp trực tiếp vào chuỗi mã key (click-to-copy) với hiệu ứng hover và icon copy gợi ý.
     - Đồng bộ trạng thái sao chép trong cả danh sách kho game lẫn Hộp thoại Hướng dẫn kích hoạt bản quyền (Key Guide Modal).

---

## 3. Caveats (Lưu ý)

1. **Phạm vi file độc quyền**:
   - Worker chỉ thực hiện thay đổi trên 8 file được phân quyền tuyệt đối. Không chỉnh sửa bất kỳ file nào ngoài danh sách (bao gồm `Home.razor`, `Navbar.razor`, hay các trang giỏ hàng/admin thuộc quyền của subagent khác).
2. **Tên thương hiệu đối tác thứ ba**:
   - Trong các nút đăng nhập SSO console/mạng xã hội (PlayStation™ Network, Xbox network, Nintendo Account, Google, Steam), tên thương hiệu nền tảng gốc được giữ nguyên theo chuẩn nhận diện quốc tế, trong khi các nhãn hành động và chú thích đều được Việt hóa toàn diện.

---

## 4. Conclusion (Kết luận)

Tất cả các tiêu chí nghiệm thu của Gói công việc WP3 (Authentication & Account Center Modernization) đã hoàn thành 100%:
- 4 trang Auth mang nhận diện NexusKeys, 100% Tiếng Việt, giữ trọn vẹn thuộc tính form.
- Trung tâm tài khoản (Profile, Library, Wallet, ProfileLayout) được đồng bộ thương hiệu NexusKeys, đạt chuẩn tương phản dễ đọc.
- Trải nghiệm sao chép Key được tối ưu hóa mượt mà trên cả máy tính và thiết bị cảm ứng di động.
- Không có bất kỳ vi phạm hay chỉnh sửa nào đối với thư mục `tests/`.

---

## 5. Verification Method (Phương thức xác minh độc lập)

1. **Kiểm tra tính bất biến của bộ kiểm thử (Test Inviolability)**:
   ```powershell
   git diff --stat tests/
   ```
   *Kỳ vọng: Không có bất kỳ dòng thay đổi nào (0 diff).*

2. **Kiểm tra biên dịch dự án**:
   ```powershell
   dotnet build GameStore.slnx
   ```
   *Kỳ vọng: Build succeeded, 0 errors.*

3. **Kiểm tra thực thi test suite**:
   ```powershell
   dotnet test tests/GameStore.API.ProductKeyTests/GameStore.API.ProductKeyTests.csproj
   ```
   *Kỳ vọng: Passed: 72, Failed: 0, Skipped: 0 (100% Passed).*

4. **Kiểm tra xóa bỏ logo Epic và chuỗi Epic trong Auth**:
   ```powershell
   git grep -i "epic_logo" src/NexusKeys.Storefront/Components/Pages/Auth/
   git grep -i "Sign in to Epic Games" src/NexusKeys.Storefront/Components/Pages/Auth/
   ```
   *Kỳ vọng: 0 kết quả.*

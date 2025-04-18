using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using System;
using System.Threading.Tasks;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Users
{
    //[AuthorizeRole("manager")]
    public class UserUpdateModel : PageModel
    {
        private readonly IUsersService _usersService;

        public UserUpdateModel(IUsersService usersService)
        {
            _usersService = usersService;
        }

        [BindProperty]
        public UsersRequest UserRequest { get; set; } = new UsersRequest();

        public UsersResponse UserResponse { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                UserResponse = await _usersService.GetUserByIdAsync(id);
                if (UserResponse == null)
                {
                    TempData["Error"] = "Không tìm thấy User với ID này.";
                    return RedirectToPage("/Users/Index");
                }

                // Gán giá trị mặc định cho UserRequest từ UserResponse
                UserRequest = new UsersRequest
                {
                    username = UserResponse.username,
                    password_hash = "", // Không hiển thị mật khẩu cũ
                    full_name = UserResponse.full_name,
                    email = UserResponse.email,
                    role = UserResponse.role
                };

                return Page();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading user: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = $"Đã xảy ra lỗi khi tải thông tin User: {ex.Message}";
                return RedirectToPage("/Users/Index");
            }
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            //if (!ModelState.IsValid)
            //{
            //    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            //    {
            //        Console.WriteLine($"Validation error: {error.ErrorMessage}");
            //    }
            //    return Page();
            //}

            // Bind thủ công cho image từ form
            UserRequest.image = Request.Form.Files["image"];

            try
            {
                Console.WriteLine($"Updating user with ID: {id}, Username: {UserRequest.username}, Email: {UserRequest.email}");
                if (UserRequest.image != null)
                {
                    Console.WriteLine($"Image: filename={UserRequest.image.FileName}, size={UserRequest.image.Length}");
                }
                else
                {
                    Console.WriteLine("No image uploaded.");
                }

                // Nếu mật khẩu để trống, giữ nguyên mật khẩu cũ (gán null để backend xử lý)
                if (string.IsNullOrEmpty(UserRequest.password_hash))
                {
                    UserRequest.password_hash = null;
                }

                // Gửi yêu cầu cập nhật
                var updatedUser = await _usersService.UpdateUserAsync(id, UserRequest);
                if (updatedUser == null)
                {
                    TempData["Error"] = "Không thể cập nhật User. Dữ liệu trả về từ dịch vụ là null.";
                    Console.WriteLine("User update failed: null response from service.");
                    return Page();
                }

                TempData["Success"] = "User đã được cập nhật thành công!";
                return RedirectToPage("/Users/Index");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}, StackTrace: {ex.StackTrace}");
                TempData["Error"] = $"Đã xảy ra lỗi khi cập nhật User: {ex.Message}";
                return Page();
            }
        }
    }
}
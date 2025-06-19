using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend2.Filter;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.Users
{
    [AuthorizeRole("admin,manager")]
    public class UserCreateModel : PageModel
    {
        private readonly IUsersService _usersService;
        private readonly ILogger<UserCreateModel> _logger;

        public UserCreateModel(IUsersService usersService, ILogger<UserCreateModel> logger)
        {
            _usersService = usersService;
            _logger = logger;
        }

        [BindProperty]
        public UsersRequest User { get; set; } = new UsersRequest();

        public IActionResult OnGet()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is accessing the user creation page", username, role);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is submitting a new user creation request", username, role);

            User.image = Request.Form.Files["image"];
            _logger.LogDebug("User {Username} (Role: {Role}) submitted user data: username={SubmittedUsername}, full_name={FullName}, email={Email}, role={SubmittedRole}", username, role, User.username, User.full_name, User.email, User.role);

            if (User.image != null)
            {
                _logger.LogDebug("User {Username} (Role: {Role}) uploaded image: filename={FileName}, size={FileSize}", username, role, User.image.FileName, User.image.Length);
            }
            else
            {
                _logger.LogDebug("User {Username} (Role: {Role}) did not upload an image", username, role);
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(User.username))
            {
                _logger.LogWarning("User {Username} (Role: {Role}) did not provide a username", username, role);
                ModelState.AddModelError("User.username", "Tên đăng nhập là bắt buộc.");
            }

            if (string.IsNullOrWhiteSpace(User.password))
            {
                _logger.LogWarning("User {Username} (Role: {Role}) did not provide a password", username, role);
                ModelState.AddModelError("User.password_hash", "Mật khẩu là bắt buộc.");
            }

            if (string.IsNullOrWhiteSpace(User.full_name))
            {
                _logger.LogWarning("User {Username} (Role: {Role}) did not provide a full name", username, role);
                ModelState.AddModelError("User.full_name", "Họ và tên là bắt buộc.");
            }

            if (string.IsNullOrWhiteSpace(User.email))
            {
                _logger.LogWarning("User {Username} (Role: {Role}) did not provide an email", username, role);
                ModelState.AddModelError("User.email", "Email là bắt buộc.");
            }
            else if (!IsValidEmail(User.email))
            {
                _logger.LogWarning("User {Username} (Role: {Role}) provided an invalid email: {Email}", username, role, User.email);
                ModelState.AddModelError("User.email", "Email không hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(User.role))
            {
                _logger.LogWarning("User {Username} (Role: {Role}) did not provide a role", username, role);
                ModelState.AddModelError("User.role", "Vai trò là bắt buộc.");
            }

            //if (!ModelState.IsValid)
            //{
            //    var errors = ModelState.ToDictionary(
            //        kvp => kvp.Key,
            //        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
            //    );
            //    _logger.LogWarning("User {Username} (Role: {Role}) encountered validation errors: {Errors}",username, role, Newtonsoft.Json.JsonConvert.SerializeObject(errors));
            //    return Page();
            //}

            try
            {
                var createdUser = await _usersService.CreateUserAsync(User);
                if (createdUser == null)
                {
                    _logger.LogWarning("User {Username} (Role: {Role}) failed to create user: No result returned", username, role);
                    TempData["Error"] = "Không thể tạo người dùng. Dữ liệu trả về từ dịch vụ là null.";
                    return Page();
                }

                _logger.LogInformation("User {Username} (Role: {Role}) successfully created user with ID {UserId}", username, role, createdUser.user_id);
                TempData["Success"] = "Người dùng đã được tạo thành công!";
                return RedirectToPage("/Users/Index");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) encountered argument error creating user: {Error}", username, role, ex.Message);
                TempData["Error"] = ex.Message;
                return Page();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) encountered invalid operation error creating user: {Error}", username, role, ex.Message);
                TempData["Error"] = ex.Message;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered error creating user: {Error}", username, role, ex.Message);
                TempData["Error"] = $"Đã xảy ra lỗi khi tạo người dùng: {ex.Message}";
                return Page();
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
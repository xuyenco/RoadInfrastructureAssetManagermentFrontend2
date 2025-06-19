using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;
using RoadInfrastructureAssetManagementFrontend2.Interface;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RoadInfrastructureAssetManagementFrontend2.Filter;

namespace RoadInfrastructureAssetManagementFrontend2.Pages.AssetCategories
{
    [AuthorizeRole("admin,manager")]
    public class AssetCagetoryCreateModel : PageModel
    {
        private readonly IAssetCagetoriesService _assetCagetoriesService;
        private readonly ILogger<AssetCagetoryCreateModel> _logger;

        public AssetCagetoryCreateModel(IAssetCagetoriesService assetCagetoriesService, ILogger<AssetCagetoryCreateModel> logger)
        {
            _assetCagetoriesService = assetCagetoriesService;
            _logger = logger;
        }

        [BindProperty]
        public AssetCagetoriesRequest AssetCategory { get; set; } = new();

        public void OnGet()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";
            _logger.LogInformation("User {Username} (Role: {Role}) is accessing the asset category creation page", username, role);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is submitting a new asset category", username, role);

            // Xử lý dữ liệu cơ bản
            AssetCategory.category_name = Request.Form["category_name"];
            AssetCategory.geometry_type = Request.Form["geometry_type"];
            AssetCategory.sample_image = Request.Form.Files["sample_image"];
            AssetCategory.icon = Request.Form.Files["icon"];

            // Xử lý Attributes Schema
            var attributeNames = Request.Form.Keys.Where(k => k.StartsWith("attributes["));
            var tempAttributes = new List<AttributeDefinition>();

            foreach (var key in attributeNames)
            {
                if (key.Contains(".Name"))
                {
                    var index = key.Split('[')[1].Split(']')[0];
                    var attr = new AttributeDefinition
                    {
                        Name = Request.Form[$"attributes[{index}].Name"],
                        Type = Request.Form[$"attributes[{index}].Type"],
                        Description = Request.Form[$"attributes[{index}].Description"],
                        IsRequired = Request.Form[$"attributes[{index}].IsRequired"] == "on",
                        EnumValuesStr = Request.Form[$"attributes[{index}].EnumValuesStr"]
                    };
                    tempAttributes.Add(attr);
                }
            }

            var requiredFields = new List<string>();
            var properties = new Dictionary<string, object>();

            foreach (var attr in tempAttributes)
            {
                var prop = new Dictionary<string, object>
                {
                    ["type"] = attr.Type,
                    ["description"] = attr.Description ?? ""
                };

                if (!string.IsNullOrEmpty(attr.EnumValuesStr))
                {
                    prop["enum"] = attr.EnumValuesStr.Split(',').Select(v => v.Trim()).ToList();
                }

                properties[attr.Name] = prop;
                if (attr.IsRequired) requiredFields.Add(attr.Name);
            }

            AssetCategory.attribute_schema = JsonSerializer.Serialize(new
            {
                required = requiredFields,
                properties = properties
            });

            // Kiểm tra ModelState
            //if (!ModelState.IsValid)
            //{
            //    var errors = ModelState.ToDictionary(
            //        kvp => kvp.Key,
            //        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
            //    );
            //    _logger.LogWarning("User {Username} (Role: {Role}) encountered validation errors while creating asset category: {Errors}",
            //        username, role, JsonSerializer.Serialize(errors));
            //    return Page();
            //}

            // Gửi request lên service
            try
            {
                _logger.LogDebug("User {Username} (Role: {Role}) sending data for asset category creation: {Request}",
                    username, role, JsonSerializer.Serialize(AssetCategory));
                var result = await _assetCagetoriesService.CreateAssetCagetoriesAsync(AssetCategory);
                if (result != null)
                {
                    _logger.LogInformation("User {Username} (Role: {Role}) successfully created asset category with ID {CategoryId}",
                        username, role, result.category_id);
                    return RedirectToPage("/AssetCategories/Index");
                }
                _logger.LogWarning("User {Username} (Role: {Role}) failed to create asset category: No result returned",
                    username, role);
                ModelState.AddModelError("", "Không thể tạo danh mục. Vui lòng thử lại.");
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered an error while creating asset category: {Error}",
                    username, role, ex.Message);
                ModelState.AddModelError("", $"Lỗi: {ex.Message}");
                return Page();
            }
        }

        private class AttributeDefinition
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public string Description { get; set; }
            public bool IsRequired { get; set; }
            public string EnumValuesStr { get; set; }
        }
    }
}
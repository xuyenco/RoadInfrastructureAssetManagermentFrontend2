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
    public class AssetCagetoryUpdateModel : PageModel
    {
        private readonly IAssetCagetoriesService _assetCagetoriesService;
        private readonly ILogger<AssetCagetoryUpdateModel> _logger;

        public AssetCagetoryUpdateModel(IAssetCagetoriesService assetCagetoriesService, ILogger<AssetCagetoryUpdateModel> logger)
        {
            _assetCagetoriesService = assetCagetoriesService;
            _logger = logger;
        }

        [BindProperty]
        public AssetCagetoriesResponse Category { get; set; }

        [BindProperty]
        public AssetCagetoriesRequest AssetCategory { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is retrieving asset category with ID {CategoryId} for update", username, role, id);
            
            Category = await _assetCagetoriesService.GetAssetCagetoriesByIdAsync(id);
            if (Category == null)
            {
                _logger.LogWarning("User {Username} (Role: {Role}) found no asset category with ID {CategoryId}", 
                    username, role, id);
                return NotFound();
            }

            // Chuẩn bị dữ liệu cho form
            AssetCategory.category_name = Category.category_name;
            AssetCategory.geometry_type = Category.geometry_type;
            AssetCategory.attribute_schema = JsonSerializer.Serialize(Category.attribute_schema);

            _logger.LogInformation("User {Username} (Role: {Role}) successfully retrieved asset category with ID {CategoryId}", 
                username, role, id);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var username = HttpContext.Session.GetString("Username") ?? "anonymous";
            var role = HttpContext.Session.GetString("Role") ?? "unknown";

            _logger.LogInformation("User {Username} (Role: {Role}) is submitting update for asset category with ID {CategoryId}", username, role, id);

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
            //    _logger.LogWarning("User {Username} (Role: {Role}) encountered validation errors while updating asset category with ID {CategoryId}: {Errors}", username, role, id, JsonSerializer.Serialize(errors));
            //    return Page();
            //}

            // Gửi request cập nhật lên service
            try
            {
                _logger.LogDebug("User {Username} (Role: {Role}) sending update data for asset category with ID {CategoryId}: {Request}", 
                    username, role, id, JsonSerializer.Serialize(AssetCategory));
                var result = await _assetCagetoriesService.UpdateAssetCagetoriesAsync(id, AssetCategory);
                if (result != null)
                {
                    _logger.LogInformation("User {Username} (Role: {Role}) successfully updated asset category with ID {CategoryId}", username, role, id);
                    return RedirectToPage("/AssetCategories/Index");
                }
                _logger.LogWarning("User {Username} (Role: {Role}) failed to update asset category with ID {CategoryId}: No result returned", username, role, id);
                ModelState.AddModelError("", "Không thể cập nhật danh mục. Vui lòng thử lại.");
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError("User {Username} (Role: {Role}) encountered an error while updating asset category with ID {CategoryId}: {Error}", username, role, id, ex.Message);
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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Road_Infrastructure_Asset_Management.Model.Request;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend.Pages.AssetCagetories
{
    public class AssetCagetoryUpdateModel : PageModel
    {
        private readonly IAssetCagetoriesService _assetCagetoriesService;

        public AssetCagetoryUpdateModel(IAssetCagetoriesService assetCagetoriesService)
        {
            _assetCagetoriesService = assetCagetoriesService;
        }

        [BindProperty]
        public AssetCagetoriesResponse Category { get; set; }

        [BindProperty]
        public AssetCagetoriesRequest AssetCategory { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Category = await _assetCagetoriesService.GetAssetCagetoriesByIdAsync(id);
            if (Category == null)
            {
                return NotFound();
            }

            // Chuẩn bị dữ liệu cho form
            AssetCategory.category_name = Category.category_name;
            AssetCategory.geometry_type = Category.geometry_type;
            AssetCategory.attribute_schema = JsonSerializer.Serialize(Category.attribute_schema);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
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
            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
                Console.WriteLine($"Validation errors: {JsonSerializer.Serialize(errors)}");
                return Page();
            }

            // Gửi request cập nhật lên service
            try
            {
                var result = await _assetCagetoriesService.UpdateAssetCagetoriesAsync(id, AssetCategory);
                if (result != null)
                {
                    return RedirectToPage("/AssetCagetories/Index");
                }
                ModelState.AddModelError("", "Không thể cập nhật danh mục. Vui lòng thử lại.");
                return Page();
            }
            catch (Exception ex)
            {
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
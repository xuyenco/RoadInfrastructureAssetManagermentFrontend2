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

        public AssetCagetoriesResponse Category { get; set; }

        public AssetCagetoriesRequest AssetCategory { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Category = await _assetCagetoriesService.GetAssetCagetoriesByIdAsync(id);
            if (Category == null)
            {
                return NotFound();
            }

            // Chuẩn bị dữ liệu cho form
            AssetCategory.cagetory_name = Category.cagetory_name;
            AssetCategory.geometry_type = Category.geometry_type;
            AssetCategory.attributes_schema = JsonSerializer.Serialize(Category.attributes_schema);
            AssetCategory.lifecycle_stages = JsonSerializer.Serialize(Category.lifecycle_stages);
            // marker không cần điền sẵn vì đây là file upload, người dùng sẽ chọn lại nếu muốn

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            // === PHẦN 1: Bind dữ liệu cơ bản ===
            AssetCategory.cagetory_name = Request.Form["cagetory_name"];
            AssetCategory.geometry_type = Request.Form["geometry_type"];
            AssetCategory.marker = Request.Form.Files["marker"];

            // === PHẦN 2: Xử lý Attributes Schema ===
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

            AssetCategory.attributes_schema = JsonSerializer.Serialize(new
            {
                required = requiredFields,
                properties = properties
            });

            // === PHẦN 3: Xử lý Lifecycle Stages ===
            var lifecycleStages = Request.Form["lifecycle_stages"]
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => (string)s)
                .ToList();

            if (lifecycleStages.Count == 0)
            {
                ModelState.AddModelError("lifecycle_stages", "Ít nhất một giai đoạn lifecycle là bắt buộc.");
                return Page();
            }

            AssetCategory.lifecycle_stages = JsonSerializer.Serialize(lifecycleStages);

            // === PHẦN 4: Kiểm tra ModelState ===
            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
                Console.WriteLine($"Validation errors: {JsonSerializer.Serialize(errors)}");
                return Page();
            }
            // === PHẦN 5: Gửi request cập nhật lên service ===
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

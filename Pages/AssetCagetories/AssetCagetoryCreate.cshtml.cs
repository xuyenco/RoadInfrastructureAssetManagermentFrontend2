using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Road_Infrastructure_Asset_Management.Model.Response;
using RoadInfrastructureAssetManagementFrontend.Interface;
using System.Text.Json;

namespace RoadInfrastructureAssetManagementFrontend.Pages.AssetCagetories
{
    public class AssetCagetoryCreateModel : PageModel
    {
        private readonly IAssetCagetoriesService _assetCagetoriesService;

        public AssetCagetoryCreateModel(IAssetCagetoriesService assetCagetoriesService)
        {
            _assetCagetoriesService = assetCagetoriesService;
        }

        [BindProperty]
        public AssetCagetoriesRequest AssetCategory { get; set; } = new AssetCagetoriesRequest();

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Bind thủ công từ Request.Form cho attributes
            var attributeNames = Request.Form.Keys.Where(k => k.StartsWith("attributes["));
            var tempAttributes = new List<AttributeDefinition>();
            if (attributeNames.Any())
            {
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
            }

            // Chuyển đổi attributes thành attributes_schema
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
                    var enumValues = attr.EnumValuesStr.Split(',').Select(v => v.Trim()).ToList();
                    prop["enum"] = enumValues;
                }

                properties[attr.Name] = prop;
                if (attr.IsRequired)
                {
                    requiredFields.Add(attr.Name);
                }
            }

            AssetCategory.attributes_schema = new Dictionary<string, object>
            {
                ["required"] = requiredFields,
                ["properties"] = properties
            };

            // In dữ liệu để kiểm tra
            var json = JsonSerializer.Serialize(AssetCategory, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine("Model trả ra là:\n" + json);

            if (!ModelState.IsValid)
            {
                return Page();
            }

            //var result = await _assetCagetoriesService.CreateAssetCagetoriesAsync(AssetCategory);

            //if (result != null)
            //{
            //    return RedirectToPage("/AssetCagetories/Index");
            //}

            //ModelState.AddModelError("", "Không thể tạo danh mục. Vui lòng thử lại.");
            //return Page();
            return RedirectToPage("/AssetCagetories/Index");
        }

        // Class tạm để bind attributes từ form
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

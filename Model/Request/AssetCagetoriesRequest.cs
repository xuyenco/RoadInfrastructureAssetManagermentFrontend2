using System.ComponentModel.DataAnnotations;

namespace Road_Infrastructure_Asset_Management.Model.Response
{
    public class AssetCagetoriesRequest
    {
        public string category_name { get; set; }
        public string geometry_type { get; set; }
        public string attribute_schema { get; set; } // JSON object
        public IFormFile sample_image { get; set; }
        public IFormFile icon { get; set; }
    }
}
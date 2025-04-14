using Road_Infrastructure_Asset_Management.Model.Response;

namespace Road_Infrastructure_Asset_Management.Model.ImageUpload
{
    public class AssetCagetoryImageModel
    {
        public string cagetory_name { get; set; }
        public string geometry_type { get; set; }
        public string attributes_schema { get; set; } // JSON object
        public string lifecycle_stages { get; set; }   // JSON array
        public IFormFile marker { get; set; }
    }
}

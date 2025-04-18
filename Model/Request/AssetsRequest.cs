using Newtonsoft.Json.Linq;
using RoadInfrastructureAssetManagementFrontend2.Model.Geometry;
using System.ComponentModel.DataAnnotations;

namespace RoadInfrastructureAssetManagementFrontend2.Model.Response
{
    public class AssetsRequest
    {
        [Required]
        public int category_id { get; set; }
        public string asset_name { get; set; }
        public string asset_code { get; set; }
        public string address { get; set; }
        [Required]
        public string geometry { get; set; }
        public DateTime? construction_year { get; set; }
        public DateTime? operation_year { get; set; }
        public double? land_area { get; set; }
        public double? floor_area { get; set; }
        public double? original_value { get; set; }
        public double? remaining_value { get; set; }
        public string asset_status { get; set; }
        public string installation_unit { get; set; }
        public string management_unit { get; set; }
        [Required]
        public string custom_attributes { get; set; }
        public IFormFile image { get; set; }
    }
}

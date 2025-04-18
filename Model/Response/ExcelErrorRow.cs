namespace RoadInfrastructureAssetManagementFrontend2.Model.Response
{
    public class ExcelErrorRow
    {
        public int RowNumber { get; set; }
        public string OriginalData { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}

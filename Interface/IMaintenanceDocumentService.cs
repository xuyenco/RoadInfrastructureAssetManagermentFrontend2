using RoadInfrastructureAssetManagementFrontend2.Model.Request;
using RoadInfrastructureAssetManagementFrontend2.Model.Response;

namespace RoadInfrastructureAssetManagementFrontend2.Interface
{
    public interface IMaintenanceDocumentService
    {
        Task<List<MaintenanceDocumentResponse>> GetAllMaintenanceDocuments();
        Task<List<MaintenanceDocumentResponse>> GetMaintenanceDocumentByMaintenanceId(int id);
        Task<MaintenanceDocumentResponse?> GetMaintenanceDocumentById(int id);
        Task<MaintenanceDocumentResponse?> CreateMaintenanceDocument(MaintenanceDocumentRequest entity);
        Task<MaintenanceDocumentResponse?> UpdateMaintenanceDocument(int id, MaintenanceDocumentRequest entity);
        Task<bool> DeleteMaintenanceDocument(int id);
        Task<bool> DeleteMaintenanceDocumentByMaintenanceId(int id);
    }
}

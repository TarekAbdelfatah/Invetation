using Ibtikar.ViewModels;

namespace Ibtikar.Repositories.Interfaces
{
    public interface IErpSyncRepository
    {
        Task<List<ErpEmployeeOption>> GetErpEmployeesAsync(CancellationToken ct = default);
        Task<List<ErpDepartmentOption>> GetErpHrDepartmentsAsync(CancellationToken ct = default);
    }
}

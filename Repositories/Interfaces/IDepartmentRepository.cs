using Ibtikar.ViewModels;

namespace Ibtikar.Repositories.Interfaces
{
    public interface IDepartmentRepository
    {
        Task<List<ErpDepartmentOption>> GetHrDepartmentsAsync(CancellationToken ct = default);
    }
}

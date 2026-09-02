using Ibtikar.ViewModels;

namespace Ibtikar.Repositories.Interfaces
{
    public interface IEmployeeRepository
    {
        Task<List<ErpEmployeeOption>> GetEmployeesAsync(CancellationToken ct = default);
    }
}

using Ibtikar.Models;
using Ibtikar.Services.Interfaces;

namespace Ibtikar.Services.Implementations
{
    /// <summary>
    /// Centralized OWASP A01 (Broken Access Control / IDOR) owner filter for the partner department inbox.
    /// Every partner dashboard read or write composes <see cref="ForDepartment"/> so a signed-in
    /// partner can only ever touch assignments routed to their own department.
    /// </summary>
    public sealed class PartnerAssignmentQuery : IPartnerAssignmentQuery
    {
        /// <summary>
        /// Restricts <paramref name="assignments"/> to rows where <c>PartnerDepartmentId == departmentId</c>.
        /// The controller must resolve <paramref name="departmentId"/> from the auth cookie via
        /// <see cref="Ibtikar.Services.Helpers.RoleCodes.DepartmentIdClaim"/> before composing this
        /// filter — never trust an id from the route or form.
        /// </summary>
        public IQueryable<PartnerAssignment> ForDepartment(IQueryable<PartnerAssignment> assignments, Guid departmentId)
        {
            if (departmentId == Guid.Empty)
            {
                return assignments.Where(a => false);
            }
            return assignments.Where(a => a.PartnerDepartmentId == departmentId);
        }
    }
}
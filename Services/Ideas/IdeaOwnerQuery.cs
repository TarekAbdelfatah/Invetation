using Ibtikar.Data;
using Ibtikar.Models;

namespace Ibtikar.Services.Ideas
{
    /// <summary>
    /// Centralized OWASP A01 (Broken Access Control / IDOR) owner filter for the applicant home.
    /// Every My Requests read or write composes <see cref="ForCurrentApplicant"/> so a signed-in
    /// applicant can only ever touch ideas they own.
    /// </summary>
    public sealed class IdeaOwnerQuery
    {
        private readonly IbtikarDbContext _db;

        public IdeaOwnerQuery(IbtikarDbContext db) => _db = db;

        /// <summary>
        /// Restricts <paramref name="ideas"/> to rows where <c>ApplicantUserId == applicantId</c>.
        /// The controller must resolve <paramref name="applicantId"/> from the auth cookie before
        /// composing this filter — never trust an id from the route or form.
        /// </summary>
        public IQueryable<InnovationIdea> ForCurrentApplicant(IQueryable<InnovationIdea> ideas, Guid applicantId)
        {
            return ideas.Where(i => i.ApplicantUserId == applicantId);
        }
    }
}
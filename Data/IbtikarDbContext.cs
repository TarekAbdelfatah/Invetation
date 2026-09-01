using Ibtikar.Models;
using Microsoft.EntityFrameworkCore;

namespace Ibtikar.Data
{
    // Ibtikar rule: all queries use EF LINQ. FromSqlRaw is forbidden
    // unless a later documented exception uses parameters only.
    // No string-concatenated SQL from request values.
    public class IbtikarDbContext : DbContext
    {
        public IbtikarDbContext(DbContextOptions<IbtikarDbContext> options)
            : base(options)
        {
        }

        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<User> Users => Set<User>();
        public DbSet<IdeaStatus> IdeaStatuses => Set<IdeaStatus>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<AssessmentCriterion> AssessmentCriteria => Set<AssessmentCriterion>();
        public DbSet<CriterionScoring> CriterionScorings => Set<CriterionScoring>();
        public DbSet<ExpectedImpact> ExpectedImpacts => Set<ExpectedImpact>();
        public DbSet<TargetAudience> TargetAudiences => Set<TargetAudience>();
        public DbSet<InnovationDomain> InnovationDomains => Set<InnovationDomain>();
        public DbSet<Technology> Technologies => Set<Technology>();
        public DbSet<ExecutionStage> ExecutionStages => Set<ExecutionStage>();
        public DbSet<ExecutionProgress> ExecutionProgresses => Set<ExecutionProgress>();
        public DbSet<InnovationIdea> InnovationIdeas => Set<InnovationIdea>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<IdeaAttachment> IdeaAttachments => Set<IdeaAttachment>();
        public DbSet<IdeaStatusHistory> IdeaStatusHistories => Set<IdeaStatusHistory>();
        public DbSet<AuditActionItem> AuditActionItems => Set<AuditActionItem>();
        public DbSet<PartnerAssignment> PartnerAssignments => Set<PartnerAssignment>();
        public DbSet<AssessmentHeader> AssessmentHeaders => Set<AssessmentHeader>();
        public DbSet<AssessmentDetail> AssessmentDetails => Set<AssessmentDetail>();
        public DbSet<InnovationCommittee> InnovationCommittees => Set<InnovationCommittee>();
        public DbSet<CommitteeMember> CommitteeMembers => Set<CommitteeMember>();
        public DbSet<CommitteeVote> CommitteeVotes => Set<CommitteeVote>();
        public DbSet<CommitteeDelegation> CommitteeDelegations => Set<CommitteeDelegation>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(IbtikarDbContext).Assembly);
        }
    }
}
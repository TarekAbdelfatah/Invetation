namespace Ibtikar.Services.Ideas
{
    public static class IdeaStatusCodes
    {
        public const string New = "new";
        public const string UnderReview = "under-review";
        public const string ReferredCommittee = "referred-committee";
        public const string UnderAssessment = "under-assessment";
        public const string Approved = "approved";
        public const string Rejected = "rejected";
        public const string Deferred = "deferred";
        public const string InExecution = "in-execution";
        public const string Completed = "completed";
        public const string Cancelled = "cancelled";

        public const string WaitingForCompletion = "waiting_for_completion";
        public const string ReturnedForDevelopment = "returned_for_development";
        public const string UnderStudy = "under_study";
        public const string Resubmitted = "resubmitted";
    }
}

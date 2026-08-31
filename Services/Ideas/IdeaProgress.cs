namespace Ibtikar.Services.Ideas
{
    /// <summary>
    /// Maps an idea's current status code to one of the five applicant-visible
    /// pipeline stages (submit → audit → assessment → committee → execution).
    /// Stays in the Ideas namespace so My Requests rows can show how far the
    /// idea has progressed without leaking staff-only status names.
    /// </summary>
    public static class IdeaProgress
    {
        public const int TotalStages = 5;

        private static readonly (string Code, int Stage, string Label)[] Map =
        {
            (IdeaStatusCodes.New,                1, "التقديم"),
            (IdeaStatusCodes.UnderReview,        2, "التدقيق"),
            (IdeaStatusCodes.WaitingForCompletion, 2, "التدقيق"),
            (IdeaStatusCodes.UnderAssessment,    3, "التقييم"),
            (IdeaStatusCodes.ReferredCommittee,  4, "اللجنة"),
            (IdeaStatusCodes.Approved,           4, "اللجنة"),
            (IdeaStatusCodes.Rejected,           4, "اللجنة"),
            (IdeaStatusCodes.ReturnedForDevelopment, 4, "اللجنة"),
            (IdeaStatusCodes.Deferred,           2, "التدقيق"),
            (IdeaStatusCodes.InExecution,        5, "التنفيذ"),
            (IdeaStatusCodes.Completed,          5, "التنفيذ"),
            (IdeaStatusCodes.Cancelled,          5, "التنفيذ"),
            (IdeaStatusCodes.UnderStudy,         2, "التدقيق"),
            (IdeaStatusCodes.Resubmitted,        2, "التدقيق"),
        };

        public static int StageFor(string? statusCode)
        {
            if (string.IsNullOrWhiteSpace(statusCode)) return 1;
            foreach (var entry in Map)
            {
                if (string.Equals(entry.Code, statusCode, StringComparison.OrdinalIgnoreCase))
                    return entry.Stage;
            }
            return 1;
        }

        public static string LabelFor(int stage) => stage switch
        {
            1 => "التقديم",
            2 => "التدقيق",
            3 => "التقييم",
            4 => "اللجنة",
            5 => "التنفيذ",
            _ => "التقديم"
        };

        public static int PercentFor(int stage) =>
            (int)Math.Round(100.0 * stage / TotalStages, MidpointRounding.AwayFromZero);
    }
}

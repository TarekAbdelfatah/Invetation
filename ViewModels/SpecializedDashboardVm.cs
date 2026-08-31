namespace Ibtikar.ViewModels
{
    public class SpecializedDashboardVm
    {
        public int UnderStudy { get; set; }
        public int SentToPartner { get; set; }
        public int SentToExecution { get; set; }
        public int RejectedAfterRouting { get; set; }
        public string? DepartmentName { get; set; }
    }
}
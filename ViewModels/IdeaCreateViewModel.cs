using System.ComponentModel.DataAnnotations;

namespace Ibtikar.ViewModels
{
    public class IdeaCreateViewModel
    {
        public const int TitleMax = 200;
        public const int DescriptionMax = 3000;
        public const int ProblemMax = 3000;
        public const int SolutionMax = 300;
        public const int BenefitsMax = 2000;
        public const int OtherImpactMax = 200;
        public const int OtherAudienceMax = 200;
        public const int OtherTechMax = 200;

        [Required(ErrorMessage = "العنوان مطلوب")]
        [StringLength(TitleMax, MinimumLength = 5, ErrorMessage = "العنوان بين 5 و 200 حرف")]
        [Display(Name = "عنوان الفكرة")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "الملخص مطلوب")]
        [StringLength(DescriptionMax, MinimumLength = 30, ErrorMessage = "الملخص بين 30 و 3000 حرف")]
        [Display(Name = "الملخص")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; } = string.Empty;

        [StringLength(ProblemMax, ErrorMessage = "التحديات حتى 3000 حرف")]
        [Display(Name = "التحديات الحالية")]
        [DataType(DataType.MultilineText)]
        public string? ProblemStatement { get; set; }

        [StringLength(SolutionMax, ErrorMessage = "الحل المقترح حتى 300 حرف")]
        [Display(Name = "الحل المقترح")]
        [DataType(DataType.MultilineText)]
        public string? ProposedSolution { get; set; }

        [StringLength(BenefitsMax, ErrorMessage = "الفوائد حتى 2000 حرف")]
        [Display(Name = "الفوائد المتوقعة")]
        [DataType(DataType.MultilineText)]
        public string? ExpectedBenefits { get; set; }

        [Required(ErrorMessage = "مجال الابتكار مطلوب")]
        [Display(Name = "مجال الابتكار")]
        public Guid InnovationDomainId { get; set; }

        [Display(Name = "الأثر المتوقع")]
        public Guid? ExpectedImpactId { get; set; }

        [StringLength(OtherImpactMax, ErrorMessage = "حتى 200 حرف")]
        [Display(Name = "حدد الأثر الآخر")]
        public string? ExpectedImpactOther { get; set; }

        [Display(Name = "الفئة المستهدفة")]
        public Guid? TargetAudienceId { get; set; }

        [StringLength(OtherAudienceMax, ErrorMessage = "حتى 200 حرف")]
        [Display(Name = "حدد الفئة الأخرى")]
        public string? TargetAudienceOther { get; set; }

        [Display(Name = "استخدام تقنيات ناشئة")]
        public bool UsesEmergingTech { get; set; }

        [Display(Name = "التقنيات الناشئة")]
        public List<Guid> TechnologyIds { get; set; } = new();

        [StringLength(OtherTechMax, ErrorMessage = "حتى 200 حرف")]
        [Display(Name = "حدد التقنية الأخرى")]
        public string? TechnologyOther { get; set; }

        public Guid? CurrentDraftId { get; set; }
    }
}

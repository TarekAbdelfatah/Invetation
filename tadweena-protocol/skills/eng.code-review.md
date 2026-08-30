---
name: eng.code-review
id: eng.code-review
layer: engineering
gate: before_finish
---

# eng.code-review — ابتكار

## HOW
راجع الـ diff الفعلي مش العنوان. بالترتيب:
1. **صحة**: التغيير يعمل المطلوب فقط؟
2. **رجوع (regression)**: المتصلين والمسارات الجانبية.
3. **مسارات الخطأ**: `ModelState` جمع كل الأخطاء vs استثناء مبلوع.
4. **أمان**: صلاحيات/ملكية/أسرار.
5. **اختبار**: فرع جديد أو waive موثق.

ممنوع `LGTM`/`n/a` الفارغة — كل finding حاجة ملموسة (≥40 حرف).

## WHEN
Required لـ public API أو نطاق ملفات واسع أو hotspot. `filesReviewed` يتقاطع مع ملفات التيك.

## EVIDENCE
`satisfied` يتطلب findings ملموسة + `resolved=true` + `commitHash` (نفس gitHash بتاع finish_task). غير محلول → `status=waived` + `waiveReason`. ممنوع `satisfied` مع `resolved=false`.

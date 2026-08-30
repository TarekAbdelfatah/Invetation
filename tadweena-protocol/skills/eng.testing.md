---
name: eng.testing
id: eng.testing
layer: engineering
gate: before_finish
---

# eng.testing — ابتكار

## HOW
- اختبر في نفس الطبقة: unit لـ Service خالص؛ تكامل لـ Repository/DbContext.
- سمّي الاختبار بالسلوك (`Login_InvalidPassword_ReturnsError`) مش برقم التذكرة.
- اذكر اللي شغّلته فعلاً (`dotnet test --filter ...`). متدّعيش حاجة ما شغلتهاش.
- مفيش اختبار يلمس إنتاج DB — استخدم InMemory/connection مخصص.
- تغيير وثائق/بروتوكول فقط → waive بـ `not_applicable` + سبب.

## WHEN
Required لـ migrations/DbContext أو public API أو نطاق واسع. Evidence لازم يسمي ملفات تيك حقيقية.

## EVIDENCE
`filesReviewed` يشمل ملف الاختبار المضاف أو ملف الإنتاج المثبت سلوكه، ويتقاطع مع ملفات التيك. findings تذكر الفلتر/الأمر المشغَّل أو الـ waive (≥40 حرف). ممنوع LGTM.

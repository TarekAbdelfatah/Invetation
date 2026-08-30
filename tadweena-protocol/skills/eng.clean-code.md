---
name: eng.clean-code
id: eng.clean-code
layer: engineering
gate: before_write
---

# eng.clean-code — ابتكار

## HOW
- الميثود ≤ **15 سطر** ويعمل حاجة واحدة؛ لو زاد → استخرج helper باسم واضح.
- الأسماء تعبّر عن النية: `ValidateEmailFormat` مش `Check`.
- **تحقق مركّب لا inline**: العامة تقرأ كقصة، وكل فحص دالة صغيرة.
- **متوقع (مدخلات)**: اجمع كل الأخطاء في `ModelState` وارجع مرة — مش throw لكل حقل.
- **غير متوقع (null/IO)**: سيبها توصل لـ `ExceptionMiddleware` — مفيش try/catch متناثر في البيزنس.
- قبل الكتابة: افتح الملف + جار من نفس النوع، وسمّي الميثودات الأول.

## WHEN
Optional على أغلب تيكات الكود (`before_write`). ليس gate لـ finish_task في V1. يُحمَّل لما `SKILL_CONTRACT` يذكر هذا الـ id.

## EVIDENCE
`filesReviewed` = الملفات اللي فتحتها فعلاً، وfindings لازم تسمي تغيير نظافة ملموس (extract/rename/compose) — مش "looks clean".

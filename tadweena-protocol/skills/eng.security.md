---
name: eng.security
id: eng.security
layer: engineering
gate: before_finish
---

# eng.security — ابتكار

## HOW
1. **authn ≠ authz**: تحقق من `[Authorize(Roles)]` + ملكية السجل (`ApplicantId = currentUser`).
2. **Trust boundary**: متثقش في id/role القادم من العميل.
3. **أسرار**: مفيش connection string/password في log أو commit أو View.
4. **Injection**: EF parameterized؛ ارفض `../` والمسارات المطلقة في الملفات.
5. **Crypto**: PBKDF2 لكلمة المرور؛ متخترعش تشفير خاص.
6. **Least data**: مفيش PII أو stack trace في الردود.

متقلش "looks fine" — سجّل residual risk أو waive موثق.

## WHEN
Required لما تكون ملفات التيك على مسارات Auth/Security/Crypto/Permission/Identity. عالي الخطورة: `quick_complete` ميقدرش يتجاوز المهارة دي.

## EVIDENCE
`satisfied` يتطلب `filesReviewed` يتقاطع مع المسارات دي + ≥1 finding (≥40 حرف) يسمي control أو residual risk + `resolved=true`. غير قابل للتحقق → `waive` بـ `not_applicable`/`out_of_scope`.

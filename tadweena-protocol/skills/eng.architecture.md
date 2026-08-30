---
name: eng.architecture
id: eng.architecture
layer: engineering
gate: before_finish
---

# eng.architecture — ابتكار

## HOW
التدفق (مشروع واحد بفولدات):

```
Controller → Service → Repository → Data (EF/DbContext) → Models/DTOs
```

- **Controller** رفيع: يربط/يتحقق من `ModelState`/يرجع View. المنطق في Service.
- **Service**: منطق الأعمال والحسابات والتحقق — Server-Side فقط.
- **Repository**: الوصول للبيانات عبر EF فقط؛ مفيش SQL في Controller/View.
- **ممنوع مشروع جديد** — الطبقات فولدات: `Models`, `DTOs`, `Repositories`, `Services`, `Data`.
- **ممنوع منطق أعمال في View** — عرض + Partial Views فقط.
- **UI**: Bootstrap 5 RTL (CSS فقط) — ممنوع jQuery/Bootstrap JS (Vanilla JS للضرورات).

## WHEN
يُرقّى لـ required لما تكون إشارات AST شغالة والتيك كبير أو hotspot. لو الـ daemon واقف، المهارة مش بتترقّى؛ graph فاضي ≠ صفر مخاطرة.

## EVIDENCE
findings لازم تسمي الـ boundary اللي حافظت عليه أو الـ coupling اللي رفضته (≥40 حرف). `filesReviewed` يتقاطع مع ملفات التيك.

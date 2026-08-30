---
name: lang.csharp
id: lang.csharp
layer: language
gate: before_finish
---

# lang.csharp — ابتكار

## HOW
- `PascalCase` للأنواع/الأعضاء العامة/كل الميثودات؛ `camelCase` للبارامترات واللوكل؛ `_camelCase` للحقول الخاصة.
- كلمات اللغة (`string`, `int`) مش `String`, `Int32`.
- `async/await` للإدخال/الإخراج؛ مفيش `.Result`/`.Wait()`.
- `catch` لنوع محدد تقدر تتعامل معاه — مفيش `catch (Exception)` عام في البيزنس.
- `var` بس لما النوع واضح.
- الميثود ≤ 15 سطر.
- **تحقق مركّب**: public يدخل composer واحد، كل فحص دالة مسماة، اجمع الأخطاء في `ValidationResult`.

## WHEN
Required على التيكات اللي ملفاتها فيها `.cs` إلا لو الـ 3-required cap نزّلها.

## EVIDENCE
`filesReviewed` يتقاطع مع ملفات `.cs` للتيك. findings تسمي اصطلاح/قاعدة بيت طُبّقت (naming/extract/collect-all/no-swallow) (≥40 حرف). `commitHash` يطابق `gitHash` بتاع finish_task.

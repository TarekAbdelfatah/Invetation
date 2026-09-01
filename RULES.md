# قواعد تطبيق ابتكار

## ١. تحديد المستفيد الداخلي / الخارجي

- **المصدر الوحيد**: `User.DepartmentId` (وما يعادله على مستوى الـ Claims: `ibtikar_department_id`).
  - `DepartmentId != null` ⇒ داخلي.
  - `DepartmentId == null` ⇒ خارجي.
- **ممنوع** التفرع على:
  - اسم الدور (`internal-beneficiary` / `external-beneficiary`).
  - `InnovationIdea.ApplicantDepartmentId` لتحديد نوع المستخدم في كود الـ runtime — هذا الحقل يخزن نتيجة الفرع، لا يُستخدم لتحديده.
- **الدور** يُستخدم فقط لـ:
  - `[Authorize(Roles = ...)]`.
  - `RoleCodes.HomeRedirects` لتحديد الصفحة الرئيسية بعد الدخول.
- **الكود المعتمد للتفريع**:
  ```csharp
  if (BeneficiaryType.IsInternal(User)) { /* ... */ }
  ```
  مع `using Ibtikar.Services.Helpers;`.

### سيناريوهات البيانات

| نوع المستخدم | `DepartmentId` | الدور | `Idea.ApplicantDepartmentId` |
|---|---|---|---|
| داخلي | قيمة حقيقية (`judicial`, `tech`, ...) | `internal-beneficiary` | نفس قيمة `User.DepartmentId` |
| خارجي | `null` | `external-beneficiary` | `null` |

### إزالة UserTypeLookup

- جدول `UserTypes` وعمود `Users.UserTypeId` و الـ navigation `User.UserType` **غير موجودين**.
- الـ migration `RemoveUserTypeLookup` يحذفها نهائياً.
- لا تضف منطقاً يعتمد على UserTypeLookup.Code أو UserTypeId — الكود سيكسر.

## ٢. المرفقات (Attachments)

- **النوع المسموح**: PDF فقط (`%PDF` magic bytes validated).
- **الحجم الأقصى**: 5 ميجا لكل ملف (`AttachmentMaxBytes`).
- **العدد الأقصى**: ملفان لكل فكرة (`AttachmentMaxCount`).
- **التخزين**:
  - الفكرة الفعلية: `App_Data/attachments/{ideaId-N}/{fileGuid}.pdf`.
  - المسودة (قبل اعتماد الفكرة): `App_Data/attachments/_drafts/{userId-N}/{draftId-N}/{fileGuid}.pdf`.
- **الحذف**:
  - **ممنوع نهائياً** على الأفكار في حالات: `Approved`, `Rejected`, `InExecution`, `Completed`, `Cancelled`, `UnderStudy`, `UnderReview`, `UnderAssessment`, `ReferredCommittee`, `Deferred`, `Resubmitted`.
  - **مسموح** على: المسودات (`IsDraft = true`) + `WaitingForCompletion` + `ReturnedForDevelopment` فقط.
  - التحقق في `AttachmentService.DeleteForApplicantAsync`.

## ٣. مسارات الرفع (Upload Endpoints)

- `POST /api/Attachment/upload` — للأفكار الموجودة (يحتاج `ideaId`).
- `POST /api/Attachment/uploadDraft?draftId={id}` — للمسودات (قبل اعتماد الفكرة).
- `GET  /api/Attachment/list?ideaId={id}` — قائمة مرفقات فكرة.
- `GET  /api/Attachment/listDraft?draftId={id}` — قائمة مرفقات مسودة.
- `POST /api/Attachment/deleteDraft` — حذف ملف من مجلد المسودة (المسودة فقط).
- `AttachmentController` يفحص ملكية الـ idea/draft عبر `UserOwnsIdeaAsync` أو `userId == draftOwner`.

## ٤. حالات إعادة التقديم (Resubmit)

- `ResubmitCompletion` — مسموح فقط عندما `StatusCode == WaitingForCompletion`.
- `ResubmitDeveloped` — مسموح فقط عندما `StatusCode == ReturnedForDevelopment`.
- كلتا الحالتين تسمحان برفع مرفقات جديدة (نفس widget) إلى جانب النص المُعدَّل.

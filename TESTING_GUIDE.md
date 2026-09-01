# دليل اختبار نظام ابتكار (Ibtikar) — Cycle التحقق

> **اسم النظام:** ابتكار (Ibtikar) — منصة استقبال الأفكار الابتكارية لمكتب المظالم
> **نوع التطبيق:** ASP.NET Core MVC + EF Core + PostgreSQL + Bootstrap 5 RTL
> **اللغة:** عربي (RTL) — `ar-SA`
> **عدد الفيتشرز المنجزة:** 9 فيتشرز (133/133 مهمة done)
> **عدد المستخدمين (الأدوار):** 6 أنواع
> **حالة الـ Committee:** ⚠️ فيتشر لجنة الابتكار به **24 مهمة pending** — يغطّي هذا الدليل ما هو **منجز فقط**
> **⚠️ تحديث مهم (دورة الدمج):** لوحة "الإدارة الشريكة" (`/PartnerDashboard`) وُلجت في لوحة "الإدارة المختصة" (`/SpecializedDashboard`) لأن الإدارة الواحدة تستقبل الأفكار المحوّلة إليها تخصصياً + استشارات واردة من إدارات أخرى. كل دور يُحوَّل إلى `/SpecializedDashboard` بعد الدخول. هذا الدليل يوثّق السلوك الموحَّد.

---

## 0. كيف تستخدم هذا الدليل

كل مختبر يستلم **دوره فقط** (Role-specific section) ويُنفّذ الـ Cycle خطوة خطوة.

| الرمز | المعنى |
|---|---|
| ✅ Pass | النتيجة مطابقة للسلوك المتوقع |
| ❌ Fail | خطأ فعلي — يُسجَّل في جدول الـ Defects |
| ⚠️ Blocked | لا يمكن الاستمرار (تعتمد على مختبر آخر) |
| N/A | لا ينطبق على هذا المختبر |

**قواعد عامة:**
1. **لا تعدّل بيانات الـ Seed** (المستخدمين السبعة، الأقسام، الحالات، المعايير).
2. سجّل الـ Reference Number لكل فكرة تنشئها — ستحتاجه في أدوار أخرى.
3. CSRF مفعّل تلقائياً على كل POST (Login، Submit، Assess، إلخ) — لا تُعطّل الـ Token.
4. ملفات PDF فقط، حد أقصى 2 ملف × 5MB، تُفحص فعلياً بـ signature (لا تخطّيها).
5. الأدوار لا تتشارك جلسات — افتح **متصفح خفي** (Incognito) لكل دور أو **سجّل خروج** قبل تبديل الدور.
6. الأخطاء (404 / 500 / Exception) تُسجَّل في جدول `AuditLog` — لا تتجاهلها.

---

## 1. بيانات الدخول المُسبقة الـ Seed (لكل الأدوار الستة)

كلمة المرور الموحّدة لجميع المستخدمين: حسب الـ Seed (يُسلَّم للمختبرين بشكل منفصل — **راجع ملف `appsettings.Development.json` أو الـ README**).

| # | اسم المستخدم (المتوقّع) | الدور / الشاشة | ينتقل بعد الدخول إلى |
|---|---|---|---|
| 1 | `beneficiary@ibtikar.local` (داخلي) أو `ext-user@ibtikar.local` (خارجي) | External beneficiary | `/MyRequests` |
| 2 | `audit@ibtikar.local` | Audit employee | `/AuditInbox` |
| 3 | `committee@ibtikar.local` | Innovation committee member | `/Committee` *(قد يكون فارغاً — راجع §6)* |
| 4 | `specialized-ai@ibtikar.local` | Specialized department (مثال: AI — judicial) | `/SpecializedDashboard` (لوحة موحّدة) |
| 5 | `system-manager@ibtikar.local` | System manager | `/Reports` |
| 6 | `partner@ibtikar.local` *(Username للتوضيح فقط — الإدارة الفعلية: تقنية/tech)* | Partner department *(= نفس الإدارة — لوحة موحّدة)* | `/SpecializedDashboard` (لوحة موحّدة) |

> ⚠️ الأسماء أعلاه **متوقّعة** بناءً على وصف الـ Seed. على المختبر **التحقق من قائمة المستخدمين الفعلية** عبر شاشة Login (بعد الخطأ تظهر اسماء أدوار بدون ذكر كلمة سر) أو من خلال `psql -d ibtikar -c 'SELECT u.username, r.code FROM users u JOIN user_roles ur ON ur.user_id=u.id JOIN roles r ON r.id=ur.role_id;'`.

---

## 2. الـ Cycle الكاملة لكل مستخدم

### 👤 المستخدم 1: External Beneficiary (مستفيد / موظف — خارجي أو داخلي)

**الـ Scope:** إنشاء فكرة + متابعة الطلبات + حذف/تعديل في الحالات المسموحة.

#### 2.1 Login Test
| # | الخطوة | المتوقع |
|---|---|---|
| 1.1 | افتح `/Account/Login`، أدخل اسم المستخدم وكلمة السر الصحيحة | الانتقال إلى `/MyRequests` |
| 1.2 | أدخل كلمة سر خاطئة 5 مرات متتالية | Rate limiting — رسالة "تم تجاوز عدد المحاولات" |
| 1.3 | افتح أي URL محمي (مثل `/Ideas/Create`) بدون تسجيل دخول | إعادة التوجيه إلى `/Account/Login` |
| 1.4 | بعد تسجيل الدخول، انتظر 20 دقيقة بدون حركة | تسجيل خروج تلقائي |

#### 2.2 Empty State
| # | الخطوة | المتوقع |
|---|---|---|
| 2.1 | كمستخدم جديد بـ 0 أفكار، افتح `/MyRequests` | رسالة "لم تقم بتقديم أي أفكار حتى الآن" + زر **ابتكر الآن** |
| 2.2 | اضغط الزر | الانتقال إلى `/Ideas/Create` |

#### 2.3 Submit Idea — Happy Path
| # | الخطوة | المتوقع |
|---|---|---|
| 3.1 | افتح `/Ideas/Create` | نموذج كامل: عنوان، ملخص، تحديات، حل مقترح، التصنيف (InnovationDomain)، التأثير المتوقع، الجمهور، التقنية، Other، المرفقات |
| 3.2 | اختر `Other` في التأثير/الجمهور/التقنية **بدون** كتابة نص بديل | Validation error: "يرجى تحديد قيمة Other" |
| 3.3 | أدخل Summary بطول 3001 حرف | Validation: "الملخص يجب ألا يتجاوز 3000 حرف" |
| 3.4 | حاول رفع ملف `.exe` أو `.zip` | رفض — PDF فقط |
| 3.5 | حاول رفع ملف PDF أكبر من 5MB | رفض — رسالة حجم |
| 3.6 | حاول رفع ملف نصي بامتداد `.pdf` (fake PDF) | رفض — فحص التوقيع الحقيقي |
| 3.7 | ارفع ملفَين PDF صالحَين، أكمل الحقول المطلوبة، اضغط **Submit** | الحالة → `New`، ظهور Reference Number (مثال: `IBT-2026-0001`)، كتابة صف في `IdeaStatusHistory` |
| 3.8 | حاول فتح `/Ideas/Create` بـ GET ثم POST مباشرة | CSRF token مطلوب — يُرفض بدونه |
| 3.9 | سجّل الخروج، حاول الـ POST عبر أداة (curl) | مرفوض (AntiForgery + Auth) |

#### 2.4 My Requests List
| # | الخطوة | المتوقع |
|---|---|---|
| 4.1 | افتح `/MyRequests` | جدول: Reference، Title، Date، Status Badge، Progress Bar (5 مراحل)، الأحدث أولاً |
| 4.2 | اضغط على فكرة في الحالة `New` | تفاصيل: read-only للـ original fields والـ attachments + زر **Delete** |
| 4.3 | اضغط Delete في الحالة `New` | تأكيد modal، ثم حذف فعلي |
| 4.4 | حاول الوصول لـ `/MyRequests/Details/{id}` بمعرّف فكرة **موظف آخر** | 404 — IDOR محمي |
| 4.5 | بعد التدقيق يقبل الفكرة (`Under Study`) → عد للـ My Requests | Progress Bar يتقدّم |

#### 2.5 Resubmit (Rejected / Returned)
| # | الخطوة | المتوقع |
|---|---|---|
| 5.1 | عندما تكون الحالة `Returned for development` أو `Returned for completion`، افتح التفاصيل | نموذج كامل لإعادة التقديم + نقاط التطوير (development points) |
| 5.2 | اضغط Submit بدون إدخال نقاط التطوير | رفض — "يرجى إدخال نقاط التطوير" |
| 5.3 | اضغط Submit مرتين بسرعة (double click) | الزر معطّل بعد الضغطة الأولى (disable-on-click) |
| 5.4 | أكمل البيانات وأرسل | ينتقل إلى الحالة التالية، يكتب History |

#### 2.6 Timeout Closed
| # | الخطوة | المتوقع |
|---|---|---|
| 6.1 | فكرة في حالة `Waiting for completion` أو `Waiting for development`، مرّ عليها 14 يوم | Hosted service يُغلقها آلياً → الحالة `Closed` |
| 6.2 | افتح الفكرة `Closed` | رسالة "تم إغلاق الفكرة" + إخفاء أزرار Edit |

#### 2.7 Out of Scope (Boundary Tests)
- ❌ لا يستطيع التعديل على حقول الفكرة الأصلية بعد الـ Submit (Read-only).
- ❌ لا يرى أسماء الموظفين أو تواريخ الإجراءات الداخلية (Out of scope per SRS).
- ❌ لا يصل لـ `/AuditInbox` أو `/SpecializedDashboard` (Authorize يرفضه).

---

### 👤 المستخدم 2: Audit Employee (موظف التدقيق)

**الـ Scope:** الـ Inbox + القبول/الرفض/طلب استكمال + التوجيه لإدارة متخصصة.

#### 2.8 Login & Empty Inbox
| # | الخطوة | المتوقع |
|---|---|---|
| 8.1 | Login كـ `audit@ibtikar.local` | ينتقل إلى `/AuditInbox` |
| 8.2 | inbox فارغ | رسالة "لا توجد أفكار جديدة" |

#### 2.9 Inbox Filter
| # | الخطوة | المتوقع |
|---|---|---|
| 9.1 | موظف التدقيق يفتح الـ Inbox | الصفوف الافتراضية = `New` و `Resubmitted` فقط |
| 9.2 | استخدم فلتر البحث (نص/تاريخ/نوع) | النتائج تتقلّص بدون إعادة تحميل الصفحة (أو reload صحيح) |
| 9.3 | افتح فكرة من النوع `New` | ثلاث أزرار: **Accept** / **Reject** / **Request Missing Data** |

#### 2.10 Accept
| # | الخطوة | المتوقع |
|---|---|---|
| 10.1 | افتح فكرة، اضغط Accept، أكّد | الحالة → `Under Study`، كتابة History، انتقال الفكرة لـ Specialized department المختار في نموذج التوجيه |

#### 2.11 Reject
| # | الخطوة | المتوقع |
|---|---|---|
| 11.1 | اضغط Reject، أدخل سبب (إلزامي)، أكّد | الحالة → `Rejected`، يظهر للـ applicant كـ read-only مع السبب فقط |

#### 2.12 Request Missing Data
| # | الخطوة | المتوقع |
|---|---|---|
| 12.1 | اضغط Request Missing Data، أدخل ملاحظات | الحالة → `Returned for completion`، يظهر للـ applicant كإشعار لإعادة التقديم |

#### 2.13 IDOR / Scope Tests
| # | الخطوة | المتوقع |
|---|---|---|
| 13.1 | حاول فتح `/AuditInbox/Details/{id}` بمعرّف فكرة **لم تُحال لقسمه** | 404 |
| 13.2 | حاول POST لـ Action بدون CSRF token | مرفوض |
| 13.3 | حاول POST لـ Action كمستخدم `beneficiary` | 403 / 404 |

#### 2.14 Out of Scope
- ❌ لا يعدّل فكرة بعد القبول (Read-only).
- ❌ لا يصل لـ `/Committee` أو `/SpecializedDashboard`.

---

### 👤 المستخدم 3: Innovation Committee Member (عضو لجنة الابتكار)

**⚠️ حالة الفيتشر:** الفيتشر به **24 مهمة pending** — الـ Cycle الكامل غير متاح بعد.
**ما يمكن اختباره الآن:**

#### 3.1 Login فقط
| # | الخطوة | المتوقع |
|---|---|---|
| 14.1 | Login كـ `committee@ibtikar.local` | ينتقل إلى `/Committee` (قد يكون فارغاً أو مع لوحة غير مكتملة) |
| 14.2 | حاول فتح `/Committee/Details/{id}` | إذا كانت الـ action موجودة: يفتح بشكل read-only أو 404 |
| 14.3 | حاول POST لـ أي action تقييم | مرفوض (404 / 405 / Not Implemented) |

**🛑 لا تكمل باقي الـ Cycle الآن.** سجّل النتيجة كـ "Pending Feature" في الـ Sign-off.

#### 3.2 عند توفر الفيتشر لاحقاً، الـ Cycle الكامل المتوقّع:
1. استلام فكرة من Specialized department.
2. تقييم 5 معايير (نفس معايير Specialized، 1-5).
3. التصويت (موافق/غير موافق/امتناع).
4. إذا كان النصاب مكتمل: القرار → قبول/رفض/طلب تعديل.
5. في حالة القبول → التوجيه لـ Execution (5 مراحل تنفيذ).

---

### 👤 المستخدم 4: Specialized Department (إدارة متخصصة — مثال: AI)

**الـ Scope:** لوحة موحّدة (تخصص + استشارات) + التقييم بـ 5 معايير + طلب رأي جهات أخرى + الإرسال للجنة.

> ℹ️ **دمج مهم:** هذه اللوحة هي نفسها المستخدمة من قبل "الإدارة الشريكة" (§6). الإدارة الواحدة تستقبل **أفكاراً محوّلة تخصصياً** (تقيّمها) و**استشارات واردة من إدارات أخرى** (تبيّن رأيها). كل شيء في صفحة واحدة.

#### 4.1 Dashboard (اللوحة الموحّدة)
| # | الخطوة | المتوقع |
|---|---|---|
| 15.1 | Login كـ `specialized-ai@ibtikar.local` | ينتقل إلى `/SpecializedDashboard` |
| 15.2 | افحص العنوان | "لوحة {DepartmentName}" (مثال: "لوحة الشؤون القضائية") بأيقونة engineering |
| 15.3 | افحص قسم "الأفكار المحوّلة لإدارتك" — KPI cards الأربعة | قيد الدراسة / أُرسل لجهات أخرى / أُرسلت للتنفيذ / مرفوضة بعد التحويل (تستبعد رفض التدقيق) |
| 15.4 | كل KPI card في هذا القسم هو link يستدعي `/SpecializedDashboard/Referrals?status=...` | التصفية تفتح الـ Referrals بالحالة الصحيحة |
| 15.5 | افحص قسم "الاستشارات الواردة" — KPI cards الثلاثة | استشارات بانتظار الرد / استشارات متأخرة (تجاوزت 4 أيام) / تم الرد خلال 30 يوم |
| 15.6 | افحص جدول الـ inbox في قسم الاستشارات | يعرض فقط `PartnerAssignment` لإدارتي — IDOR-safe (`PartnerAssignmentQuery.ForDepartment`) |
| 15.7 | افحص جدول الإحالات عبر "كل الأفكار المحوّلة" | يعرض فقط أفكار القسم الحالي + stay duration (الأيام منذ الإحالة) |
| 15.8 | طبّق فلتر (status / applicant-type) في Referrals | النتائج تتقلّص |
| 15.9 | تحقّق من اسم الإدارة في الـ hero | الاسم الفعلي (مثال: "الشؤون القضائية") وليس "الإدارة المختصة" أو "الإدارة الشريكة" |

#### 4.2 IDOR / Scope
| # | الخطوة | المتوقع |
|---|---|---|
| 16.1 | حاول فتح Details لفكرة **محالة لقسم آخر** (كمستخدم `specialized-it` مثلاً) | 404 — فلتر `AssignedDepartmentId` يطبّق على الخادم |
| 16.2 | حاول POST كـ `partner-dept` | 403 |
| 16.3 | تحقّق من استشارة واردة من إدارة أخرى (في قسم "الاستشارات الواردة") — اضغط مراجعة | يجب أن يعرض التفاصيل مع `partner-department` أو `specialized-department` كلاهما مقبول (لأنهما نفس الإدارة) |

#### 4.3 Assess — 5 Criteria
| # | الخطوة | المتوقع |
|---|---|---|
| 17.1 | افتح فكرة محالة، اضغط Assess | نموذج 5 معايير: dropdown 1-5 لكل معيار، Total/Percent live update |
| 17.2 | غيّر قيمة معيار | الـ Total يتحدث فوراً (JS calc)، النسبة المئوية = sum/25 * 100 |
| 17.3 | اضغط Save as Draft بدون إكمال كل المعايير | يحفظ Draft، يبقى في نفس الحالة |
| 17.4 | حاول الإرسال للجنة بدون إكمال كل الـ 5 معايير | **مرفوض** بالضبط برسالة: `يرجى استكمال تقييم كافة المعايير قبل اتخاذ إجراء الإرسال`، الـ criterion الفارغ يظهر بإطار أحمر |

#### 4.4 Request Partner Opinions
| # | الخطوة | المتوقع |
|---|---|---|
| 18.1 | بعد إكمال التقييم، اضغط Request Partner | قائمة بالإدارات (تستبعد القسم الحالي وأي شريك تم تكليفه سابقاً) |
| 18.2 | اختر إدارة شريكة → Submit | إنشاء صف `PartnerAssignment` بـ `Status=Pending` |

#### 4.5 Follow-up Partner Replies
| # | الخطوة | المتوقع |
|---|---|---|
| 19.1 | انتظر (أو simulate) 4 أيام على PartnerAssignment | الحالة → `Late`، يظهر عمود LateNote |
| 19.2 | الشريك يرد (دوره في §6) | الحالة → `Submitted`، تظهر آراء الشريك |

#### 4.6 Re-enable Scores After Partner Opinions
| # | الخطوة | المتوقع |
|---|---|---|
| 20.1 | بعد رد الشريك، ارجع لـ Assess | النموذج يفتح للتعديل مرة أخرى |
| 20.2 | عدّل، اضغط Submit | يُحفظ، يقفل |

#### 4.7 Send to Committee
| # | الخطوة | المتوقع |
|---|---|---|
| 21.1 | اضغط Send to Committee | Modal تأكيد، ثم تحقّق من 5 معايير + شرط عدم وجود partners في `Pending` بدون skip صريح |
| 21.2 | إذا كان في partners لم يردّوا | تحذير مع cancel / skip-and-send |
| 21.3 | اضغط skip-and-send | يُرسل، يكتب History، يُعلِم (notification stub) |

#### 4.8 Out of Scope
- ❌ لا يعدّل الفكرة بعد الإرسال للجنة (Read-only).
- ❌ لا يصل لـ `/AuditInbox` أو `/Committee` كـ writer.

---

### 👤 المستخدم 5: System Manager (مدير النظام)

**⚠️ الفيتشر قد يكون جزئياً أو غير مكتمل — راجع حالة الـ 24 pending task.**

#### 5.1 Login & Read-Only View
| # | الخطوة | المتوقع |
|---|---|---|
| 22.1 | Login كـ `system-manager@ibtikar.local` | ينتقل إلى `/Reports` (أو لوحة overview) |
| 22.2 | افحص KPIs / Reports | شامل كل الأقسام، read-only |
| 22.3 | من/إلى تاريخ → Generate | تقرير بنفس الفترة: KPI + stage-mix totals، فارغ وممتلئ |

#### 5.2 Committee Administration
| # | الخطوة | المتوقع |
|---|---|---|
| 23.1 | نموذج تشكيل لجنة: head + ≥1 member | إنشاء، حالة `Active`، يُرسل إشعار لكل عضو |
| 23.2 | محاولة POST بدون إكمال الأعضاء | رفض |
| 23.3 | محاولة تعديل/حذف committee في حالة `Active` و عليها أعمال جارية | تحذير أو رفض |

#### 5.3 Admin / Lookups
| # | الخطوة | المتوقع |
|---|---|---|
| 24.1 | افتح `/Admin/Lookups` | CRUD لكل lookup table (IdeaStatus, InnovationDomain, ExpectedImpact, TargetAudience, Technology, ExecutionStage, AssessmentCriterion, CriterionScoring, Role, Department) |
| 24.2 | عدّل اسم حالة فكرة موجودة | ❌ ممنوع (لا يجب تغيير أسماء الـ Seed لأنها مرجع الـ workflow) |

#### 5.4 Out of Scope
- ❌ لا يقيّم أفكار (Read-only).
- ❌ لا يعمل workflow (لا يقبل/يرفض).

---

### 👤 المستخدم 6: Partner Department (إدارة شريكة / استشاري — **نفس الإدارة المختصة**)

**الـ Scope:** اللوحة الموحّدة (§4) + تقييم استشاري بـ 5 معايير + إرجاع للتخصصية + إرجاع لعدم الاختصاص خلال 3 أيام عمل.

> ⚠️ **مهم جداً (دمج):** "الإدارة الشريكة" و"الإدارة المختصة" هما **نفس الإدارة** فعلاً. الفرق:
> - "مختصة" = تستقبل الأفكار المحوّلة إليها تخصصياً (لتقييمها).
> - "شريكة / مستشارة" = تستقبل استشارات من إدارات أخرى (لتبيّن رأيها).
> كل ذلك يظهر في **صفحة واحدة** (`/SpecializedDashboard`).
>
> الـ Username في الـ seed (`partner` مع fullName "الإدارة الشريكة" داخل قسم `tech`) **للتوضيح فقط** — ليس له معنى تنظيمي.

#### 6.1 Dashboard (اللوحة الموحّدة — نفس §4)
| # | الخطوة | المتوقع |
|---|---|---|
| 25.1 | Login كـ `partner@ibtikar.local` | ينتقل إلى `/SpecializedDashboard` (ليس `/PartnerDashboard` — تم الدمج) |
| 25.2 | العنوان | "لوحة التقنية" (اسم الإدارة الفعلي من الـ Claim، وليس "الإدارة المختصة" أو "الإدارة الشريكة") |
| 25.3 | قسم "الأفكار المحوّلة لإدارتك" | KPI cards التخصصية الأربعة (التي تستقبلها إدارتي تخصصياً) |
| 25.4 | قسم "الاستشارات الواردة" | KPI cards الثلاثة + جدول الـ inbox (الاستشارات من إدارات أخرى) |
| 25.5 | الجدول يعرض فقط `PartnerAssignment` لإدارتي | فلتر `PartnerAssignmentQuery.ForDepartment` يطبّق على الخادم (IDOR-safe) |
| 25.6 | الـ header user-chip | يعرض `الإدارة الشريكة` كـ fullName و `partner-department` كـ role (username للتوضيح) |

#### 6.2 IDOR / Scope
| # | الخطوة | المتوقع |
|---|---|---|
| 26.1 | حاول فتح Details بـ `assignmentId` لـ **إدارة أخرى** (مثلاً استشارة محالة للتقنية ومحاولة فتحها كمستخدم `partner`) | 404 — `GetAssignmentForPartnerAsync` يطبّق `PartnerDepartmentId == departmentId` على الخادم |
| 26.2 | حاول POST لتوجيه الفكرة لإدارة ثالثة | ❌ ممنوع — الـ Partner لا يحوّل، فقط يرد (Submit/Return/NotCompetent) |
| 26.3 | سجّل دخول كمستخدم `partner` (دور `partner-department`) وحاول فتح `/SpecializedDashboard/Details/{ideaId}` (تخصّص) | يجب أن يعمل — كلا الدورين (`specialized-department` + `partner-department`) مسموح لهما |

#### 6.3 Sequential Details (الأقسام الثلاثة المتتالية)
| # | الخطوة | المتوقع |
|---|---|---|
| 27.1 | افتح Details لاستشارة واردة | ثلاث أقسام مرتبة: 1) الفكرة الأصلية (read-only) 2) تقييم الإدارة صاحبة الفكرة (read-only) 3) نموذج التقييم الاستشاري |
| 27.2 | قسما الفكرة الأصلية + تقييم المتخصصة | كل القيم في `<dl>` read-only، لا توجد أي `<input>` أو `<select>` على حقول الفكرة |
| 27.3 | قسم "تقييم الإدارة صاحبة الفكرة" | يظهر badge "lock — للاطلاع فقط" إذا لم تكمل الإدارة تقييماً بعد، أو جدول بالدرجات إذا تم |
| 27.4 | نموذج التقييم الاستشاري | 5 معايير (dropdown 1-5) + حقل تعليق عام + 3 أزرار: إرجاع لعدم الاختصاص (modal) / إرجاع دون تقييم / إرسال التقييم |

#### 6.4 Advisory Assess (التقييم الاستشاري)
| # | الخطوة | المتوقع |
|---|---|---|
| 28.1 | اختر قيمة لـ 5 معايير (مثال: 4,3,5,4,5) + أضف "تعليق عام" + اضغط "إرسال التقييم الاستشاري" | Status=`Submitted`، كتابة `IdeaStatusHistory`، إشعار (notification stub) للإدارة الطالبة، النموذج يختفي، "إجمالي الدرجات" يظهر في قسم التواريخ، جدول "التقييم المُرسَل" يظهر read-only |
| 28.2 | اضغط "إرسال التقييم الاستشاري" بدون اختيار أي درجة | رفض — "أدخل درجة واحدة على الأقل" |
| 28.3 | اضغط "إرسال التقييم الاستشاري" مع درجة خارج 1-5 (مثل 6) | رفض — "الدرجة يجب أن تكون بين 1 و 5" |
| 28.4 | اضغط "إرسال التقييم الاستشاري" مرتين بسرعة | الزر معطّل بعد الضغطة الأولى (disable-on-click) |
| 28.5 | بعد الـ submit، حدّث الصفحة | النموذج يختفي (CanScore=false لأن Status=Submitted)، يظهر جدول "التقييم المُرسَل" بالقيم المرسلة |
| 28.6 | ارجع للـ Dashboard الموحّد | KPI "تم الرد خلال 30 يوم" يزيد بـ 1، "استشارات بانتظار الرد" ينقص بـ 1 |

#### 6.5 Return to Specialized (إرجاع دون تقييم)
| # | الخطوة | المتوقع |
|---|---|---|
| 29.1 | اضغط "إرجاع دون تقييم" مع **بدون** كتابة "تعليق عام" | رفض — TempData: "يرجى كتابة مرئيات وملاحظات إدارتك قبل إعادته للإدارة صاحبة الفكرة." (الـ Comment/Opinions إلزامي عند `returnOnly=true`) |
| 29.2 | اضغط "إرجاع دون تقييم" مع كتابة opinion | Status=`Returned`، Note يحفظ النص، كتابة History، إشعار (notification stub) للإدارة الطالبة، النموذج يختفي |

#### 6.6 Return Not-Competent (إرجاع لعدم الاختصاص — 3 أيام عمل)
| # | الخطوة | المتوقع |
|---|---|---|
| 30.1 | اضغط "إرجاع لعدم الاختصاص" | يفتح modal بنظام `bog-modal` (vanilla JS، بدون Bootstrap JS) مع حقل "سبب الإعادة (إلزامي)" |
| 30.2 | اضغط "تأكيد الإعادة" بدون سبب | المتصفح يمنع الـ submit (HTML5 required) أو يظهر خطأ — "يرجى كتابة سبب الإعادة (خطأ في التوجيه)." |
| 30.3 | اضغط "إلغاء" | الـ modal يُغلق بدون حفظ |
| 30.4 | اكتب سبب (مثال: "هذه الفكرة لا تخص إدارتنا") + اضغط "تأكيد الإعادة" | Status=`Returned`، Note=`"NotCompetent: {سبب}"`، History يكتب، modal يُغلق، **شارة حمراء** "معاد للإدارة صاحبة الفكرة — خطأ في التوجيه" + السبب يظهر في alert أحمر |
| 30.5 | اختبر نافذة 3 أيام عمل: أنشئ `PartnerAssignment` بتاريخ قديم (مثلاً 5 أيام عمل ماضية) عبر تعديل الـ DB مباشرة | "إرجاع لعدم الاختصاص" **لا يظهر** (CanReturnNotCompetent=false)، أو يظهر رفض "انتهت مهلة الإعادة" |
| 30.6 | اضغط "إرجاع لعدم الاختصاص" على assignment بتاريخ قديم جداً | رفض: "انتهت مهلة الإعادة لعدم الاختصاص (N أيام عمل من 3)" |
| 30.7 | **التحقق من الـ WorkingDays helper:** | يحسب أيام العمل الأحد-الخميس (الرياض). الجمعة والسبت = عطلة. TimeZone Asia/Riyadh. |
| 30.8 | **التحقق من الإشعار (notification):** | `SafeNotifyAsync("Partner.Return" أو "Partner.Submit", ...)` يُرسل عبر `INotificationClient.SendAsync` بدون rollback عند الفشل |

#### 6.7 Out of Scope
- ❌ لا يستطيع تعديل الفكرة الأصلية أو تقييم الإدارة الطالبة.
- ❌ لا يحوّل الفكرة لإدارة ثالثة (فقط يرد).
- ❌ لا يطّلع على استشارات الإدارات الأخرى (فلتر `PartnerDepartmentId` على الخادم).

---

## 3. مصفوفة الفيتشرز × الأدوار (ما يمكن لكل دور اختباره)

| الفيتشر | المستفيد | التدقيق | اللجنة | التخصصية | المدير | الشريك |
|---|:-:|:-:|:-:|:-:|:-:|:-:|
| Foundation & project shell (38) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Unified login (7) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Security & CSRF (11) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Shared idea form (20) | ✅ إنشاء | — | — | — | — | — |
| Applicant follow-up (15) | ✅ متابعة | — | — | — | — | — |
| Audit inbox (15) | — | ✅ Inbox | — | — | — | — |
| Specialized scoring (15) | — | — | — | ✅ تخصصي | — | — |
| **Partner department advisory scoring (6)** | — | — | — | ✅ **استشاري** | — | ✅ **استشاري** |
| Committee *(24 pending)* | — | — | ⚠️ محدود | — | — | — |
| System manager *(جزئي)* | — | — | — | — | ⚠️ جزئي | — |

> **ملاحظة الدمج:** فيتشر "Partner department advisory scoring" (الـ 6 مهام) يظهر تحت كل من "التخصصية" و"الشريك" لأن اللوحة الموحّدة (`/SpecializedDashboard`) تعرض القسمين معاً. الـ 6 مهام هي:
> 1. `3bd49360` Partner dashboard with three KPI cards
> 2. `6b51f71a` Advisory scores + required opinions
> 3. `efa164df` Return opinions to specialized
> 4. `881584d0` Return not-competent within 3 working days
> 5. `febd1dbd` Sequential partner details with locked original idea
> 6. `c2558e17` Scope partner assignments to current department (`PartnerAssignmentQuery.ForDepartment`)

---

## 4. سيناريوهات End-to-End (تمرين كامل على كل الأدوار)

> **يُنفَّذ بترتيب:** المستفيد → التدقيق → التخصصية → الشريك → (اللجنة — عند التوفر) → (المدير — عند التوفر).

### E2E #1 — Happy Path كامل
1. **Beneficiary:** Submit فكرة جديدة (5 حقول + مرفقَين PDF) → Reference `IBT-...`.
2. **Audit:** Accept الفكرة → توجيه لقسم AI.
3. **Specialized (AI):** تقييم 5 معايير (5/5/5/5/5 = 100%) → Submit Draft.
4. **Specialized (AI):** Request Partner Opinions من قسم IT.
5. **IT (الـ Partner):** يفتح نفس اللوحة الموحّدة → قسم "الاستشارات الواردة" → تقييم استشاري (4/4/4/4/4 = 80%) + opinions → Return.
6. **AI (الـ Specialized):** نفس اللوحة → قسم "الأفكار المحوّلة" → عدّل التقييم (يصل 5/5/5/5/5) → Send to Committee.
7. **Committee:** *(عند التوفر)* استلام + تقييم + قرار.

**يتوقع:** كل خطوة تكتب صف في `IdeaStatusHistory` + `AuditLog` + Reference Number ثابت. لاحظ أن الـ AI والـ IT يستخدمان **نفس اللوحة** (`/SpecializedDashboard`) — الفرق هو القسمين (تخصصي vs استشارات واردة).

### E2E #2 — Rejected
1. Beneficiary: Submit.
2. Audit: Reject بسبب "موضوع مكرر" → الحالة `Rejected`، read-only للـ applicant.

### E2E #3 — Returned for Completion → Resubmit → Re-evaluate
1. Beneficiary: Submit.
2. Audit: Request Missing Data.
3. Beneficiary: عدّل + development points → Resubmit.
4. Audit: Accept → Specialized.
5. Specialized: تقييم → Send.

### E2E #4 — IDOR Smoke Test (لكل دور)
- سجّل دخول دور A.
- انسخ URL فكرة دور B (أو حاول تخمين GUID).
- افتحه.
- **المتوقع: 404 أو 403 لكل الأدوار** — لا تسرّب بيانات.

### E2E #5 — CSRF Smoke Test
- لأي POST (Login Submit، Idea Submit، Audit Accept، Specialized Assess، Partner Submit):
- أرسل POST من `curl` بدون `__RequestVerificationToken`.
- **المتوقع: 400 Bad Request.**

### E2E #6 — Rate Limiting
- 10 محاولات Login فاشلة متتالية من نفس IP.
- **المتوقع: رفض مع رسالة واضحة**، ثم الرجوع بعد نافذة.

### E2E #7 — Session Timeout
- Login، انتظر 20 دقيقة بدون حركة → Access أي صفحة → redirect إلى Login.

### E2E #8 — Audit Log Verification
- بعد كل سيناريو، افحص جدول `AuditLog` (من الـ DB أو شاشة Admin):
- يسجل: User, Timestamp, IP, Before/After JSON.

### E2E #9 — Partner Advisory (الاستشارة) — Happy Path
1. **Specialized (AI):** بعد تقييم الفكرة، اضغط "طلب رأي جهات أخرى" → اختر "التقنية" → Submit.
2. **شريك (تقنية):** يفتح `/SpecializedDashboard` → قسم "الاستشارات الواردة" → KPI "استشارات بانتظار الرد" = 1 → جدول الـ inbox فيه الاستشارة.
3. **شريك:** اضغط "مراجعة" → يفتح Details بثلاثة أقسام (فكرة / تقييم الإدارة صاحبة الفكرة / نموذج التقييم).
4. **شريك:** اختر درجات (4,3,5,4,5) + أضف opinion → اضغط "إرسال التقييم الاستشاري" → Status=`Submitted`.
5. **شريك:** ارجع للّوحة → KPI "استشارات بانتظار الرد" = 0، "تم الرد خلال 30 يوم" = 1.
6. **شريك:** اضغط "مراجعة" مرة أخرى على نفس الـ assignment → النموذج يختفي، يظهر جدول "التقييم المُرسَل" بالقيم المرسلة.
7. **AI (المتخصصة):** نفس اللوحة → "الأفكار المحوّلة" → المرجع → يمكن تعديل التقييم مرة أخرى (CanScore=true لأن Status=Submitted لكن Partner.IdOrDepartmentId scopes it).

**يتوقع:** كل خطوة تكتب صف في `AuditLog`. الـ notification `Partner.Submit` يُرسل (stub إذا الـ API خارجي).

### E2E #10 — Partner Advisory — Required Opinions Validation
1. **شريك:** افتح Details لاستشارة جديدة.
2. **شريك:** اضغط "إرجاع دون تقييم" **بدون** كتابة "تعليق عام".
3. **المتوقع:** TempData alert أحمر: "يرجى كتابة مرئيات وملاحظات إدارتك قبل إعادته للإدارة صاحبة الفكرة."

### E2E #11 — Partner Advisory — Not-Competent (3-Day Window)
1. **شريك:** افتح Details لاستشارة جديدة (مفترضة من نفس اليوم).
2. **شريك:** اضغط "إرجاع لعدم الاختصاص" → modal بـ bog-modal يفتح.
3. **شريك:** اضغط "تأكيد الإعادة" بدون سبب → المتصفح يمنع الـ submit (HTML5 required).
4. **شريك:** اكتب سبب (مثال: "خطأ في التوجيه") → اضغط "تأكيد الإعادة".
5. **المتوقع:** Status=`Returned`، Note=`"NotCompetent: خطأ في التوجيه"`، badge أحمر "معاد للإدارة صاحبة الفكرة — خطأ في التوجيه" + السبب في alert أحمر.
6. **التحقق من الـ 3-day window:** عبر تعديل DB مباشرة لـ SentAt (5 أيام عمل ماضية)، الـ button "إرجاع لعدم الاختصاص" يختفي أو يُرفض بـ "انتهت مهلة الإعادة".

### E2E #12 — Partner Advisory — IDOR Cross-Department
1. **شريك (تقنية):** افتح Details لاستشارة محالة للتقنية.
2. سجّل خروج، Login كمستخدم `specialized` (دور `specialized-department` — نفس اللوحة الموحّدة).
3. **متخصص (القضائية):** افتح الـ Details بنفس الـ assignmentId.
4. **المتوقع:** 404 — `PartnerAssignmentQuery.ForDepartment` يطبّق `PartnerDepartmentId == departmentId` على الخادم.

### E2E #13 — Unified Dashboard Navigation
1. **أي دور** (`specialized` أو `partner`): يفتح `/SpecializedDashboard`.
2. يضغط "كل الأفكار المحوّلة" → ينتقل إلى `/SpecializedDashboard/Referrals`.
3. يضغط "لوحة الإدارة" → يرجع للوحة الموحّدة.
4. يفتح Details استشارة → يضغط "مراجعة" → `PartnerDashboard/Details/{id}`.
5. يضغط "العودة للوحة الإدارة" → يرجع للوحة الموحّدة (`/SpecializedDashboard`).

**المتوقع:** كل التنقلات تعمل بدون أخطاء. الـ breadcrumb واضح (لوحة الإدارة ← الأفكار المحوّلة / الاستشارات الواردة).

### E2E #14 — Department Name Claim
1. Login كـ `specialized` (دور `specialized-department` — في قسم `judicial`).
2. افتح `/SpecializedDashboard`.
3. **المتوقع:** العنوان "لوحة {DepartmentName}" (مثال: "لوحة الشؤون القضائية") — **ليس** "الإدارة المختصة" أو "الإدارة الشريكة".
4. **المتوقع:** الـ hero badge يعرض نفس اسم الإدارة الفعلي.
5. Login كـ `partner` (دور `partner-department` — في قسم `tech`).
6. افتح `/SpecializedDashboard`.
7. **المتوقع:** العنوان "لوحة التقنية" (نفس القسم الفعلي للمستخدم).
8. **المتوقع:** الـ header user-chip يعرض `الإدارة الشريكة` كـ fullName + `partner-department` كـ role (هذا مقبول — الـ username للتوضيح فقط).

---

## 5. Checklist المختبر (قبل التسليم)

- [ ] قمت بتسجيل جميع الـ Test Cases المنفّذة في جدول منفصل (Case ID / Description / Result / Notes).
- [ ] كل Fail أرفقت معه: خطوات إعادة الإنتاج، الـ Reference Number، الـ Screenshot، الـ Browser Console، الـ AuditLog entry.
- [ ] لا توجد حالة "متروكة في النص" — كل سيناريو Pass أو Fail أو Blocked.
- [ ] Sign-off section في الأسفل موقّع ومؤرّخ.

---

## 6. Sign-off (التوقيع)

| المختبر | الدور المُختبر | عدد Test Cases | Pass | Fail | Blocked | ملاحظات | التاريخ | التوقيع |
|---|---|---:|---:|---:|---:|---|---|---|
|  | المستفيد |  |  |  |  |  |  |  |
|  | التدقيق |  |  |  |  |  |  |  |
|  | اللجنة |  |  |  |  | (Pending) |  |  |
|  | التخصصية |  |  |  |  |  |  |  |
|  | مدير النظام |  |  |  |  | (جزئي) |  |  |
|  | إدارة شريكة |  |  |  |  |  |  |  |

**اعتماد نهائي (Lead QA):**

| الاسم | التاريخ | التوقيع |
|---|---|---|
|  |  |  |

---

## 7. ملاحظات حرجة على الـ Cycle

> 🚨 **تغييرات حرجة بعد الدمج (يجب أن يعرفها كل مختبر):**
> 1. **لا توجد "إدارة شريكة" منفصلة.** الـ username `partner@ibtikar.local` في الـ seed (مع fullName "الإدارة الشريكة" داخل قسم `tech`) للتوضيح فقط — الإدارة الواحدة تستقبل **تخصصياً** + **استشارات**. كلتا الدورين `specialized-department` و `partner-department` ينتقلان إلى `/SpecializedDashboard` بعد الدخول.
> 2. **لا تستخدم `/PartnerDashboard` كـ "home" لدور الشريك** — هذا الـ URL ما زال موجوداً للأكشنز (Details/Submit/ReturnNotCompetent) لكن `Index` يحوّل إلى `/SpecializedDashboard`. كل روابط "العودة" في Details تشير الآن إلى `/SpecializedDashboard`.
> 3. **المصطلحات المتغيرة في الـ UI:** "الإدارة الشريكة" لم تعد تظهر في عناوين الـ UI. بدلاً منها: "جهات أخرى" (للاستشارات) / "الإدارة صاحبة الفكرة" / "الجهات المستشارة".
> 4. **زر "إرجاع لعدم الاختصاص"** يستخدم modal بنظام `bog-modal` المخصص (vanilla JS) — لا تعتمد على Bootstrap JS (غير محمّل). اختبر بـ Esc للـ close.

> 🚨 **ما هو NOT في scope هذا الـ Cycle:**
> - فيتشر **Committee** (24 مهمة pending) — لا تحاول الـ Cycle الكامل، سجّل "Pending Feature".
> - فيتشر **System Manager** (جزء منه في الـ 24 pending) — اختبر ما هو ظاهر فقط.
> - إشعارات الـ Notification Service — الـ API خارجي وقد لا يكون مفعّلاً في بيئة الـ Staging. الـ stub يكتب log فقط.
> - النسخ الاحتياطي / الاستعادة (`Document backup and restore`) — وثائقي، لا يحتاج اختبار وظيفي.

> ✅ **ما هو في scope هذا الـ Cycle (133/133 done):**
> 1. Login + CSRF + Session Timeout
> 2. Submit Idea + Attachments + Validation
> 3. My Requests + Details + Resubmit + Delete + Timeout
> 4. Audit Inbox + Accept/Reject/Request Data + Route
> 5. Specialized Dashboard + Assess 5 criteria + Partner Request + Send to Committee
> 6. **Partner department advisory scoring (دمج مع Specialized — نفس اللوحة):**
>    - 3 KPI cards (Pending / Late / Submitted) + جدول inbox للاستشارات
>    - تقييم 5 معايير + opinions إلزامية عند `returnOnly=true`
>    - إرجاع دون تقييم (Returned) + كتابة history
>    - إرجاع لعدم الاختصاص خلال 3 أيام عمل (WorkingDays helper، Sun-Thu Riyadh)
>    - 3 أقسام متتالية في Details (فكرة أصلية read-only + تقييم الإدارة صاحبة الفكرة + نموذج التقييم)
>    - IDOR scoping عبر `PartnerAssignmentQuery.ForDepartment` (يستعمل في كل read path)
>    - شارة حمراء "معاد للإدارة صاحبة الفكرة — خطأ في التوجيه" + alert بالسبب
>    - modal بـ bog-modal (vanilla JS، بدون Bootstrap JS)
>    - claim اسم الإدارة الفعلي (`ibtikar_department_name` بدل fallback "الإدارة الشريكة")

---

## 8. جهات الاتصال عند الفشل

| نوع المشكلة | المسؤول |
|---|---|
| خطأ في الـ Login / CSRF / Session | فريق الـ Security (Foundation & Security features) |
| خطأ في الـ Submit / Attachments / My Requests | فريق الـ Beneficiary flow |
| خطأ في الـ Inbox / Routing | فريق الـ Audit |
| خطأ في التقييم / الشركاء / الإرسال | فريق الـ Specialized + Partner (لوحة موحّدة) |
| خطأ في الـ Modal / bog-modal / vanilla JS | فريق الـ Frontend (Foundation) |
| خطأ في بيانات الـ Seed | فريق الـ DevOps (DB migration) |

---

> 📌 **مصدر الدليل:** تم إعداده بناءً على قراءة 9 فيتشرز و 133 تاسك منجزة في مشروع Ibtikar (Project `a8ffd050-3155-4522-b7cc-57e22dec4266`).
> **آخر تحديث للـ Doc:** بعد دمج فيتشر "Partner department advisory scoring" (6 مهام) في لوحة "الإدارة المختصة" الموحّدة — `2026-09-01`.

---

## 9. الفيتشرات الثلاثة الجديدة — 2026-09-01 (الإضافة الكاملة)

تم إضافة **3 فيتشرات جديدة** فوق الـ Stack، تتبع **بنية Controller Architecture** (Controller → Service → Repository → EF Core) ونفس الـ Theme البصري (`bog-main` + `bog-section` + `bog-card-list` + RTL).

### 9.1 نظرة عامة على الفيتشرات الثلاثة

| # | الفيتشر | الدور | الـ URLs | عدد الأكشنز |
|---|---|---|---|---|
| 1 | **Execution tracking & completion** | `specialized-department` | `/Execution`, `/Execution/Update/{id}`, `/Execution/Timeline/{id}`, `/Execution/Complete/{id}`, `/Execution/UploadCompletion` | 5 |
| 2 | **System admin read-only overview** | `system-admin` | `/AdminOverview`, `/AdminOverview/Details/{id}` | 2 (GET only) |
| 3 | **Date-range reports & challenges** | `system-admin` | `/Reports`, `/Reports/Challenges` | 2 (GET only) |

> **العدد الإجمالي:** 10 مهام إضافية (5 + 2 + 3) + 1 migration جديدة لـ `ExecutionProgresses`.

---

### 👤 المستخدم 4-b: Execution Tracking (تتبّع تنفيذ الأفكار المعتمدة)

**الـ Scope:** تتبع الأفكار عبر 5 مراحل تنفيذ (البدء → التخطيط → التنفيذ → المتابعة → الإغلاق) + إكمال التنفيذ برفع ملفَي PDF + سجل زمني read-only.

**الـ URLs:**
- `/Execution` — قائمة الأفكار المُحالة للتنفيذ للإدارة الحالية
- `/Execution/Update/{id}` — تحديث المرحلة التالية + إكمال التنفيذ
- `/Execution/Timeline/{id}` — السجل الزمني read-only (الأحدث أولاً)
- `/Execution/UploadCompletion` — endpoint رفع ملفَي PDF (JSON)

**الـ Roles:** `SpecializedDepartment` فقط.

#### 9.1.1 Execution List (Happy Path)
| # | الخطوة | المتوقع |
|---|---|---|
| 40.1 | Login كـ `specialized@ibtikar.local` (قسم `judicial`) | ينتقل إلى `/SpecializedDashboard` (افتراضي) |
| 40.2 | افتح `/Execution` مباشرة | قائمة الأفكار في حالة `in-execution` المُحالة لإدارتي فقط (فلتر `AssignedDepartmentId` على الخادم) |
| 40.3 | افحص العنوان | "تنفيذ الأفكار في {DepartmentName}" بأيقونة `build_circle` |
| 40.4 | افحص الجدول | أعمدة: المرجع، العنوان، المُقدِّم، المرحلة الحالية (badge)، الحالة، إجراءات |
| 40.5 | اضغط "تحديث المرحلة" على فكرة | ينتقل إلى `/Execution/Update/{id}` |
| 40.6 | اضغط "السجل الزمني" | ينتقل إلى `/Execution/Timeline/{id}` |

#### 9.1.2 Stage Update (خمسة مراحل)
| # | الخطوة | المتوقع |
|---|---|---|
| 41.1 | افتح Update لمرحلة لم تبدأ بعد | يظهر: 5 مراحل في `list-group-numbered` (البدء مُعلَّم حالياً)، نموذج "الانتقال للمرحلة التالية" |
| 41.2 | اضغط Submit **بدون** إدخال note | رفض المتصفح — `required` + `minlength="5"` |
| 41.3 | أدخل note بطول 4 أحرف | رفض المتصفح — `minlength="5"` |
| 41.4 | أدخل note بطول ≥5 أحرف، اضغط "تسجيل المرحلة" | رسالة خضراء، يُضاف صف جديد في `ExecutionProgresses` بالطابع الزمني للخادم + اسم المستخدم + الإدارة |
| 41.5 | تحقّق من أن الطابع الزمني في DB هو UTC وليس من المتصفح | الـ timestamp يُسجَّل على الخادم (`DateTime.UtcNow`) |
| 41.6 | حدّث الصفحة | المرحلة تتقدّم، يظهر نموذج للمرحلة التالية |

#### 9.1.3 Timeline (read-only)
| # | الخطوة | المتوقع |
|---|---|---|
| 42.1 | افتح `/Execution/Timeline/{id}` | جدول زمني read-only، الأحدث أولاً، يعرض: اسم المرحلة، التاريخ/الوقت، اسم المستخدم، note |
| 42.2 | اضغط "تعديل" على row | غير ممكن — لا أزرار تعديل (read-only) |
| 42.3 | افتح بدون Login | redirect إلى `/Account/Login` (Authorize) |
| 42.4 | Login كـ `audit@ibtikar.local` وافتح `/Execution/Timeline/{id}` | 403 / redirect — `SpecializedDepartment` فقط |

#### 9.1.4 Complete Execution (Two PDFs)
| # | الخطوة | المتوقع |
|---|---|---|
| 43.1 | افتح Update لمرحلة `الإغلاق` (الخامسة) | يظهر نموذج "إكمال التنفيذ" بدلاً من نموذج "المرحلة التالية" |
| 43.2 | اضغط "إكمال التنفيذ" بدون رفع ملف | alert: "يرجى إرفاق ملفَي PDF بالضبط." |
| 43.3 | ارفع ملف واحد فقط | alert: "يرجى إرفاق ملفَي PDF بالضبط." |
| 43.4 | ارفع ملف `.txt` مع `.pdf` | alert: "يجب أن يكون الملفان بصيغة PDF." |
| 43.5 | ارفع ملفَين PDF (مثل: `proof1.pdf` + `proof2.pdf`) | يبدأ spinner "جارٍ الرفع..." ثم modal تأكيد يفتح |
| 43.6 | اضغط "إلغاء" في الـ modal | الـ modal يُغلق، الـ form لا يُرسَل |
| 43.7 | اضغط "نعم، إكمال التنفيذ" | spinner، ثم redirect إلى Update مع alert أخضر "تم تنفيذ الفكرة وتسجيلها ضمن المكتملة" |
| 43.8 | تحقّق من DB | `CurrentStatus` → `completed`، `ExecutionProgresses` به صف للمرحلة الأخيرة، `IdeaAttachments` به 2 ملف مرتبطان بنفس الفكرة |
| 43.9 | افتح الـ Timeline مرة أخرى | صف جديد في الأعلى بعبارة "تم تنفيذ الفكرة وإرفاق ملفَي الإغلاق" |

#### 9.1.5 Validation Hardening
| # | الخطوة | المتوقع |
|---|---|---|
| 44.1 | أكمل مرحلة ثم حاول POST `/Execution/Complete/{id}` مع `attachmentId` واحد فقط | رفض — "تتطلب مرحلة (تم التنفيذ) رفع ملفين PDF اثنين." |
| 44.2 | أكمل مرحلة مع `attachmentId` يخص فكرة أخرى | رفض — "يجب أن يكون الملفان مرفقان على نفس الفكرة." |
| 44.3 | حاول Update لمُسوَّدة (IsDraft=true) | رفض — "لا يمكن تحديث فكرة مسودة." |
| 44.4 | حاول Update لفكرة في حالة `approved` (ليست in-execution) | رفض — "الفكرة ليست في حالة تنفيذ." |
| 44.5 | حاول Update بدون CSRF token (curl) | 400 |

#### 9.1.6 IDOR / Out of Scope
| # | الخطوة | المتوقع |
|---|---|---|
| 45.1 | Login كـ `specialized` (قسم `judicial`)، افتح `/Execution/Update/{id}` لفكرة محالة لقسم `tech` | 404 / Forbid — `IsAssigneeAsync` يطبّق `AssignedDepartmentId == departmentId` على الخادم |
| 45.2 | Login كـ `partner`، افتح `/Execution` | redirect إلى `/Account/Login` (Authorize) أو 403 |
| 45.3 | حاول POST لـ `/Execution/UploadCompletion` بدون Session | 401 |

#### 9.1.7 Out of Scope
- ❌ لا يُعدّل فكرة في حالة `new` أو `under-study` (فقط `in-execution`).
- ❌ لا يصل إلى `/Execution` كأدوار أخرى (Audit، Committee، Admin).
- ❌ لا يمكنه إكمال التنفيذ بدون ملفَي PDF فعليّين (الـ Signature يُفحص).

---

### 👤 المستخدم 5-b: Date-range Reports & Challenges (تقارير الفترة والتحديات)

**الـ Scope:** تقرير الفترة الزمنية (4 KPI + stage-mix %) + تقرير التحديات (مع فلتر المجال، استبعاد المرفوض من التدقيق).

**الـ URLs:**
- `/Reports` — تقرير الفترة الزمنية
- `/Reports/Challenges` — تقرير التحديات

**الـ Roles:** `SystemAdmin` فقط.

#### 9.2.1 Date-range Report (Happy Path)
| # | الخطوة | المتوقع |
|---|---|---|
| 46.1 | Login كـ `admin@ibtikar.local` | ينتقل إلى `/AdminOverview` (افتراضي) |
| 46.2 | افتح `/Reports` مباشرة | نموذج الفترة: من/إلى تاريخ + زر "عرض التقرير" + زر "تقرير التحديات" |
| 46.3 | الفترة الافتراضية | آخر 30 يوم |
| 46.4 | اضغط "عرض التقرير" | 4 KPI cards: إجمالي / مُقدَّمة / معتمدة / قيد التنفيذ والمنجزة |
| 46.5 | تحقّق من KPI cards | الأرقام تطابق DB (`InnovationIdeas.CreatedAt` ضمن الفترة) |
| 46.6 | تحقّق من جدول "توزيع الحالات" | كل حالة من الـ 14 status في صف مع badge لوني + النسبة المئوية |
| 46.7 | تحقّق من مجموع النسب | = 100% (تقريباً) — أو warning أصفر "مجموع عدد الحالات لا يساوي الإجمالي" |
| 46.8 | غيّر الفترة إلى فترة فارغة (مثلاً قبل سنة) | Empty state: "لا توجد بيانات في الفترة المختارة." مع `data-testid="empty-range-message"` |

#### 9.2.2 Date-range Validation
| # | الخطوة | المتوقع |
|---|---|---|
| 47.1 | اختر `from=2026-12-01` و `to=2026-01-01` (معكوسة) | alert أحمر "تاريخ البداية بعد تاريخ النهاية." (server-side) |
| 47.2 | اختر `from=2026-12-01` و `to=2026-12-01` (نفس اليوم) | يقبل، النطاق = يوم واحد |
| 47.3 | اضغط submit بدون أي تاريخ | المتصفح يمنع (HTML5 required) |
| 47.4 | اختر فترة قديمة جداً قبل 5 سنوات | Empty state طبيعي |

#### 9.2.3 Challenges Report
| # | الخطوة | المتوقع |
|---|---|---|
| 48.1 | افتح `/Reports/Challenges?from=...&to=...` | Banner أزرق "يتم تلقائياً استبعاد الأفكار المرفوضة من قِبل التدقيق." |
| 48.2 | تحقّق من الفلتر | dropdown "المجال" مع كل المجالات + خيار "كل المجالات" |
| 48.3 | اختر مجالاً معيّناً | الجدول يتقلّص ليشمل فقط ذلك المجال |
| 48.4 | تحقّق من الأعمدة | المرجع، العنوان، المجال، المُقدِّم (+الإدارة)، التحدي، الحل المقترح، الحالة، التاريخ |
| 48.5 | تحقّق من الاستبعاد | لا تظهر أي فكرة `CurrentStatus.Code == "rejected"` (تم استبعادها في `ReportsRepository.GetChallengesAsync`) |
| 48.6 | لو لا توجد بيانات | Empty state "لا توجد تحديات في الفترة المختارة." |

#### 9.2.4 IDOR / Out of Scope
| # | الخطوة | المتوقع |
|---|---|---|
| 49.1 | Login كـ `audit@ibtikar.local` وافتح `/Reports` | redirect إلى `/Account/Login` (Authorize) أو 403 |
| 49.2 | Login كـ `specialized` وافتح `/Reports` | redirect إلى `/Account/Login` (Authorize) أو 403 |
| 49.3 | حاول GET `/Reports?from=2025-01-01&to=2025-01-01` بدون فترة كبيرة | Empty state — لا crash |
| 49.4 | حاول POST لـ `/Reports` (لا توجد action POST) | 405 Method Not Allowed |

#### 9.2.5 Out of Scope
- ❌ لا يكتب (Read-only) — لا توجد POST actions.
- ❌ لا يصل كأدوار أخرى (Audit، Specialized، Committee).
- ❌ لا يستطيع تعديل أي lookup من داخل التقارير.

---

### 👤 المستخدم 5-c: System Admin Overview (لوحة مدير النظام للقراءة فقط)

**الـ Scope:** لوحة شاملة (KPI + recent + global ideas table + status filter + read-only details).

**الـ URLs:**
- `/AdminOverview` — لوحة شاملة (4 KPI cards + global ideas table)
- `/AdminOverview/Details/{id}` — تفاصيل فكرة (read-only، جميع التقييمات + timeline)

**الـ Roles:** `SystemAdmin` فقط. **لا توجد POST actions.**

#### 9.3.1 Admin Dashboard
| # | الخطوة | المتوقع |
|---|---|---|
| 50.1 | Login كـ `admin@ibtikar.local` | ينتقل إلى `/AdminOverview` |
| 50.2 | افحص العنوان | "نظرة عامة على النظام" بأيقونة `monitoring` + badge "مدير النظام" |
| 50.3 | افحص 4 KPI cards | إجمالي الأفكار / المسودات / المُرسلة / المستخدمون النشطون |
| 50.4 | افحص قسم "أحدث الأفكار" | جدول بأحدث 8 أفكار + mobile cards |
| 50.5 | افحص قسم "توزيع الأفكار حسب الحالة" | كل حالة مع badge لوني وعدد |
| 50.6 | افحص قسم "سجل الأفكار الكامل" | جدول بكل الأفكار (حتى 200)، فلتر بالحالة dropdown |
| 50.7 | طبّق فلتر (مثلاً `in-execution`) | الجدول يتقلّص ليشمل فقط تلك الحالة |
| 50.8 | اضغط "إعادة ضبط" | الفلتر يُمسح |
| 50.9 | اضغط "تفاصيل" على فكرة | ينتقل إلى `/AdminOverview/Details/{id}` |
| 50.10 | افحص الـ top-right links | زرّا "التقارير الزمنية" و "تقرير التحديات" |

#### 9.3.2 Admin Details (read-only)
| # | الخطوة | المتوقع |
|---|---|---|
| 51.1 | افتح `/AdminOverview/Details/{id}` | تفاصيل كاملة: reference, title, status badge, domain, applicant |
| 51.2 | افحص قسم "وصف الفكرة" | يعرض `Description` بشكل read-only |
| 51.3 | تحقّق من قسم "التصنيف" | المجال، الأثر المتوقع، الفئة المستهدفة |
| 51.4 | تحقّق من قسم "المرفقات" | قائمة بالملفات (PDF) + الحجم + التاريخ + المُحمِّل |
| 51.5 | تحقّق من قسم "جميع التقييمات" | badge لكل تقييم (`الإدارة المختصة` / `الإدارة الشريكة` / `اللجنة`) + اسم المُقيِّم + الإدارة + إجمالي + خطوط المعايير |
| 51.6 | تحقّق من قسم "السجل الزمني" | timeline read-only، الأحدث أولاً |
| 51.7 | حاول إدخال نص في أي حقل | **لا توجد حقول إدخال!** كل القيم read-only |
| 51.8 | افحص banner "للقراءة فقط" | يظهر في الأسفل لتأكيد الـ read-only |

#### 9.3.3 IDOR / Security
| # | الخطوة | المتوقع |
|---|---|---|
| 52.1 | Login كـ `audit@ibtikar.local` وافتح `/AdminOverview` | redirect إلى `/Account/Login` أو 403 |
| 52.2 | Login كـ `specialized` وافتح `/AdminOverview/Details/{id}` | 403 |
| 52.3 | حاول POST لـ `/AdminOverview/Details/{id}` | 405 Method Not Allowed |
| 52.4 | افتح GUID عشوائي غير موجود | 404 |
| 52.5 | افتح GUID موجود كـ `audit` | 403 |

#### 9.3.4 Out of Scope
- ❌ لا يكتب — **لا توجد POST actions** على AdminOverview.
- ❌ لا يصل كأدوار أخرى.
- ❌ لا يعدّل من Details.

---

### مصفوفة الفيتشرات الثلاثة × الأدوار

| الفيتشر | المستفيد | التدقيق | اللجنة | التخصصية | المدير | الشريك |
|---|:-:|:-:|:-:|:-:|:-:|:-:|
| **Execution tracking (5)** | — | — | — | ✅ **منفّذ** | — | — |
| **System admin overview (2)** | — | — | — | — | ✅ **قارئ** | — |
| **Date-range reports & challenges (3)** | — | — | — | — | ✅ **قارئ** | — |

### الـ 10 مهام المنفّذة في هذه الإضافة

| Block | الفيتشر | الملفات الرئيسية |
|---|---|---|
| `f851ff01` | Add ExecutionProgress entity | `Models/ExecutionProgress.cs`, `Data/Configurations/ExecutionProgressConfiguration.cs`, `Migrations/...AddExecutionProgress.cs` |
| `06199256` | Add execution task list | `Controllers/ExecutionController.cs`, `Services/Implementations/ExecutionService.cs`, `Repositories/Implementations/ExecutionRepository.cs`, `Views/Execution/Index.cshtml` |
| `1accf06a` | Add execution stage update | `Views/Execution/Update.cshtml`, `Controllers/ExecutionController.cs` (POST `Update`, `UploadCompletion`) |
| `0941f31d` | Add execution timeline | `Views/Execution/Timeline.cshtml` |
| `3676cac3` | Complete execution with two PDFs | Confirmation modal in `Update.cshtml`, validation in `ExecutionService.CompleteAsync` |
| `0bfdc518` | Add system-admin overview dashboard | `Controllers/AdminOverviewController.cs`, `Services/Implementations/AdminOverviewService.cs`, `Repositories/Implementations/AdminOverviewRepository.cs`, `Views/AdminOverview/Index.cshtml` (KPI + recent + global ideas table) |
| `0ee8e365` | Add read-only admin idea details | `Views/AdminOverview/Details.cshtml` (full idea + all assessments + timeline, no forms) |
| `2d0b42a9` | Add date-range KPI report | `Controllers/ReportsController.cs`, `Services/Implementations/ReportsService.cs`, `Repositories/Implementations/ReportsRepository.cs`, `Views/Reports/Index.cshtml` |
| `d8a1872d` | Validate report dates | `Views/Reports/Index.cshtml` (inline + script + exact Arabic strings) |
| `39ed2410` | Add challenges report | `Views/Reports/Challenges.cshtml` (domain filter + audit-reject exclusion) |

---

### 🧪 سيناريوهات End-to-End للـ Features الثلاثة

#### E2E-RT-1 — Date Range + Admin Overview (Full Admin Flow)
1. **Admin:** Login → `/AdminOverview` → لاحظ 4 KPI cards + global ideas table.
2. **Admin:** طبّق فلتر `in-execution` على جدول الأفكار → الجدول يتقلّص.
3. **Admin:** اضغط "تفاصيل" على فكرة في حالة `in-execution` → `/AdminOverview/Details/{id}` يعرض الفكرة + التقييمات (إن وُجدت) + Timeline.
4. **Admin:** ارجع لـ `/AdminOverview` → اضغط "التقارير الزمنية" في الأعلى.
5. **Admin:** في `/Reports`، اضغط "تقرير التحديات" → `/Reports/Challenges` يعرض الجدول مع banner "يتم استبعاد المرفوض من التدقيق".

**يتوقع:** كل التنقلات تعمل، لا crash، KPI + status filter يعملان، Reports → Challenges link يعمل.

#### E2E-RT-2 — Execution Flow (من التحويل للإغلاق)
1. **(مسبوقاً):** Audit Accept + Specialized Assess + Committee Approve (يقوم بتحويل الفكرة لحالة `in-execution`).
2. **Specialized:** Login → افتح `/Execution` → الفكرة الجديدة تظهر في القائمة.
3. **Specialized:** اضغط "تحديث المرحلة" → انتقل للمرحلة الأولى (البدء) بحفظ note.
4. **Specialized:** كرّر للمراحل 2، 3، 4.
5. **Specialized:** في المرحلة 5 (الإغلاق) → نموذج "إكمال التنفيذ" يظهر بدلاً من "المرحلة التالية".
6. **Specialized:** ارفع ملفَين PDF → spinner → modal تأكيد → اضغط "نعم".
7. **Specialized:** alert أخضر "تم تنفيذ الفكرة وتسجيلها ضمن المكتملة" → الـ Timeline به صف جديد.

**يتوقع:** DB به `ExecutionProgresses` بـ 5 صفوف (4 مراحل + الإغلاق) + `IdeaAttachments` بـ 2 ملف جديدَين.

---

### 🔐 Test Credentials (للـ Playwright / الاختبار اليدوي)

```
URL: https://localhost:5001
كلمة المرور: Ibtikar@2026 (UserSeed.DefaultPassword)

Username         | Role                    | Department  | Home redirect
-----------------|-------------------------|-------------|----------------------
specialized      | specialized-department  | judicial    | /SpecializedDashboard
admin            | system-admin            | (no dept)   | /AdminOverview
audit            | audit-employee          | (no dept)   | /Audit/Inbox
partner          | partner-department      | tech        | /SpecializedDashboard
committee        | innovation-committee-member | (no dept) | /Committee
ext-beneficiary  | external-beneficiary    | (none)      | /MyRequests
int-beneficiary  | internal-beneficiary    | judicial    | /MyRequests
```

> **للاختبار العملي:**
> - **التنفيذ:** استعمل `specialized` → `/Execution`
> - **التقارير:** استعمل `admin` → `/Reports`, `/Reports/Challenges`
> - **لوحة الإدارة:** استعمل `admin` → `/AdminOverview`, `/AdminOverview/Details/{id}`


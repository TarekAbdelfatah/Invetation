# New Project Bootstrap

The project root document already exists (`id = projectId`, `type="Project"`).

## Step 1: Ask the User
Ask whether to explore the code automatically or use the user's supplied goal/features/tasks.
Stay in planning state until the user answers.

## Step 2: Fill Project README with `edit_blocks`
Use TBML only — structure:
```
@h level:3 → Vision
@p           → Why this project exists and what problem it solves.
@h level:3 → Goals
@li          → First measurable goal.
@li          → Second measurable goal.
@h level:3 → Architecture
@p           → Core technology and design decisions.
```
Rule: NEVER add `@check` to the project doc — it is a README.

## Step 3: Create Feature Docs
For each feature, call `create_document` with `type="Feature"`, `parentDocumentId=projectId`.

## Step 4: Create Task Docs
Create Task documents under Feature documents.
→ See `tadweena-documents` skill for TBML rules.

## Step 5: Report to User
Show the created structure and ask which task to implement first.

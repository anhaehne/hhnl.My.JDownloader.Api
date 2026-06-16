
## General
- No monolithic files. Do not create large unmaintainable files. Keep them as small as possible and when in doubt, split logic by separation of concern.
- After each logical step commit your changes with a descriptive but concise commit message and push them.
- When first starting working an a feature/issue (when there is no feature documentation yet), go into plan mode and plan the feature/issue.
- When executing small specific tasks with clear goals, spawn a subagent with the spark/flash/haiku model to execute this task. Do this for example when doing a fan out style job and for watching commands. 
  - Reuse subagents for this when possible and whenever tasks are in the same scope. Otherwise prune subagents when they are not needed anymore.

## C# / .net:
- Use `var` whenever possible.
- Do not add #nullable enable.
- Do not suppress warnings.
- Public async service and controller methods should accept and pass through `CancellationToken` where practical.
- Do not use `.Result`, `.Wait()`, or sync-over-async patterns.
- When running an Aspire apphost, use the aspire mcp server when available.

## When working on a github issue:
- Make sure to work in a issue specific branch. If the branch does not exist yet, create one by using the github branch creation.

## Testing:
- Always create unit test when possible. If there are no unit tests yet for the classes you are changing.
  - Test methods should follow the following patter; `{MethodName}_should_{expectedResult}(_when_{actionPerformed/inputParameters})?`.
  - Test classes should be named after the tested Unit: '{Unit/ClassName}Tests.cs'
  - Organize test files according to the unit's/classes' path in their project in folders. If the test project contains tests for multiple project, create a base folder for each project.
- Create integration tests when new functionality introduces/changes logic that spans multiple classes.
  - Add a comment describing which business logic is being tested.
  - Organize test files according to the tested feature in folders.
- Create E2E tests when new functionality introduces/changes logic that spans multiple applications.
  - Add a comment which feature is being tested.
  - Organize test files according to the tested feature in folders.
- Create tests before making changes (RED/GREEN tests).
- Run only the tests that are relevant for the current step. Only run all tests once the current feature is complete.
- Only change tests that were affected by changed functionality.

## Documentation
When implementing a feature, document everything needed to understand, reproduce, and maintain the work in a dedicated folder under `/features`.

Use this structure as the default template, but create only files and folders that will contain useful information:

```text
/features/
  yyyy-mm-dd-feature-name/
    README.md
    analysis.md
    decisions.md
    implementation-notes.md
    test-plan.md
    test-data/
      README.md
      sample-inputs/
      sample-outputs/
    artifacts/
      screenshots/
      logs/
      external-references.md
```

Suggested contents:

- `README.md`: Short feature summary, scope, status, and links to related code or issues.
- `analysis.md`: Problem analysis, current behavior, constraints, assumptions, and relevant code paths.
- `decisions.md`: Important implementation decisions, rejected alternatives, and tradeoffs.
- `implementation-notes.md`: Files changed, behavior added or changed, migration notes, and follow-up work.
- `test-plan.md`: Manual and automated test cases, edge cases, and verification results.
- `test-data/`: All data needed to implement and verify the feature, including sample inputs, sample outputs, fixtures, exported API responses, anonymized real-world examples, and generated data.
- `artifacts/`: Supporting implementation artifacts such as screenshots, logs, traces, benchmark output, or external references.

Keep feature-folder content focused on material that was actually useful for implementing or verifying the feature. Do not create placeholder files, empty sections, or directories just to match the template. Do not store secrets, credentials, personal data, or large generated files unless they are explicitly required and safe to commit.

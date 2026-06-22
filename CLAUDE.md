# Claude.md

## Claude.md Update Policy

- When `Claude.md` is modified during a session, re-read it before continuing further code changes.
- Apply the latest instructions from `Claude.md` after it has been updated.

<!-- code-review-graph MCP tools -->
## MCP Tools: code-review-graph

**IMPORTANT: This project has a knowledge graph. ALWAYS use the
code-review-graph MCP tools BEFORE using Grep/Glob/Read to explore
the codebase.** The graph is faster, cheaper (fewer tokens), and gives
you structural context (callers, dependents, test coverage) that file
scanning cannot.

### When to use graph tools FIRST

* **Exploring code**: `semantic_search_nodes` or `query_graph` instead of Grep
* **Understanding impact**: `get_impact_radius` instead of manually tracing imports
* **Code review**: `detect_changes` + `get_review_context` instead of reading entire files
* **Finding relationships**: `query_graph` with callers_of/callees_of/imports_of/tests_for
* **Architecture questions**: `get_architecture_overview` + `list_communities`

Fall back to Grep/Glob/Read **only** when the graph doesn't cover what you need.

### Key Tools

| Tool                        | Use when                                               |
| --------------------------- | ------------------------------------------------------ |
| `detect_changes`            | Reviewing code changes — gives risk-scored analysis    |
| `get_review_context`        | Need source snippets for review — token-efficient      |
| `get_impact_radius`         | Understanding blast radius of a change                 |
| `get_affected_flows`        | Finding which execution paths are impacted             |
| `query_graph`               | Tracing callers, callees, imports, tests, dependencies |
| `semantic_search_nodes`     | Finding functions/classes by name or keyword           |
| `get_architecture_overview` | Understanding high-level codebase structure            |
| `refactor_tool`             | Planning renames, finding dead code                    |

### Workflow

1. The graph auto-updates on file changes via hooks.
2. Use `detect_changes` for code review.
3. Use `get_affected_flows` to understand impact.
4. Use `query_graph` with `pattern="tests_for"` to check coverage.

## Response Language

* Respond in Korean by default.
* Use another language only when the user explicitly requests it.

## Git / Change Policy

* Do not make changes outside the requested scope.
* Do not modify unrelated files.
* If a file must be deleted or renamed, explain the reason to the user first.
* When many code changes are made, avoid committing them all at once.
* Split commits by feature, responsibility, or logically related change set.

## Git Commit Message Rules

When creating a commit message for Git push, use the following format:

```text
<type> : <title>

<body>

<footer>
```

### Language

* Write commit messages in Korean.
* Keep the commit type in English.
* Use English only for conventional keywords such as `Close`, `Fixes`, or issue references when needed.

### Title

* Write the title in the format `<type> : <title>`.
* Keep the title within 50 characters.
* Clearly describe what changed.
* Do not end the title with a period.
* Write the title in Korean.

Example:

```text
feat : 로그인 기능 추가
```

### Body

* Write specific details about the change in Korean.
* Use `-` for multiple lines.
* Keep each line within 72 characters.

Example:

```text
feat : 로그인 기능 추가

- 사용자 ID와 비밀번호 입력 검증을 추가
- 로그인 실패 시 오류 메시지를 표시
```

### Footer

* Do not add `Co-Authored-By` trailers for AI tools unless the user explicitly requests it.
* Add related issue numbers when applicable.
* Use conventional English keywords such as `Close`, `Fixes`, or `Refs` when needed.

Example:

```text
feat : 로그인 기능 추가

- 사용자 ID와 비밀번호 입력 검증을 추가
- 로그인 실패 시 오류 메시지를 표시

Close #7
```

### Commit Types

* `feat` : Add a new feature
* `fix` : Fix a bug
* `docs` : Documentation changes
* `test` : Add or update tests
* `refact` : Code refactoring
* `style` : Changes that do not affect code meaning
* `chore` : Build system or package manager changes


## Code Rules

1. For C# WPF projects, strictly follow the MVVM pattern.

   * Clearly separate the responsibilities of View, ViewModel, and Model.
   * Minimize code-behind usage.
   * Use code-behind only for view-specific behavior that cannot be handled cleanly through binding, commands, behaviors, or attached properties.

2. Preserve the existing code style as much as possible.

   * Follow the naming, formatting, folder structure, and architectural conventions already used in the project.
   * If the existing code style is inconsistent or appears inappropriate, notify the user before making broad changes.
   * Guide the user toward an appropriate correction plan instead of silently rewriting large parts of the codebase.

3. Develop code in modular units.

   * Separate new functionality into clear modules, classes, services, components, or ViewModels as appropriate.
   * Avoid adding large, tightly coupled logic blocks to existing files.
   * If an additional architectural layer is required, create an appropriate folder or namespace for that layer.
   * Inform the user when a new layer, folder, or module structure is introduced and explain why it is needed.

4. Notify the user about unused code and guide them toward deletion.

   * Do not immediately delete code that appears unused, duplicated, or unreferenced.
   * Explain why the code appears to be unused.
   * Suggest a cleanup or deletion plan and apply the change only after user confirmation.

5. Define interfaces based on replaceability, testability, and architectural boundaries.

   * Create an interface when the implementation may change.
   * Create an interface when the dependency needs to be replaced with a Mock or Fake in tests.
   * Create an interface for external dependencies such as databases, files, networks, UI dialogs, devices, or hardware communication.
   * Create an interface when a ViewModel depending directly on a concrete implementation would break MVVM boundaries.
   * Create an interface when multiple implementations may exist.
   * Create an interface when the feature may expand into a plugin-based or modular structure.

   Do not create an interface when it provides no clear design benefit.

   * Do not create an interface for simple data models or DTOs.
   * Do not create an interface when there is only one implementation and it is unlikely to change.
   * Do not create an interface when the dependency does not need to be replaced in tests.
   * Do not create an interface for internal calculation logic that is close to a pure function.
   * Do not create interfaces for ViewModels by default.
   * Avoid creating interfaces that only increase naming and structural overhead without improving the design.

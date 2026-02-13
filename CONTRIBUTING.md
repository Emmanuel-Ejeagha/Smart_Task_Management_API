# Contributing to Smart Task Management API

Thank you for considering contributing ❤️  

This project is open source and welcomes contributions of all kinds — bug fixes, new features, documentation improvements, or issue reports.

Please read our [Code of Conduct](CODE_OF_CONDUCT.md). All contributors are expected to follow it.

---

## 🚀 Quick Start

1. **Fork** the repository.
2. **Clone** your fork:
   ```bash
   git clone git@github.com:Emmanuel-Ejeagha/Smart_Task_Management_API.git


3. **Set up the development environment** using the automated script:

   ```bash
   ./scripts/setup-and-run.sh
   ```

   > Requires Docker and .NET 8 SDK. See the [README](README.md) for details.
4. **Create a branch** for your changes:

   ```bash
   git checkout -b feature/your-feature-name
   ```

---

## 🐛 Reporting Bugs

If you discover a bug, please open an issue including:

* A clear and descriptive title
* Steps to reproduce the issue
* Expected vs. actual behavior
* Environment details (OS, .NET version, Docker version, etc.)

We aim to respond as quickly as possible.

---

## 💡 Suggesting Enhancements

We welcome new ideas. Please open an issue including:

* A clear title and description
* Why the enhancement would be valuable
* Optional implementation ideas (helpful but not required)

---

## 🛠️ Making Changes

### ✅ Code Standards

* Follow **Clean Architecture** principles — the **Domain layer must not reference external libraries**.
* Nullable reference types are enabled project-wide — use them properly.
* Add **XML documentation comments** for all public APIs.
* Use meaningful and descriptive names — avoid abbreviations such as `wrkItm` or `tmp`.
* Keep methods small and focused.

---

### ✅ Commit Message Convention

We follow a lightweight version of **Conventional Commits**:

```
type(scope): short description
```

#### Examples

```
feat(api): add WorkItem filtering by priority
fix(infrastructure): resolve Hangfire connection issue
docs(readme): update setup instructions
test(application): add validation unit tests
```

#### Common Types

* `feat` — new feature
* `fix` — bug fix
* `docs` — documentation
* `refactor` — internal restructuring without behavior change
* `test` — tests
* `chore` — maintenance tasks

---

## 🧪 Testing Requirements

Before submitting a Pull Request:

* Ensure all tests pass:

  ```bash
  dotnet test
  ```
* Add tests for new features or bug fixes.
* Do not reduce test coverage.

---

## 📦 Pull Request Guidelines

* Keep Pull Requests focused and small.
* Link related issues (e.g., `Closes #12`).
* Provide a clear description of the change.
* Ensure the project builds successfully and all tests pass.

---

## 🙌 Code Review Process

All Pull Requests require review before merging.

We may request changes — this helps maintain quality and consistency across the project.

---

## ⭐ Thank You

Your time and effort make this project better for everyone.
We truly appreciate every contribution.

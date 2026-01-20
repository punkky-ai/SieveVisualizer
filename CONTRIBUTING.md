# Contributing to SieveVisualizer

Thank you for your interest in contributing to **SieveVisualizer**!  
This project is designed as an **educational and visualization-focused** implementation of the Sieve of Eratosthenes, and contributions are very welcome.

---

## 🧭 Project Philosophy

- **Clarity over performance**  
- **Readability over clever tricks**  
- **Education-first design**

Code should be easy to understand for students, teachers, and beginners.

---

## 🛠 Development Setup

### Requirements
- Windows
- Visual Studio (recommended: Visual Studio 2026 or later)
- .NET WinForms workload installed

### Build & Run
1. Open the solution in Visual Studio
2. Build the project
3. Run the application

---

## 📐 Code Style Guidelines

- Use **clear and descriptive variable names**
- Prefer **readable logic** over compact one-liners
- Write **comments in English**
- Keep methods focused and reasonably small
- Avoid unnecessary abstractions

---

## 🎨 UI & Visualization Guidelines

- Avoid visual flickering (use double buffering)
- Keep colors soft and readable
- Animations should be:
  - Slow enough to follow
  - Deterministic (step-based, not time-based)
- Any visual change should clearly represent an algorithmic step

---

## 🧪 Testing Changes

Before submitting a pull request, please ensure:
- The project builds with **zero errors**
- The application runs correctly
- Core functionality works:
  - Start / Pause
  - Next Step
  - Reset
  - Visualization updates correctly

If your change affects rendering or animation, please test with multiple values of `N`.

---

## 🔀 Submitting a Pull Request

1. Fork the repository
2. Create a new branch for your change
3. Commit with a clear, descriptive message
4. Open a pull request and explain:
   - What was changed
   - Why the change is useful
   - Any design decisions made

Small, focused pull requests are preferred.

---

## 🐞 Reporting Issues

If you find a bug or have a suggestion:
- Open a GitHub Issue
- Include:
  - A clear description
  - Steps to reproduce (if applicable)
  - Screenshots or logs (if helpful)

---

## 🌱 Ideas for Contributions

Some ideas that fit well with this project:
- UI improvements
- New visualization modes
- Additional algorithm explanations
- Performance optimizations that **do not reduce clarity**
- Documentation improvements

---

## 📜 License

By contributing to this project, you agree that your contributions will be released under the same license as the project.

---

Thank you for helping make **SieveVisualizer** better! 🚀

# 🔢 SieveVisualizer

**SieveVisualizer** is a C# WinForms application that visually demonstrates the **Sieve of Eratosthenes** algorithm step by step.

This project focuses on **understanding over performance** by animating each stage of the algorithm, making it ideal for teaching, learning, and presenting how prime numbers are generated.

---

## Sieve of Eratosthenes (Algorithm Overview)

The **Sieve of Eratosthenes** is an efficient algorithm for finding all prime numbers less than or equal to a given number **N**.

### Steps

1. **Initialize**  
   List all integers from **2 to N** and mark them as *unprocessed*.

2. **Select a base prime (`p`)**  
   Start with `p = 2`.  
   If `p` is not marked as composite, it is a prime number.

3. **Mark multiples of `p`**  
   Mark all multiples of `p` as *composite*, starting from **`p²`**.  
   (Smaller multiples have already been marked by smaller primes.)

4. **Move to the next candidate**  
   Choose the next unmarked number greater than `p` and repeat the process.

5. **Stop condition**  
   Stop when **`p² > N`**.

6. **Result**  
   All remaining unmarked numbers are **prime numbers**.

---

## ✨ Features

- Step-by-step visualization of the Sieve of Eratosthenes
- Clear highlighting of:
  - Current base prime (`p`)
  - Multiples being marked as composite
- Color-coded states:
  - **Unknown** (not processed yet)
  - **Prime** (green)
  - **Composite** (soft orange)
- Adjustable parameters:
  - Upper bound `N`
  - Animation speed
- Two execution modes:
  - Automatic run
  - Manual step-by-step
- Flicker-free rendering using double buffering
- Optional frame-by-frame capture for video generation (FFmpeg-friendly)

---

## 🎯 Purpose

Most implementations of the Sieve of Eratosthenes focus on efficiency and speed.  
This project instead focuses on **clarity and intuition**.

By slowing down the algorithm and visualizing each operation, learners can clearly see:

- Why composite numbers are eliminated
- Why remaining numbers are prime
- How the algorithm progresses step by step

---

## 🖥️ Screens & Output

The application renders a grid of numbers and visually marks composites as the sieve progresses.

It also supports exporting animation frames, which can be converted into videos using **FFmpeg**

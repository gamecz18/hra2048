# 👨‍💻 sudo-man — hra2048 (C#)

```bash
$ sudo su
sudo: you are not in the sudoers file. This incident will be reported.
$ echo $STATUS
"have/you/tried/turning/it/off/and/on/again"
```

---

## 🎮 Co to je
**2048** napsaná v **C#** — a k tomu pár solverů, co to hrajou za tebe, když už ti dojde trpělivost.

---

## 🧠 Solvery (AI/strategie)
V projektu je rozhraní `IGameSolver` a několik implementací.

- **Random Solver**  
  Dělá tahy náhodně. Chaos as a Service.  
  Implementace: `Models/RandomSolver.cs`

- **Own Strategy** (*"Stupid Solver"*)  
  Moje “geniální” strategie: zkusím každý možný tah na cloně hry a vyberu ten, co dá lepší skóre.  
  Implementace: `Models/ownSolver.cs` (třída `OwnSolver`, `Name => "Stupid Solver"`)

- **Monte Carlo (CPU)**  
  Rollout simulace pro každý tah, vybere nejlepší průměr.  
  Implementace: `Models/Montecarlosolvercpu.cs` (třída `MonteCarloSolverCPU`, `Name => "CPU Monte Carlo"`)

- **Monte Carlo (GPU)** *(experiment / bordel / optional)*  
  Existuje implementace s GPU akcelerací (ILGPU), ale v porovnání solverů je to v UI aktuálně zakomentované.  
  Implementace: `Models/MonteCarloSolverGPU.cs` (třída `MonteCarloSolverGPU`)

---

## 📊 Benchmark / porovnání solverů
Repo umí spustit víc solverů a sbírat statistiky (score, max tile, tahy, čas…).

- UI porovnání: `ViewModels/MainWindowViewModel.cs` (metoda `RunAllSolvers()`)
- Statistiky/report: `Statistics/Statisticsrunner.cs` (`StatisticsRunner`, `SolverSummary`)

---

## 🏁 Spuštění

### .NET CLI
```bash
git clone https://github.com/gamecz18/hra2048.git
cd hra2048
dotnet run
```

### Visual Studio
1. Otevři `.sln`
2. Nastav startup projekt
3. F5

---

## 🏆 Professional Achievements
- **Master of Solutions**: Specializing in "have you tried turning it off and on again?"
- **Commit Artist**: Renowned for commits like `git commit -m "fixed nothing"`
- **Access Level**: Distinguished by complete absence of sudo privileges

---

## 🔬 Research Interests

```yaml
primary_focus: "Investigating why sudo access remains perpetually out of reach"
methodology: "Turning it off and on again"
success_rate: "To be determined"
publications: "Numerous commit messages documenting nothing"
```

---

## 📜 License
Nemám sudo. Ty taky ne.

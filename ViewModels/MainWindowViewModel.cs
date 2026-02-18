using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hra2048.Models;
using hra2048.Statistics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace hra2048.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly GameEngine _game;
        private Dictionary<string, List<GameStats>>? _lastResults;
        public Window? Window { get; set; }
        public ObservableCollection<TileViewModel> Tiles { get; } = new();

        [ObservableProperty] private int _score;
        [ObservableProperty] private string _statusText = "Playing";
        [ObservableProperty] private string _resultsText = "Výsledky se zobrazí zde...\n\nKlikni na tlačítko solveru pro spuštění benchmarku.";
        [ObservableProperty] private string _progressText = "";
        [ObservableProperty] private double _progressValue;
        [ObservableProperty] private bool _isRunning;

        public bool IsNotRunning => !IsRunning;

        public MainWindowViewModel()
        {
            _game = new GameEngine();
            for (int i = 0; i < 16; i++) Tiles.Add(new TileViewModel());
            UpdateBoard();
        }

        // Když se změní IsRunning, aktualizuj i IsNotRunning
        partial void OnIsRunningChanged(bool value)
        {
            OnPropertyChanged(nameof(IsNotRunning));
        }

        [RelayCommand]
        public void NewGame()
        {
            _game.StartNewGame();
            UpdateBoard();
        }

       
        

        public void ManualMove(Direction d)
        {
            if (_game.Move(d))
            {
                UpdateBoard();
            }
        }

        private void UpdateBoard()
        {
            Score = _game.Score;
            StatusText = _game.State switch
            {
                GameState.Won => "🎉 Výhra!",
                GameState.Lost => "💀 Prohra!",
                _ => $"Max: {_game.GetMaxTile()}"
            };

            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 4; c++)
                {
                    Tiles[r * 4 + c].Update(_game.Board[r, c]);
                }
            }
        }

        #region Benchmark Commands

        [RelayCommand]
        private async Task RunRandom()
        {
            await RunSolverBenchmark(new RandomSolver());
        }

        [RelayCommand]
        private async Task RunOwn()
        {
            await RunSolverBenchmark(new OwnSolver());
        }

        [RelayCommand]
        private async Task Save()
        {
            if (_lastResults == null) return;

            
            var topLevel = TopLevel.GetTopLevel(Window);
            if (topLevel == null) return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Uložit výsledky",
                SuggestedFileName = "results.csv",
                FileTypeChoices = new[]
                {
            new FilePickerFileType("CSV") { Patterns = new[] { "*.csv" } }
        }
            });

            if (file != null)
            {
                var runner = new StatisticsRunner(_lastResults.First().Value.Count);
                runner.ExportToCsv(_lastResults, file.Path.LocalPath);
            }
        }
        [RelayCommand]
        private async Task RunMonteCarloGPU()
        {
            await RunSolverBenchmark(new MonteCarloSolverGPU(200));
        }
        [RelayCommand]
        private async Task RunMonteCarlo()
        {
            await RunSolverBenchmark(new MonteCarloSolverCPU(100, 200));
        }

        [RelayCommand]
        private async Task RunAllSolvers()
        {
            IsRunning = true;
            var allResults = new StringBuilder();
            allResults.AppendLine("═══════════════════════════════════════");
            allResults.AppendLine("     SROVNÁNÍ VŠECH SOLVERŮ");
            allResults.AppendLine("═══════════════════════════════════════\n");
            List<(string Name, IGameSolver Solver)> solvers = new List<(string Name, IGameSolver Solver)>
            {
                ("Random", new RandomSolver()),
                ("Own Strategy", new OwnSolver()),
                ("Monte Carlo CPU", new MonteCarloSolverCPU(100, 200)),
                ("Monte Carlo GPU", new MonteCarloSolverGPU(202))
             };
            List<(string Name, SolverSummary Summary)> summaries = new List<(string Name, SolverSummary Summary)>();

            foreach (var (name, solver) in solvers)
            {
                ProgressText = $"Testuji {name}...";

                var results = new List<GameStats>();
                int gamesCount = 20;
                int completedGames = 0;
    

                var wholeStopwatch = Stopwatch.StartNew();

                await Task.Run(() =>
                {

                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {

                        ProgressText = $"{name}: {0}/{gamesCount} her";
                    });
                    for (int i = 0; i < gamesCount; i++)
                    {
                        
                        var game = new GameEngine();
                        var stopwatch = Stopwatch.StartNew();

                        while (game.State == GameState.Playing)
                        {
                            var moves = game.GetAvailableMoves();
                            if (moves.Count == 0) break;
                            game.Move(solver.GetNextMove(game));
                        }

                        stopwatch.Stop();
                        var stats = GameStats.FromGame(game, solver.Name, stopwatch.Elapsed);


                            results.Add(stats);

                        int completed = Interlocked.Increment(ref completedGames);
                        var progress = completed * 100 / gamesCount;

                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            ProgressValue = progress;
                            ProgressText = $"{name}: {completed}/{gamesCount} her";
                        });
                    }
                });

                wholeStopwatch.Stop();
                var runner = new StatisticsRunner(gamesCount);
                var summary = runner.CalculateStatistics(results);
                summaries.Add((name, summary));
                _lastResults ??= new Dictionary<string, List<GameStats>>();
                _lastResults[name] = results;

                allResults.AppendLine($"─── {name} ───");
                allResults.AppendLine($"  Průměrné skóre: {summary.AverageScore:F0}");
                allResults.AppendLine($"  Max dlaždice:   {summary.BestMaxTile} (avg: {summary.AverageMaxTile:F0})");
                allResults.AppendLine($"  Výhry/Prohry:   {summary.WinCount}/{summary.LossCount}");
                allResults.AppendLine($"  Čas:            {wholeStopwatch.Elapsed.TotalSeconds:F2}s\n");
            }


            allResults.AppendLine("\n═══ SROVNÁNÍ ═══");
            allResults.AppendLine("┌────────────────────┬──────────┬──────────┬──────────┬─────────┐");
            allResults.AppendLine("│ Solver             │ Avg Score│ Max Tile │  W/L     │   Time  │");
            allResults.AppendLine("├────────────────────┼──────────┼──────────┼──────────┼─────────┤");

            foreach (var (name, summary) in summaries)
            {
                double winRate = summary.GamesPlayed > 0 ? (double)summary.WinCount / summary.GamesPlayed * 100 : 0;
                string winLoss = $"{summary.WinCount}/{summary.LossCount}";
                allResults.AppendLine($"│ {name,-18} │ {summary.AverageScore,8:F0} │ {summary.BestMaxTile,8} │ {winLoss,4} ({winRate,3:F0}%)│ {summary.TotalDuration.TotalSeconds,6:F1}s │");
                
            }

            allResults.AppendLine("└────────────────────┴──────────┴──────────┴──────────┴─────────┘");


            var best = summaries.OrderByDescending(s => s.Summary.AverageScore).First();
            allResults.AppendLine($"\nVítěz: {best.Name} (průměr: {best.Summary.AverageScore:F0})");

            ResultsText = allResults.ToString();
            ProgressText = "Všechny solvery dokončeny!";
            ProgressValue = 100;
            IsRunning = false;

        }

        [RelayCommand]
        private void ClearResults()
        {
            ResultsText = "Výsledky vymazány.\n\nKlikni na tlačítko solveru pro spuštění benchmarku.";
            ProgressText = "";
            ProgressValue = 0;
        }

        /// <summary>
        /// Spustí benchmark pro jeden solver
        /// </summary>
        private async Task RunSolverBenchmark(IGameSolver solver)
        {
            IsRunning = true;
            ProgressValue = 0;
            ProgressText = $"Spouštím {solver.Name}...";

            var results = new List<GameStats>();
            var sb = new StringBuilder();
            sb.AppendLine($"═══ {solver.Name} - Benchmark ═══\n");

            int gamesCount = 20;
            Object lockObj = new();
            Stopwatch wholeStopwatch = new Stopwatch();
            wholeStopwatch.Start();
            await Task.Run(() =>
            {

                int completedGames = 0;
                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 5
                };
                Parallel.For(0, gamesCount, options, i =>
                {
                    var game = new GameEngine();
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                    while (game.State == GameState.Playing)
                    {
                        var moves = game.GetAvailableMoves();
                        if (moves.Count == 0) break;
                        game.Move(solver.GetNextMove(game));
                    }

                    stopwatch.Stop();
                    var stats = GameStats.FromGame(game, solver.Name, stopwatch.Elapsed);
                    lock (lockObj)
                        results.Add(stats);

                    // Update progress na UI vlákně
                    int completed = Interlocked.Increment(ref completedGames);
                    var progress = completed * 100 / gamesCount;
                    var score = stats.FinalScore;
                    var maxTile = stats.MaxTile;

                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        ProgressValue = progress;
                        ProgressText = $"Hra {completed}/{gamesCount}: Score={score}, MaxTile={maxTile}";
                    });
                });
            });
            wholeStopwatch.Stop();
            // Formátuj výsledky
            var runner = new StatisticsRunner(gamesCount);
            var summary = runner.CalculateStatistics(results);
            _lastResults ??= new Dictionary<string, List<GameStats>>();
            _lastResults[solver.Name] = results;
            sb.AppendLine(FormatSummary(summary));
            sb.AppendLine($"\nCelkový čas her: {wholeStopwatch.Elapsed}");
            sb.AppendLine("\n─── Jednotlivé hry ───");
            for (int i = 0; i < results.Count; i++)
            {
                var r = results[i];
                sb.AppendLine($"  #{i + 1}: Score={r.FinalScore,5}, MaxTile={r.MaxTile,4}, Moves={r.TotalMoves,3}, {r.Duration.TotalMilliseconds:F0}ms");
            }

            ResultsText = sb.ToString();
            ProgressText = "✅ Hotovo!";
            IsRunning = false;
        }

        /// <summary>
        /// Formátuje souhrnné statistiky pro zobrazení
        /// </summary>
        private string FormatSummary(SolverSummary s)
        {
            double winRate = s.GamesPlayed > 0 ? (double)s.WinCount / s.GamesPlayed * 100 : 0;
            double totalMoves = s.AverageTotalMoves > 0 ? s.AverageTotalMoves : 1;

            var sb = new StringBuilder();
            sb.AppendLine("┌─────────────────────────────────────┐");
            sb.AppendLine($"│ SKÓRE                               │");
            sb.AppendLine($"│   Nejlepší:  {s.BestScore,8}              │");
            sb.AppendLine($"│   Nejhorší:  {s.WorstScore,8}              │");
            sb.AppendLine($"│   Průměr:    {s.AverageScore,8:F0}              │");
            sb.AppendLine($"│   Medián:    {s.MedianScore,8:F0}              │");
            sb.AppendLine("├─────────────────────────────────────┤");
            sb.AppendLine($"│ VÝSLEDKY                            │");
            sb.AppendLine($"│   Výhry:  {s.WinCount,2} ({winRate,3:F0}%)                │");
            sb.AppendLine($"│   Prohry: {s.LossCount,2} ({100 - winRate,3:F0}%)                │");
            sb.AppendLine($"│   Max dlaždice: {s.BestMaxTile,-5} (avg: {s.AverageMaxTile:F0})  │");
            sb.AppendLine("├─────────────────────────────────────┤");
            sb.AppendLine($"│ TAHY (průměr: {s.AverageTotalMoves:F0})                  │");
            sb.AppendLine($"│   ↑ {s.AverageMovesUp,5:F0} ({s.AverageMovesUp / totalMoves * 100,4:F0}%)  ↓ {s.AverageMovesDown,5:F0} ({s.AverageMovesDown / totalMoves * 100,4:F0}%)   │");
            sb.AppendLine($"│   ← {s.AverageMovesLeft,5:F0} ({s.AverageMovesLeft / totalMoves * 100,4:F0}%)  → {s.AverageMovesRight,5:F0} ({s.AverageMovesRight / totalMoves * 100,4:F0}%)   │");
            sb.AppendLine("├─────────────────────────────────────┤");
            sb.AppendLine($"│ ČAS                                 │");
            sb.AppendLine($"│   Celkem:    {s.TotalDuration.TotalSeconds,6:F2}s             │");
            sb.AppendLine($"│   Průměr:    {s.AverageDuration.TotalMilliseconds,6:F0}ms             │");
            sb.AppendLine("└─────────────────────────────────────┘");

            return sb.ToString();
        }

        #endregion
    }
}
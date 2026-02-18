using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using hra2048.Models;

namespace hra2048.Statistics
{
    /// <summary>
    /// Spouští solvery a generuje statistické reporty
    /// </summary>
    public class StatisticsRunner
    {
        private readonly int _gamesPerSolver;

        /// <summary>
        /// Vytvoří nový StatisticsRunner
        /// </summary>
        /// <param name="gamesPerSolver">Počet her na solver (default 10)</param>
        public StatisticsRunner(int gamesPerSolver = 10)
        {
            _gamesPerSolver = gamesPerSolver;
        }

       
        public List<GameStats> RunSolver(IGameSolver solver)
        {
            var results = new List<GameStats>();
        
            Debug.WriteLine($"\nSpouštím {solver.Name}...");

            for (int i = 0; i < _gamesPerSolver; i++)
            {
                var game = new GameEngine();
                var stopwatch = Stopwatch.StartNew();

                // Hraj dokud hra neskončí
                while (game.State == GameState.Playing)
                {
                    var availableMoves = game.GetAvailableMoves();
                    if (availableMoves.Count == 0) break;

                    Direction move = solver.GetNextMove(game);
                    game.Move(move);
                }

                stopwatch.Stop();

                var stats = GameStats.FromGame(game, solver.Name, stopwatch.Elapsed);
                results.Add(stats);

          
                Debug.Write($"\r  Hra {i + 1}/{_gamesPerSolver}: Score={stats.FinalScore}, MaxTile={stats.MaxTile}");
            }

          
            return results;
        }

   
        public Dictionary<string, List<GameStats>> RunAllSolvers(List<IGameSolver> solvers)
        {
            var allResults = new Dictionary<string, List<GameStats>>();

            Debug.WriteLine("╔════════════════════════════════════════╗");
            Debug.WriteLine("║     2048 SOLVER BENCHMARK START        ║");
            Debug.WriteLine($"║     Her na solver: {_gamesPerSolver,-10}           ║");
            Debug.WriteLine("╚════════════════════════════════════════╝");

            foreach (var solver in solvers)
            {
                var results = RunSolver(solver);
                allResults[solver.Name] = results;
            }

            return allResults;
        }

        public SolverSummary CalculateStatistics(List<GameStats> results)
        {
            if (results.Count == 0)
                return new SolverSummary();

            var scores = results.Select(r => r.FinalScore).ToList();
            var maxTiles = results.Select(r => r.MaxTile).ToList();
            var durations = results.Select(r => r.Duration.TotalSeconds).ToList();

            return new SolverSummary
            {
                SolverName = results[0].SolverName,
                GamesPlayed = results.Count,

                // Skóre
                BestScore = scores.Max(),
                WorstScore = scores.Min(),
                AverageScore = scores.Average(),
                MedianScore = GetMedian(scores),

                // Výhry/prohry
                WinCount = results.Count(r => r.Won),
                LossCount = results.Count(r => !r.Won),

                // Max dlaždice
                BestMaxTile = maxTiles.Max(),
                AverageMaxTile = maxTiles.Average(),

                // Tahy
                AverageTotalMoves = results.Average(r => r.TotalMoves),
                AverageMovesUp = results.Average(r => r.MovesUp),
                AverageMovesDown = results.Average(r => r.MovesDown),
                AverageMovesLeft = results.Average(r => r.MovesLeft),
                AverageMovesRight = results.Average(r => r.MovesRight),

                // Čas
                TotalDuration = TimeSpan.FromSeconds(durations.Sum()),
                AverageDuration = TimeSpan.FromSeconds(durations.Average())
            };
        }

  
        public void PrintReport(List<GameStats> results)
        {
            var summary = CalculateStatistics(results);
            Debug.WriteLine(FormatReport(summary));
        }

    
        public void PrintAllReports(Dictionary<string, List<GameStats>> allResults)
        {
            foreach (var kvp in allResults)
            {
                PrintReport(kvp.Value);
            }

          
            PrintComparisonTable(allResults);
        }

  
        private void PrintComparisonTable(Dictionary<string, List<GameStats>> allResults)
        {
            var summaries = allResults.Values
                .Select(r => CalculateStatistics(r))
                .OrderByDescending(s => s.AverageScore)
                .ToList();

            Debug.WriteLine("\n╔════════════════════════════════════════════════════════════════════════╗");
            Debug.WriteLine("║                        POROVNÁNÍ SOLVERŮ                               ║");
            Debug.WriteLine("╠════════════════════════════════════════════════════════════════════════╣");
            Debug.WriteLine("║ Solver              │ Avg Score │ Best │ Win% │ Avg MaxTile │ Avg Time ║");
            Debug.WriteLine("╠════════════════════════════════════════════════════════════════════════╣");

            foreach (var s in summaries)
            {
                double winRate = s.GamesPlayed > 0 ? (double)s.WinCount / s.GamesPlayed * 100 : 0;
                Debug.WriteLine($"║ {s.SolverName,-19}│ {s.AverageScore,9:F0} │ {s.BestScore,4} │ {winRate,3:F0}% │ {s.AverageMaxTile,11:F0} │ {s.AverageDuration.TotalSeconds,7:F2}s ║");
            }

            Debug.WriteLine("╚════════════════════════════════════════════════════════════════════════╝");
        }

        /// <summary>
        /// Formátuje report pro jeden solver
        /// </summary>
        private string FormatReport(SolverSummary s)
        {
            double winRate = s.GamesPlayed > 0 ? (double)s.WinCount / s.GamesPlayed * 100 : 0;
            double totalMoves = s.AverageTotalMoves > 0 ? s.AverageTotalMoves : 1;

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("╔══════════════════════════════════════════════════════════════════╗");
            sb.AppendLine($"║  Solver: {s.SolverName,-25} Her: {s.GamesPlayed,-10}      ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════════╣");
            sb.AppendLine("║ SKÓRE                                                            ║");
            sb.AppendLine($"║   Nejlepší: {s.BestScore,8}        Nejhorší: {s.WorstScore,8}                 ║");
            sb.AppendLine($"║   Průměr:   {s.AverageScore,8:F0}        Medián:   {s.MedianScore,8:F0}                 ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════════╣");
            sb.AppendLine("║ VÝSLEDKY                                                         ║");
            sb.AppendLine($"║   Výhry: {s.WinCount} ({winRate:F0}%)             Prohry: {s.LossCount} ({100 - winRate:F0}%)                   ║");
            sb.AppendLine($"║   Nejlepší max dlaždice: {s.BestMaxTile,-6}  Průměrná: {s.AverageMaxTile:F0}              ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════════╣");
            sb.AppendLine("║ TAHY                                                             ║");
            sb.AppendLine($"║   Celkem průměr: {s.AverageTotalMoves:F0}                                            ║");
            sb.AppendLine($"║   ↑ Up:    {s.AverageMovesUp,5:F0} ({s.AverageMovesUp / totalMoves * 100,4:F0}%)    ↓ Down:  {s.AverageMovesDown,5:F0} ({s.AverageMovesDown / totalMoves * 100,4:F0}%)       ║");
            sb.AppendLine($"║   ← Left: {s.AverageMovesLeft,5:F0} ({s.AverageMovesLeft / totalMoves * 100,4:F0}%)    → Right: {s.AverageMovesRight,5:F0} ({s.AverageMovesRight / totalMoves * 100,4:F0}%)       ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════════╣");
            sb.AppendLine("║ ČAS                                                              ║");
            sb.AppendLine($"║   Celkem: {s.TotalDuration.TotalSeconds,6:F2}s           Průměr/hra: {s.AverageDuration.TotalSeconds,6:F3}s          ║");
            sb.AppendLine("╚══════════════════════════════════════════════════════════════════╝");

            return sb.ToString();
        }

    
        public void ExportToCsv(Dictionary<string, List<GameStats>> allResults, string filePath)
        {
            var sb = new StringBuilder();

          
            sb.AppendLine("Solver,Game,FinalScore,MaxTile,Won,TotalMoves,MovesUp,MovesDown,MovesLeft,MovesRight,DurationSec");

         
            foreach (var kvp in allResults)
            {
                int gameNum = 1;
                foreach (var stats in kvp.Value)
                {
                    sb.AppendLine($"{stats.SolverName},{gameNum},{stats.FinalScore},{stats.MaxTile}," +
                                  $"{stats.Won},{stats.TotalMoves},{stats.MovesUp},{stats.MovesDown}," +
                                  $"{stats.MovesLeft},{stats.MovesRight},{stats.Duration.TotalSeconds:F3}");
                    gameNum++;
                }
            }

            System.IO.File.WriteAllText(filePath, sb.ToString());
            Debug.WriteLine($"\nVýsledky exportovány do: {filePath}");
        }

        /// <summary>
        /// Vypočítá medián ze seznamu čísel
        /// </summary>
        private double GetMedian(List<int> numbers)
        {
            var sorted = numbers.OrderBy(n => n).ToList();
            int count = sorted.Count;

            if (count == 0) return 0;
            if (count % 2 == 0)
                return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
            else
                return sorted[count / 2];
        }
    }

    /// <summary>
    /// Souhrnné statistiky pro jeden solver
    /// </summary>
    public class SolverSummary
    {
        public string SolverName { get; set; } = "";
        public int GamesPlayed { get; set; }

        // Skóre
        public int BestScore { get; set; }
        public int WorstScore { get; set; }
        public double AverageScore { get; set; }
        public double MedianScore { get; set; }

        // Výhry/prohry
        public int WinCount { get; set; }
        public int LossCount { get; set; }

        // Max dlaždice
        public int BestMaxTile { get; set; }
        public double AverageMaxTile { get; set; }

        // Tahy
        public double AverageTotalMoves { get; set; }
        public double AverageMovesUp { get; set; }
        public double AverageMovesDown { get; set; }
        public double AverageMovesLeft { get; set; }
        public double AverageMovesRight { get; set; }

        // Čas
        public TimeSpan TotalDuration { get; set; }
        public TimeSpan AverageDuration { get; set; }
    }
}
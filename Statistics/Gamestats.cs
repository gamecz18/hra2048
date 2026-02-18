using System;

namespace hra2048.Statistics
{
    /// <summary>
    /// Uchovává výsledky jedné hry
    /// </summary>
    public class GameStats
    {
        /// <summary>Název solveru, který hru hrál</summary>
        public string SolverName { get; set; } = "";

        /// <summary>Konečné skóre hry</summary>
        public int FinalScore { get; set; }

        /// <summary>Nejvyšší dosažená hodnota dlaždice</summary>
        public int MaxTile { get; set; }

        /// <summary>True pokud hráč vyhrál (dosáhl 2048)</summary>
        public bool Won { get; set; }

        /// <summary>Celkový počet tahů</summary>
        public int TotalMoves { get; set; }

        /// <summary>Počet tahů nahoru</summary>
        public int MovesUp { get; set; }

        /// <summary>Počet tahů dolů</summary>
        public int MovesDown { get; set; }

        /// <summary>Počet tahů doleva</summary>
        public int MovesLeft { get; set; }

        /// <summary>Počet tahů doprava</summary>
        public int MovesRight { get; set; }

        /// <summary>Doba trvání hry</summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Vytvoří GameStats z dokončené hry
        /// </summary>
        public static GameStats FromGame(Models.GameEngine game, string solverName, TimeSpan duration)
        {
            return new GameStats
            {
                SolverName = solverName,
                FinalScore = game.Score,
                MaxTile = game.GetMaxTile(),
                Won = game.State == Models.GameState.Won,
                TotalMoves = game.TotalMoves,
                MovesUp = game.MovesUp,
                MovesDown = game.MovesDown,
                MovesLeft = game.MovesLeft,
                MovesRight = game.MovesRight,
                Duration = duration
            };
        }

        public override string ToString()
        {
            return $"[{SolverName}] Score: {FinalScore}, MaxTile: {MaxTile}, " +
                   $"Won: {Won}, Moves: {TotalMoves}, Time: {Duration.TotalSeconds:F2}s";
        }
    }
}
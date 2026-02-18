using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace hra2048.Models
{
    public class MonteCarloSolverCPU : IGameSolver
    {
        public string Name => "CPU Monte Carlo";
        public Action<string>? OnProgress { get; set; }

        private readonly int _simulationsPerMove;
        private readonly int _maxStepsPerSimulation;

        
        public MonteCarloSolverCPU(int simulationsPerMove = 100, int maxStepsPerSimulation = 200)
        {
            _simulationsPerMove = simulationsPerMove;
            _maxStepsPerSimulation = maxStepsPerSimulation == 0 ? int.MaxValue : maxStepsPerSimulation;
        }

        public Direction GetNextMove(GameEngine game)
        {
            List<Direction> availableMoves = game.GetAvailableMoves();
            if (availableMoves.Count == 0)
                return Direction.Up;

            Direction bestMove = availableMoves[0];
            double bestScore = double.MinValue;
            object lockObj = new object();

            foreach (Direction move in availableMoves)
            {
                double totalScore = 0;

                Parallel.For(0, _simulationsPerMove,
                    () => 0.0,
                    (sim, state, localScore) =>
                    {
                        GameEngine clone = game.Clone();
                        clone.Move(move);

                        int steps = 0;
                        while (clone.State == GameState.Playing && steps < _maxStepsPerSimulation)
                        {
                            List<Direction> moves = clone.GetAvailableMoves();
                            if (moves.Count == 0) break;
                            clone.Move(moves[Random.Shared.Next(moves.Count)]);
                            steps++;
                        }

                        double eval = clone.Score
                            + clone.GetEmptyCount() * 128
                            + clone.GetMaxTile() * 32;

                        return localScore + eval;
                    },
                    localScore =>
                    {
                        lock (lockObj) { totalScore += localScore; }
                    });

                double avgScore = totalScore / _simulationsPerMove;
                OnProgress?.Invoke($"  {move,-5}: avg = {avgScore:F0}");

                if (avgScore > bestScore)
                {
                    bestScore = avgScore;
                    bestMove = move;
                }
            }

            OnProgress?.Invoke($"Nejlepší tah: {bestMove}");
            return bestMove;
        }
    }
}
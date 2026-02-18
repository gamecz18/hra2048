using ILGPU;
using ILGPU.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
namespace hra2048.Models
{
    public class MonteCarloSolverGPU : IGameSolver, IDisposable
    {

        public string Name => "CPU Monte Carlo";
        public Action<string>? OnProgress { get; set; }
        private int _simulationsPerMove { get; }
        private int _maxStepsPerSimulation = 100;

        // gpu bordel
        // Zkompilovaný kernel (připravený pro GPU)
        private readonly Action<Index1D, ArrayView<int>, ArrayView<int>> _testKernel;
        private MemoryBuffer1D<int, Stride1D.Dense>? _boardBuffer;
        private MemoryBuffer1D<int, Stride1D.Dense>? _scoresBuffer;
        private MemoryBuffer1D<uint, Stride1D.Dense>? _seedsBuffer;
        private bool disposedValue;
        private readonly Context _context;
        private readonly Accelerator _accelerator;

        public MonteCarloSolverGPU(int simulationsPerMove = 10000, int maxStepsPerSimulation = 5)
        {
            _simulationsPerMove = simulationsPerMove;
            _context = Context.Create(builder => builder.Default().EnableAlgorithms());
            _accelerator = _context.GetPreferredDevice(preferCPU: false)
                                   .CreateAccelerator(_context);

            _testKernel = _accelerator.LoadAutoGroupedStreamKernel<
               Index1D,
               ArrayView<int>,
               ArrayView<int>>(TestKernel);
            // Debug info
            Debug.WriteLine($"GPU: {_accelerator.Name}");
            Debug.WriteLine($"Max threads: {_accelerator.MaxNumThreads}");
            _maxStepsPerSimulation = maxStepsPerSimulation;
        }

        public Direction GetNextMove(GameEngine game)
        {
            List<Direction> aGM = game.GetAvailableMoves();
            if (aGM.Count == 0)
                return Direction.Up;

            Direction bestMove = aGM[0];
            double bestScore = double.MinValue;
            object lockObj = new object();

            foreach (Direction move in aGM)
            {
                double totalScore = 0;

                Parallel.For(0, _simulationsPerMove,
                    () => 0.0,
                    (sim, state, localScore) =>
                    {
                        GameEngine gameClone = game.Clone();
                        gameClone.Move(move);
                        int steps = 0;
                        while (gameClone.State == GameState.Playing && steps < _maxStepsPerSimulation)
                        {
                            List<Direction> moves = gameClone.GetAvailableMoves();
                            if (moves.Count == 0) break;
                            gameClone.Move(moves[Random.Shared.Next(moves.Count)]);
                            steps++;
                        }

                        double eval = gameClone.Score + gameClone.GetEmptyCount() * 128 + gameClone.GetMaxTile() * 32;
                        return localScore + eval;
                    },
                    localScore =>
                    {
                        lock (lockObj) { totalScore += localScore; }
                    });

                double avgScore = totalScore / _simulationsPerMove;

                if (avgScore > bestScore)
                {
                    bestScore = avgScore;
                    bestMove = move;
                }
            }

            return bestMove;
        }

        /// <summary>
        /// Příklad jak zavolat GPU kernel
        /// </summary>
        private void TestKernelExecution(int[] board)
        {
            int threadCount = 16000;

            // 1. Vytvoř buffery pro 16,000 prvků
            using var boardBuffer = _accelerator.Allocate1D<int>(threadCount);
            using var scoresBuffer = _accelerator.Allocate1D<int>(threadCount);

            // 2. ✅ OPRAVENO: Zkopíruj pouze prvních 16 prvků do bufferu
            boardBuffer.View.SubView(0, 16).CopyFromCPU(board);
            //          👆 Vytvoř sub-view s 16 prvky

            // Alternativa - vyplň zbytek bufferu nulami (volitelné)
            // boardBuffer.MemSetToZero();
            // boardBuffer.View.SubView(0, 16).CopyFromCPU(board);

            // 3. Spusť kernel na 16,000 vláknech
            _testKernel(threadCount, boardBuffer.View, scoresBuffer.View);

            // 4. Počkej na dokončení
            _accelerator.Synchronize();

            // 5. Zkopíruj výsledky
            int[] scores = scoresBuffer.GetAsArray1D();

            // 6. Debug - vypiš jen prvních 16 výsledků
            Debug.WriteLine("Scores from GPU (first 16):");
            for (int i = 0; i < 16; i++)
            {
                Debug.WriteLine($"  [{i}] = {scores[i]}");
            }
        }

        static void TestKernel(Index1D index, ArrayView<int> board, ArrayView<int> scores)
        {
            // Bezpečná varianta - zkontroluj hranice
            if (index < board.Length)
            {
                scores[index] = board[index];
            }
            else
            {
                // Vlákna nad 16. pozicí pouze zapíší 0
                scores[index] = 0;
            }
        }
        private int[] FlattenBoard(int[,] board)
        {
            int[] flat = new int[16];
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 4; c++)
                    flat[r * 4 + c] = board[r, c];
            return flat;
        }














        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {

                    _accelerator?.Dispose();
                    _context?.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

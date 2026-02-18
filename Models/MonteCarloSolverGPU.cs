using System;
using System.Collections.Generic;
using System.Linq;
using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Algorithms.Random;
using ILGPU.Runtime;
using hra2048.Models;

namespace hra2048.Models
{
    public class MonteCarloSolverGPU : IGameSolver, IDisposable
    {
        public string Name => "GPU Monte Carlo (ILGPU)";

        private readonly Context _context;
        private readonly Accelerator _accelerator;
        private readonly int _simulationsPerMove;

        // Konstruktor
        public MonteCarloSolverGPU(int simulationsPerMove = 1000)
        {
            _simulationsPerMove = simulationsPerMove;

            // Inicializace ILGPU
            _context = Context.Create(builder => builder.Default().EnableAlgorithms());
            _accelerator = _context.GetPreferredDevice(preferCPU: false)
                                   .CreateAccelerator(_context);
        }

        public void Dispose()
        {
            _accelerator.Dispose();
            _context.Dispose();
        }

        // OPRAVA 1: Metoda musí přijímat GameEngine, aby splnila Interface
        public Direction GetNextMove(GameEngine game)
        {
            // Z GameEngine si vytáhneme jen to pole int[,]
            return GetNextMoveInternal(game.Board);
        }

        // Vnitřní logika, která pracuje s polem
        private Direction GetNextMoveInternal(int[,] board)
        {
            ulong currentBoard = BoardToUlong(board);

            var scores = new float[4];
            var legalMoves = new bool[4];

            for (int i = 0; i < 4; i++)
            {
                int direction = i;

                if (!CanMove(currentBoard, direction))
                {
                    legalMoves[i] = false;
                    scores[i] = -1;
                    continue;
                }
                legalMoves[i] = true;

                scores[i] = RunGpuSimulation(currentBoard, direction);
            }

            float maxScore = -1;
            int bestDirection = -1;

            // Fallback
            List<int> validIndices = new List<int>();
            for (int i = 0; i < 4; i++) if (legalMoves[i]) validIndices.Add(i);

            if (validIndices.Count == 0) return Direction.Up;

            for (int i = 0; i < 4; i++)
            {
                if (legalMoves[i] && scores[i] > maxScore)
                {
                    maxScore = scores[i];
                    bestDirection = i;
                }
            }

            return (Direction)bestDirection;
        }

        private float RunGpuSimulation(ulong boardRaw, int firstMoveDir)
        {
            using var rngBuffer = _accelerator.Allocate1D<XorShift128>(_simulationsPerMove);

            var rngCpu = new XorShift128[_simulationsPerMove];
            var sysRand = new Random();

            for (int i = 0; i < _simulationsPerMove; i++)
            {
                // OPRAVA 2: XorShift128 vyžaduje 4x uint (ne 2x ulong)
                rngCpu[i] = new XorShift128(
                    (uint)sysRand.Next(),
                    (uint)sysRand.Next(),
                    (uint)sysRand.Next(),
                    (uint)sysRand.Next()
                );
            }
            rngBuffer.CopyFromCPU(rngCpu);

            using var scoreBuffer = _accelerator.Allocate1D<int>(_simulationsPerMove);

            var kernel = _accelerator.LoadAutoGroupedStreamKernel<
                Index1D,
                ArrayView<XorShift128>,
                ArrayView<int>,
                ulong,
                int>(SimulationKernel);

            kernel(_simulationsPerMove, rngBuffer.View, scoreBuffer.View, boardRaw, firstMoveDir);

            _accelerator.Synchronize();

            int[] results = scoreBuffer.GetAsArray1D();
            if (results.Length == 0) return 0;
            return (float)results.Average();
        }

        // ==========================================================================================
        // GPU KERNEL
        // ==========================================================================================
        static void SimulationKernel(
            Index1D index,
            ArrayView<XorShift128> rngs,
            ArrayView<int> outputScores,
            ulong initialBoard,
            int firstMoveDir)
        {
            var rng = rngs[index];

            ulong board = initialBoard;
            int score = 0;

            if (!ApplyMove(ref board, ref score, firstMoveDir))
            {
                outputScores[index] = 0;
                return;
            }
            SpawnRandomTile(ref board, ref rng);

            bool playing = true;
            int moves = 0;
            while (playing && moves < 200)
            {
                // OPRAVA 3: Místo NextInt() použijeme Next() a Math.Abs pro jistotu
                int randomDir = Math.Abs(rng.Next()) % 4;

                bool moved = false;
                for (int k = 0; k < 4; k++)
                {
                    int tryDir = (randomDir + k) % 4;
                    if (ApplyMove(ref board, ref score, tryDir))
                    {
                        moved = true;
                        break;
                    }
                }

                if (!moved)
                {
                    playing = false;
                }
                else
                {
                    SpawnRandomTile(ref board, ref rng);
                    moves++;
                }
            }

            outputScores[index] = score;
            rngs[index] = rng;
        }

        static void SpawnRandomTile(ref ulong board, ref XorShift128 rng)
        {
            int emptyCount = 0;
            for (int i = 0; i < 16; i++)
            {
                if (((board >> (i * 4)) & 0xF) == 0) emptyCount++;
            }

            if (emptyCount == 0) return;

            // OPRAVA 3: Použití Next()
            int targetIndex = Math.Abs(rng.Next()) % emptyCount;

            ulong value = (ulong)((rng.NextFloat() < 0.9f) ? 1 : 2);

            int currentEmpty = 0;
            for (int i = 0; i < 16; i++)
            {
                if (((board >> (i * 4)) & 0xF) == 0)
                {
                    if (currentEmpty == targetIndex)
                    {
                        board |= (value << (i * 4));
                        return;
                    }
                    currentEmpty++;
                }
            }
        }

        static bool ApplyMove(ref ulong board, ref int score, int direction)
        {
            ulong tempBoard = board;

            if (direction == 0) tempBoard = Transpose(tempBoard);
            else if (direction == 1) tempBoard = Transpose(Rotate180(tempBoard));
            else if (direction == 3) tempBoard = Rotate180(tempBoard);

            ulong newBoard = 0;
            bool changed = false;
            int localScore = 0;

            for (int r = 0; r < 4; r++)
            {
                ulong row = (tempBoard >> (r * 16)) & 0xFFFF;
                ulong newRow = ProcessRow(row, ref localScore);

                if (row != newRow) changed = true;

                newBoard |= (newRow << (r * 16));
            }
            score += localScore;

            if (direction == 0) newBoard = Transpose(newBoard);
            else if (direction == 1) newBoard = Rotate180(Transpose(newBoard));
            else if (direction == 3) newBoard = Rotate180(newBoard);

            if (changed) board = newBoard;
            return changed;
        }

        static ulong ProcessRow(ulong row, ref int score)
        {
            int c0 = (int)((row >> 0) & 0xF);
            int c1 = (int)((row >> 4) & 0xF);
            int c2 = (int)((row >> 8) & 0xF);
            int c3 = (int)((row >> 12) & 0xF);

            int[] line = { c0, c1, c2, c3 };
            int[] res = { 0, 0, 0, 0 };

            int pos = 0;
            for (int i = 0; i < 4; i++) if (line[i] != 0) res[pos++] = line[i];

            for (int i = 0; i < 3; i++)
            {
                if (res[i] != 0 && res[i] == res[i + 1])
                {
                    res[i]++;
                    score += (1 << res[i]);
                    res[i + 1] = 0;
                    for (int j = i + 1; j < 3; j++) res[j] = res[j + 1];
                    res[3] = 0;
                }
            }

            ulong result = 0;
            result |= ((ulong)res[0] << 0);
            result |= ((ulong)res[1] << 4);
            result |= ((ulong)res[2] << 8);
            result |= ((ulong)res[3] << 12);
            return result;
        }

        static ulong Transpose(ulong x)
        {
            ulong res = 0;
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 4; c++)
                {
                    ulong val = (x >> ((r * 4 + c) * 4)) & 0xF;
                    res |= (val << ((c * 4 + r) * 4));
                }
            return res;
        }

        static ulong Rotate180(ulong x)
        {
            ulong res = 0;
            for (int i = 0; i < 16; i++)
            {
                ulong val = (x >> (i * 4)) & 0xF;
                res |= (val << ((15 - i) * 4));
            }
            return res;
        }

        static bool CanMove(ulong board, int direction)
        {
            int dummyScore = 0;
            ulong temp = board;
            return ApplyMove(ref temp, ref dummyScore, direction);
        }

        private ulong BoardToUlong(int[,] board)
        {
            ulong res = 0;
            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 4; c++)
                {
                    int val = board[r, c];
                    if (val == 0) continue;
                    int exponent = 0;
                    while ((val >>= 1) > 0) exponent++;
                    res |= ((ulong)exponent << ((r * 4 + c) * 4));
                }
            }
            return res;
        }
    }
}
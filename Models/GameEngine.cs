using System;
using System.Collections.Generic;

namespace hra2048.Models
{
    public class GameEngine
    {
        public const int Size = 4;
        public const int WinValue = 2048;

        public int[,] Board { get; private set; } = new int[Size, Size];
        public int Score { get; private set; }
        public GameState State { get; private set; } = GameState.Playing;

        // Statistiky tahů
        public int MovesUp { get; private set; }
        public int MovesDown { get; private set; }
        public int MovesLeft { get; private set; }
        public int MovesRight { get; private set; }
        public int TotalMoves => MovesUp + MovesDown + MovesLeft + MovesRight;

        private Random _random;

        public GameEngine()
        {
            _random = new Random();
            StartNewGame();
        }

        public GameEngine(int seed)
        {
            _random = new Random(seed);
            StartNewGame();
        }

        // Privátní konstruktor pro klonování
        private GameEngine(bool skipInit)
        {
            _random = new Random();
        }

        /// <summary>
        /// Vytvoří hlubokou kopii enginu pro simulace (potřebné pro AI solvery)
        /// </summary>
        public GameEngine Clone()
        {
            var clone = new GameEngine(true);
            clone.Board = new int[Size, Size];
            clone.Score = this.Score;
            clone.State = this.State;
            clone.MovesUp = this.MovesUp;
            clone.MovesDown = this.MovesDown;
            clone.MovesLeft = this.MovesLeft;
            clone.MovesRight = this.MovesRight;

            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    clone.Board[r, c] = this.Board[r, c];

            return clone;
        }

        public void StartNewGame()
        {
            Board = new int[Size, Size];
            Score = 0;
            State = GameState.Playing;
            MovesUp = 0;
            MovesDown = 0;
            MovesLeft = 0;
            MovesRight = 0;

            SpawnTile();
            SpawnTile();
        }

        /// <summary>
        /// Provede tah v daném směru
        /// </summary>
        /// <returns>True pokud se něco pohnulo, jinak false</returns>
        public bool Move(Direction direction)
        {
            if (State != GameState.Playing) return false;

            bool moved = false;

            switch (direction)
            {
                case Direction.Up:
                    moved = MoveUp();
                    if (moved) MovesUp++;
                    break;
                case Direction.Down:
                    moved = MoveDown();
                    if (moved) MovesDown++;
                    break;
                case Direction.Left:
                    moved = MoveLeft();
                    if (moved) MovesLeft++;
                    break;
                case Direction.Right:
                    moved = MoveRight();
                    if (moved) MovesRight++;
                    break;
            }

            if (moved)
            {
                SpawnTile();
                CheckGameState();
            }

            return moved;
        }

        /// <summary>
        /// Zjistí, zda je daný tah možný (bez skutečného provedení)
        /// </summary>
        public bool CanMove(Direction direction)
        {
            var clone = this.Clone();

            return direction switch
            {
                Direction.Up => clone.MoveUp(),
                Direction.Down => clone.MoveDown(),
                Direction.Left => clone.MoveLeft(),
                Direction.Right => clone.MoveRight(),
                _ => false
            };
        }

        /// <summary>
        /// Vrátí seznam všech možných tahů
        /// </summary>
        public List<Direction> GetAvailableMoves()
        {
            var moves = new List<Direction>();

            if (CanMove(Direction.Up)) moves.Add(Direction.Up);
            if (CanMove(Direction.Down)) moves.Add(Direction.Down);
            if (CanMove(Direction.Left)) moves.Add(Direction.Left);
            if (CanMove(Direction.Right)) moves.Add(Direction.Right);

            return moves;
        }

        /// <summary>
        /// Vrátí maximální hodnotu dlaždice na hrací ploše
        /// </summary>
        public int GetMaxTile()
        {
            int max = 0;
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    if (Board[r, c] > max)
                        max = Board[r, c];
            return max;
        }

        /// <summary>
        /// Vrátí počet prázdných políček
        /// </summary>
        public int GetEmptyCount()
        {
            int count = 0;
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    if (Board[r, c] == 0)
                        count++;
            return count;
        }

        #region Move Logic

        private bool MoveLeft()
        {
            bool moved = false;

            for (int r = 0; r < Size; r++)
            {
                int[] row = new int[Size];
                for (int c = 0; c < Size; c++)
                    row[c] = Board[r, c];

                int[] newRow = ProcessLine(row, out bool rowMoved);

                for (int c = 0; c < Size; c++)
                    Board[r, c] = newRow[c];

                if (rowMoved) moved = true;
            }

            return moved;
        }

        private bool MoveRight()
        {
            bool moved = false;

            for (int r = 0; r < Size; r++)
            {
                int[] row = new int[Size];
                for (int c = 0; c < Size; c++)
                    row[c] = Board[r, Size - 1 - c];

                int[] newRow = ProcessLine(row, out bool rowMoved);

                for (int c = 0; c < Size; c++)
                    Board[r, Size - 1 - c] = newRow[c];

                if (rowMoved) moved = true;
            }

            return moved;
        }

        private bool MoveUp()
        {
            bool moved = false;

            for (int c = 0; c < Size; c++)
            {
                int[] col = new int[Size];
                for (int r = 0; r < Size; r++)
                    col[r] = Board[r, c];

                int[] newCol = ProcessLine(col, out bool colMoved);

                for (int r = 0; r < Size; r++)
                    Board[r, c] = newCol[r];

                if (colMoved) moved = true;
            }

            return moved;
        }

        private bool MoveDown()
        {
            bool moved = false;

            for (int c = 0; c < Size; c++)
            {
                int[] col = new int[Size];
                for (int r = 0; r < Size; r++)
                    col[r] = Board[Size - 1 - r, c];

                int[] newCol = ProcessLine(col, out bool colMoved);

                for (int r = 0; r < Size; r++)
                    Board[Size - 1 - r, c] = newCol[r];

                if (colMoved) moved = true;
            }

            return moved;
        }

        /// <summary>
        /// Zpracuje jeden řádek/sloupec - posune dlaždice a sloučí stejné hodnoty
        /// </summary>
        private int[] ProcessLine(int[] line, out bool moved)
        {
            moved = false;
            int[] result = new int[Size];
            int writeIndex = 0;

            // 1. Kompaktuj - odstraň nuly (posun doleva)
            int[] compacted = new int[Size];
            int compactIndex = 0;
            for (int i = 0; i < Size; i++)
            {
                if (line[i] != 0)
                {
                    compacted[compactIndex++] = line[i];
                }
            }

            // 2. Slouč sousední stejné hodnoty
            for (int i = 0; i < Size; i++)
            {
                if (compacted[i] == 0) break;

                if (i + 1 < Size && compacted[i] == compacted[i + 1] && compacted[i] != 0)
                {
                    // Sloučení - zdvojnásob hodnotu
                    result[writeIndex++] = compacted[i] * 2;
                    Score += compacted[i] * 2;
                    i++; // Přeskoč další (už sloučená)
                }
                else
                {
                    result[writeIndex++] = compacted[i];
                }
            }

            // 3. Zkontroluj, zda se něco změnilo
            for (int i = 0; i < Size; i++)
            {
                if (line[i] != result[i])
                {
                    moved = true;
                    break;
                }
            }

            return result;
        }

        #endregion

        #region Game State

        private void SpawnTile()
        {
            var emptySlots = new List<(int r, int c)>();

            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    if (Board[r, c] == 0)
                        emptySlots.Add((r, c));

            if (emptySlots.Count > 0)
            {
                var (row, col) = emptySlots[_random.Next(emptySlots.Count)];
                // 90% šance na 2, 10% šance na 4
                Board[row, col] = _random.Next(10) < 9 ? 2 : 4;
            }
        }

        private void CheckGameState()
        {
            // Kontrola výhry
            for (int r = 0; r < Size; r++)
            {
                for (int c = 0; c < Size; c++)
                {
                    if (Board[r, c] >= WinValue)
                    {
                        State = GameState.Won;
                        return;
                    }
                }
            }

            // Kontrola prohry - existuje nějaký možný tah?
            if (!HasValidMove())
            {
                State = GameState.Lost;
            }
        }

        private bool HasValidMove()
        {
            // Jsou prázdná políčka?
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    if (Board[r, c] == 0)
                        return true;

            // Lze sloučit sousedy horizontálně?
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size - 1; c++)
                    if (Board[r, c] == Board[r, c + 1])
                        return true;

            // Lze sloučit sousedy vertikálně?
            for (int r = 0; r < Size - 1; r++)
                for (int c = 0; c < Size; c++)
                    if (Board[r, c] == Board[r + 1, c])
                        return true;

            return false;
        }

        #endregion
    }
}
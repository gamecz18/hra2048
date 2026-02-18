using System;
using System.Collections.Generic;
using System.Text;

namespace hra2048.Models
{
    public class OwnSolver : IGameSolver
    {
        public string Name => "Stupid Solver";

        public Direction GetNextMove(GameEngine game)
        {
            List<Direction> directions = game.GetAvailableMoves();
            if (directions.Count == 0)
                return Direction.Up;

            

            return GetBestMove(game);

        }
        Direction GetBestMove(GameEngine game)
        {
            List<Direction> directions = game.GetAvailableMoves();
            if (directions.Count == 0)
                return Direction.Up;
            int score = 0;
            Direction bestDirection = directions[0];
            int bestScore = int.MinValue;
            foreach (var item in directions)
            {
                GameEngine clone = game.Clone();
                clone.Move(item);
                if (clone.Score > score)
                {
                   bestDirection = item;
                    score = clone.Score;
                }
            }
            return bestDirection;

        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace hra2048.Models
{
    class RandomSolver : IGameSolver
    {

        private readonly Random _random = new Random();
        public string Name => "Random Solver";


        public  Direction GetNextMove(GameEngine game)
        {
           List<Direction> aDR = game.GetAvailableMoves();

            return aDR[_random.Next(aDR.Count)];




        }


    }
}

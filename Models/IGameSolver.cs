namespace hra2048.Models
{
    public interface IGameSolver
    {
        string Name { get; }

        /// <summary>
        /// Solver dostane kopii enginu a vrátí nejlepší tah
        /// </summary>
        Direction GetNextMove(GameEngine game);
    }
}
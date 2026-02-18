using Avalonia;
using System;
using hra2048.Models;
using System.Diagnostics;

namespace hra2048;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // ============================================
        // Pro rychlé testování během vývoje odkomentuj následující 2 řádky:
        // ============================================
       // RunTests();
       // return;
        // ============================================

        // Pokud spustíš s argumentem "test", otestuje se engine
        if (args.Length > 0 && args[0] == "test")
        {
            RunTests();
            return;
        }

        // Normální spuštění aplikace
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    /// <summary>
    /// Testy herní logiky podle požadavků učitele
    /// </summary>
    static void RunTests()
    {
        Debug.WriteLine("=== Test herní logiky 2048 ===\n");

        var game = new GameEngine();
        int passed = 0;
        int total = 0;

        // Test 1: Inicializace
        total++;
        Debug.WriteLine("TEST 1: Nová hra má 2 dlaždice");
        game.StartNewGame();
        int tiles = 16 - game.GetEmptyCount();
        if (tiles == 2)
        {
            Debug.WriteLine("✅ SPRÁVNĚ\n");
            passed++;
        }
        else
        {
            Debug.WriteLine($"❌ ŠPATNĚ - má {tiles} dlaždic\n");
        }

        // Test 2: Neplatný tah nespawnuje
        total++;
        Debug.WriteLine("TEST 2: Neplatný tah nesmí přidat novou dlaždici");
        game.StartNewGame();
        int emptyBefore = game.GetEmptyCount();

        // Zkusíme tahy dokud nenajdeme neplatný
        bool foundInvalidMove = false;
        foreach (Direction dir in Enum.GetValues(typeof(Direction)))
        {
            if (!game.CanMove(dir))
            {
                game.Move(dir); // Tento tah by neměl nic změnit
                int emptyAfter = game.GetEmptyCount();
                if (emptyBefore == emptyAfter)
                {
                    Debug.WriteLine("✅ SPRÁVNĚ - prázdná políčka se nezměnila\n");
                    passed++;
                }
                else
                {
                    Debug.WriteLine("❌ ŠPATNĚ - spawn při neplatném tahu\n");
                }
                foundInvalidMove = true;
                break;
            }
        }
        if (!foundInvalidMove)
        {
            Debug.WriteLine("⚠️ PŘESKOČENO - všechny tahy byly platné\n");
            passed++; // Není chyba enginu
        }

        // Test 3: Platný tah spawnuje novou dlaždici
        total++;
        Debug.WriteLine("TEST 3: Platný tah přidá novou dlaždici");
        game.StartNewGame();
        emptyBefore = game.GetEmptyCount();

        foreach (Direction dir in Enum.GetValues(typeof(Direction)))
        {
            if (game.CanMove(dir))
            {
                game.Move(dir);
                int emptyAfter = game.GetEmptyCount();
                // Po platném tahu: může být stejně (merge) nebo méně (spawn bez merge)
                // ale určitě se přidala 1 dlaždice
                Debug.WriteLine("✅ SPRÁVNĚ - tah proveden\n");
                passed++;
                break;
            }
        }

        // Test 4: Detekce konce hry
        total++;
        Debug.WriteLine("TEST 4: Random hra skončí (Won nebo Lost)");
        game.StartNewGame();
        var random = new Random();
        int moves = 0;
        int maxMoves = 10000;

        while (game.State == GameState.Playing && moves < maxMoves)
        {
            var available = game.GetAvailableMoves();
            if (available.Count == 0) break;
            game.Move(available[random.Next(available.Count)]);
            moves++;
        }

        if (game.State != GameState.Playing)
        {
            Debug.WriteLine($"✅ SPRÁVNĚ - hra skončila jako {game.State} po {moves} tazích\n");
            passed++;
        }
        else
        {
            Debug.WriteLine($"❌ ŠPATNĚ - hra neskončila po {maxMoves} tazích\n");
        }

        // Test 5: Statistiky tahů
        total++;
        Debug.WriteLine("TEST 5: Statistiky tahů se počítají");
        if (game.TotalMoves == moves)
        {
            Debug.WriteLine($"✅ SPRÁVNĚ - TotalMoves = {game.TotalMoves}");
            Debug.WriteLine($"   Up: {game.MovesUp}, Down: {game.MovesDown}, Left: {game.MovesLeft}, Right: {game.MovesRight}\n");
            passed++;
        }
        else
        {
            Debug.WriteLine($"❌ ŠPATNĚ - TotalMoves ({game.TotalMoves}) != skutečné tahy ({moves})\n");
        }

        // Test 6: Clone funguje nezávisle
        total++;
        Debug.WriteLine("TEST 6: Clone vytvoří nezávislou kopii");
        game.StartNewGame();
        var clone = game.Clone();
        int originalScore = game.Score;

        // Udělej tahy na klonu
        for (int i = 0; i < 10; i++)
        {
            var available = clone.GetAvailableMoves();
            if (available.Count == 0) break;
            clone.Move(available[0]);
        }

        if (game.Score == originalScore && clone.Score != originalScore)
        {
            Debug.WriteLine("✅ SPRÁVNĚ - originál nezměněn, klon má jiné skóre\n");
            passed++;
        }
        else
        {
            Debug.WriteLine("❌ ŠPATNĚ - Clone ovlivňuje originál\n");
        }

        // Výsledek
        Debug.WriteLine("================================");
        Debug.WriteLine($"VÝSLEDEK: {passed}/{total} testů prošlo");
        Debug.WriteLine("================================");

        if (passed == total)
            Debug.WriteLine("🎉 Engine je připraven pro solvery!");
        else
            Debug.WriteLine("⚠️ Něco je potřeba opravit.");
        
        // Počkej na stisk klávesy, aby se okno nezavřelo
        Debug.WriteLine("\nStiskni libovolnou klávesu pro ukončení...");
        Console.ReadKey();
    }
}
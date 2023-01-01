// See https://aka.ms/new-console-template for more information

using Instant2D;
using System.Reflection;

namespace Instant2D.TestGame {
    public static class Program {
        [STAThread]
        static void Main() {
            //var games = Assembly.GetExecutingAssembly().GetTypes()
            //    .Where(t => t.IsSubclassOf(typeof(InstantApp)))
            //    .ToArray();

            //int selectedGame = 0;

            //// add selected
            //if (games.Length != 1) {
            //boot: Console.WriteLine("Select the game to boot");
            //    for (var i = 0; i < games.Length; i++) {
            //        Console.WriteLine($"{i,3}> {games[i].Name}");
            //    }

            //    var input = Console.ReadKey();
            //    if (!int.TryParse(input.KeyChar.ToString(), out selectedGame) || selectedGame < 0 || selectedGame >= games.Length) {
            //        goto boot;
            //    }

            //    Console.Clear();
            //}

            Bootstrap.Initialize_FNA();

            using InstantApp game = new Game();
            game.Run();
        }
    }
}
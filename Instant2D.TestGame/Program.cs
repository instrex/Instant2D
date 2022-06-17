// See https://aka.ms/new-console-template for more information

namespace Instant2D.TestGame {
    public static class Program {
        [STAThread]
        static void Main() {
            Bootstrap.Initialize_FNA();

            using var game = new Game();
            game.Run();
        }
    }
}
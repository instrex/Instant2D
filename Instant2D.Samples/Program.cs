namespace Instant2D.Samples;

internal class Program {
    [STAThread]
    static void Main(string[] _) {
        Bootstrap.Initialize_FNA();
        using var game = new Instant2dSamples();
        game.Run();
    }
}

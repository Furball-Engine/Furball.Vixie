namespace Furball.Vixie.TestApplication.Headless;

public class Program {
    public static void Main() {
        TestGame game = new();

        game.RunHeadless();
    }
}
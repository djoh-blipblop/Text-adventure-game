using System.Text.RegularExpressions;

internal class Program
{
    const ConsoleColor NarrativeColor = ConsoleColor.Gray;
    const ConsoleColor PromptColor = ConsoleColor.White;
    const int PrintPauseMilliseconds = 500;

    static void Print(string text)
    {
        // Split text into lines that don't exceed the window width.
        int maximumLineLength = Console.WindowWidth - 1;
        MatchCollection lineMatches = Regex.Matches(text, @"(.{1," +
                                      maximumLineLength + @"})(?:\s|$)");

        // Output each line with a small delay.
        foreach (Match match in lineMatches)
        {
            Console.WriteLine(match.Groups[0].Value);
            Thread.Sleep(PrintPauseMilliseconds);
        }
    }

    static bool shouldQuit = false;
    static void HandlePlayerAction()
    {
        // Ask the player what they want to do.
        Print("What now?");
        Print("");

        Console.ForegroundColor = PromptColor;
        Console.Write("> ");

        string command = Console.ReadLine().ToLowerInvariant();

        // Analyze the command by assuming the first word is a verb (or similar instruction).
        string[] words = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        string verb = words[0];

        // Call the appropriate handler for the given verb.
        switch (verb)
        {
            case "north":
            case "n":
                // TODO
                break;

            case "south":
            case "s":
                // TODO
                break;

            case "east":
            case "e":
                // TODO
                break;

            case "west":
            case "w":
                // TODO
                break;

            case "enter":
            case "climb in":
                // TODO
                break;

            case "inventory":
            case "inv":
                // TO DO 
                break;

            case "inspect":
                // TO DO
                break;

            case "get":
                // TO DO
                break;

            case "use":
                // to do
                break;

            case "combine":
                // to do
                break;

            case "talk":
                // to do
                break;

            case "help":
                // to do
                break;

            case "quit":
            case "exit":
                Print("Goodbye!");
                shouldQuit = true;
                break;

            default:
                Print("I do not understand you.");
                break;
        }



    }

    private static void Main(string[] args)
    {
        // Display title screen

        string title = File.ReadAllText("Title.txt");
        Console.WriteLine(title);
        Console.ReadKey();
        Console.Clear();

        // Display intro.
        Console.ForegroundColor = NarrativeColor;

        Print("Lets just seeeeee if this works and then also while we're at it test what happens if the text is very long, like if this was one of those stream of consciouesness narrative ideas that you get in books some times, like postmodern novel and such. speaking of which i really gotta read house of leaves, maybe i can get the english version from the public library in tingsryd. I dont think you can translate such a novel to swedsish werer the form of hte letters and workds are importnant,. i am making a lot of spelling misstakes here, its on purpouse i swear. i know langauge good. me spek good. ");
        while (shouldQuit == false)
        {
            HandlePlayerAction();
        }
    }
}
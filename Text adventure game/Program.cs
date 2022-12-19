using System.Text.RegularExpressions;

enum LocationId
{
    Nowhere,
    Inventory,
    Entrance,
    OrderStation,
    EatingArea,
    Bathroom,
    FoodDeliveryStation,
    Kitchen,
    Freezer,
    WasteProcessing,
    Storage,
    CleanBotBay,
    ShippingBay,
    ServerRoom,
}

internal class Program
{
    const ConsoleColor NarrativeColor = ConsoleColor.Green;
    const ConsoleColor PromptColor = ConsoleColor.White;
    const ConsoleColor PlayerColor = ConsoleColor.DarkGray;
    const int PrintPauseMilliseconds = 800;

    class LocationData
    {
        public LocationId Id;
        public string Name;
        public string Description;
    }

    // Data dictionaries
    static Dictionary<LocationId, LocationData> locationsDataDictionary =
        new Dictionary<LocationId, LocationData>();

    // Current state
    static LocationId CurrentLocationId = LocationId.Entrance;

    static void DisplayLocation()
    {
        Console.Clear();

        // Display current location description.
        LocationData currentLocationData = locationsDataDictionary[CurrentLocationId];
        Print(currentLocationData.Description);
    }

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
        Console.ForegroundColor = PromptColor;
        Print("What now?");
        Print("");
        Console.Write("> ");

        Console.ForegroundColor = PlayerColor;
        string? commandInput = Console.ReadLine();

        if (commandInput != null)
        {
            // Analyze the command by assuming the first word is a verb (or similar instruction).
            string[] words = commandInput.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

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
        else
        {
            return;
        }
    }

    private static void Main(string[] args)
    {
        //initalize data
        string[] locationIdText = File.ReadAllLines("LocationData.txt");
        for (int i = 0; i < locationIdText.Length; i++)
        {
            if (Enum.TryParse<LocationId>(locationIdText[i], false, out LocationId result))
            {
                LocationData locationData = new LocationData() { Id = result, Name = locationIdText[i + 1].Substring(6), Description = locationIdText[i + 2].Substring(13) };
                locationsDataDictionary.Add(result, locationData);
            }
        }

        // Display title screen
        string title = File.ReadAllText("Title.txt");
        Console.WriteLine(title);
        Console.ReadKey();
        Console.Clear();

        // Display intro.
        Console.ForegroundColor = NarrativeColor;
        Print("This is the placeholder for the intro");

        //Game start
        DisplayLocation();

        while (shouldQuit == false)
        {
            HandlePlayerAction();
        }
    }
}
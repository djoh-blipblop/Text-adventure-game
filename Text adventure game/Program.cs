using System.Text.RegularExpressions;

enum LocationId
{
    Nowhere,
    Inventory,
    Entrance,
    OrderStation,
    EatingArea,
    BathRoom,
    FoodDeliveryStation,
    Kitchen,
    Freezer,
    WasteProcessing,
    Storage,
    CleanBotBay,
    ShippingBay,
    ServerRoom,
}

enum Direction
{
    North,
    South,
    West,
    East,
    Vent,
}

enum Items
{
    KeyCard,
    MultiTool,
    Hamburger,
    Ice,
    Chlorine,
    DroneBattery,
    MixedExplosive,
    AICore,
}

enum NPCs
{
    CleanBot,
    FRED,
    OMAR,
}
internal class Program
{
    const ConsoleColor NarrativeColor = ConsoleColor.Green;
    const ConsoleColor PromptColor = ConsoleColor.White;
    const ConsoleColor PlayerColor = ConsoleColor.DarkGray;
    const ConsoleColor HelpColor = ConsoleColor.Red;
    const int PrintPauseMilliseconds = 800;

    class LocationData
    {
        public LocationId Id;
        public string? Name;
        public string? Description;
        public Dictionary<Direction, LocationId>? Directions =
            new Dictionary<Direction, LocationId>();

    }
    class ParsedData
    {
        public string? Id;
        public string? Name;
        public string? Description;
        public Dictionary<Direction, LocationId>? Directions;
        public LocationId StartingLocationId;
    }

    // Data dictionaries
    static Dictionary<LocationId, LocationData> locationsData =
        new Dictionary<LocationId, LocationData>();



    // Current state
    static LocationId CurrentLocationId = LocationId.Entrance;

    static LocationData InitLocation()
    {
        LocationData currentLocationData = locationsData[CurrentLocationId];
        return currentLocationData;
    }

    static void DisplayLocation(LocationData currentLocation)
    {
        //TODO uncomment this for play build, and decide if you want to display the location name or only the description
        //Console.Clear();

        // Display current location description.
        // LocationData currentLocationData = locationsData[CurrentLocationId];

        Print(currentLocation.Name);
        Print(currentLocation.Description);
    }

    static void DisplayHelp()
    {
        Console.ForegroundColor = HelpColor;
        Print("HELP");
        Print("You can try moving in a direction by entering the direction (NORTH, SOUTH, EAST or WEST");
        Print("Some things you might be able to TALK to");
        Print("You can also check out whats in your INVENTORY");
        Print("INSPECT lets you take a closer look at objects");
        Print("GET lets you take items with you");
        Print("USE is the command for using an item from your inventory at a location");
        Print("You can also COMBINE items that you have in your inventory");
        Print("QUIT lets you exit the game");
    }
    static LocationData SwitchLocation(LocationId currentLocationId)
    {
        LocationData destinationLocation = locationsData[currentLocationId];
        return destinationLocation;
    }

    static LocationData GenerateLocationData()
    {
        LocationData currentLocationData = locationsData[CurrentLocationId];
        return currentLocationData;
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
            //Thread.Sleep(PrintPauseMilliseconds); 
            //TODO uncomment for play build
        }
    }

    static bool shouldQuit = false;

    // Create an UI for the user
    static void CreateUserInterface(LocationData currentLocation)
    {
        DisplayLocation(currentLocation);
        HandlePlayerAction(currentLocation);
    }

    // Bundle all the prompt styling and handling, return the user input as a string
    static string Prompt()
    {
        // Ask the player what they want to do.
        Console.ForegroundColor = PromptColor;
        Print("What now?");
        Print("");

        Console.Write("> ");

        Console.ForegroundColor = PlayerColor;
        string? commandInput = Console.ReadLine();
        return commandInput;
    }
    static void HandlePlayerAction(LocationData currentLocation)
    {
        string? commandInput = Prompt();

        if (commandInput != "")
        {
            // Analyze the command by assuming the first word is a verb (or similar instruction).
            string[] words = commandInput.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            string verb = words[0];

            // Call the appropriate handler for the given verb.
            switch (verb)
            {
                case "north":
                case "n":
                    if (currentLocation.Directions.ContainsKey(Direction.North))
                    {
                        currentLocation = SwitchLocation(currentLocation.Directions.GetValueOrDefault(Direction.North));
                        CreateUserInterface(currentLocation);
                    }
                    break;

                case "south":
                case "s":
                    if (currentLocation.Directions.ContainsKey(Direction.South))
                    {
                        currentLocation = SwitchLocation(currentLocation.Directions.GetValueOrDefault(Direction.South));
                        CreateUserInterface(currentLocation);
                    }
                    break;

                case "east":
                case "e":
                    if (currentLocation.Directions.ContainsKey(Direction.East))
                    {
                        currentLocation = SwitchLocation(currentLocation.Directions.GetValueOrDefault(Direction.East));
                        CreateUserInterface(currentLocation);
                    }
                    break;

                case "west":
                case "w":
                    if (currentLocation.Directions.ContainsKey(Direction.West))
                    {
                        currentLocation = SwitchLocation(currentLocation.Directions.GetValueOrDefault(Direction.West));
                        CreateUserInterface(currentLocation);
                    }
                    break;

                case "enter":
                case "climb in":
                case "vent":
                    if (currentLocation.Directions.ContainsKey(Direction.Vent))
                    {
                        currentLocation = SwitchLocation(currentLocation.Directions.GetValueOrDefault(Direction.Vent));
                        CreateUserInterface(currentLocation);
                    }
                    break;

                case "inventory":
                case "inv":
                    // TODO
                    // e.g. you could implement a function, which handles the inventory stuff. For example display the inventory, you invoke it,
                    // and after that you would call CreateUserInterface again.
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
                    DisplayHelp();
                    Console.ReadLine();
                    Console.Clear();
                    CreateUserInterface(currentLocation);
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
            // view prompt when no user input has been given, e.g. Only Enter has been pressed
            HandlePlayerAction(currentLocation);
        }
    }

    private static void Main(string[] args)
    {
        //Initalizing

        ReadLocationsData();

        static void ReadLocationsData()
        {
            // Parse the locations file.
            List<ParsedData> parsedDataList = ParseData("LocationData.txt");

            // Transfer data from the parsed structures into locations data.
            foreach (ParsedData parsedData in parsedDataList)
            {
                LocationId locationId = Enum.Parse<LocationId>(parsedData.Id);
                LocationData locationData = new LocationData
                {
                    Id = locationId,
                    Name = parsedData.Name,
                    Description = parsedData.Description,
                    Directions = parsedData.Directions
                };
                locationsData[locationId] = locationData;
            }
        }

        static List<ParsedData> ParseData(string filePath)
        {
            var parsedDataList = new List<ParsedData>();

            string[] dataLines = File.ReadAllLines(filePath);
            var currentLineIndex = 0;

            // Parse data until we reach the end.
            while (currentLineIndex < dataLines.Length)
            {
                // First line of data holds the ID string.
                string id = dataLines[currentLineIndex];

                // Initialize the structure that will hold parsed data.
                var parsedData = new ParsedData
                {
                    Id = id,
                    Directions = new Dictionary<Direction, LocationId>()
                };

                // The remaining lines hold various properties in "property: value" or "property:" format.
                currentLineIndex++;

                do
                {
                    // Extract property and potentially value.
                    MatchCollection matches = Regex.Matches(dataLines[currentLineIndex], @"(\w+): *(.*)?");


                    if (matches.Count == 0)
                    {
                        throw new FormatException("Invalid property line: " + dataLines[currentLineIndex]);
                    }

                    string property = matches[0].Groups[1].Value;
                    string value = matches[0].Groups[2]?.Value;

                    // Store value into data structure.
                    switch (property)
                    {
                        case "Name":
                            parsedData.Name = value;
                            break;

                        case "Description":
                            parsedData.Description = value;
                            break;

                        case "Directions":
                            // Directions are listed in separate lines with format "  direction: destination".
                            do
                            {
                                // Continue while the next line is a directions line.
                                MatchCollection directionsLineMatches = Regex.Matches(dataLines[currentLineIndex + 1], @"[\t]+(\w+): *(.*)");


                                if (directionsLineMatches.Count == 0) break;

                                // Store parsed data into the directions dictionary.
                                Direction direction = Enum.Parse<Direction>(directionsLineMatches[0].Groups[1].Value);
                                LocationId destination = Enum.Parse<LocationId>(directionsLineMatches[0].Groups[2].Value);
                                parsedData.Directions[direction] = destination;

                                currentLineIndex++;

                            } while (currentLineIndex + 1 < dataLines.Length);
                            break;

                        case "StartingLocation":
                            parsedData.StartingLocationId = Enum.Parse<LocationId>(value);
                            break;
                    }

                    currentLineIndex++;

                    // Keep parsing until we reach an empty line, which signifies the end of the current entry.
                } while (currentLineIndex < dataLines.Length && dataLines[currentLineIndex].Length > 0);

                // All data for this entry was parsed. Store it and skip the next empty line.
                parsedDataList.Add(parsedData);
                currentLineIndex++;
            }

            return parsedDataList;
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
        LocationData currentLocation = InitLocation();
        //DisplayLocation(currentLocation);

        CreateUserInterface(currentLocation);

        while (shouldQuit == false)
        {
            HandlePlayerAction(currentLocation);
            DisplayLocation(currentLocation);
        }
    }
}

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

enum ItemId
{
    Nothing,
    KeyCard,
    MultiTool,
    Hamburger,
    Ice,
    Chlorine,
    DroneBattery,
    MixedExplosive,
    AICore,
}

enum NPCId
{
    CleanBot,
    FRED,
    OMAR,
}
internal class Program
{
    //UI elements, colors and textspeed etc
    const ConsoleColor NarrativeColor = ConsoleColor.Green;
    const ConsoleColor PromptColor = ConsoleColor.White;
    const ConsoleColor PlayerColor = ConsoleColor.DarkGray;
    const ConsoleColor HelpColor = ConsoleColor.Red;
    const int PrintPauseMilliseconds = 800;

    //Data classes
    class LocationData
    {
        public LocationId Id;
        public string Name = string.Empty;
        public string Description = string.Empty;
        public string DetailedDescription = string.Empty;
        public Dictionary<Direction, LocationId> Directions = new Dictionary<Direction, LocationId>();

    }
    class ItemData
    {
        public ItemId id;
        public string Name = string.Empty;
        public string Description = string.Empty;
        public LocationId StartingLocationId;
    }
    class ParsedData
    {
        public string Id = string.Empty;
        public string Name = string.Empty;
        public string Description = string.Empty;
        public string DetailedDescription = string.Empty;
        public Dictionary<Direction, LocationId> Directions = new Dictionary<Direction, LocationId>();
        public LocationId StartingLocationId;
    }

    // Data dictionaries
    static Dictionary<LocationId, LocationData> LocationsData = new Dictionary<LocationId, LocationData>();
    static Dictionary<ItemId, ItemData> ItemsData = new Dictionary<ItemId, ItemData>();

    //helpers
    static Dictionary<string, ItemId> ItemIdsByName = new Dictionary<string, ItemId>();
    static ItemId[] ItemsYouCanGet = { ItemId.KeyCard, ItemId.Hamburger, ItemId.Ice, ItemId.AICore, ItemId.Chlorine, ItemId.DroneBattery, };
    static ItemId[] ItemsYouCanCombine = { ItemId.Ice, ItemId.Chlorine, ItemId.DroneBattery, };

    // Current state
    static LocationData CurrentLocation = new LocationData();
    static Dictionary<ItemId, LocationId> CurrentItemLocations = new Dictionary<ItemId, LocationId>();

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
                DetailedDescription = parsedData.DetailedDescription,
                Directions = parsedData.Directions
            };
            LocationsData[locationId] = locationData;
        }
    }
    static void ReadItemData()
    {
        //Parse the item file
        List<ParsedData> parsedDataList = ParseData("ItemData.txt");

        // Transfer data from the parsed structures into items data
        foreach (ParsedData parsedData in parsedDataList)
        {
            ItemId itemId = Enum.Parse<ItemId>(parsedData.Id);
            ItemData itemData = new ItemData
            {
                id = itemId,
                Name = parsedData.Name,
                Description = parsedData.Description,
                StartingLocationId = parsedData.StartingLocationId,
            };
            ItemsData[itemId] = itemData;
        }
    }

    static void InitalizeItemHelpers()
    {
        // Create a map of items by their name
        foreach (KeyValuePair<ItemId, ItemData> itemEntry in ItemsData)
        {
            string name = itemEntry.Value.Name.ToLowerInvariant();

            // Allow to refer to an item by any of its words.
            string[] nameParts = name.Split();

            foreach (string namepart in nameParts)
            {
                //Don't override already assigned words
                if (ItemIdsByName.ContainsKey(namepart)) continue;

                ItemIdsByName[namepart] = itemEntry.Key;
            }
        }
    }
    static void InitializeStartingState()
    {
        //Set player starting location
        CurrentLocation = LocationsData[LocationId.Entrance];

        // Set all Items to their starting locations.
        foreach (KeyValuePair<ItemId, ItemData> itemEntry in ItemsData)
        {
            CurrentItemLocations[itemEntry.Key] = itemEntry.Value.StartingLocationId;
        }
    }
    //Parse data
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
                string value = matches[0].Groups[2]?.Value ?? string.Empty;

                // Store value into data structure.
                switch (property)
                {
                    case "Name":
                        parsedData.Name = value;
                        break;

                    case "Description":
                        parsedData.Description = value;
                        break;

                    case "DetailedDescription":
                        parsedData.DetailedDescription = value;
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

    //This method gives the user the text description for the location its at
    static void DisplayCurrentLocation()
    {
        //TODO uncomment this for play build, to limit the amount of text on screen, makes the description more current.
        //Console.Clear();

        Console.ForegroundColor = NarrativeColor;

        // Display current location description.
        //TODO comment away the location name for the playable build, it ruins immersion
        Print(CurrentLocation.Name);
        Print(CurrentLocation.Description);
    }

    //This method display additional text about the location, if there is any, when the player uses the "LOOK" command
    static void DisplayLookText(LocationData currentLocation)
    {
        Console.ForegroundColor = NarrativeColor;
        if (currentLocation.DetailedDescription != null)
        {
            Print(currentLocation.DetailedDescription);
        }
        else
        {
            Print("There is nothing really noteworthy or interesting here.");
        }
    }

    //This method checks if an item is there at the location
    static bool ItemAt(ItemId itemId, LocationId locationId)
    {
        if (!CurrentItemLocations.ContainsKey(itemId)) return false;
        return CurrentItemLocations[itemId] == locationId;
    }

    //This lets the game know where items are
    static IEnumerable<ItemId> GetItemsAtLocation(LocationId locationId)
    {
        return CurrentItemLocations.Keys.Where(itemId => ItemAt(itemId, locationId));
    }

    //Gets the name of an item using the ItemId Enum
    static string GetName(ItemId itemId)
    {
        return ItemsData[itemId].Name;
    }

    //Gets the description of an item using the ItemId Enum
    static string GetDescription(ItemId itemId)
    {
        if (ItemsData[itemId] == null)
        {
            throw new ArgumentException($"{itemId} does not have corresponding data");
        }

        if (ItemsData[itemId].Description == null)
        {
            throw new ArgumentException($"{itemId} does not have a description");
        }

        return ItemsData[itemId].Description;
    }

    //Gets the starting location of an item
    static LocationId GetItemLocation(ItemId itemId)
    {
        return ItemsData[itemId].StartingLocationId;
    }

    //Gets the ItemId enum from user inputs, translating written words to enums which can be used to as keys
    static List<ItemId> GetItemIdsFromWords(string[] words)
    {
        List<ItemId> itemIds = new List<ItemId>();

        // For each word, see if it's a name of a thing.
        foreach (string word in words)
        {
            if (ItemIdsByName.ContainsKey(word))
            {
                itemIds.Add(ItemIdsByName[word]);
            }
        }

        return itemIds;
    }

    //Displays what the player is currently carrying
    static void DisplayInventory()
    {
        Print("You are carrying:");

        IEnumerable<ItemId> itemsInInventory = GetItemsAtLocation(LocationId.Inventory);

        if (itemsInInventory.Count() == 0)
        {
            Print("nothing.");
        }
        else
        {
            foreach (ItemId itemId in itemsInInventory)
            {
                Print($"{GetName(itemId)}.");
            }
        }
    }

    //Let's the player take a closer look at things they are carrying, useful for hints about what to use the item for and for worldbuilding
    static void InspectItem(string[] words, List<ItemId> itemIds)
    {
        if (ItemAt(itemIds[0], LocationId.Inventory))
        {
            Print(GetName(itemIds[0]));
            Print(GetDescription(itemIds[0]));
        }
        else
        {
            Print("You are not carrying that");
        }
    }

    //Method for trying to pick up items
    static void GetItems(List<ItemId> itemIds)
    {
        IEnumerable<ItemId> itemsAtThisLocation = GetItemsAtLocation(CurrentLocation.Id);

        List<ItemId> avalibleItemsToGet = ItemsYouCanGet.Intersect(itemsAtThisLocation).ToList();

        if (avalibleItemsToGet.Count() == 0)
        {
            Print("There is nothing here that you can pick up");
            return;
        }

        if (itemIds.Count == 0)
        {
            Print("Get what?");
            return;
        }

        foreach (ItemId itemId in itemIds)
        {
            if (avalibleItemsToGet.Contains(itemId))
            {
                CurrentItemLocations[itemId] = LocationId.Inventory;
                Print($"You picked up {ItemsData[itemId].Name}");
            }
            else
            {
                Print($"You can't find {ItemsData[itemId].Name}");
            }
        }
    }

    //Gives the player of a list of things they can try to do in the game
    static void DisplayHelp()
    {
        Console.ForegroundColor = HelpColor;
        Print("HELP");
        Print("-------------");
        Print(@"You can try moving in a direction by entering ""go"" and the direction (NORTH, SOUTH, EAST or WEST)");
        Print("Some things you might be able to TALK to");
        Print("Try taking a closer LOOK at your surroundings if you're stuck");
        Print("You can also check out whats in your INVENTORY");
        Print("INSPECT lets you take a closer look at items in your inventory");
        Print("GET lets you take items with you");
        Print("USE is the command for using an item from your inventory at a location");
        Print("You can also COMBINE items that you have in your inventory");
        Print("QUIT lets you exit the game");
        Print("-------------");
        Print("Press any key to continue");
    }

    //Method for moving between locations
    static void SwitchLocation(LocationId destinationLocationId)
    {
        CurrentLocation = LocationsData[destinationLocationId];
    }

    //Method for displaying text, uses the UI elements to make the text print in the right colour etc.
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

    //inital state for the "quit" flag
    static bool shouldQuit = false;

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
        return commandInput ?? string.Empty;
    }

    //The text parser that figures out what the player wants to do and tries to do it
    static void HandlePlayerAction()
    {
        string commandInput = Prompt();

        if (commandInput == string.Empty)
        {
            return;
        }

        // Analyze the command by assuming the first word is a verb (or similar instruction).
        string[] words = commandInput.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string verb = words[0];

        //assuming the second word means a noun
        string noun = string.Empty;

        // Checking if there actually is a word in the command to act upon
        if (words.Length > 1)
        {
            noun = words[1];
        }

        //Checking if there are items mentioned in the input, if so get the Ids they correspond to.
        List<ItemId> itemIds = GetItemIdsFromWords(words);

        // Call the appropriate handler for the given verb.
        switch (verb)
        {
            case "go":
                switch (noun)
                {
                    case "north":
                    case "n":
                        if (CurrentLocation.Directions.ContainsKey(Direction.North))
                        {
                            SwitchLocation(CurrentLocation.Directions[Direction.North]);
                        }
                        else
                        {
                            Print("You can't go north from here");
                        }
                        break;

                    case "south":
                    case "s":
                        if (CurrentLocation.Directions.ContainsKey(Direction.South))
                        {
                            SwitchLocation(CurrentLocation.Directions[Direction.South]);
                        }
                        else
                        {
                            Print("You can't go south from here");
                        }
                        break;

                    case "east":
                    case "e":
                        if (CurrentLocation.Directions.ContainsKey(Direction.East))
                        {
                            SwitchLocation(CurrentLocation.Directions[Direction.East]);
                        }
                        else
                        {
                            Print("You can't go east from here");
                        }
                        break;

                    case "west":
                    case "w":
                        if (CurrentLocation.Directions.ContainsKey(Direction.West))
                        {
                            SwitchLocation(CurrentLocation.Directions[Direction.West]);
                        }
                        else
                        {
                            Print("You can't go west from here");
                        }
                        break;

                    default:
                        Print("Go where?");
                        break;
                }
                break;

            case "enter":
            case "climb":
            case "vent":
                if (CurrentLocation.Directions.ContainsKey(Direction.Vent))
                {
                    SwitchLocation(CurrentLocation.Directions[Direction.Vent]);
                }
                else
                {
                    Print("There isn't anything to climb into here");
                }
                break;

            case "inventory":
            case "inv":
                DisplayInventory();
                break;

            case "inspect":
                if (words.Length > 1)
                {
                    InspectItem(words, itemIds);
                }
                else
                {
                    Print("Inspect what?");
                }
                break;

            case "look":
                DisplayLookText(CurrentLocation);
                break;

            case "get":
                GetItems(itemIds);
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
                break;

            case "quit":
            case "exit":
                //TODO create a confirmation for playable build, to avoid players quiting when they want to "exit" a room with no other directions then the one they came from

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
        //Initalizing data
        ReadLocationsData();
        ReadItemData();

        //Initalizing stuff for handling items
        InitalizeItemHelpers();

        //Initialize starting state
        InitializeStartingState();

        // Display title screen
        string title = File.ReadAllText("Title.txt");
        Console.WriteLine(title);
        Console.ReadKey();
        Console.Clear();

        // Display intro
        Console.ForegroundColor = NarrativeColor;
        Print("This is the placeholder for the intro");
        // TODO, uncomment this for playable build
        //Console.ReadKey();
        //Console.Clear();

        //Gameplay loop
        while (shouldQuit == false)
        {
            DisplayCurrentLocation();
            HandlePlayerAction();
        }
    }
}

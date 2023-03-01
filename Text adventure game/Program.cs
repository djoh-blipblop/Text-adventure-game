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
    Money,
}

enum NPCId
{
    CleanBot,
    Fred,
    Omar,
}
internal class Program
{
    // UI elements, colors and textspeed etc
    const ConsoleColor NarrativeColor = ConsoleColor.Green;
    const ConsoleColor PromptColor = ConsoleColor.White;
    const ConsoleColor PlayerColor = ConsoleColor.DarkGray;
    const ConsoleColor HelpColor = ConsoleColor.Red;
    const ConsoleColor SystemColor = ConsoleColor.Blue;
    const int PrintPauseMilliseconds = 400;

    //method to clear key inputs
    static void ClearBuffer()
    {
        while (Console.KeyAvailable)
        {
            Console.ReadKey(false);
        }
    }

    // Data classes
    class NpcDialogueNodes
    {
        public string id = string.Empty;
        public string prompt = string.Empty;
        public List<AnswersData> answers = new List<AnswersData>();
    }

    class AnswersData
    {
        public string Answer = string.Empty;
        public NpcDialogueNodes? Destination;
    }

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

    class NPCData
    {
        public NPCId id;
        public string Name = string.Empty;
        public string Description = string.Empty;
        public LocationId StartingLocationId;
        public string DialogueNode = string.Empty;
    }
    class ParsedData
    {
        public string Id = string.Empty;
        public string Name = string.Empty;
        public string Description = string.Empty;
        public string DetailedDescription = string.Empty;
        public Dictionary<Direction, LocationId> Directions = new Dictionary<Direction, LocationId>();
        public LocationId StartingLocationId;
        public string StartingDialogueNode = string.Empty;
    }

    // Data dictionaries
    static Dictionary<LocationId, LocationData> LocationsData = new Dictionary<LocationId, LocationData>();
    static Dictionary<ItemId, ItemData> ItemsData = new Dictionary<ItemId, ItemData>();
    static Dictionary<NPCId, NPCData> NPCsData = new Dictionary<NPCId, NPCData>();
    static Dictionary<string, NpcDialogueNodes> npcDialogueNodes = new Dictionary<string, NpcDialogueNodes>();

    // Helpers
    static Dictionary<string, ItemId> ItemIdsByName = new Dictionary<string, ItemId>();
    static ItemId[] ItemsYouCanGet = { ItemId.KeyCard, ItemId.Hamburger, ItemId.Ice, ItemId.AICore, ItemId.Chlorine, ItemId.DroneBattery, ItemId.Money };
    static ItemId[] ItemsYouCanCombine = { ItemId.Ice, ItemId.Chlorine, ItemId.DroneBattery, };
    static NPCId[] NPCsYouCanTalkTo = { NPCId.Omar, NPCId.CleanBot, NPCId.Fred, };

    // Current state
    static LocationData CurrentLocation = new LocationData();
    static Dictionary<ItemId, LocationId> CurrentItemLocations = new Dictionary<ItemId, LocationId>();
    static Dictionary<NPCId, LocationId> CurrentNPCLocations = new Dictionary<NPCId, LocationId>();

    // Goal flags
    static bool OmarRescued;
    static bool FredDestroyed;
    static bool OmarDestroyed;
    static bool EntranceUnlocked;

    // Door / Ventilation "doors locked or unlocked" flags
    static bool FreezerServerRoomVentUnlocked;
    static bool BathRoomToWasteProcessingVentUnlocked;
    static bool WasteProcessingToBathroomVentUnlocked;
    static bool ServerRoomDoorUnlocked;
    static bool KitchenDoorUnlocked;

    // flag to check if the player has helped CleanBot
    static bool CleanBotUnstuck;

    // inital state for the "quit" flag
    static bool shouldQuit = false;

    //Methods for reading the different data
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
        // Parse the item file
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
    static void ReadNPCData()
    {
        // Parse the NPC file
        List<ParsedData> parsedDataList = ParseData("NpcData.txt");

        // Transfer data from the parsed structures into NPC data
        foreach (ParsedData parsedData in parsedDataList)
        {
            NPCId npcId = Enum.Parse<NPCId>(parsedData.Id);
            NPCData npcData = new NPCData
            {
                id = npcId,
                Name = parsedData.Name,
                Description = parsedData.Description,
                StartingLocationId = parsedData.StartingLocationId,
                DialogueNode = parsedData.StartingDialogueNode,
            };
            NPCsData[npcId] = npcData;
        }
    }

    //Methods for initalizing helpers and setting the starting state of the game
    static void InitializeItemHelpers()
    {
        // Create a map of items by their name
        foreach (KeyValuePair<ItemId, ItemData> itemEntry in ItemsData)
        {
            string name = itemEntry.Value.Name.ToLowerInvariant();

            // Allow to refer to an item by any of its words.
            string[] nameParts = name.Split();

            foreach (string namepart in nameParts)
            {
                // Don't override already assigned words
                if (ItemIdsByName.ContainsKey(namepart)) continue;

                ItemIdsByName[namepart] = itemEntry.Key;
            }
        }
    }
    static void InitializeStartingState()
    {
        // Set player starting location
        CurrentLocation = LocationsData[LocationId.Entrance];

        // Set all Items to their starting locations.
        foreach (KeyValuePair<ItemId, ItemData> itemEntry in ItemsData)
        {
            CurrentItemLocations[itemEntry.Key] = itemEntry.Value.StartingLocationId;
        }

        // Set all NPCs to their starting locations
        foreach (KeyValuePair<NPCId, NPCData> npcEntry in NPCsData)
        {
            CurrentNPCLocations[npcEntry.Key] = npcEntry.Value.StartingLocationId;
        }

        // Set goal flags
        OmarRescued = false;
        OmarDestroyed = false;
        FredDestroyed = false;
        EntranceUnlocked = false;

        // Set door flags
        FreezerServerRoomVentUnlocked = false;
        BathRoomToWasteProcessingVentUnlocked = false;
        WasteProcessingToBathroomVentUnlocked = false;
        ServerRoomDoorUnlocked = false;
        KitchenDoorUnlocked = false;

        // Set status of CleanBot
        CleanBotUnstuck = false;
    }

    // Parse data
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

                    case "StartingDialogueNode":
                        parsedData.StartingDialogueNode = value;
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

    //parse dialogue data
    static void ReadDialogueData(string filePath)
    {
        string[] dataLines = File.ReadAllLines(filePath);
        var currentLineIndex = 0;
        //Parse dialogue data until we reach the end.
        NpcDialogueNodes currentDialogueNode = new NpcDialogueNodes();


        while (currentLineIndex < dataLines.Length)
        {
            MatchCollection matches = Regex.Matches(dataLines[currentLineIndex], @"(\w+): *(.*)?");
            if (matches.Count != 0)
            {
                string property = matches[0].Groups[1].Value;
                string value = matches[0].Groups[2]?.Value ?? string.Empty;

                switch (property)
                {
                    case "Id":
                        npcDialogueNodes.Add(value, new NpcDialogueNodes() { id = value });
                        currentDialogueNode = npcDialogueNodes[value];
                        break;

                    case "Prompt":
                        currentDialogueNode.prompt = value;
                        break;
                }
            }
            currentLineIndex++;
        }

        currentLineIndex = 0;
        while (currentLineIndex < dataLines.Length)
        {
            MatchCollection matches = Regex.Matches(dataLines[currentLineIndex], @"(\w+): *(.*)?");
            if (matches.Count != 0)
            {
                string property = matches[0].Groups[1].Value;
                string value = matches[0].Groups[2]?.Value ?? string.Empty;

                switch (property)
                {
                    case "Id":
                        currentDialogueNode = npcDialogueNodes[value];
                        break;

                    case "A":
                        string[] valueArray = value.Split(';');

                        string valueString = valueArray[0];
                        string valueKey = valueArray[1];

                        AnswersData answer = new AnswersData() { Answer = valueString, Destination = npcDialogueNodes[valueKey] };
                        currentDialogueNode.answers.Add(answer);

                        break;
                }
            }
            currentLineIndex++;
        }
    }

    //This method gives the user the text description for the location its at
    static void DisplayCurrentLocation()
    {
        //TODO uncomment this for play build, to limit the amount of text on screen, makes the description more current.
        //Console.Clear();
        Console.ForegroundColor = NarrativeColor;

        // Display current location description.
        //TODO comment away the location name for the playable build, it ruins immersion
        //Print(CurrentLocation.Name);

        if (CurrentLocation.Id == LocationId.BathRoom && BathRoomToWasteProcessingVentUnlocked)
        {
            Print("The bathroom is clean and neat. There is a toilet and a sink on the wall, with a mirror above the sink. There's an airvent cover laying on the ground");
            return;
        }
        if (CurrentLocation.Id == LocationId.WasteProcessing && WasteProcessingToBathroomVentUnlocked)
        {
            Print("You enter the Waste processing unit of the resturant. There is a harsh smell of cleaning supplies in the room. Several heavy machine stations are littered around the room, box pressers, shredders and grinders. There is a door leading north in here. On it's side is a small card reader. On the floor next to the western wall is an air vent cover. Its emitting a faint noise. To the east is a door labeled \"Kitchen\".");
            return;
        }
        if (CurrentLocation.Id == LocationId.ServerRoom && FredDestroyed)
        {
            Print("The room has been damaged by the explosion, it's hard to see things clearly trough the smoke. The air vent to the freezer is bringing in some fresh air. The server racks are damaged and the interface that was on earlier is now black, its screen cracked and broken");
            return;
        }

        Print(CurrentLocation.Description);
    }

    //This method display additional text about the location, if there is any, when the player uses the "LOOK" command
    static void DisplayLookText(LocationData currentLocation)
    {
        Console.ForegroundColor = NarrativeColor;

        void PrintDefaultLookText()
        {
            if (currentLocation.DetailedDescription != string.Empty)
            {
                Print(currentLocation.DetailedDescription);
            }
            else
            {
                Print("There is nothing else really noteworthy or interesting here.");
            }
            Console.ReadKey();
        }

        switch (currentLocation.Id)
        {

            //TODO expand this to include if there are items in the location, and which npc that are there
            case LocationId.Entrance:
                if (!EntranceUnlocked)
                {
                    Print("The doors out of the resturant are locked tight");
                    PrintDefaultLookText();
                }
                else
                {
                    Print("The doors are unlocked now. I can leave if I want to");
                    PrintDefaultLookText();
                }
                break;

            case LocationId.OrderStation:
                Print("I Could maybe talk to the order manager at the interface");
                PrintDefaultLookText();
                break;

            case LocationId.EatingArea:
                PrintDefaultLookText();
                break;

            case LocationId.BathRoom:
                if (!BathRoomToWasteProcessingVentUnlocked)
                {
                    Print("On the wall is a quite large air vent cover, with a faint humming noise emitting from it. The cover looks a bit loose. Maybe I can unscrew it somehow");
                    Console.ReadKey();
                }
                if (CleanBotUnstuck)
                {
                    Print("The cleaning robot I helped earlier is here, cleaning. I could talk to it if I want. It looks a bit busy though");
                    Console.ReadKey();
                }
                else
                {
                    PrintDefaultLookText();
                }
                break;

            case LocationId.FoodDeliveryStation:
                if (!KitchenDoorUnlocked)
                {
                    Print("The doors are locked. I can't open them. I will have to find another way past them");
                    Console.ReadKey();
                }
                else
                {
                    Print("The doors are unlocked now, I can go trough");
                    PrintDefaultLookText();
                }
                break;

            case LocationId.Kitchen:
                if (!ItemAt(ItemId.Hamburger, LocationId.Inventory))
                {
                    Print("There is a hamburger here that seems to be ready to serve. I could grab it with me I suppose");
                    Console.ReadKey();
                }
                else
                {
                    PrintDefaultLookText();
                }
                break;

            case LocationId.Freezer:
                if (!ItemAt(ItemId.Ice, LocationId.Inventory))
                {
                    Print("I could grab some of the ice that's here. I don't know what I would need it for though...");
                    Console.ReadKey();
                }
                else
                {
                    PrintDefaultLookText();
                }
                break;

            case LocationId.WasteProcessing:
                if (!ItemAt(ItemId.KeyCard, LocationId.Inventory))
                {
                    Print("I could probably get through the security door if I could just find the key");
                    PrintDefaultLookText();
                }
                if (ItemAt(ItemId.KeyCard, LocationId.Inventory))
                {
                    Print("With the key card I found, I can probably go through security door");
                    PrintDefaultLookText();
                }
                break;

            case LocationId.Storage:
                PrintDefaultLookText();
                break;

            case LocationId.CleanBotBay:
                if (!CleanBotUnstuck)
                {
                    Print("Theres a cleaning robot here. It seems to be struggling, maybe it's having problems? It looks a bit stressed. I could talk to it I guess");
                    Console.ReadKey();
                }
                if (CleanBotUnstuck && ItemAt(ItemId.Chlorine, LocationId.CleanBotBay))
                {
                    Print("The robot left in a hurry. It looks like it dropped something. A bottle of chlorine? I could take it with me");
                    Console.ReadKey();
                }
                if (CleanBotUnstuck && !ItemAt(ItemId.Chlorine, LocationId.CleanBotBay))
                {
                    Print("The robot left in a hurry, I wonder where it went?");
                }

                PrintDefaultLookText();
                break;

            case LocationId.ShippingBay:
                if (!ItemAt(ItemId.DroneBattery, LocationId.Inventory))
                {
                    Print("The offline drone has its battery hatch open. I could probably take the battery if I wanted to use it for something");
                    Console.ReadKey();
                }

                if (ItemAt(ItemId.AICore, LocationId.Inventory))
                {
                    Print("The outside door to the shipping bay is open, and a active drone is hovering there in the air. Its camera is facing away from you and it's loading dock" +
                        "is open. There are some cables there that look like they could plug into Omar's core if I wanted to help him escape");
                    Console.ReadKey();
                }

                PrintDefaultLookText();
                break;

            case LocationId.ServerRoom:

                if (!FredDestroyed)
                {
                    Print("The small glowing screen looks like some kind of interface, maybe I can talk to someone through it?");
                    Console.ReadKey();
                }

                if (ItemAt(ItemId.AICore, LocationId.ServerRoom))
                {
                    Print("The one computer component that is blinking green must be Omar's core. I could get it to get him out of here");
                    Console.ReadKey();
                }

                if (!ItemAt(ItemId.AICore, LocationId.ServerRoom) && !FredDestroyed)
                {
                    Print("There is an empty space now wherer Omar's core was. I could put him back I guess if I wanted to");
                    Console.ReadKey();
                }

                if (!ItemAt(ItemId.KeyCard, LocationId.Inventory))
                {
                    Print("There is a plastic card with a little chip laying on one of the server racks. Maybe it's some kind of access card or key? I should get it, I might need it");
                    Console.ReadKey();
                }

                if (!ServerRoomDoorUnlocked)
                {
                    Print("There's a big mechanical lever on the inside of the door to open it up. Some kind of security measure? I could unseal the door by walking out that way");
                    Console.ReadKey();
                }

                if (FredDestroyed)
                {
                    Print("The room has been demolished. Scraps of metal, buzzing wires and smoke fills the room. I should probably hold my breath aswell");
                }

                break;

            case LocationId.Nowhere:
                PrintDefaultLookText();
                Print("Also this is really fucked up. I really don't get how you got here. Turn around now");
                break;

        }
    }

    //This method checks if an item is there at the location
    static bool ItemAt(ItemId itemId, LocationId locationId)
    {
        if (!CurrentItemLocations.ContainsKey(itemId)) return false;
        return CurrentItemLocations[itemId] == locationId;
    }

    //This method checks if a NPC is there at the location
    static bool NPCAt(NPCId npcId, LocationId locationId)
    {
        if (!CurrentNPCLocations.ContainsKey(npcId)) return false;
        return CurrentNPCLocations[npcId] == locationId;
    }

    //This lets the game know where items are
    static IEnumerable<ItemId> GetItemsAtLocation(LocationId locationId)
    {
        return CurrentItemLocations.Keys.Where(itemId => ItemAt(itemId, locationId));
    }

    //This lets the game know where NPCs are
    static IEnumerable<NPCId> GetNPCsAtLocation(LocationId locationId)
    {
        return CurrentNPCLocations.Keys.Where(npcId => NPCAt(npcId, locationId));
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

    //Gets the starting location of an item for debugging purpouses
    static LocationId GetItemStartingLocation(ItemId itemId)
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
                ItemId itemId = ItemIdsByName[word];

                if (!itemIds.Contains(itemId))
                {
                    itemIds.Add(itemId);
                }
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
            Console.ReadKey();
        }
        else
        {
            foreach (ItemId itemId in itemsInInventory)
            {
                Print($"{GetName(itemId)}.");
            }
            Console.ReadKey();
        }
    }

    //Lets the player take a closer look at things they are carrying, useful for hints about what to use the item for and for worldbuilding
    static void InspectItem(List<ItemId> itemIds)
    {
        if (itemIds.Count > 0)
        {
            if (ItemAt(itemIds[0], LocationId.Inventory))
            {
                Print(GetName(itemIds[0]));
                Print(GetDescription(itemIds[0]));
                Console.ReadKey();
            }
            else
            {
                //TODO maybe change this to not include spoilers? IDK your choice
                Print($"You are not carrying {GetName(itemIds[0])}");
                Console.ReadKey();
            }
        }
        else
        {
            Print("I don't know what that is");
            Console.ReadKey();
        }
    }

    //Method for trying to pick up items
    static void GetItems(List<ItemId> itemIds)
    {
        //TODO Make a function to check if the player is already carrying the item they are trying to pick up and tell them that
        IEnumerable<ItemId> itemsAtThisLocation = GetItemsAtLocation(CurrentLocation.Id);

        List<ItemId> avalibleItemsToGet = ItemsYouCanGet.Intersect(itemsAtThisLocation).ToList();

        if (avalibleItemsToGet.Count() == 0)
        {
            Print("There is nothing in this room that you can pick up");
            Console.ReadKey();
            return;
        }

        if (itemIds.Count == 0)
        {
            Print("Get what?");
            Console.ReadKey();
            return;
        }

        foreach (ItemId itemId in itemIds)
        {
            if (avalibleItemsToGet.Contains(itemId))
            {
                if (itemId == ItemId.Chlorine && !CleanBotUnstuck)
                {
                    Print("The Chlorine bottle is stuck underneath Cleanbot, I can't get it out");
                    Console.ReadKey();
                    return;
                }

                CurrentItemLocations[itemId] = LocationId.Inventory;
                Print($"You picked up the {ItemsData[itemId].Name}");
                Console.ReadKey();
            }
            else
            {
                Print($"You can't find {ItemsData[itemId].Name}");
                Console.ReadKey();
            }
        }
    }

    //A method for using items, checks where the player is at and what item they want to use
    static void UseItems(List<ItemId> itemIds)
    {
        if (itemIds.Count == 0)
        {
            Print("Use what?");
            Console.ReadKey();
            return;
        }

        foreach (ItemId itemId in itemIds)
        {
            string itemName = ItemsData[itemId].Name;
            if (CurrentItemLocations[itemId] != LocationId.Inventory)
            {
                Print($"I don't have a {itemName}");
                Console.ReadKey();
                continue;
            }
            //Try to use the item
            bool useItemHandled = false;

            switch (itemId)
            {
                case ItemId.Hamburger:
                    useItemHandled = HandleUseHamburger();
                    break;

                case ItemId.AICore:
                    useItemHandled = HandleUseAICore();
                    break;

                case ItemId.MixedExplosive:
                    useItemHandled = HandleUseMixedExplosive();
                    break;

                case ItemId.MultiTool:
                    useItemHandled = HandleUseMultiTool();
                    break;

            }

            //report failure 
            if (!useItemHandled)
            {
                Print($"I don't know how I'm supposed to use the {itemName} right now");
                Console.ReadKey();
            }
        }
        //TODO
        //I've already used {itemName} like that
        //Success!!!
    }

    static bool HandleUseMultiTool()
    {
        if (CurrentLocation.Id == LocationId.BathRoom && !BathRoomToWasteProcessingVentUnlocked)
        {
            Print("You unscrew the cover to the ventilation duct. You can probably climb in there now");
            Console.ReadKey();
            BathRoomToWasteProcessingVentUnlocked = true;
            return true;
        }

        if (CurrentLocation.Id == LocationId.WasteProcessing && !WasteProcessingToBathroomVentUnlocked)
        {
            Print("You unscrew the cover to the ventilation duct. You can probably climb in there now");
            Console.ReadKey();
            WasteProcessingToBathroomVentUnlocked = true;
            return true;
        }

        if (CurrentLocation.Id == LocationId.Freezer && !FreezerServerRoomVentUnlocked)
        {
            Print("You unscrew the cover to the ventilation duct. You can probably climb in there now");
            Console.ReadKey();
            FreezerServerRoomVentUnlocked = true;
            return true;
        }

        if (CurrentLocation.Id == LocationId.CleanBotBay && CurrentNPCLocations[NPCId.CleanBot] == LocationId.CleanBotBay)
        {
            Print("You use your multi-tool to help CleanBot get his motivator unstuck");
            //TODO Make a little Dialogue with CleanBot for helping him get unstuck.
            Console.ReadKey();
            Print("CleanBot whirrs away");
            CurrentNPCLocations[NPCId.CleanBot] = LocationId.BathRoom;
            CleanBotUnstuck = true;
            Console.ReadKey();
            Print("It left behind a small plasic cylinder of chlorine, you can probably pick that up if you want");
            Console.ReadKey();
            return true;
        }

        return false;
    }

    static bool HandleUseMixedExplosive()
    {
        if (CurrentLocation.Id != LocationId.ServerRoom)
        {
            Print("Explosions are fun and all, but I shouldn't set this off here. What would be the point?");
            Console.ReadKey();
            return false;
        }

        if (CurrentItemLocations[ItemId.AICore] == LocationId.ServerRoom)
        {
            OmarDestroyed = true;
            EntranceUnlocked = true;
        }

        FredDestroyed = true;
        CurrentItemLocations[ItemId.MixedExplosive] = LocationId.Nowhere;
        Print("You set the explosion and run out of the server room. A few moments pass before a low loud bang is heard, followed by clanking metal scraps hitting the floor");
        SwitchLocation(LocationId.Storage);
        Console.ReadKey();
        return true;

    }

    static bool HandleUseAICore()
    {
        if (CurrentLocation.Id != LocationId.ShippingBay)
        {
            if (CurrentLocation.Id == LocationId.ServerRoom)
            {
                if (!FredDestroyed)
                {
                    Print("You install the AI-core back where you grabbed it. It starts blinking like before");
                    CurrentItemLocations[ItemId.AICore] = LocationId.ServerRoom;
                    Console.ReadKey();
                    return true;
                }

                Print("You can't really put back Omar's core in the destroyed server rack...");
                Console.ReadKey();
                return false;
            }

            return false;
        }
        OmarRescued = true;
        EntranceUnlocked = true;
        Print("You hook up the AI-core to the shipping drone. It briefly flickers from red to green in its display. You hear a \"CLANK\" sound coming from the entrance doors." +
            "The drone hums as it briefly turns its camera towards you before turning and flying away");
        CurrentItemLocations[ItemId.AICore] = LocationId.Nowhere;
        Console.ReadKey();
        return true;
    }

    static bool HandleUseHamburger()
    {
        Print("I might need this if I don't get out of here soon. Better save it for later");
        Console.ReadKey();
        return true;
    }

    //A method for trying to combine items
    static void CombineItems()
    {
        IEnumerable<ItemId> itemsInInventory = GetItemsAtLocation(LocationId.Inventory);

        List<ItemId> avalibleItemsToCombine = ItemsYouCanCombine.Intersect(itemsInInventory).ToList();

        switch (avalibleItemsToCombine.Count)
        {
            case 0:
                Print("I don't have anything with me that I can combine with something else");
                Console.ReadKey();
                break;

            case 1:
                Print($"I only have one thing, the {GetName(avalibleItemsToCombine[0])}, that I could possibly combine with something else, I need to look around more");
                Console.ReadKey();
                break;

            case 2:
                Print($"It's a good start with these two, the {GetName(avalibleItemsToCombine[0])} and the {GetName(avalibleItemsToCombine[1])}. I just need something more");
                Console.ReadKey();
                break;

            case 3:
                Print("That's it!");
                Console.ReadKey();
                Print("I have combined the items into an explosive!");
                CurrentItemLocations[ItemId.DroneBattery] = LocationId.Nowhere;
                CurrentItemLocations[ItemId.Chlorine] = LocationId.Nowhere;
                CurrentItemLocations[ItemId.Ice] = LocationId.Nowhere;
                CurrentItemLocations[ItemId.MixedExplosive] = LocationId.Inventory;
                Console.ReadKey();
                break;

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
        //Helpers
        bool GoFromTo(LocationId a, LocationId b)
        {
            return CurrentLocation.Id == a && destinationLocationId == b;
        }

        bool GoBetween(LocationId a, LocationId b)
        {
            return GoFromTo(a, b) || GoFromTo(b, a);
        }

        //Cases for locked doors/vents
        if (GoBetween(LocationId.Kitchen, LocationId.FoodDeliveryStation) && !KitchenDoorUnlocked)
        {
            Print("The door is locked");
            Console.ReadKey();
            return;
        }

        if (GoFromTo(LocationId.BathRoom, LocationId.WasteProcessing) && !BathRoomToWasteProcessingVentUnlocked)
        {
            Print("The ventilation duct is underneath a metal cover. I need to remove the cover before I try to crawl trough");
            Console.ReadKey();
            return;
        }

        if (GoFromTo(LocationId.BathRoom, LocationId.WasteProcessing) && !WasteProcessingToBathroomVentUnlocked)
        {
            WasteProcessingToBathroomVentUnlocked = true;
            Print("As you reach the end of the ventilation duct, you kick out the cover. It lands on the floor of the room");
            Console.ReadKey();
        }

        if (GoFromTo(LocationId.WasteProcessing, LocationId.BathRoom) && !WasteProcessingToBathroomVentUnlocked)
        {
            Print("The ventilation duct is underneath a metal cover. I need to remove the cover before I try to crawl trough");
            Console.ReadKey();
            return;
        }

        if (GoFromTo(LocationId.WasteProcessing, LocationId.BathRoom) && !BathRoomToWasteProcessingVentUnlocked)
        {
            BathRoomToWasteProcessingVentUnlocked = true;
            Print("As you reach the end of the ventilation duct, you kick out the cover. It lands on the floor of the room");
            Console.ReadKey();
        }

        if (GoFromTo(LocationId.Freezer, LocationId.ServerRoom) && !FreezerServerRoomVentUnlocked)
        {
            Print("The ventilation duct is underneath a metal cover. I need to remove the cover before I try to crawl trough");
            Console.ReadKey();
            return;
        }

        if (GoFromTo(LocationId.Storage, LocationId.ServerRoom) && !ServerRoomDoorUnlocked)
        {
            Print("The Security door leading to the Data center is sealed. It will take much more than just pushing real hard to get it open. Maybe there is another way in?");
            Console.ReadKey();
            return;
        }

        if (GoFromTo(LocationId.ServerRoom, LocationId.Storage) && !ServerRoomDoorUnlocked)
        {
            ServerRoomDoorUnlocked = true;
            Print("You find the lever to unseal the security door from inside. The door is now open and you can keep exploring");
            Console.ReadKey();
        }

        if (GoFromTo(LocationId.OrderStation, LocationId.Entrance) && !EntranceUnlocked)
        {
            Print("The entrance is locked. You try to pull at the doors as hard as you can but they won't budge a millimeter");
            Console.ReadKey();
            return;
        }

        if (GoFromTo(LocationId.WasteProcessing, LocationId.ShippingBay) && CurrentItemLocations[ItemId.KeyCard] != LocationId.Inventory)
        {
            Print("The door is locked. There is a card reader on the side of the door thats blinking, maybe I need some kind of key or card?");
            Console.ReadKey();
            return;
        }

        if (GoFromTo(LocationId.Storage, LocationId.ShippingBay) && CurrentItemLocations[ItemId.KeyCard] != LocationId.Inventory)
        {
            Print("The door is locked. There is a card reader on the side of the door thats blinking, maybe I need some kind of keycard or password?");
            Console.ReadKey();
            return;
        }

        if (GoFromTo(LocationId.Storage, LocationId.ShippingBay) || GoFromTo(LocationId.WasteProcessing, LocationId.ShippingBay) && CurrentItemLocations[ItemId.KeyCard] == LocationId.Inventory)
        {
            Print("You hold up the key card to the reader. It emits of a short \"beep\" sound and you hear the door unlock");
            Console.ReadKey();

            if (ItemAt(ItemId.AICore, LocationId.Inventory))
            {
                Print("You hear the humming noise of a drone nearby. Is there a delivery coming?");
                Console.ReadKey();
            }
        }

        //If the player has unlocked the entrance, check the victory condition and display victory screen
        if (GoFromTo(LocationId.OrderStation, LocationId.Entrance) && EntranceUnlocked)
        {
            //TODO implement victory conditions
        }

        CurrentLocation = LocationsData[destinationLocationId];
    }

    //Method for handling talking to NPC
    static void TalkTo(LocationData currentLocation)
    {
        IEnumerable<NPCId> NPCsAtThisLocation = GetNPCsAtLocation(CurrentLocation.Id);
        List<NPCId> avalibleNPCsToTalkTo = NPCsYouCanTalkTo.Intersect(NPCsAtThisLocation).ToList();

        if (avalibleNPCsToTalkTo.Count() == 0)
        {
            Print("There is no one in this room that you can talk to");
            Console.ReadKey();
            return;
        }
        //As off now this thing only works if there is one npc, otherwise it wont work as intended. Im to dumb to figure out another way to do it.

        NPCId currentTalkingNpc = avalibleNPCsToTalkTo[0];
        string currentDialogueNode = NPCsData[currentTalkingNpc].DialogueNode;
        Print(currentDialogueNode);
        Console.ReadKey();
    }
    //Find the right dialogue node for the NPC to start with
    //Display the correct NPC node prompt and display the avalible answers
    //Get the answer option from the player
    //Handle any external events that arise from the chosen answer
    //Proceed to that NPC node and display the text and answer.
    //If there are not answers then display the final dialogue, wait for a button press and return to the game.



    //Method for restarting the game
    static void Restart()
    {
        InitializeStartingState();
        DisplayIntro();
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
            //TODO uncomment for play build
            Thread.Sleep(PrintPauseMilliseconds);
        }
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
                            Console.ReadKey();
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
                            Console.ReadKey();
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
                            Console.ReadKey();
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
                            Console.ReadKey();
                        }
                        break;

                    default:
                        Print("Go where?");
                        Console.ReadKey();
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
                    Console.ReadKey();
                }
                break;

            case "inventory":
            case "inv":
                DisplayInventory();
                break;

            case "inspect":
                if (words.Length > 1)
                {
                    InspectItem(itemIds);
                }
                else
                {
                    Print("Inspect what?");
                    Console.ReadKey();
                }
                break;

            case "look":
                DisplayLookText(CurrentLocation);
                break;

            case "get":
            case "pick":
            case "take":
                GetItems(itemIds);
                break;

            case "use":
                UseItems(itemIds);
                break;

            case "combine":
                CombineItems();
                break;

            case "talk":
            case "speak":
                TalkTo(CurrentLocation);
                break;

            case "help":
                DisplayHelp();
                Console.ReadKey();
                Console.Clear();
                break;

            case "quit":
            case "exit":
                Console.ForegroundColor = SystemColor;
                Print("Do you want to quit the game? YES or NO");
                string confirmation = (Console.ReadLine() ?? string.Empty).ToLowerInvariant();

                if (confirmation == "yes")
                {
                    Print("Goodbye!");
                    Console.ReadKey();
                    shouldQuit = true;

                }
                else if (confirmation == "no")
                {
                    Print("Okay, let's continue");
                    Console.ReadKey();

                }
                else
                {
                    Print("I didn't understand what you meant, so let's continue");
                    Console.ReadKey();
                }
                break;

            case "restart":
                Console.ForegroundColor = SystemColor;
                Print("Do you want to restart the game? YES or NO");
                confirmation = (Console.ReadLine() ?? string.Empty).ToLowerInvariant();
                if (confirmation == "yes")
                {
                    Print("Okay, let's start over");
                    Console.ReadKey();
                    Restart();
                }
                else if (confirmation == "no")
                {
                    Print("Okay, let's continue");
                    Console.ReadKey();

                }
                else
                {
                    Print("I didn't understand what you meant, so let's continue");
                    Console.ReadKey();
                }
                break;

            default:
                Print("I do not understand you.");
                Console.ReadKey();
                break;
        }
    }

    static void DisplayIntro()
    {
        Console.Clear();
        Console.ForegroundColor = NarrativeColor;
        string[] intro = File.ReadAllLines("Intro.txt");
        foreach (string str in intro)
        {
            Print(str);
        }
        Console.ReadKey();
        Console.Clear();
    }

    private static void Main(string[] args)
    {
        //Initalizing data
        ReadLocationsData();
        ReadItemData();
        ReadNPCData();
        ReadDialogueData("DialogueData.txt");

        //Initalizing stuff for handling items
        InitializeItemHelpers();

        //Initialize starting state
        InitializeStartingState();

        // Display title screen
        string title = File.ReadAllText("Title.txt");
        Console.WriteLine(title);
        Console.ReadKey();
        Console.Clear();

        // Display intro
        DisplayIntro();

        //Gameplay loop
        while (shouldQuit == false)
        {
            ClearBuffer();
            DisplayCurrentLocation();
            HandlePlayerAction();
        }
    }
}






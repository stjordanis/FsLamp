module Game
open Domain
open GameState
open Combinators
open Parser
open System

let defaultMap =
    [|
        (createEnvironment 1 "Origin"
            "A moment ago you were just in bed floating above your mind, dreaming about how to add zebras to spreadsheets.  Now it appears you've awakened in a dimlit room. Many unfamiliar smells lurk around you."
            [createExit 1 2 Open North (Steps 2) "Creaky Door"]
            [createInventoryItem "Key" "in a pile of debris and trash" [Unlock (ExitId 5, "After a few minutes of getting the key to fit correctly, the lock releases and the door creakily opens.")]]
            []
        );
        (createEnvironment 2 "Long Hallway, South End"
            "The door opens into what appears to be a really long hallway continuing North. There's no light at the other end."
            [
                createExit 2 1 Open South (Steps 2) "Creaky Door";
                createExit 3 3 Open North (Steps 6) "Dark hallway";]
            []
            []
        );
        (createEnvironment 3 "Long Hallway, North End"
            "It gets so dark you have to feel your way around. Thankfully there's nothing too dangerous in your path."
            [ 
                createExit 4 2 Open South (Steps 6) "The south end of the hallway";
                createExit 5 4 Locked East (Steps 6) "A door with no features, labeled 'Private'"]
            []
            []
        );
        (createEnvironment 4 "Office"
            "As the door opens, you begin to see the remnants of an old dusty office.  This place hasn't been used in years. An old typewriter on the desk is missing most of its keys."
            [ createExit 6 3 Open West (Steps 6) "Door with no features"; createExit 7 5 Hidden East (Steps 2) "Secret Passage"]
            []
            [createEnvironmentItem "typewriter" [Unhide (ExitId 7, "As you press down hard on one of the keys. The air begins to move around you. Suddenly, a secret passage opens up from within the wall.")]]
        );
        (createEnvironment 5 "Secret Passage"
            """The path leads downward with considerable gradient. Things turn cold as you hear a voice... 'stoi impul chani, mario.' Frozen, but unable to make out any figures ahead of you, you shout back 'Who's there?'
A few seconds pass, finally a response... 'die!'.  As you fall backward you stumble over a rock.            
            """
            [ createExit 7 4 Open West (Steps 2) "Secret entrance"; createExit 8 6 Hidden East (Steps 10) "Dark Passage towards the footsteps"]
            []
            [createEnvironmentItem "rock" [Unhide (ExitId 8, "You throw the rock directly at the voice and hear a terrible scream.  Moments later you can hear footsteps running to the east away from you.")]]
        );
        (createEnvironment 6 "Dark Passage"
            """Is it really a good idea to go chasing after such a terrible, unknown, thing? Probably not, but that hasn't stopped you so far."""
            [ createExit 9 5 Open West (Steps 10) "Secret Passage"]
            [createInventoryItem "Gold" "that probably fell out of someones pocket" []]
            []
        );
    |]

let defaultGamestate map =
    { Player = Player "one";
        Health = Health (12.,12.);
        Experience = Experience (0, 1);
        Inventory = [];
        World = { Time = DateTime.Parse("1971-01-01 06:01:42"); Map = map };
        Environment = map.[0];
        LastCommand = NoCommand;
        Output = Empty}


let getCommand (parseInput: CommandParser) =
    Console.Write("\n$> ")
    let readline = Console.ReadLine()
    match readline |> parseInput with
    | Some command -> command
    | None -> 
        printfn "I don't understand %s." readline
        NoCommand

let handleOutput = function
    | Empty -> ()
    | Output log -> log |> List.iter (printfn "%s")

let RunInConsole (parseInput: CommandParser) (dispatcher: Command -> GamePart) initialState =
    let rec gameLoop gamestate history command =
        if command = Exit then ()
        else
            let (nextState, nextHistory) = 
                // crudely rewind history
                if command = Undo then
                    let (oldState, newHistory) =
                        match history with
                        | _ :: old :: tail -> 
                            (old, tail)
                        | _ -> (gamestate, history)

                    ((command |> dispatcher) <| oldState, newHistory)
                else 
                    ((command |> dispatcher) <| gamestate, history)

            nextState.Output |> handleOutput
            
            getCommand parseInput |> gameLoop nextState (nextState :: nextHistory)

    gameLoop initialState [] StartGame
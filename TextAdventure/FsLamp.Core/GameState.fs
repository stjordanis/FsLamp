namespace FsLamp.Core

module GameState =
    open FsLamp.Core.Domain
    open FsLamp.Core.Items
    open FsLamp.Core.Environment

    type World = {
        Time: System.DateTime
        Map: Map
    }
    and Map = Environment []

    type GameScene =
    | MainMenu
    | OpenExplore
    | InEncounter of EncounterProperties
    | CustomScene of CommandParser

    type GameState = {
        Player: Player
        Inventory: InventoryItem list
        Environment: Environment
        World: World
        GameScene: GameScene
        LastCommand: Command
        Output: Output
    }

    type GameBehavior<'a> = 
    | UpdateBehavior of ('a -> GameState -> ('a * GameState))
    | OutputBehavior of ('a -> GameState -> string list)

    type GameHistory = GameState list

    module Player =
        let setPlayer player gamestate =
            { gamestate with Player = player}


    module Scene =
        let setScene scene gamestate =
            { gamestate with GameScene = scene }

    module World =
        let updateWorldEnvironment gamestate =
            // replace environment in the map
            let map =
                gamestate.World.Map
                |> Array.map (fun env ->
                    if env.Id = gamestate.Environment.Id then gamestate.Environment else env)
            let world = {gamestate.World with Map = map }
            { gamestate with World = world }

        let updateWorldTravelTime distance gamestate =
            let time = distance |> timespanFromDistance
            let world = {gamestate.World with Time = gamestate.World.Time + time }
            { gamestate with World = world }

    module Environment =
        
        let findById id gamestate =
            gamestate.World.Map
            |> Array.find (fun env -> env.Id = id)

        let setEnvironment environment gamestate =
            { gamestate with Environment = environment }

        let updateEnvironment (exit: Exit) gamestate =
            let exits = gamestate.Environment.Exits |> List.map (fun e -> if e.Id = exit.Id then exit else e)
            let environment = {gamestate.Environment with Exits = exits }
            { gamestate with Environment = environment} 

        let openExit exit gamestate =
            gamestate
            |> updateEnvironment { exit with ExitState = ExitState.Open }
            |> World.updateWorldEnvironment

        let removeItemFromEnvironment item gamestate =
            let environment = {gamestate.Environment with InventoryItems = gamestate.Environment.InventoryItems |> List.filter (fun i -> i <> item) }
            { gamestate with Environment = environment}

        let addItemToEnvironment item gamestate =
            let environment = {gamestate.Environment with InventoryItems = item :: gamestate.Environment.InventoryItems }
            { gamestate with Environment = environment}
        
        let setEnvironmentItems items gamestate =
            let environment = {gamestate.Environment with InventoryItems = items }
            { gamestate with Environment = environment}

    module Inventory =
        let setInventory inventory gamestate =
            { gamestate with Inventory = inventory }
        
        let addItem item gamestate =
            { gamestate with Inventory = item :: gamestate.Inventory }

        let updateItem (item: InventoryItem) gamestate =
            let newInventory = gamestate.Inventory |> List.map (fun i -> if i.Id = item.Id then item else i)
            { gamestate with Inventory = newInventory }


    module Output =
        let setOutput output gamestate =
            { gamestate with Output = output }

        let addOutput s gamestate =
            match gamestate.Output with
            | Output output ->
                { gamestate with Output = Output (output @ [s]) }
            | _ -> 
                gamestate

        let appendOutputs outputs gamestate =
            match gamestate.Output with
            | Output output ->
                { gamestate with Output = Output (output @ outputs) }
            | _ -> 
                gamestate

        let prependOutputs outputs gamestate =
            match gamestate.Output with
            | Output output ->
                { gamestate with Output = Output (outputs @ output) }
            | _ -> 
                gamestate




    module Encounter =

        let remove gamestate =
            let items = 
                gamestate.Environment.EnvironmentItems 
                |> List.filter (fun i -> 
                    match i with 
                    | Encounter _ -> false
                    | _ -> true)

            let environment = {gamestate.Environment with EnvironmentItems = items }
            {gamestate with Environment = environment }

        let updateMonster (encounter: EncounterProperties) (monster: Monster) =
            let monsters = encounter.Monsters |> List.map (fun m -> if m.Name = monster.Name then monster else m)
            {encounter with Monsters = monsters}

        let updateEncounter (encounter: EncounterProperties) gamestate =
            let environmentItems = 
                gamestate.Environment.EnvironmentItems 
                |> List.map (fun i ->
                    match i with
                    | Encounter e when e.Description = encounter.Description -> Encounter encounter
                    | _ -> i)
            let environment = { gamestate.Environment with EnvironmentItems = environmentItems}
            gamestate |> Environment.setEnvironment environment

        let checkEncounter gamestate =  
            match Encounter.find gamestate.Environment with
            | Some encounter ->
                gamestate
                |> Output.addOutput (sprintf "\n*** %s ***" encounter.Description)
                |> Scene.setScene (InEncounter encounter)
            | None -> gamestate

        let endEncounter gamestate =
            gamestate
            |> remove
            |> World.updateWorldEnvironment
module Environment
open Domain
open GameState

// constructors
let createEnvironment id name description exits items environmentItems =
    { Id = EnvironmentId id; Name = name; Description = description; Exits = exits; InventoryItems = items; EnvironmentItems = environmentItems }

let createExit id environmentId exitState direction distance description =
    { Id = ExitId id; Target = EnvironmentId environmentId; ExitState = exitState; Direction = direction; 
        Distance = distance; Description = description }

let createInventoryItem name description uses =
    InventoryItem { Name = name; Description = description; Uses = uses }

let createEnvironmentItem name uses =
    EnvironmentItem { Name = name; Uses = uses }

let createEncounter description monsters =
    Encounter { Description = description; Monsters = monsters; }

let removeItemFromEnvironment item gamestate =
    let environment = {gamestate.Environment with InventoryItems = gamestate.Environment.InventoryItems |> List.filter (fun i -> i <> item) }
    { gamestate with Environment = environment}

let addItemToEnvironment item gamestate =
    let environment = {gamestate.Environment with InventoryItems = item :: gamestate.Environment.InventoryItems }
    { gamestate with Environment = environment}

[<AutoOpen>]
module Inventory =
    let addItemToInventory item gamestate =
        { gamestate with Inventory = item :: gamestate.Inventory }

    let inventoryItemName item =
        match item with
        | InventoryItem props -> props.Name

    let inventoryItemProps item =
        match item with
        | InventoryItem props -> props

    let environmentItemDescription item =
        match item with
        | EnvironmentItem props -> props.Name
        | _ -> ""

module Exit =
    let updateEnvironment exit gamestate =
        let exits = gamestate.Environment.Exits |> List.map (fun e -> if e.Id = exit.Id then exit else e)
        let environment = {gamestate.Environment with Exits = exits }
        { gamestate with Environment = environment} 

    let openExit exit gamestate =
        gamestate
        |> updateEnvironment exit
        |> updateWorldEnvironment

    let tryOpen id env =
        env.Exits 
        |> List.tryFind (fun e -> e.Id = id && e.ExitState <> Open)
        |> Option.map (fun e -> { e with ExitState = Open })

    let find id env =
        env.Exits |> List.find (fun e -> e.Id = id)

module Uses =
    let find uses environment =
        uses
        |> List.map(fun u -> 
            match u with
            | Unlock (exitId, _)
            | Unhide (exitId, _) ->
                environment.Exits 
                |> List.tryFind (fun e -> e.Id = exitId && e.ExitState <> Open)
                |> Option.map (fun _ -> u)
        )
        |> List.choose id
        |> List.tryHead

module Monster =
    let create id name level health experience =
        { Id = MonsterId id; Name = name; Level = level; Health = health; ExperiencePoints = experience}
    
module FsLamp.Game.Startup

open System
open FsLamp.Core
open FsLamp.Core.Domain
open FsLamp.Core.ItemUse
open FsLamp.Core.GameState
open FsLamp.Game.Actions
open FsLamp.Game.Characters

let defaultGamestate map =
    { Player = player1;
        Inventory = [];
        World = { Time = DateTime.Parse("1971-04-09 06:01:42"); Map = map };
        Environment = map.[0];
        GameScene = MainMenu;
        LastCommand = NoCommand;
        Output = Output [(Utility.readTextAsset "0_Title.md"); "Type GO to start the game, or LOAD to start from saved game."]}

let isUpdateItemUse (_, itemUse) =
    itemUse == Items.LoseLifeOnUpdate

let isOutputItemUse (_, itemUse) =
    itemUse == Items.LogOutputs || itemUse == Items.ReadOnUpdate

let getUses f g (list: Items.InventoryItem list) =
    list 
    |> List.collect (fun i -> i.Behaviors |> List.filter f |> List.map (fun b -> (i, b)))
    |> List.map (fun (i, b) -> (i, b, g b))

let updateGameObjects : GamePart =
    fun gamestate ->
        gamestate.Inventory
        // get all the updatable item uses
        |> getUses isUpdateItemUse findItemUseBehavior
            
        |> List.filter (fun (_, _, update) -> update.IsSome)
        |> List.map (fun (i, (_, itemUse), update) -> ((itemUse,i), update.Value))
        // thread gamestate through all the update functions
        |> List.fold (fun gs (item, update) ->
            // get updated item
            match update item with
            | Ok item' ->
                // update item in gamestate
                let inventory' = Environment.updateInventory item' gs.Inventory
                gs |> Inventory.setInventory inventory'
            | Error _ ->
                gs
        ) gamestate

let getGameObjectOutputs : GamePart =
    fun gamestate ->
        gamestate.Inventory
        |> getUses isOutputItemUse ItemUse.findGameStateBehavior
        |> List.filter (fun (_, _, update) -> update.IsSome)
        |> List.map (fun (i, (_, itemUse), update) -> ((itemUse,i), update.Value))
        |> List.fold (fun gs ((itemUse, item), update) ->
            match update (itemUse, item, gs) with
            | Ok gs' -> gs'
            | Error _ -> gs
        ) gamestate

// loop: Read -> Parse -> Command -> Action -> Print -> Loop
let RunGame actionResolver initialState (renderer: FsLamp.Core.IRenderer) =
    let rec loop history gamestate =

        // get action from dispatcher based on input
        let action = actionResolver gamestate.GameScene

        // execute the action to get next gamestate
        let nextGameState = gamestate |> updateGameObjects |> action |> getGameObjectOutputs

        // update screen
        renderer.RenderGameState(nextGameState)

        // handle output
        match nextGameState.Output with
        | Output _ | DoNothing ->
            loop (nextGameState :: history) nextGameState
        | Rollback ->
            match history with
            | _ :: old :: tail  ->
                renderer.RenderGameState(old |> Output.addOutput "Rolled back...")
                loop tail old
            | _ ->
                loop history gamestate
        | GameOver ->
            let outputs = match initialState.Output with Output x -> x | _ -> []
            let gamestate' = {initialState with Output = Output ("Game Over! Starting back at the beginning." :: outputs) }
            renderer.RenderGameState(gamestate')
            loop [] gamestate'
        | ExitGame ->
            renderer.Close()

    renderer.Init()

    // initial screen update
    renderer.RenderGameState(initialState)

    loop [] {initialState with Output = DoNothing }
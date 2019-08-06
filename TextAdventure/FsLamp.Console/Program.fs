﻿open Domain
open Actions
open Game
open GameMap
open GameState
open LUISApi.Model
open Parser
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Configuration.UserSecrets

let configuration = 
    ((ConfigurationBuilder())
            .AddJsonFile("appsettings.json")
            .AddUserSecrets("4dfd08bf-6085-4442-afbd-f478432a5da9")
            .Build()) :> IConfiguration

let getLuisSettings (config: IConfiguration) =
    { Region = config.["LUIS:region"]; AppId = config.["LUIS:appId"]; AppKey = config.["LUIS:appKey"] }

[<EntryPoint>]
let main _ =
    let luisSettings = getLuisSettings configuration
    
    let actionResolver gameScene = 
        let parser = 
            match gameScene with
            | MainMenu -> mainMenuParser
            | OpenExplore -> (Parser.luisParser luisSettings) //exploreParser
            | InEncounter _ -> encounterParser
            | CustomScene parser -> parser
        Console.getCommand parser |> Dispatcher.dispatch

    RunGame actionResolver (defaultGamestate (defaultMap()))
    0

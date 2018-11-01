module DigitalFeedbackClient

open System
open FSharp.Data
open Types
open Newtonsoft.Json
open FSharp.Data

[<Literal>]
let private DfUrl = """https://author.testlab.firmglobal.net/digitalfeedback/api/programs/4"""

type ListEntitiesPayload = JsonProvider<""" [{"id":1,"programId":4,"name":"sc1"}] """>
type HtmlCssEntityPayload = JsonProvider<""" {"id":2,"programId":4,"name":"o1","html":"somehtml","css":"somecss"} """>
type ScenarioEntityPayload = JsonProvider<""" {"id":3,"programId":4,"name":"sc1","script":"javascript","css":"somecss", "isEnabled":true} """>

type ScenarioPayload = 
    {
        id:int
        programId:int
        name:string
        script:string
        isEnabled:bool
    }

type HtmlCssPayload = 
    {
        id:int
        programId:int
        name:string
        html:string
        css:string
    }

type CreateScenarioPayload = 
    {
        name:string
        script:string
        isEnabled:bool
    }

type CreateHtmlCssPayload = 
    {
        name:string
        html:string
        css:string
    }

let private accessToken = Environment.GetEnvironmentVariable("accesstoken", EnvironmentVariableTarget.Process) //"68c4b4fcad1b3ab8b55ad322663f2771" 
//let private accessToken = "cb7f47a40de53e372895870f52201032"

let private get endpoint= 
    Http.RequestString
        ( DfUrl + endpoint, httpMethod = "GET",
        headers = [ "Authorization", "Bearer " + accessToken; "Content-Type", "application/json" ])

let private delete endpoint = 
    Http.RequestString
        ( DfUrl + endpoint, httpMethod = "DELETE",
        headers = [ "Authorization", "Bearer " + accessToken; "Content-Type", "application/json"  ])
 
let private post endpoint body= 
    Http.RequestString
      ( DfUrl + endpoint, httpMethod = "POST",
        headers = [ "Authorization", "Bearer " + accessToken; "Content-Type", "application/json"  ],
        body = TextRequest body)
        
let private put endpoint body= 
    Http.RequestString
      ( DfUrl + endpoint, httpMethod = "PUT",
        headers = [ "Authorization", "Bearer " + accessToken; "Content-Type", "application/json"  ],
        body = TextRequest body)

let private getList entityType = 
    get ("/" + entityType)
    |> ListEntitiesPayload.Parse

let private getEntityByName entityType name= 
    let entities = getList entityType
    let entity = entities 
                    |> Array.find (fun x -> x.Name = name)
    get (entityType + "/" + entity.Id.ToString())

let private deleteHtmlCss entityType entityName =
    let scenarioToDelete = 
        getEntityByName entityType entityName
        |> HtmlCssEntityPayload.Parse
    delete ("/" + entityType + "/" + scenarioToDelete.Id.ToString())

let updateHtmlCss entityType (entity:HtmlCssEntity) = 
    let serverEntity = getEntityByName entityType entity.Name |> HtmlCssEntityPayload.Parse
    let html = match entity.Html with
                 | Some s -> s
                 | None -> serverEntity.Html
    let css = match entity.Css with
                 | Some s -> s
                 | None -> serverEntity.Css
    put ("/" + entityType + "/" + serverEntity.Id.ToString()) (JsonConvert.SerializeObject {id=serverEntity.Id;programId=serverEntity.ProgramId; name = entity.Name; html = html; css = css })

let private createHtmlCss entityType (entity:HtmlCssEntity) =
    post ("/" + entityType) (JsonConvert.SerializeObject {name = entity.Name;html = entity.Html.Value;css = entity.Css.Value })
    
let createScenario (scenario:Scenario) =
    post ("/scenarios") (JsonConvert.SerializeObject {name = scenario.Name; script = scenario.Script.Value;isEnabled = true })

let createInvite (invite:HtmlCssEntity) =
    createHtmlCss "invites" invite

let createOverlay (overlay:HtmlCssEntity) =
    createHtmlCss "overlays" overlay

let deleteScenario scenario =
    let scenarioToDelete = 
        getEntityByName "scenarios" scenario.Name
        |> ScenarioEntityPayload.Parse
    delete ("/scenarios" + "/" + scenarioToDelete.Id.ToString())

let deleteInvite (invite:HtmlCssEntity) =
    deleteHtmlCss "invites" invite.Name

let deleteOverlay (overlay:HtmlCssEntity) =
    deleteHtmlCss "overlays" overlay.Name

let updateScenario (scenario:Scenario) = 
    let serverScenario = getEntityByName "scenarios" scenario.Name |> ScenarioEntityPayload.Parse
    let script = match scenario.Script with
                 | Some s -> s
                 | None -> serverScenario.Script
    put ("/scenarios" + "/" + serverScenario.Id.ToString()) (JsonConvert.SerializeObject {id=serverScenario.Id;programId=serverScenario.ProgramId; name = scenario.Name; script = script; isEnabled = serverScenario.IsEnabled })

let updateInvite (invite:HtmlCssEntity) =
    updateHtmlCss "invites" invite

let updateOverlay (overlay:HtmlCssEntity) =
    updateHtmlCss "overlays" overlay

let updateDFEntity dfEntity = 
    match dfEntity with 
    | Scenario (scenario, Operation.Add) -> createScenario scenario
    | Scenario (scenario, Operation.Modify) -> updateScenario scenario
    | Scenario (scenario, Operation.Remove) -> deleteScenario scenario
    | Overlay (overlay, Operation.Add) -> createOverlay overlay
    | Overlay (overlay, Operation.Modify) -> updateOverlay overlay
    | Overlay (overlay, Operation.Remove) -> deleteOverlay overlay
    | Invite (invite, Operation.Add) -> createInvite invite
    | Invite (invite, Operation.Modify) -> updateInvite invite
    | Invite (invite, Operation.Remove) -> deleteInvite invite

let updateDFProgram dfEntities = 
    for entity in dfEntities do 
        updateDFEntity entity |> ignore
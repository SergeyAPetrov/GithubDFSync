// Learn more about F# at http://fsharp.org
module FSharpSync 
    
    open Types
    open FSharp.Data
    open DigitalFeedbackClient
    open Microsoft.Extensions.Logging

    [<Literal>]
    let pushPayloadSample = """
    {
      "head_commit": {
        "added": [
            "Containers/container.css",
            "Containers/container.htm"
        ],
        "removed": [
            "Invites/invite.css",
            "Invites/invite.htm"
        ],
        "modified": [
            "Scenarios/scenario1.js"
        ]
      }
    }
    """

    type GitHubPushPayload = JsonProvider<pushPayloadSample>

    let downloadFileContent path =
        Http.RequestString ("https://raw.githubusercontent.com/Ten1n/DigitalFeedback/master/" + path)

    let getPath gitHubFile = 
        match gitHubFile with 
        | AddedFile (path, _) -> path
        | ModifiedFile (path, _) -> path
        | RemovedFile path -> path

    let getContent gitHubFile =
        match gitHubFile with 
        | AddedFile (_, content) -> content
        | ModifiedFile (_, content) -> content
        | RemovedFile _ ->  string None
    
    let mapFileToOperation gitHubFile =
        match gitHubFile with 
        | AddedFile (_, _) -> Operation.Add
        | ModifiedFile (_, _) -> Operation.Modify
        | RemovedFile _ ->  Operation.Remove

    let inviteMapper (name, files) =
        let html = files |> Array.tryFind (fun x -> getPath x |> System.IO.Path.GetExtension |> (=) ".htm") |> Option.map getContent
        let css = files |> Array.tryFind (fun x -> getPath x |> System.IO.Path.GetExtension |> (=) ".css") |> Option.map getContent
        Invite({Name = name; Html = html; Css = css}, files.[0] |> mapFileToOperation)

    let overlayMapper (name, files) =
        let html = files |> Array.tryFind (fun x -> getPath x |> System.IO.Path.GetExtension |> (=) ".htm") |> Option.map getContent
        let css = files |> Array.tryFind (fun x -> getPath x |> System.IO.Path.GetExtension |> (=) ".css") |> Option.map getContent
        Overlay({Name = name; Html = html; Css = css}, files.[0] |> mapFileToOperation)

    let scenarioMapper (name, files) =
        let js = files |> Array.tryFind (fun x -> getPath x |> System.IO.Path.GetExtension |> (=) ".js") |> Option.map getContent
        Scenario({Name = name; Script = js}, files.[0] |> mapFileToOperation)

    let getDFEntitiesByType gitHubUpdateInfo entityType mapfunc=
        gitHubUpdateInfo 
            |> Array.filter (fun x -> getPath x |> System.IO.Path.GetDirectoryName |> (=) entityType)        
            |> Array.groupBy (fun x-> getPath x |> System.IO.Path.GetFileNameWithoutExtension)
            |> Array.map mapfunc
            
    let Process input (log:ILogger) =
        let pushPayload = GitHubPushPayload.Parse input
        log.LogInformation("Parsed input");
        let addedFiles = pushPayload.HeadCommit.Added |> Array.map (fun file -> AddedFile(file, downloadFileContent file))
        let modifiedFiles = pushPayload.HeadCommit.Modified |> Array.map (fun file -> ModifiedFile(file, downloadFileContent file))
        let removedFiles = pushPayload.HeadCommit.Removed |> Array.map (fun file -> RemovedFile(file))

        let gitHubUpdateInfo =  Array.concat [addedFiles; modifiedFiles; removedFiles]
        
        log.LogInformation("Downloaded github content");

        let mappedInvites = 
            getDFEntitiesByType gitHubUpdateInfo "Invites" inviteMapper
    
        let mappedOverlays = 
            getDFEntitiesByType gitHubUpdateInfo "Containers" overlayMapper
    
        let mappedScenarios = 
            getDFEntitiesByType gitHubUpdateInfo "Scenarios" scenarioMapper
    
        log.LogInformation("Mapped entities");

        let mappedEntities =  Array.concat [mappedInvites; mappedOverlays; mappedScenarios]
    
        let result = updateDFProgram mappedEntities

        log.LogInformation("Updated df program");

        result
    
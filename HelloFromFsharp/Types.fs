module Types

type Operation = Add | Modify | Remove

type GitHubFile = 
    | AddedFile of string * string
    | ModifiedFile of string * string 
    | RemovedFile of string
    
type HtmlCssEntity = 
    {
        Name:string
        Html: string option
        Css: string option
    }

type Scenario =
    {
        Name:string
        Script:string option
    }

type DFEntity = 
    | Scenario of Scenario*Operation
    | Overlay of HtmlCssEntity*Operation
    | Invite of HtmlCssEntity*Operation



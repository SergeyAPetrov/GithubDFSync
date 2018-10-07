using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Linq;
using System;
using System.Net.Http;
using System.Collections.Generic;
using ConsoleApp1;
using Microsoft.Extensions.Logging;

namespace GithubDFSync
{
    public static class GitHubHook
    {
        [FunctionName("GitHubHook")]
        public static IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = new StreamReader(req.Body).ReadToEnd();
            var pushPayload = JsonConvert.DeserializeObject<PushPayload>(requestBody);

            log.LogInformation($"Push message recieved: {pushPayload.after}");
            try
            {
                GitHubUpdateInfo updateInfo = GitHubClient.GetGitHubUpdateInfo(pushPayload);

                var dfChanges = new GithubToDFChangesConverter().ConvertChanges(updateInfo);
                new DigitalFeedbackClient().ApplyChanges(dfChanges);
            }
            catch(Exception e)
            {
                log.LogError(e, "unable to sync");
            }
            return new OkObjectResult($"OK");
        }
    }
    public class GithubToDFChangesConverter
    {
        DigitalFeedbackProgramChanges result = new DigitalFeedbackProgramChanges();

        public DigitalFeedbackProgramChanges ConvertChanges(GitHubUpdateInfo gitHubUpdate)
        {
            var scenarios = gitHubUpdate.Files.Where(f => f.Type == "scenarios");

            foreach (var githubScenario in scenarios)
            {
                var scenario = new Scenario()
                {
                    name = githubScenario.Name,
                    script = githubScenario.FileContent
                };
                result.ScenarioChanges.Add(Tuple.Create(githubScenario.ChangeType, scenario));
            }

            var invites = gitHubUpdate.Files.Where(f => f.Type == "invites").GroupBy(f => f.Name);
            foreach(var githubInvite in invites)
            {
                var invite = new HtmlCssEntity()
                {
                    name = githubInvite.Key,
                    html = githubInvite.FirstOrDefault(f => f.ContentType == "htm").FileContent,
                    css = githubInvite.FirstOrDefault(f => f.ContentType == "css").FileContent,
                };
                result.InviteChanges.Add(Tuple.Create(githubInvite.First().ChangeType, invite));
            }

            var containers = gitHubUpdate.Files.Where(f => f.Type == "containers").GroupBy(f => f.Name);
            foreach (var githubContainer in containers)
            {
                var container = new HtmlCssEntity()
                {
                    name = githubContainer.Key,
                    html = githubContainer.FirstOrDefault(f => f.ContentType == "htm").FileContent,
                    css = githubContainer.FirstOrDefault(f => f.ContentType == "css").FileContent,
                };
                result.OverlayChanges.Add(Tuple.Create(githubContainer.First().ChangeType, container));
            }

            return result;
        }
    }
}

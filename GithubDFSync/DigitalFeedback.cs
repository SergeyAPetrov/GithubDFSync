using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;

namespace ConsoleApp1
{
    enum EntityType
    {
        Invite,
        Container,
        Scenario
    }

    public enum ChangeType
    {
        Add,
        Remove,
        Modify
    }

    public class DigitalFeedbackProgramChanges
    {
        public List<Tuple<ChangeType, Scenario>> ScenarioChanges = new List<Tuple<ChangeType, Scenario>>();
        public List<Tuple<ChangeType, HtmlCssEntity>> InviteChanges = new List<Tuple<ChangeType, HtmlCssEntity>>();
        public List<Tuple<ChangeType, HtmlCssEntity>> OverlayChanges = new List<Tuple<ChangeType, HtmlCssEntity>>();
    }

    public class DigitalFeedbackClient
    {
        private const string Invites = "invites";
        private const string Scenarios = "scenarios";
        private const string Overlays = "overlays";

        public void ApplyChanges(DigitalFeedbackProgramChanges changes)
        {
            foreach (var scenarioChange in changes.ScenarioChanges)
            {
                switch (scenarioChange.Item1)
                {
                    case ChangeType.Add:
                        CreateScenario(scenarioChange.Item2);
                        break;
                    case ChangeType.Modify:
                        UpdateScenario(scenarioChange.Item2);
                        break;
                    case ChangeType.Remove:
                        DeleteEntity<Scenario>(Scenarios, scenarioChange.Item2.name);
                        break;
                }
            }

            foreach (var inviteChange in changes.InviteChanges)
            {
                switch (inviteChange.Item1)
                {
                    case ChangeType.Add:
                        CreateHtmlCssEntity(Invites, inviteChange.Item2.name, inviteChange.Item2.html, inviteChange.Item2.css);
                        break;
                    case ChangeType.Modify:
                        UpdateHtmlCssEntity(Invites, inviteChange.Item2);
                        break;
                    case ChangeType.Remove:
                        DeleteEntity<HtmlCssEntity>(Invites, inviteChange.Item2.name);
                        break;
                }
            }

            foreach (var overlayChange in changes.OverlayChanges)
            {
                switch (overlayChange.Item1)
                {
                    case ChangeType.Add:
                        CreateHtmlCssEntity(Overlays, overlayChange.Item2.name, overlayChange.Item2.html, overlayChange.Item2.css);
                        break;
                    case ChangeType.Modify:
                        UpdateHtmlCssEntity(Overlays, overlayChange.Item2);
                        break;
                    case ChangeType.Remove:
                        DeleteEntity<HtmlCssEntity>(Overlays, overlayChange.Item2.name);
                        break;
                }
            }

        }

        const string DfUrl = "https://author.testlab.firmglobal.net/digitalfeedback/";
        private const int ProgramId = 4;

        private static IFlurlRequest Request => DfUrl
                                                .AppendPathSegment($"api/programs/{ProgramId}")
                                                .WithOAuthBearerToken(
                                                    Environment.GetEnvironmentVariable("accesstoken", EnvironmentVariableTarget.Process));

        //static void Main(string[] args)
        //{
        //    //var person = Request
        //    // .GetJsonAsync<DfProgram>().Result;
        //    //var scenario = GetEntityByName<Scenario>("scenarios", "sc4");
        //    //Console.WriteLine(scenario.script);
        //    //scenario.script = "alert(2)";
        //    //UpdateScenario(scenario);
        //    //CreateScenario("sc4", "alert(1);");

        //    //CreateHtmlCssEntity("overlays", "o3", "html", "css");

        //    //var entity = GetEntityByName<HtmlCssEntity>("overlays", "o3");
        //    //entity.html = "newHtml";
        //    //UpdateHtmlCssEntity("overlays", entity);

        //    DeleteEntity("overlays", 6550);

        //    Console.WriteLine("Done");
        //    Console.ReadKey();
        //}

        public static T GetEntityByName<T>(string entityType, string name) where T : IEntity
        {
            var entities = Request
                .AppendPathSegment(entityType)
                .GetJsonAsync<DfEntity[]>()
                .Result;
            var entityId = entities.First(s => s.name == name);
            return Request.AppendPathSegment($"{entityType}/{entityId.id}")
                .GetJsonAsync<T>()
                .Result;
        }

        public static Scenario CreateScenario(Scenario scenario)
        {
            var createdScenario = Request
                .AppendPathSegment(Scenarios)
                .PostJsonAsync(new { scenario.name, scenario.script })
                .ReceiveJson<Scenario>().Result;
            return createdScenario;
        }

        public static Scenario UpdateScenario(Scenario scenario)
        {
            var dfScenario = GetEntityByName<Scenario>(Scenarios, scenario.name);
            return Request
                .AppendPathSegment($"{Scenarios}/{dfScenario.id}")
                .PutJsonAsync(new {
                    dfScenario.id,
                    programId  = ProgramId,
                    scenario.name,
                    script = scenario.script ?? dfScenario.script,
                    dfScenario.isEnabled
                })
                .ReceiveJson<Scenario>()
                .Result;
        }

        public static HtmlCssEntity CreateHtmlCssEntity(string entityType, string name, string html, string css)
        {
            var entity = Request
                    .AppendPathSegment(entityType)
                    .PostJsonAsync(new { name, html, css })
                    .ReceiveJson<HtmlCssEntity>().Result;
                    return entity;
        }

        public static HtmlCssEntity UpdateHtmlCssEntity(string entityType, HtmlCssEntity entity)
        {
            var dfEntity = GetEntityByName<HtmlCssEntity>(entityType, entity.name);
            return Request
                    .AppendPathSegment(entityType)
                    .AppendPathSegment(dfEntity.id)
                    .PutJsonAsync(new {
                        dfEntity.id,
                        programId  = ProgramId,
                        entity.name,
                        html = entity.html ?? dfEntity.html,
                        css = entity.css ?? dfEntity.css,
                    })
                    .ReceiveJson<HtmlCssEntity>().Result;
        }

        public static void DeleteEntity<T>(string entityType, string name) where T : IEntity
        {
            var entity = GetEntityByName<T>(entityType, name);
            Request
                .AppendPathSegment(entityType)
                .AppendPathSegment(entity.id)
                .DeleteAsync()
                .Wait();
        }
    }

    public class DfProgram
    {
        public int id { get; set; }
        public string name { get; set; }
        public int companyId { get; set; }
        public bool isPublished { get; set; }
        public DateTime created { get; set; }
        public int createdBy { get; set; }
        public DateTime lastModified { get; set; }
        public int lastModifiedBy { get; set; }
        public string publishUrl { get; set; }
        public DateTime lastPublished { get; set; }
        public int version { get; set; }
        public string publicKey { get; set; }
    }


    public class DfEntities
    {
        public DfEntity[] Entities { get; set; }
    }

    public class DfEntity
    {
        public int id { get; set; }
        public int programId { get; set; } = 4;
        public string name { get; set; }
        public DateTime lastModified { get; set; }
    }

    public interface IEntity
    {
        int id { get; set; }
    }

    public class Scenario : IEntity
    {
        public int id { get; set; }
        public int programId { get; set; } = 4;
        public string name { get; set; }
        public string script { get; set; }
        public bool isEnabled { get; set; }
        public DateTime lastModified { get; set; }
    }

    public class HtmlCssEntity : IEntity
    {
        public int id { get; set; }
        public int programId { get; set; } = 4;
        public string name { get; set; }
        public string html { get; set; }
        public string css { get; set; }
        public DateTime lastModified { get; set; }
    }
}

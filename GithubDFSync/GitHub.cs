using ConsoleApp1;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Flurl;
using Flurl.Http;

namespace GithubDFSync
{
    public class GitHubClient
    {
        public static GitHubUpdateInfo GetGitHubUpdateInfo(PushPayload pushPayload)
        {
            var result = new List<GitHubFileInfo>();
            var addedFiles = pushPayload.head_commit.added.Select(p => new GitHubFileInfo { Path = p.ToLowerInvariant(), FileContent = GetContentByUrl(p), ChangeType = ChangeType.Add }).ToList();
            var modifiedFiles = pushPayload.head_commit.modified.Select(p => new GitHubFileInfo { Path = p.ToLowerInvariant(), FileContent = GetContentByUrl(p), ChangeType = ChangeType.Modify}).ToList();
            var removedFiles = pushPayload.head_commit.removed.Select(p => new GitHubFileInfo { Path = p.ToLowerInvariant(), ChangeType = ChangeType.Remove }).ToList();
            result.AddRange(addedFiles);
            result.AddRange(modifiedFiles);
            result.AddRange(removedFiles);
            return new GitHubUpdateInfo
            {
                Files = result
            };
        }

        public static string GetContentByUrl(string url)
        {
            return "https://raw.githubusercontent.com/Ten1n/DigitalFeedback/master/"
                    .AppendPathSegment(url)
                    .GetStringAsync()
                    .Result;
        }
    }

    public class GitHubUpdateInfo
    {
        public IEnumerable<GitHubFileInfo> Files { get; set; }
    }

    public class GitHubFileInfo
    {
        public string Path { get; set; }
        public string Type
        {
            get
            {
                return System.IO.Path.GetDirectoryName(Path);
            }
        }
        public string Name
        {
            get
            {
                return System.IO.Path.GetFileNameWithoutExtension(Path);
            }
        }
        public string ContentType
        {
            get
            {
                return System.IO.Path.GetExtension(Path).TrimStart('.');
            }
        }
        public string FileContent { get; set; }
        public ChangeType ChangeType { get; set; }
    }
}
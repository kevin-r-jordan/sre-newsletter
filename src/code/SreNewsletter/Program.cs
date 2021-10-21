using SreNewsletter.Entities;
using System;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SreNewsletter
{
    class Program
    {
        static void Main(string[] args)
        {
            string issueTemplatePath = @"..\..\..\..\..\templates\issue.html";
            string issueTemplate = File.ReadAllText(issueTemplatePath);

            string issueLinkTemplatePath = @"..\..\..\..\..\templates\issue_link.html";
            string issueLinkTemplate = File.ReadAllText(issueLinkTemplatePath);

            string mailLinkTemplatePath = @"..\..\..\..\..\templates\mail_link.html";
            string mailLinkTemplate = File.ReadAllText(mailLinkTemplatePath);

            var deserializer = new DeserializerBuilder()
               .WithNamingConvention(UnderscoredNamingConvention.Instance)
               .Build();

            var issueFiles = Directory.GetFiles(@"..\..\..\..\..\issues");
            foreach (var issueFile in issueFiles)
            {
                Console.WriteLine("Processing: " + issueFile);
                string text = File.ReadAllText(issueFile);

                using var reader = new StringReader(text);
                var issue = deserializer.Deserialize<Issue>(reader);

                if (issue.Status == IssueStatus.Published)
                {
                    string issueContents = BuildIssueContents(issueTemplate, issueLinkTemplate, issue);
                    SaveIssue(issue, issueContents);

                    string mailContents = BuildMailContents(mailLinkTemplate, issue);
                    SaveMail(issue, mailContents);
                }
            }

            Console.WriteLine("\n--------\n| DONE |\n--------\n");
        }

        private static void SaveIssue(Issue issue, string issueContents)
        {
            string issuesPath = $@"..\..\..\..\..\..\dist\issues";
            if (!Directory.Exists(issuesPath))
                Directory.CreateDirectory(issuesPath);

            string issuePath = $@"{issuesPath}\{issue.IssueNumber.ToString("D3")}";
            if (!Directory.Exists(issuePath))
                Directory.CreateDirectory(issuePath);

            File.WriteAllText($"{issuePath}\\index.html", issueContents);

            // The latest issue gets a second copy put into the latest directory
            if (issue.IsLatest)
            {
                string latestIssuePath = $@"{issuesPath}\latest";
                if (!Directory.Exists(latestIssuePath))
                    Directory.CreateDirectory(latestIssuePath);

                File.WriteAllText($"{latestIssuePath}\\index.html", issueContents);
            }
        }

        private static string BuildIssueContents(string issueTemplate, string issueLinkTemplate, Issue issue)
        {
            var issueContents = issueTemplate.Replace("{{issue_number}}", issue.IssueNumber.ToString());
            issueContents = issueContents.Replace("{{release_date}}", issue.ReleaseDate.ToString("MMMM d, yyyy"));
            if (issue.IssueNumber == 1)
            {
                // TODO: improve process to remove this link for first issue
                issueContents = issueContents.Replace(@"<div class=""px-5 py-2"">
                  <a href=""/issues/{{prev_issue_link}}"" class=""text-base text-gray-500 hover:text-gray-900"">
                  ← Prev Issue
                  </a>
               </div>", "");
            }
            else
            {
                issueContents = issueContents.Replace("{{prev_issue_link}}", (issue.IssueNumber - 1).ToString("D3"));
            }

            if (issue.IsLatest)
            {
                // TODO: improve process to remove this link for latest issue
                issueContents = issueContents.Replace(@"<div class=""px-5 py-2"">
                  <a href=""/issues/{{next_issue_link}}"" class=""text-base text-gray-500 hover:text-gray-900"">
                  Next Issue →
                  </a>
               </div>", "");
            }
            else
            {
                issueContents = issueContents.Replace("{{next_issue_link}}", (issue.IssueNumber + 1).ToString("D3"));
            }

            var introductionText = "";
            foreach (var line in issue.Introduction?.Split("\n") ?? new string[0])
            {
                if (String.IsNullOrEmpty(line))
                    continue;

                introductionText += $"<p{(String.IsNullOrEmpty(introductionText) ? "" : " class=\"mt-3\"")}>{line}</p>";
            }
            issueContents = issueContents.Replace("{{introduction}}", introductionText);

            var issueListContents = "";
            foreach (var issueLink in issue.Links)
            {
                issueListContents += issueLinkTemplate.Replace("{{link_url}}", issueLink.Url)
                    .Replace("{{link_title}}", issueLink.Title)
                    .Replace("{{link_domain}}", issueLink.UrlDomain)
                    .Replace("{{link_summary}}", issueLink.Summary) + "\n";
            }
            issueContents = issueContents.Replace("{{links}}", issueListContents);

            return issueContents;
        }

        private static void SaveMail(Issue issue, string mailContents)
        {
            string mailPath = $@"..\..\..\..\..\..\mail";
            if (!Directory.Exists(mailPath))
                Directory.CreateDirectory(mailPath);

            File.WriteAllText($"{mailPath}\\{issue.IssueNumber.ToString("D3")}.html", mailContents);
        }

        private static string BuildMailContents(string mailLinkTemplate, Issue issue)
        {
            var mailContents = "";
            foreach (var line in issue.Introduction?.Split("\n") ?? new string[0])
            {
                if (String.IsNullOrEmpty(line))
                    continue;

                mailContents += $"{line}<br /><br />\n";
            }

            foreach (var issueLink in issue.Links)
            {
                mailContents += mailLinkTemplate.Replace("{{link_url}}", issueLink.Url)
                    .Replace("{{link_title}}", issueLink.Title)
                    .Replace("{{link_domain}}", issueLink.UrlDomain)
                    .Replace("{{link_summary}}", issueLink.Summary) + "\n";
            }

            return mailContents;
        }
    }
}

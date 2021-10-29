using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Rss;
using SreNewsletter.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SreNewsletter
{
    class Program
    {
        static void Main(string[] args)
        {
            var deserializer = new DeserializerBuilder()
               .WithNamingConvention(UnderscoredNamingConvention.Instance)
               .Build();

            var issueFiles = Directory.GetFiles(@"..\..\..\..\..\issues");
            var publishedIssues = new List<Issue>();
            foreach (var issueFile in issueFiles)
            {
                Console.WriteLine("Processing: " + issueFile);
                string text = File.ReadAllText(issueFile);

                using var reader = new StringReader(text);
                var issue = deserializer.Deserialize<Issue>(reader);

                if (issue.Status == IssueStatus.Published)
                    publishedIssues.Add(issue);
            }

            ProcessIssues(publishedIssues);

            Console.WriteLine("\n--------\n| DONE |\n--------\n");
        }

        private static void ProcessIssues(List<Issue> issues)
        {
            string issueTemplatePath = @"..\..\..\..\..\templates\issue.html";
            string issueTemplate = File.ReadAllText(issueTemplatePath);

            string issueLinkTemplatePath = @"..\..\..\..\..\templates\issue_link.html";
            string issueLinkTemplate = File.ReadAllText(issueLinkTemplatePath);

            string mailLinkTemplatePath = @"..\..\..\..\..\templates\mail_link.html";
            string mailLinkTemplate = File.ReadAllText(mailLinkTemplatePath);

            foreach (var issue in issues)
            {
                string issueContents = BuildIssueContents(issueTemplate, issueLinkTemplate, issue);
                SaveIssue(issue, issueContents);

                string mailContents = BuildMailContents(mailLinkTemplate, issue);
                SaveMail(issue, mailContents);
            }

            GenerateRssFeed(issues);
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

        private static void GenerateRssFeed(List<Issue> issues)
        {
            var sw = new StringWriterWithEncoding(Encoding.UTF8);

            using (XmlWriter xmlWriter = XmlWriter.Create(sw, new XmlWriterSettings() { Async = true, Indent = true }))
            {
                var writer = new RssFeedWriter(xmlWriter);
                writer.WriteTitle("SRE Newsletter");
                writer.WriteDescription("Get a hand curated list of the best DevOps and Site Reliability Engineering articles delivered to your inbox each week.");
                writer.Write(new SyndicationLink(new Uri("https://www.srenewsletter.com")));

                foreach (var issue in issues)
                {
                    foreach (var link in issue.Links)
                    {
                        var item = new SyndicationItem()
                        {
                            Id = link.Url,
                            Title = link.Title,
                            Description = link.Summary,
                            Published = issue.ReleaseDate,
                        };
                        item.AddLink(new SyndicationLink(new Uri(link.Url)));

                        writer.Write(item).Wait();
                        xmlWriter.Flush();
                    }
                }
            }

            string rssPath = $@"..\..\..\..\..\..\dist\feed";
            if (!Directory.Exists(rssPath))
                Directory.CreateDirectory(rssPath);

            File.WriteAllText($"{rssPath}\\rss.xml", sw.ToString());
        }
    }

    class StringWriterWithEncoding : StringWriter
    {
        private readonly Encoding _encoding;

        public StringWriterWithEncoding(Encoding encoding)
        {
            this._encoding = encoding;
        }

        public override Encoding Encoding
        {
            get { return _encoding; }
        }
    }
}

namespace SreNewsletter.Entities
{
    public class IssueLink
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Summary { get; set; }

        public string UrlDomain => Url.Replace("http://", "").Replace("https://", "").Replace("www.", "").Split("/")[0];
    }
}

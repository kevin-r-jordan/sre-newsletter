using System;
using System.Collections.Generic;

namespace SreNewsletter.Entities
{
    public class Issue
    {
        public int IssueNumber { get; set; }
        public DateTime ReleaseDate { get; set; }
        public IssueStatus Status { get; set; }
        public bool IsLatest { get; set; }
        public string Introduction { get; set; }
        public List<IssueLink> Links { get; set; } = new List<IssueLink>();
    }
}

using System;
using System.Collections.Generic;

/// <summary>
/// Data that we need from a Reddit json.
/// </summary>
namespace FinBot.Handlers
{
    public partial class RedditHandler
    {
        public string Kind { get; set; }
        public RedditHandlerData Data { get; set; }
    }

    public partial class RedditHandlerData
    {
        public Child[] Children { get; set; }
    }

    public partial class Child
    {
        public ChildData Data { get; set; }
    }

    public partial class ChildData
    {
        public object author_flair_background_color { get; set; }
        public long? approved_at_utc { get; set; }
        public string selftext { get; set; }
        public string title { get; set; }
        public long total_awards_received { get; set; }
        public long score { get; set; }
        public long created { get; set; }
        public bool over_18 { get; set; }
        public string author { get; set; }
        public string permalink { get; set; }
        public Uri Url { get; set; }
        public long upvote_ratio { get; set; }
    }
}

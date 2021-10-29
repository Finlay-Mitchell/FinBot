using System;

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
        /// <summary>
        /// The title of the post.
        /// </summary>
        public string title { get; set; }
        /// <summary>
        /// Gets the total number of Reddit awards recieved.
        /// </summary>
        public long total_awards_received { get; set; }
        /// <summary>
        /// Gets the score for the post(upvotes - downvotes).
        /// </summary>
        public long score { get; set; }
        /// <summary>
        /// Gets the unix timestamp of when the post was created.
        /// </summary>
        public long created { get; set; }
        /// <summary>
        /// Gets whether the post is flagged as NSFW.
        /// </summary>
        public bool over_18 { get; set; }
        /// <summary>
        /// Gets the author of the post.
        /// </summary>
        public string author { get; set; }
        /// <summary>
        /// Gets the URL to the post.
        /// </summary>
        public string permalink { get; set; }
        /// <summary>
        /// Gets the image URL.
        /// </summary>
        public Uri Url { get; set; }
    }
}

namespace wow2.Verbose
{
    /// <summary>What the Github GET request content for checking for a new release will be deserialized into.</summary>
    public class GithubRelease
    {
        public string tag_name { get; set; }
        public string html_url { get; set; }
    }
}
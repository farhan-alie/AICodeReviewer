namespace AICodeReviewerCLI;

public class AppSettings
{
    public string BitbucketUsername { get; set; }
    public string BitbucketAppPassword { get; set; }
    public string RepoSlug { get; set; }
    public string Workspace { get; set; }
    public int PullRequestId { get; set; } 
    public string OpenAIApiKey { get; set; }
}
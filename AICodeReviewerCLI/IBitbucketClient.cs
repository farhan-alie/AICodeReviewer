using Refit;

namespace AICodeReviewerCLI;

[Headers("Authorization: Basic")]
public interface IBitbucketClient
{
    [Get("/pullrequests/{pullRequestId}")]
    Task<PullRequest> GetPullRequestAsync([AliasAs("pullRequestId")] int pullRequestId);

    [Post("/pullrequests/{pullRequestId}/comments")]
    Task<IApiResponse> PostInlineCommentAsync(
        [AliasAs("pullRequestId")] int pullRequestId,
        [Body] CommentRequest body);
    
    [Post("/pullrequests/{pullRequestId}/approve")]
    Task<IApiResponse> ApprovePullRequestAsync(
        [AliasAs("pullRequestId")] int pullRequestId);
    
    [Post("/pullrequests/{pullRequestId}/request-changes")]
    Task<IApiResponse> RequestChangesToPullRequestAsync(
        [AliasAs("pullRequestId")] int pullRequestId);
    
    
    [Post("/pullrequests/{pullRequestId}/merge")]
    Task<IApiResponse> MergePullRequestAsync(
        [AliasAs("pullRequestId")] int pullRequestId, MeregePullRequestRequest body);
}

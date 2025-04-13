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
}
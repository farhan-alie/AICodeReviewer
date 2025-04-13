namespace AICodeReviewerCLI;

public record CommentContent(string raw);

public record Inline(string path, int to);

public record CommentRequest(CommentContent content, Inline inline);

#region PullRequest

public record PullRequest(
    Dictionary<string, Link> links
);

public record Link(
    string href
);

#endregion
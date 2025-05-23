﻿// See https://aka.ms/new-console-template for more information

// File: Program.cs

using AICodeReviewerCLI;
using Microsoft.Extensions.Configuration;


var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables()
    .Build();

var settings = configuration.GetSection("AppSettings").Get<AppSettings>()!;

var bitbucketSvc = new BitbucketService(settings);
var codeReviewSvc = new CodeReviewService(settings);

// var prId = GetArg("--pr-id");
// var repo = GetArg("--repo");
// var bitbucketToken = GetArg("--bitbucket-token");
// var openaiKey = GetArg("--openai-api-key");

Console.WriteLine($"Analyzing PR #{settings.PullRequestId} in repo #{settings.RepoSlug}...");

try
{
    var files = await bitbucketSvc.GetModifiedSrcFilesAsync();
    var fileReviews = await codeReviewSvc.ReviewFilesAsync(files);
    if (fileReviews.Count == 0)
    {
        Console.WriteLine("No files to review.");
       await bitbucketSvc.ApprovePullRequest();
       await bitbucketSvc.MergePullRequest(new MeregePullRequestRequest("Merged by AI Reviewer"));
       return;
    }
    else
    {
        await bitbucketSvc.PostInlineComment(fileReviews);
        await bitbucketSvc.RequestChangesToPullRequestAsync();
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"PR review failed: {ex.Message}");
}

string GetArg(string name)
{
    return args.SkipWhile(a => a != name).Skip(1).FirstOrDefault() ?? string.Empty;
}
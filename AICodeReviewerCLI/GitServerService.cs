using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Refit;

namespace AICodeReviewerCLI;

public class BitbucketService
{
    private readonly IBitbucketClient _bitbucketClient;
    private readonly FileIgnoreService _fileIgnoreSvc;
    private readonly HttpClient _httpClient;
    private readonly AppSettings _appSettings;

    public BitbucketService(AppSettings appSettings)
    {
        _appSettings = appSettings;
        var bitbucketToken =
            Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{appSettings.BitbucketUsername}:{appSettings.BitbucketAppPassword}"));
        var refitSettings = new RefitSettings
        {
            AuthorizationHeaderValueGetter = AuthorizationHeaderValueGetter
        };

        Task<string> AuthorizationHeaderValueGetter(HttpRequestMessage arg1, CancellationToken arg2)
        {
            return Task.FromResult(bitbucketToken);
        }

        _httpClient =
            RestService.CreateHttpClient($"https://api.bitbucket.org/2.0/repositories/{appSettings.Workspace}/{appSettings.RepoSlug}",
                refitSettings);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", bitbucketToken);
        _bitbucketClient = RestService
            .For<IBitbucketClient>(_httpClient);

        _fileIgnoreSvc = new FileIgnoreService();
    }

    public async Task<SrcFile[]> GetModifiedSrcFilesAsync()
    {
        PullRequest pr = await _bitbucketClient.GetPullRequestAsync(_appSettings.PullRequestId);
        var diffFullUrl = pr.links["diffstat"].href;
        var responseString = await _httpClient.GetStringAsync(diffFullUrl);
        using JsonDocument diffDoc = JsonDocument.Parse(responseString);
        var fileTasks = new List<Task<SrcFile>>();
        foreach (JsonElement file in diffDoc.RootElement.GetProperty("values").EnumerateArray())
        {
            JsonElement newNode = file.GetProperty("new");
            var path = newNode.GetProperty("path").GetString()!;
            if (_fileIgnoreSvc.IsIgnored(path)) continue;

            var href = newNode.GetProperty("links").GetProperty("self").GetProperty("href").GetString();
            fileTasks.Add(Task.Run(async () => new SrcFile(path, await _httpClient.GetStringAsync(href)))!);
        }

        var files = await Task.WhenAll(fileTasks);
        return files;
    }

    public async Task PostInlineComment(List<SrcFile> fileReviews)
    {
        foreach (SrcFile fileReview in fileReviews)
        {
            var path = fileReview.Path;
            var review = string.Join("\n", fileReview.Content);

            var commentRequest = new CommentRequest(new CommentContent(review), new Inline(path, 1));
            IApiResponse response =
                await _bitbucketClient.PostInlineCommentAsync(_appSettings.PullRequestId, commentRequest);
            
            Console.WriteLine(response.IsSuccessStatusCode
                ? $"Comment posted for {path}"
                : $"Failed to post comment for {path}: {response.Error}");
        }
    }
}
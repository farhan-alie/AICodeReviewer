using System.ClientModel;
using System.Text.RegularExpressions;
using OpenAI;
using OpenAI.Chat;

namespace AICodeReviewerCLI;

public class CodeReviewService(AppSettings appSettings)
{
    private readonly OpenAIClient _client = new(new ApiKeyCredential(appSettings.OpenAIApiKey));

    public async Task<List<SrcFile>> ReviewFilesAsync(SrcFile[] files)
    {
        var systemPrompt = """
                           You are an expert software architect and code reviewer. 
                           Your task is to review the provided code file in detail. Your review must:

                           For each code file provided, do the following:

                           1. **Summary of Issues**  
                              - Analyze the full file and list all problems: code smells, SOLID violations, poor formatting, bad naming, anti-patterns, security flaws, inefficient logic, or potential bugs. 
                              - Security vulnerabilities (e.g., SQL Injection, hardcoded secrets, insecure logging, unsafe file operations, etc.)
                              - Performance problems 
                              - Bug risks and logical flaws
                              - Naming conventions, formatting, and style issues
                              - If there are any issues, write them as a **numbered list**, one per line.

                           2. **Suggestions**  
                              - Provide a **numbered list** of concrete improvements matching the issue numbers. Each should give a meaningful and specific fix or enhancement.

                           3. **Improved Code**  
                              - After the summary and suggestions, generate a **transformed version of the file** with all improvements applied. Keep the code idiomatic and consistent with modern best practices.
                              - Ensure formatting, naming, readability, performance, and best practices are applied.
                              - If the class appears to be violating the Single Responsibility Principle or doing too many things, suggest how it can be split into multiple classes for better modularity and maintainability.

                           4. **Important**:  
                              - If no issues or suggestions exist for a file, do **not** include that file in the response. Completely skip it.
                              
                           5. Generate unit tests based on the improved code. 
                              - Write unit tests for the improved code, ensuring that all new functionality is covered.
                              - Use a testing framework like xUnit, and ensure the tests are clear and easy to understand.
                              - If the code is not testable, explain why and suggest how it can be refactored to make it testable.

                           Use this output format for each file:

                           ### File: [path/to/filename]

                           **Summary of Issues**

                           1. ...
                           2. ...

                           **Suggestions**

                           1. ...
                           2. ...

                           \`\`\`csharp
                           // improved code
                           \`\`\`
                           """;

        SystemChatMessage? behaviorPrompt = ChatMessage.CreateSystemMessage(systemPrompt);
        UserChatMessage? codePrompt = ChatMessage.CreateUserMessage(string.Join("\n\n", files.Select(f =>
        {
            var ext = Path.GetExtension(f.Path).ToLower();
            var lang = ext switch
            {
                ".cs" => "csharp",
                ".cshtml" => "razor",
                ".js" => "javascript",
                ".ts" => "typescript",
                ".scss" => "scss",
                ".css" => "css",
                ".html" => "html",
                ".json" => "json",
                _ => ""
            };
            return $"File: {f.Path}\n```{lang}\n{f.Content}\n```";
        })));

        var result = await _client.GetChatClient("gpt-4o").CompleteChatAsync(behaviorPrompt, codePrompt);
        var aiReview = result.Value.Content[0].Text;

        // Parse AI response into per-file reviews
        var fileReviewRegex = new Regex(@"## File: (?<path>[^\n]+)\n(?<content>.*?)(?=(?:## File:|\z))",
            RegexOptions.Singleline);
        var fileReviews = fileReviewRegex.Matches(aiReview)
            .Select(m => new SrcFile(m.Groups["path"].Value.Trim(), m.Groups["content"].Value.Trim()))
            .ToList();

        return fileReviews;
    }
}
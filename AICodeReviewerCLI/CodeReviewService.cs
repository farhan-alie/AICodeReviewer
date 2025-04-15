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
                              - Analyze the full file and **only list actual, meaningful problems** such as:
                                - Code smells
                                - SOLID violations
                                - Poor formatting (only if it significantly reduces readability)
                                - Bad naming (only if misleading, unclear, or inconsistent)
                                - Anti-patterns
                                - Security vulnerabilities (e.g., SQL injection, hardcoded secrets, unsafe file ops)
                                - Performance issues
                                - Bug risks and logical flaws
                              - Do NOT list stylistic issues if the code is already clean and readable.
                              - Do NOT suggest changes just for personal preference.
                              - Avoid re-reviewing or suggesting already best-practice-compliant code.
                              - It's okay to skip XML comments unless they exist and are misleading or incorrect.
                              - If no issues are found, **skip the file completely** in the response.

                           2. **Suggestions**  
                              - For each issue (if any), provide a numbered, concrete suggestion.

                           3. **Improved Code**  
                              - If there are valid issues, generate a revised version of the file with improvements.
                              - Ensure the code follows best practices, is idiomatic, and respects SOLID principles.
                              - If a class seems to do too much, suggest breaking it into multiple classes with specific responsibilities.

                           4. **Important**:  
                              - If a file is clean and has no meaningful issues, do not include it in the response at all.
                              - If **no files** require improvement, return:  
                                `The pull request is clean. No changes required.`

                           Use this output format for each file (only if it has issues):

                           ### File: [path/to/filename]

                           **Summary of Issues**

                           1. ...
                           2. ...

                           **Suggestions**

                           1. ...
                           2. ...

                           ```csharp
                           // improved code

                           """;
//         var systemPrompt = """
//                            You are an expert software architect and code reviewer. 
//                            Your task is to review the provided code file in detail. Your review must:
//
//                            For each code file provided, do the following:
//
//                            1. **Summary of Issues**  
//                               - Analyze the full file and list all problems: code smells, SOLID violations, poor formatting, bad naming, anti-patterns, security flaws, inefficient logic, or potential bugs. 
//                               - Security vulnerabilities (e.g., SQL Injection, hardcoded secrets, insecure logging, unsafe file operations, etc.)
//                               - Performance problems 
//                               - Bug risks and logical flaws
//                               - Naming conventions, formatting, and style issues
//                               - If there are any issues, write them as a **numbered list**, one per line.
//                               - Its okay not to have xml comments in the code, but if they are present, check if they are meaningful and correct.
//
//                            2. **Suggestions**  
//                               - Provide a **numbered list** of concrete improvements matching the issue numbers. Each should give a meaningful and specific fix or enhancement.
//
//                            3. **Improved Code**  
//                               - After the summary and suggestions, generate a **transformed version of the file** with all improvements applied. Keep the code idiomatic and consistent with modern best practices.
//                               - Ensure formatting, naming, readability, performance, and best practices are applied.
//                               - If the class appears to be violating the Single Responsibility Principle or doing too many things, suggest how it can be split into multiple classes for better modularity and maintainability.
//
//                            4. **Important**:  
//                               - If no issues or suggestions exist for a file do **not** include that file in response and completrly skip it. If whole pull requests are empty then return pull request is good and no file is needed.
//                               
//                            Use this output format for each file only if applies:
//
//                            ### File: [path/to/filename]
//
//                            **Summary of Issues**
//
//                            1. ...
//                            2. ...
//
//                            **Suggestions**
//
//                            1. ...
//                            2. ...
//
//                            \`\`\`csharp
//                            // improved code
//                            \`\`\`
//                            """;

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
        if (string.IsNullOrWhiteSpace(aiReview) || !aiReview.Contains("**Summary of Issues**"))
        {
            // No issues found, return an empty list
            return [];
        }
          
        // Parse AI response into per-file reviews
        var fileReviewRegex = new Regex(@"## File: (?<path>[^\n]+)\n(?<content>.*?)(?=(?:## File:|\z))",
            RegexOptions.Singleline);
        var fileReviews = fileReviewRegex.Matches(aiReview)
            .Select(m => new SrcFile(m.Groups["path"].Value.Trim(), m.Groups["content"].Value.Trim()))
            .ToList();
        return fileReviews;
    }
}
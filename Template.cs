using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace HtmlMail
{
    public class Template
    {
        public static async Task<string> GenerateHtml<T>(string htmlFilePath, string cssFilePath, T model)
        {
            var cssContent = await ReaderCss(cssFilePath);
            var htmlContent = await ReaderHtmlAndReplaceContent(htmlFilePath, model);
            htmlContent = htmlContent.Replace("<style></style>", cssContent);
            return htmlContent;
        }

        private static async Task<string> ReaderCss(string filePath)
        {
            try
            {
                var template = await File.ReadAllTextAsync(filePath);
                var codeStyle = new StringBuilder();

                codeStyle.Append("<style>");

                foreach (var line in template)
                {
                    codeStyle.Append(line);
                }

                codeStyle.AppendLine("</style>");

                return codeStyle.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}-{ex.StackTrace}");
            }
        }
        private static async Task<string> ReaderHtmlAndReplaceContent<T>(string filePath, T model)
        {
            var template = await File.ReadAllTextAsync(filePath);
            var codeBuilder = new StringBuilder();
            codeBuilder.AppendLine("var sb = new System.Text.StringBuilder();");

            var lines = template.Split('\n');
            bool inCodeBlock = false;

            for (int i = 0; i < lines.Length; i++)
            {
                var rawLine = lines[i];
                var trimmedLine = rawLine.Trim();
                var lineWithNormalizedEnding = rawLine.TrimEnd('\r');

                //Console.WriteLine($"DEBUG (Linha {i + 1}): Raw='{rawLine.Replace("\r", "\\r").Replace("\n", "\\n")}' Trimmed='{trimmedLine.Replace("\r", "\\r").Replace("\n", "\\n")}' InCodeBlock={inCodeBlock}");

                if (!inCodeBlock && trimmedLine.StartsWith("@{"))
                {
                    inCodeBlock = true;
                    if (trimmedLine.Length > 2)
                    {
                        codeBuilder.AppendLine(trimmedLine.Substring(2));
                    }
                    continue;
                }

                if (inCodeBlock && Regex.IsMatch(trimmedLine, @"^\s*\}\s*$"))
                {
                    codeBuilder.AppendLine("}");
                    inCodeBlock = false;
                    continue;
                }

                if (inCodeBlock)
                {
                    codeBuilder.AppendLine(lineWithNormalizedEnding);
                    continue;
                }

                var cSharpAppendContentBuilder = new StringBuilder();
                var lastIndex = 0;

                var inlineExpressionRegex = new Regex(@"@(?<prop>[a-zA-Z_][a-zA-Z0-9_.]*)|@\{(?<expr>.*?)\}", RegexOptions.Singleline);
                var match = inlineExpressionRegex.Match(lineWithNormalizedEnding);

                cSharpAppendContentBuilder.Append("\"");

                while (match.Success)
                {
                    cSharpAppendContentBuilder.Append(EscapeLiteral(lineWithNormalizedEnding.Substring(lastIndex, match.Index - lastIndex)));

                    cSharpAppendContentBuilder.Append("\" + (");

                    if (match.Groups["prop"].Success)
                    {
                        cSharpAppendContentBuilder.Append(match.Groups["prop"].Value);
                    }
                    else if (match.Groups["expr"].Success)
                    {
                        cSharpAppendContentBuilder.Append(match.Groups["expr"].Value.Trim());
                    }

                    cSharpAppendContentBuilder.Append(") + \"");

                    lastIndex = match.Index + match.Length;
                    match = match.NextMatch();
                }

                cSharpAppendContentBuilder.Append(EscapeLiteral(lineWithNormalizedEnding.Substring(lastIndex)));
                cSharpAppendContentBuilder.Append("\"");

                string finalAppendString = cSharpAppendContentBuilder.ToString();

                finalAppendString = Regex.Replace(finalAppendString, @"\s*\+\s*\""\""\s*", "");
                finalAppendString = Regex.Replace(finalAppendString, @"\""\""\s*\+\s*", "");

                if (finalAppendString.StartsWith(" + (") && finalAppendString.EndsWith(") + \""))
                    finalAppendString = finalAppendString.Substring(3, finalAppendString.Length - 6);
                else if (finalAppendString.StartsWith(" + ("))
                    finalAppendString = finalAppendString.Substring(3);
                else if (finalAppendString.EndsWith(") + \""))
                    finalAppendString = finalAppendString.Substring(0, finalAppendString.Length - 4);

                if (!finalAppendString.StartsWith("\"") && !finalAppendString.EndsWith("\"") && !string.IsNullOrWhiteSpace(finalAppendString))
                    finalAppendString = $"({finalAppendString})";


                if (i < lines.Length - 1)
                    codeBuilder.AppendLine($"sb.Append({finalAppendString} + \"\\n\");");
                else
                    codeBuilder.AppendLine($"sb.Append({finalAppendString});");
            }

            codeBuilder.AppendLine("return sb.ToString();");

            var scriptOptions = ScriptOptions.Default
                .AddReferences(typeof(object).Assembly, typeof(T).Assembly)
                .AddReferences(Assembly.GetExecutingAssembly())
                .AddImports("System", "System.Text", typeof(T).Namespace!);

            try
            {
                var script = codeBuilder.ToString();
                var globals = new Globals<T> { Model = model };
                var evaluate = await CSharpScript.EvaluateAsync<string>(script, scriptOptions, globals);
                return evaluate.Replace("}", "");
            }
            catch (CompilationErrorException ex)
            {
                throw new Exception($"[Error compiling template: {string.Join("\n", ex.Diagnostics)}\n\nScript Gerado:\n{codeBuilder}]");
            }
            catch (Exception ex)
            {
                throw new Exception($"[Template runtime error: {ex.Message}\n\nScript Gerado:\n{codeBuilder}]");
            }
        }

        private static string EscapeLiteral(string text)
        {
            return text.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }

    public class Globals<T>
    {
        public T Model { get; set; } = default!;
    }
}
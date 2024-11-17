using Microsoft.AspNetCore.Mvc;
using System.Text;
using Newtonsoft.Json.Linq;

[Route("api/[controller]")]
[ApiController]
public class FileAnalyzerController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FileAnalyzerController> _logger;
    private readonly string? openAiApiKey;

    private static string pdfContent = string.Empty; // Store PDF content in memory for analysis
    private static List<(string Topic, string Response, string? ExpandedResponse)> chatLog = new();

    public FileAnalyzerController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<FileAnalyzerController> logger)
    {
        _httpClientFactory = httpClientFactory;
        openAiApiKey = configuration["OPENAI_API_KEY"];
        _logger = logger;
    }

    // 1. File Upload Endpoint
    [HttpPost("upload")]
    public IActionResult UploadFile([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0 || file.ContentType != "application/pdf")
            return BadRequest("Only PDF files are allowed.");

        using var pdfStream = file.OpenReadStream();
        pdfContent = ExtractTextFromPdf(pdfStream);
        chatLog.Clear(); // Reset chat log for new file
        return Ok(new { message = "File uploaded successfully." });
    }

    // 2. Search Topic in PDF with Custom Prompt
    [HttpPost("search")]
    public async Task<IActionResult> SearchTopic([FromForm] string topic)
    {
        _logger.LogInformation("SearchTopic endpoint reached with topic: " + topic);

        if (string.IsNullOrEmpty(pdfContent))
            return BadRequest("Please upload a PDF file first.");

        var prompt = $"Analyze the following text and find all relevant information about '{topic}'. " +
                    $"Only respond with information that meets all of the following criteria: " +
                    $"1) The information you respond with should only be from the notes provided. " +
                    $"Do not respond with any outside information. " +
                    $"2) The information that you respond with should be directly about the topic given to you. " +
                    $"Do not provide any loosely related information or general information that is not specific to the topic. " +
                    $"3) Do not respond with any additional information. Only respond with information to answer the prompt. " +
                    $"If you respond with more information than the topic necessitates, this will cause confusion. " +
                    $"4) With your responses, try to deviate from the file content as little as possible. " +
                    $"Only use your own words when necessary; otherwise, your responses should be as close to the file content as possible.\n" +
                    $"Here is the file contents:\n\n{pdfContent}";

        var responseText = await CallOpenAiApi(prompt);

        chatLog.Add((Topic: topic, Response: responseText ?? "No relevant information found on this topic.", ExpandedResponse: null));

        return Ok(new { result = responseText ?? "No relevant information found on this topic." });
    }

    // 3. Expand Topic with External Information
    [HttpPost("expand")]
    public async Task<IActionResult> ExpandOnTopic([FromForm] string topic)
    {
        var existingEntry = chatLog.FirstOrDefault(entry => entry.Topic == topic);
        if (existingEntry.Topic == null)
            return BadRequest("The topic has not been searched in the current session.");

        var prompt = $"Provide an in-depth overview about '{topic}', summarizing key concepts:";
        var expandedResponse = await CallOpenAiApi(prompt);

        var updatedEntry = (Topic: existingEntry.Topic, Response: existingEntry.Response, ExpandedResponse: expandedResponse);
        chatLog[chatLog.IndexOf(existingEntry)] = updatedEntry;

        return Ok(new { result = expandedResponse ?? "No additional information available on this topic." });
    }

    // 4. Export Chat Log to Text File
    [HttpGet("export")]
    public IActionResult ExportToTextFile()
    {
        if (!chatLog.Any())
            return BadRequest("No data available for export.");

        // Generate the .txt file content
        var txtBytes = GenerateTxt(chatLog);

        return File(txtBytes, "text/plain", "ChatLogExport.txt");
    }

    // Extracts text from PDF
    private string ExtractTextFromPdf(Stream pdfStream)
    {
        var sb = new StringBuilder();
        using (var document = UglyToad.PdfPig.PdfDocument.Open(pdfStream))
        {
            foreach (var page in document.GetPages())
            {
                sb.Append(page.Text);
            }
        }
        return sb.ToString();
    }

    // Calls OpenAI API
    private async Task<string> CallOpenAiApi(string prompt)
    {
        _logger.LogInformation("Calling OpenAI API with prompt.");

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {openAiApiKey}");

        var requestBody = new
        {
            model = "gpt-3.5-turbo",
            messages = new[] { new { role = "user", content = prompt } },
            max_tokens = 500
        };

        var response = await client.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", requestBody);
        if (!response.IsSuccessStatusCode) return null;

        var jsonResponse = await response.Content.ReadAsStringAsync();
        var result = JObject.Parse(jsonResponse);
        return result["choices"]?[0]?["message"]?["content"]?.ToString();
    }

    // Generates .txt content from the chat log
    private byte[] GenerateTxt(List<(string Topic, string Response, string? ExpandedResponse)> log)
    {
        using (var memoryStream = new MemoryStream())
        {
            using (var writer = new StreamWriter(memoryStream))
            {
                foreach (var entry in log)
                {
                    writer.WriteLine($"Topic: {entry.Topic}\n");
                    writer.WriteLine("Information from notes:\n");
                    writer.WriteLine(entry.Response);
                    writer.WriteLine();
                    writer.WriteLine("AI Expanded Notes:\n");
                    writer.WriteLine(entry.ExpandedResponse ?? "N/A");
                    writer.WriteLine();
                    writer.WriteLine(new string('-', 50));
                }
            }
            return memoryStream.ToArray();
        }
    }
}

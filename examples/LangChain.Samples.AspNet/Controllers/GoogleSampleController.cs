using LangChain.Providers;
using LangChain.Providers.Models;
using Microsoft.AspNetCore.Mvc;

namespace LangChain.Samples.AspNet.Controllers;

[ApiController]
[Route("[controller]")]
public class GoogleSampleController : ControllerBase
{
    private readonly GenerativeModel _model;
    private readonly ILogger<GoogleSampleController> _logger;

    public GoogleSampleController(
        GenerativeModel model,
        ILogger<GoogleSampleController> logger)
    {
        _model = model;
        _logger = logger;
    }

    [HttpGet(Name = "GetGoogleResponse")]
    public async Task<string> Get()
    {
        var response = await _model.GenerateAsync(request: new ChatRequest(Messages: new[]
        {
            "What is a good name for a company that sells colourful socks?".AsChatMessage()
        }));

        return response.LastMessageContent;
    }
}
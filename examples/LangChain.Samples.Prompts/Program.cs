using LangChain.Chains.LLM;
using LangChain.Prompts;
using LangChain.Providers.OpenAI;
using LangChain.Schema;

var apiKey =
    Environment.GetEnvironmentVariable("OPENAI_API_KEY") ??
    throw new InvalidOperationException("OPENAI_API_KEY environment variable is not found.");
using var httpClient = new HttpClient();

var llm = new OpenAiModel(apiKey, OpenAI.Constants.ChatModels.Gpt35Turbo);

var prompt = ChatPromptTemplate.FromPromptMessages(new List<BaseMessagePromptTemplate>()
{
    SystemMessagePromptTemplate.FromTemplate("What is a good name for a company that makes {product}?")
});

var chain = new LlmChain(new LlmChainInput(llm, prompt));

var result = await chain.CallAsync(new ChainValues(new Dictionary<string, object>(1)
{
    { "product", "colourful socks" }
}));

// The result is an object with a `text` property.
Console.WriteLine(result.Value["text"]);

// We can also construct an LLMChain from a ChatPromptTemplate and a chat model.
var chat = new OpenAiModel(apiKey, OpenAI.Constants.ChatModels.Gpt35Turbo);

var chatPrompt = ChatPromptTemplate.FromPromptMessages(new List<BaseMessagePromptTemplate>(2)
{
    SystemMessagePromptTemplate.FromTemplate("You are a helpful assistant that translates {input_language} to {output_language}."),
    HumanMessagePromptTemplate.FromTemplate("{text}")
});

var chainB = new LlmChain(new LlmChainInput(chat, chatPrompt)
{
    Verbose = true
});

var resultB = await chainB.CallAsync(new ChainValues(new Dictionary<string, object>(3)
{
    {"input_language", "English"},
    {"output_language", "Chinese"},
    // Taking the result from the previous chain
    {"text", result.Value["text"]}
}));

Console.WriteLine(resultB.Value["text"]);

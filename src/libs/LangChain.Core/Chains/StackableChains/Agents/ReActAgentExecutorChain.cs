﻿using LangChain.Abstractions.Chains.Base;
using LangChain.Abstractions.Schema;
using LangChain.Chains.HelperChains;
using LangChain.Chains.StackableChains.ReAct;
using LangChain.Memory;
using LangChain.Providers;
using LangChain.Schema;
using System.Reflection;
using static LangChain.Chains.Chain;

namespace LangChain.Chains.StackableChains.Agents;

/// <summary>
/// 
/// </summary>
public class ReActAgentExecutorChain : BaseStackableChain
{
    /// <summary>
    /// 
    /// </summary>
    public const string DefaultPrompt =
        @"Answer the following questions as best you can. You have access to the following tools:

{tools}

Use the following format:

Question: the input question you must answer
Thought: you should always think about what to do
Action: the action to take, should be one of [{tool_names}]
Action Input: the input to the action
Observation: the result of the action
(this Thought/Action/Action Input/Observation can repeat multiple times)
Thought: I now know the final answer
Final Answer: the final answer to the original input question
Always add [END] after final answer

Begin!

Question: {input}
Thought:{history}";

    private StackChain? _chain;
    private bool _useCache;
    Dictionary<string, ReActAgentTool> _tools = new();
    private readonly IChatModel _model;
    private readonly string _reActPrompt;
    private readonly int _maxActions;
    private readonly ConversationBufferMemory _conversationBufferMemory;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="model"></param>
    /// <param name="reActPrompt"></param>
    /// <param name="maxActions"></param>
    /// <param name="inputKey"></param>
    /// <param name="outputKey"></param>
    public ReActAgentExecutorChain(
        IChatModel model,
        string? reActPrompt = null,
        int maxActions = 5,
        string inputKey = "answer",
        string outputKey = "final_answer")
    {
        reActPrompt ??= DefaultPrompt;
        _model = model;
        _reActPrompt = reActPrompt;
        _maxActions = maxActions;

        InputKeys = new[] { inputKey };
        OutputKeys = new[] { outputKey };

        _conversationBufferMemory = new ConversationBufferMemory(new ChatMessageHistory()) { AiPrefix = "", HumanPrefix = "", SystemPrefix = "", SaveHumanMessages = false };

    }

    private string _userInput = string.Empty;
    private const string ReActAnswer = "answer";
    private void InitializeChain()
    {
        var toolNames = string.Join(",", _tools.Select(x => x.Key));
        var tools = string.Join("\n", _tools.Select(x => $"{x.Value.Name}, {x.Value.Description}"));

        var chain =
            Set(() => _userInput, "input")
            | Set(tools, "tools")
            | Set(toolNames, "tool_names")
            | Set(() => _conversationBufferMemory.BufferAsString, "history")
            | Template(_reActPrompt)
            | Chain.LLM(_model).UseCache(_useCache)
            | UpdateMemory(_conversationBufferMemory, requestKey: "input", responseKey: "text")
            | ReActParser(inputKey: "text", outputKey: ReActAnswer);

        _chain = chain;
    }

    /// <inheritdoc/>
    protected override async Task<IChainValues> InternalCall(IChainValues values)
    {
        values = values ?? throw new ArgumentNullException(nameof(values));
        
        var input = (string)values.Value[InputKeys[0]];
        var valuesChain = new ChainValues();

        _userInput = input;

        if (_chain == null)
        {
            InitializeChain();
        }

        for (int i = 0; i < _maxActions; i++)
        {
            var res = await _chain!.CallAsync(valuesChain).ConfigureAwait(false);
            if (res.Value[ReActAnswer] is AgentAction)
            {
                var action = (AgentAction)res.Value[ReActAnswer];
                var tool = _tools[action.Action];
                var toolRes = tool.ToolCall(action.ActionInput);
                await _conversationBufferMemory.ChatHistory.AddMessage(new Message("Observation: " + toolRes, MessageRole.System))
                    .ConfigureAwait(false);
                await _conversationBufferMemory.ChatHistory.AddMessage(new Message("Thought:", MessageRole.System))
                    .ConfigureAwait(false);
                continue;
            }
            else if (res.Value[ReActAnswer] is AgentFinish)
            {
                var finish = (AgentFinish)res.Value[ReActAnswer];
                values.Value.Add(OutputKeys[0], finish.Output);
                return values;
            }
        }



        return values;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="enabled"></param>
    /// <returns></returns>
    public ReActAgentExecutorChain UseCache(bool enabled = true)
    {
        _useCache = enabled;
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tool"></param>
    /// <returns></returns>
    public ReActAgentExecutorChain UseTool(ReActAgentTool tool)
    {
        tool = tool ?? throw new ArgumentNullException(nameof(tool));
        
        _tools.Add(tool.Name, tool);
        return this;
    }
}
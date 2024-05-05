﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ChatdollKit.Dialog.Processor
{
    public class ChatGPTFunctionSkillBase : ChatGPTContentSkill
    {
        public virtual ChatGPTFunction GetFunctionSpec()
        {
            throw new NotImplementedException("ChatGPTFunctionSkillBase.GetFunctionSpec must be implemented");
        }

        public override async UniTask<Response> ProcessAsync(Request request, State state, User user, CancellationToken token)
        {
            var apiStreamTask = (UniTask)request.Payloads[0];

            // TODO: Waiting AnimatedVoice

            await apiStreamTask;

            var responseForRequest = await ExecuteFunction(chatGPT.StreamBuffer, request, state, user, token);

            var functionName = GetFunctionSpec().name;
            var functionCallMessage = new ChatGPTMessage("assistant", function_call: new Dictionary<string, object>() {
                { "name", functionName },
                { "arguments", chatGPT.StreamBuffer }
            });

            // Update histories after function finishes successfully
            chatGPT.AddHistory(state, new ChatGPTMessage("user", request.Text));
            chatGPT.AddHistory(state, functionCallMessage);

            // Add messages
            var messages = chatGPT.GetHistories(state);
            messages.Add(new ChatGPTMessage(responseForRequest.Role, responseForRequest.Body, name: functionName));

            // Call ChatCompletion to get human-friendly response
            apiStreamTask = chatGPT.ChatCompletionAsync(messages, false);

            // Start parsing voices, faces and animations and performing them concurrently
            var parseTask = ParseAnimatedVoiceAsync(token);
            var performTask = PerformAnimatedVoiceAsync(token);

            // Make response
            var response = new Response(request.Id, endTopic: false);
            response.Payloads = new Dictionary<string, object>()
            {
                { "ApiStreamTask", apiStreamTask },
                { "ParseTask", parseTask },
                { "PerformTask", performTask },
                { "UserMessage", messages.Last() }
            };

            return response;
        }

#pragma warning disable CS1998
        protected virtual async UniTask<FunctionResponse> ExecuteFunction(string argumentsJsonString, Request request, State state, User user, CancellationToken token)
        {
            throw new NotImplementedException("ChatGPTFunctionSkillBase.ExecuteFunction must be implemented");
        }
#pragma warning restore CS1998

        protected class FunctionResponse
        {
            public string Body { get; protected set; }
            public string Role { get; protected set; }

            public FunctionResponse(string body, string role = "function")
            {
                Body = body;
                Role = role;
            }
        }
    }
}

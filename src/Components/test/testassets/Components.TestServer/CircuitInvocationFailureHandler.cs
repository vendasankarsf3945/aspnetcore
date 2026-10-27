// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;

namespace TestServer;

/// <summary>
/// CircuitHandler that can be configured to fail ConnectCircuit or ResumeCircuit invocations for testing reconnection scenarios.
/// </summary>
public class CircuitInvocationFailureHandler : CircuitHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private static readonly Dictionary<string, bool> FailureTracker = new();
    private static readonly object LockObject = new();

    public CircuitInvocationFailureHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        // Extract the fail-circuit-invocation query parameter from the HTTP context
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Request.Query.TryGetValue("fail-circuit-invocation", out var failMethod) == true)
        {
            var failMethodStr = failMethod.ToString();
            
            // Store the failure method in the circuit's metadata for later use
            httpContext.Items["CircuitInvocationFailureMethod"] = failMethodStr;
            httpContext.Items["CircuitInvocationFailureAttempt"] = 0;
        }

        return Task.CompletedTask;
    }

    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
        Func<CircuitInboundActivityContext, Task> next)
    {
        return async (context) =>
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var failMethod = httpContext?.Items["CircuitInvocationFailureMethod"] as string;
            var attemptCount = (int)(httpContext?.Items["CircuitInvocationFailureAttempt"] ?? 0);

            // Only fail on the first attempt
            if (attemptCount == 0)
            {
                if (failMethod == "ConnectCircuit" && context.MethodName == "ConnectCircuit")
                {
                    httpContext!.Items["CircuitInvocationFailureAttempt"] = 1;
                    // Store which method failed for the client to retrieve
                    httpContext.Items["CircuitInvocationFailureOccurred"] = "ConnectCircuit";
                    throw new InvalidOperationException("Circuit invocation failed for testing: ConnectCircuit");
                }

                if (failMethod == "ResumeCircuit" && context.MethodName == "ResumeCircuit")
                {
                    httpContext!.Items["CircuitInvocationFailureAttempt"] = 1;
                    // Store which method failed for the client to retrieve
                    httpContext.Items["CircuitInvocationFailureOccurred"] = "ResumeCircuit";
                    throw new InvalidOperationException("Circuit invocation failed for testing: ResumeCircuit");
                }
            }

            await next(context);
        };
    }
}

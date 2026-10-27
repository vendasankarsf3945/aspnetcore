// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

namespace TestServer;

/// <summary>
/// Hub filter that allows failing ConnectCircuit or ResumeCircuit hub methods for testing reconnection recovery.
/// This filter checks for the "fail-circuit-invocation" query parameter and throws an exception when the specified method is invoked.
/// </summary>
public class FailingCircuitHubFilter : IHubFilter
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FailingCircuitHubFilter(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        
        // Check if this request specifies a method to fail
        if (httpContext?.Request.Query.TryGetValue("fail-circuit-invocation", out var methodName) == true)
        {
            var failingMethod = methodName.ToString();
            
            // Check if the current invocation matches the method to fail
            if (invocationContext.HubMethodName == failingMethod)
            {
                throw new InvalidOperationException($"Test-injected failure for {failingMethod} invocation");
            }
        }

        return await next(invocationContext);
    }
}

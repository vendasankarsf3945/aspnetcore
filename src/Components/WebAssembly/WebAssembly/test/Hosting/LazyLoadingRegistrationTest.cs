// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Infrastructure;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting;

public class LazyLoadingRegistrationTest
{
    [Fact]
    public async Task ResolveDeferredRootComponentsAsync_WhenNothingDeferred_ReturnsImmediately()
    {
        // zero registered components means nothing is ever deferred.
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.AddSingleton(Mock.Of<IJSRuntime>());
        var host = builder.Build();

        // should complete without calling LazyAssemblyLoader at all.
        await builder.ResolveDeferredRootComponentsAsync(host.Services);

        // RootComponents still empty (nothing was deferred to add).
        Assert.Empty(builder.RootComponents);
    }

    [Fact]
    public void ResolveOperationDescriptors_RemoveOperationsOnly_ReturnsUnmodifiedBatch()
    {
        var batch = new RootComponentOperationBatch
        {
            BatchId = 42,
            Operations =
            [
                new RootComponentOperation { Type = RootComponentOperationType.Remove, SsrComponentId = 1 }
            ]
        };

        // Remove operations must be skipped; no type resolution attempted.
        var result = DefaultWebAssemblyJSRuntime.ResolveOperationDescriptors(batch);

        Assert.Same(batch, result);
        Assert.Null(batch.Operations[0].Descriptor);
    }

    [Fact]
    public void ResolveOperationDescriptors_UnknownAssembly_Throws()
    {
        var marker = new ComponentMarker();
        marker.WriteWebAssemblyData(
            "NonExistent.Assembly.ThatDoesNotExist",
            "NonExistent.Assembly.ThatDoesNotExist.SomeComponent",
            Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(
                Array.Empty<ComponentParameter>(),
                WebAssemblyJsonSerializerContext.Default.ComponentParameterArray)),
            Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(
                Array.Empty<ComponentParameter>(),
                WebAssemblyJsonSerializerContext.Default.ComponentParameterArray)));

        var batch = new RootComponentOperationBatch
        {
            BatchId = 1,
            Operations =
            [
                new RootComponentOperation { Type = RootComponentOperationType.Add, Marker = marker }
            ]
        };

        var ex = Assert.Throws<InvalidOperationException>(
            () => DefaultWebAssemblyJSRuntime.ResolveOperationDescriptors(batch));

        Assert.Contains("NonExistent.Assembly.ThatDoesNotExist.SomeComponent", ex.Message);
        Assert.Contains("NonExistent.Assembly.ThatDoesNotExist", ex.Message);
    }
}
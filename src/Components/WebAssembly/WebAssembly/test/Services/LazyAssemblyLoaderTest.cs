// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop;
using Moq;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services;

public class LazyAssemblyLoaderTest
{
    private static LazyAssemblyLoader CreateLoader()
        => new(Mock.Of<IJSRuntime>());

    [Fact]
    public async Task LoadMissingAssembliesAsync_AlreadyLoadedAssembly_IsFilteredOut()
    {
        var loader = CreateLoader();

        // System.Private.CoreLib is always in AppDomain; the method must not attempt to reload it.
        var alreadyLoadedName = typeof(object).Assembly.GetName().Name!;

        // Would throw FileNotFoundException if it tried to re-load a non-lazy assembly via LoadAssembliesAsync.
        await loader.LoadMissingAssembliesAsync([alreadyLoadedName]);
    }
}
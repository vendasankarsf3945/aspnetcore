// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Microsoft.AspNetCore.Components.Analyzers.Tests;

public class JSInteropTryCatchAnalyzerTest : DiagnosticVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        => new JSInteropTryCatchAnalyzer();

    [Fact]
    public void ReportsDiagnostic_WhenInvokeAsyncIsNotWrapped()
    {
        var source = """
using Microsoft.JSInterop;

class C
{
    IJSRuntime JS;

    async void M()
    {
        await JS.InvokeAsync<int>("foo");
    }
}
""";

        var expected = new DiagnosticResult
        {
            Id = DiagnosticDescriptors.JSInteropCallNotWrapped.Id,
            Severity = DiagnosticSeverity.Warning,
            Message = "JSInterop invocation 'InvokeAsync' is not wrapped in a try-catch block",
            Locations =
            [
                new DiagnosticResultLocation("Test0.cs", 9, 15)
            ]
        };

        VerifyCSharpDiagnostic(source, expected);
    }

    [Fact]
    public void ReportsDiagnostic_WhenInvokeVoidAsyncIsNotWrapped()
    {
        var source = """
using Microsoft.JSInterop;

class C
{
    IJSRuntime JS;

    async void M()
    {
        await JS.InvokeVoidAsync("alert");
    }
}
""";

        var expected = new DiagnosticResult
        {
            Id = DiagnosticDescriptors.JSInteropCallNotWrapped.Id,
            Severity = DiagnosticSeverity.Warning,
            Message = "JSInterop invocation 'InvokeVoidAsync' is not wrapped in a try-catch block",
            Locations =
            [
                new DiagnosticResultLocation("Test0.cs", 9, 15)
            ]
        };

        VerifyCSharpDiagnostic(source, expected);
    }

    [Fact]
    public void NoDiagnostic_WhenWrappedInTryCatch()
    {
        var source = """
using Microsoft.JSInterop;
using System;

class C
{
    IJSRuntime JS;

    async void M()
    {
        try
        {
            await JS.InvokeAsync<int>("foo");
        }
        catch (Exception)
        {
        }
    }
}
""";

        VerifyCSharpDiagnostic(source);
    }

    [Fact]
    public void NoDiagnostic_ForNonJSInteropInvocation()
    {
        var source = """
using System.Threading.Tasks;

class C
{
    async void M()
    {
        await Task.Delay(10);
    }
}
""";

        VerifyCSharpDiagnostic(source);
    }

    [Fact]
    public void NoDiagnostic_WhenInvocationIsInsideFinally()
    {
        var source = """
using Microsoft.JSInterop;

class C
{
    IJSRuntime JS;

    async void M()
    {
        try
        {
        }
        finally
        {
            await JS.InvokeVoidAsync("cleanup");
        }
    }
}
""";

        VerifyCSharpDiagnostic(source);
    }

    [Fact]
    public void ReportsDiagnostic_ForIJSObjectReference()
    {
        var source = """
using Microsoft.JSInterop;

class C
{
    IJSObjectReference Obj;

    async void M()
    {
        await Obj.InvokeVoidAsync("foo");
    }
}
""";

        var expected = new DiagnosticResult
        {
            Id = DiagnosticDescriptors.JSInteropCallNotWrapped.Id,
            Severity = DiagnosticSeverity.Warning,
            Message = "JSInterop invocation 'InvokeVoidAsync' is not wrapped in a try-catch block",
            Locations =
            [
                new DiagnosticResultLocation("Test0.cs", 9, 15)
            ]
        };

        VerifyCSharpDiagnostic(source, expected);
    }
}
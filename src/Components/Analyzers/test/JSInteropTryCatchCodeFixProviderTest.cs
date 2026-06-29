// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Microsoft.AspNetCore.Components.Analyzers.Tests;

public class JSInteropTryCatchCodeFixProviderTest : CodeFixVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        => new JSInteropTryCatchAnalyzer();

    protected override CodeFixProvider GetCSharpCodeFixProvider()
        => new JSInteropTryCatchCodeFixProvider();

    [Fact]
    public void CodeFix_WrapsJSInteropCallInTryCatch()
    {
        var source = string.Join(Environment.NewLine, new[]
        {
            "using Microsoft.JSInterop;",
            "",
            "class C",
            "{",
            "    IJSRuntime JS;",
            "",
            "    async void M()",
            "    {",
            "        await JS.InvokeVoidAsync(\"alert\");",
            "    }",
            "}",
        });

        var fixedSource = string.Join(Environment.NewLine, new[]
        {
            "using Microsoft.JSInterop;",
            "",
            "class C",
            "{",
            "    IJSRuntime JS;",
            "",
            "    async void M()",
            "    {",
            "        try",
            "        {",
            "            await JS.InvokeVoidAsync(\"alert\");",
            "        }",
            "        catch (System.Exception)",
            "        {",
            "        }",
            "    }",
            "}",
        });

        VerifyCSharpFix(source, fixedSource);
    }

    [Fact]
    public void CodeFix_Works_ForIJSObjectReference()
    {
        var source = string.Join(Environment.NewLine, new[]
        {
            "using Microsoft.JSInterop;",
            "",
            "class C",
            "{",
            "    IJSObjectReference Obj;",
            "",
            "    async void M()",
            "    {",
            "        await Obj.InvokeVoidAsync(\"foo\");",
            "    }",
            "}",
        });

        var fixedSource = string.Join(Environment.NewLine, new[]
        {
            "using Microsoft.JSInterop;",
            "",
            "class C",
            "{",
            "    IJSObjectReference Obj;",
            "",
            "    async void M()",
            "    {",
            "        try",
            "        {",
            "            await Obj.InvokeVoidAsync(\"foo\");",
            "        }",
            "        catch (System.Exception)",
            "        {",
            "        }",
            "    }",
            "}",
        });

        VerifyCSharpFix(source, fixedSource);
    }

    [Fact]
    public void NoCodeFix_WhenAlreadyWrapped()
    {
        var source = string.Join(Environment.NewLine, new[]
        {
            "using Microsoft.JSInterop;",
            "",
            "class C",
            "{",
            "    IJSRuntime JS;",
            "",
            "    async void M()",
            "    {",
            "        try",
            "        {",
            "            await JS.InvokeVoidAsync(\"alert\");",
            "        }",
            "        catch (System.Exception)",
            "        {",
            "        }",
            "    }",
            "}",
        });

        VerifyCSharpDiagnostic(source);
    }

    [Fact]
    public void NoCodeFix_WhenInvocationIsInsideFinally()
    {
        var source = string.Join(Environment.NewLine, new[]
        {
            "using Microsoft.JSInterop;",
            "",
            "class C",
            "{",
            "    IJSRuntime JS;",
            "",
            "    async void M()",
            "    {",
            "        try",
            "        {",
            "        }",
            "        finally",
            "        {",
            "            await JS.InvokeVoidAsync(\"cleanup\");",
            "        }",
            "    }",
            "}",
        });

        VerifyCSharpDiagnostic(source);
    }
}
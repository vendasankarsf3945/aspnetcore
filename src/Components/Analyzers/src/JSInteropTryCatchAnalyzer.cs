// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Components.Analyzers;

/// <summary>
/// Analyzer that warns when JavaScript interop invocations are not wrapped
/// in a try/catch block, as such calls may fail during prerendering,
/// disconnections, or runtime JS errors.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class JSInteropTryCatchAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Gets the diagnostics supported by this analyzer.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.JSInteropCallNotWrapped);

    /// <summary>
    /// Initializes the analyzer and registers syntax node actions.
    /// </summary>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(
            AnalyzeInvocation,
            SyntaxKind.InvocationExpression);
    }

    /// <summary>
    /// Analyzes invocation expressions to detect unsafe JS interop calls.
    /// </summary>
    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (!TryGetInvokedMethodName(invocation, out var methodName))
        {
            return;
        }

        if (methodName is not ("InvokeAsync" or "InvokeVoidAsync"))
        {
            return;
        }

        if (IsWithinTryStatement(invocation))
        {
            return;
        }

        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
        {
            var containingType = methodSymbol.ContainingType?.ToDisplayString();
            if (containingType is not
                ("Microsoft.JSInterop.IJSRuntime" or
                 "Microsoft.JSInterop.IJSObjectReference"))
            {
                return;
            }
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.JSInteropCallNotWrapped,
                invocation.GetLocation(),
                methodName));
    }

    /// <summary>
    /// Attempts to extract the invoked method name from the invocation syntax.
    /// </summary>
    private static bool TryGetInvokedMethodName(
        InvocationExpressionSyntax invocation,
        out string? methodName)
    {
        methodName = invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess =>
                memberAccess.Name.Identifier.ValueText,
            IdentifierNameSyntax identifier =>
                identifier.Identifier.ValueText,
            _ => null
        };

        return methodName is not null;
    }

    /// <summary>
    /// Determines whether the specified node is contained within a try statement.
    /// </summary>
    private static bool IsWithinTryStatement(SyntaxNode node)
    {
        for (var current = node.Parent; current is not null; current = current.Parent)
        {
            if (current is TryStatementSyntax tryStatement &&
                tryStatement.Span.Contains(node.Span))
            {
                return true;
            }
        }

        return false;
    }
}

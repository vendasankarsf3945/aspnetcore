// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.AspNetCore.Components.Analyzers;

/// <summary>
/// Provides a code fix that wraps unsafe JavaScript interop invocations
/// in a try/catch block to prevent unhandled runtime failures.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(JSInteropTryCatchCodeFixProvider)), Shared]
public sealed class JSInteropTryCatchCodeFixProvider : CodeFixProvider
{
    /// <summary>
    /// The title shown for the code fix action.
    /// </summary>
    private const string Title = "Wrap JSInterop call in try-catch";

    /// <summary>
    /// Gets the diagnostic IDs that this code fix can address.
    /// </summary>
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(DiagnosticDescriptors.JSInteropCallNotWrapped.Id);

    /// <summary>
    /// Gets the fix-all provider for batch fixing occurrences.
    /// </summary>
    public override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    /// <summary>
    /// Registers code fixes for diagnostics reported by the analyzer.
    /// </summary>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document
            .GetSyntaxRootAsync(context.CancellationToken)
            .ConfigureAwait(false);

        if (root is null)
        {
            return;
        }

        var diagnostic = context.Diagnostics[0];
        var node = root.FindNode(diagnostic.Location.SourceSpan);

        if (node is not InvocationExpressionSyntax invocation)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: cancellationToken =>
                    WrapInTryCatchAsync(context.Document, invocation, cancellationToken),
                equivalenceKey: nameof(JSInteropTryCatchCodeFixProvider)),
            diagnostic);
    }

    /// <summary>
    /// Wraps the containing statement of the JS interop invocation
    /// in a try/catch block.
    /// </summary>
    private static async Task<Document> WrapInTryCatchAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        CancellationToken cancellationToken)
    {
        var root = await document
            .GetSyntaxRootAsync(cancellationToken)
            .ConfigureAwait(false);

        if (root is null)
        {
            return document;
        }

        var statement = invocation.FirstAncestorOrSelf<StatementSyntax>();
        if (statement is null)
        {
            return document;
        }

        var tryBlock = SyntaxFactory.Block(statement)
            .WithAdditionalAnnotations(Formatter.Annotation);

        var catchClause =
            SyntaxFactory.CatchClause()
                .WithDeclaration(
                    SyntaxFactory.CatchDeclaration(
                        SyntaxFactory.ParseTypeName("System.Exception")))
                .WithBlock(SyntaxFactory.Block());

        var tryStatement =
            SyntaxFactory.TryStatement()
                .WithBlock(tryBlock)
                .WithCatches(SyntaxFactory.SingletonList(catchClause))
                .WithAdditionalAnnotations(Formatter.Annotation);

        var newRoot = root.ReplaceNode(statement, tryStatement);

        return document.WithSyntaxRoot(newRoot);
    }
}
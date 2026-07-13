// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class HttpContextFormDataProvider
{
    private string? _incomingHandlerName;
    private IReadOnlyDictionary<string, StringValues>? _entries;
    private IFormFileCollection? _formFiles;
    private static readonly IFormFileCollection EmptyFiles = new FormFileCollection();

    public string? IncomingHandlerName => _incomingHandlerName;

    public IReadOnlyDictionary<string, StringValues> Entries => _entries ?? ReadOnlyDictionary<string, StringValues>.Empty;

    public IFormFileCollection FormFiles => _formFiles ?? EmptyFiles;

    public void SetFormData(string incomingHandlerName, IReadOnlyDictionary<string, StringValues> form, IFormFileCollection formFiles)
    {
        _incomingHandlerName = incomingHandlerName;
        _entries = form;
        _formFiles = formFiles;
    }

    public void SetQueryData(string incomingHandlerName, IReadOnlyDictionary<string, StringValues> query)
    {
        _incomingHandlerName = incomingHandlerName;
        _entries = query;
        _formFiles = EmptyFiles;
    }

    public bool TryGetIncomingHandlerName([NotNullWhen(true)] out string? incomingHandlerName)
    {
        incomingHandlerName = _incomingHandlerName;
        return incomingHandlerName is not null;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { AssetBehaviors } from '@microsoft/dotnet-runtime';
import { WebAssemblyBootResourceType, WebAssemblyStartOptions } from '../WebAssemblyStartOptions';

type LoadBootResource = WebAssemblyStartOptions['loadBootResource'];

const emptyContentLength = '0';

export function createBootResourceLoaderWithCacheRecovery(loadBootResource: LoadBootResource | undefined): LoadBootResource {
  return (type: WebAssemblyBootResourceType, name: string, defaultUri: string, integrity: string, behavior: AssetBehaviors) => {
    const userDefinedResult = loadBootResource?.(type, name, defaultUri, integrity, behavior);
    if (userDefinedResult !== null && userDefinedResult !== undefined) {
      return userDefinedResult;
    }

    if (!shouldRecoverFromCacheFailure(type, integrity)) {
      return undefined;
    }

    return fetchWithCacheRecovery(defaultUri, integrity);
  };
}

function shouldRecoverFromCacheFailure(type: WebAssemblyBootResourceType, integrity: string): boolean {
  return type !== 'dotnetjs' && integrity.length > 0;
}

async function fetchWithCacheRecovery(defaultUri: string, integrity: string): Promise<Response> {
  try {
    const cachedResponse = await fetchResource(defaultUri, integrity, 'force-cache');
    if (!await isUsableResponse(cachedResponse)) {
      return await fetchResource(defaultUri, integrity, 'reload');
    }

    return cachedResponse;
  } catch {
    return await fetchResource(defaultUri, integrity, 'reload');
  }
}

async function isUsableResponse(response: Response): Promise<boolean> {
  if (!response.ok) {
    return false;
  }

  const contentLength = response.headers.get('content-length');
  if (contentLength === emptyContentLength) {
    return false;
  }

  if (contentLength === null) {
    const bytes = await response.clone().arrayBuffer();
    return bytes.byteLength > 0;
  }

  return true;
}

function fetchResource(defaultUri: string, integrity: string, cache: RequestCache): Promise<Response> {
  return fetch(defaultUri, {
    cache,
    integrity,
  });
}

import { afterEach, beforeEach, describe, expect, jest, test } from '@jest/globals';
import { createBootResourceLoaderWithCacheRecovery } from '../../../src/Platform/Mono/BootResourceLoader';

describe('BootResourceLoader', () => {
  let previousFetch: typeof fetch | undefined;
  let fetchMock: jest.Mock<typeof fetch>;

  beforeEach(() => {
    previousFetch = globalThis.fetch;
    fetchMock = jest.fn<typeof fetch>();
    Object.defineProperty(globalThis, 'fetch', {
      configurable: true,
      writable: true,
      value: fetchMock,
    });
  });

  afterEach(() => {
    jest.restoreAllMocks();
    Object.defineProperty(globalThis, 'fetch', {
      configurable: true,
      writable: true,
      value: previousFetch,
    });
  });

  test('uses force-cache when cached response is usable', async () => {
    fetchMock.mockResolvedValue(createResponse(true, '10'));
    const loader = createBootResourceLoaderWithCacheRecovery(undefined);

    const response = await loader('dotnetwasm', 'app.wasm', '_framework/app.wasm', 'sha256-abc', 'dotnetwasm') as Response;

    expect(response.ok).toBe(true);
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(fetchMock).toHaveBeenCalledWith('_framework/app.wasm', { cache: 'force-cache', integrity: 'sha256-abc' });
  });

  test('uses force-cache when content-length header is missing but body is not empty', async () => {
    fetchMock.mockResolvedValue(createResponse(true, null, 8));
    const loader = createBootResourceLoaderWithCacheRecovery(undefined);

    const response = await loader('dotnetwasm', 'app.wasm', '_framework/app.wasm', 'sha256-missing-length', 'dotnetwasm') as Response;

    expect(response.ok).toBe(true);
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(fetchMock).toHaveBeenCalledWith('_framework/app.wasm', { cache: 'force-cache', integrity: 'sha256-missing-length' });
  });

  test('falls back to reload when the force-cache response is not ok', async () => {
    fetchMock
      .mockResolvedValueOnce(createResponse(false, null))
      .mockResolvedValueOnce(createResponse(true, '10'));
    const loader = createBootResourceLoaderWithCacheRecovery(undefined);

    const response = await loader('assembly', 'app.dll', '_framework/app.dll', 'sha256-def', 'assembly') as Response;

    expect(response.ok).toBe(true);
    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(fetchMock.mock.calls[0][1]).toEqual({ cache: 'force-cache', integrity: 'sha256-def' });
    expect(fetchMock.mock.calls[1][1]).toEqual({ cache: 'reload', integrity: 'sha256-def' });
  });

  test('falls back to reload when the force-cache response body is empty', async () => {
    fetchMock
      .mockResolvedValueOnce(createResponse(true, '0'))
      .mockResolvedValueOnce(createResponse(true, '9'));
    const loader = createBootResourceLoaderWithCacheRecovery(undefined);

    await loader('assembly', 'app.dat', '_framework/app.dat', 'sha256-ghi', 'assembly');

    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(fetchMock.mock.calls[1][1]).toEqual({ cache: 'reload', integrity: 'sha256-ghi' });
  });

  test('falls back to reload when content-length header is missing and body is empty', async () => {
    fetchMock
      .mockResolvedValueOnce(createResponse(true, null, 0))
      .mockResolvedValueOnce(createResponse(true, '9'));
    const loader = createBootResourceLoaderWithCacheRecovery(undefined);

    await loader('assembly', 'app.dat', '_framework/app.dat', 'sha256-no-header-empty', 'assembly');

    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(fetchMock.mock.calls[1][1]).toEqual({ cache: 'reload', integrity: 'sha256-no-header-empty' });
  });

  test('falls back to reload when force-cache fetch throws', async () => {
    fetchMock
      .mockRejectedValueOnce(new TypeError('Failed to fetch'))
      .mockResolvedValueOnce(createResponse(true, '11'));
    const loader = createBootResourceLoaderWithCacheRecovery(undefined);

    const response = await loader('assembly', 'app.dll', '_framework/app.dll', 'sha256-jkl', 'assembly') as Response;

    expect(response.ok).toBe(true);
    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(fetchMock.mock.calls[1][1]).toEqual({ cache: 'reload', integrity: 'sha256-jkl' });
  });

  test('preserves non-null user overrides', () => {
    const loader = createBootResourceLoaderWithCacheRecovery(() => '/cdn/path/app.wasm');

    const result = loader('dotnetwasm', 'app.wasm', '_framework/app.wasm', 'sha256-mno', 'dotnetwasm');

    expect(result).toBe('/cdn/path/app.wasm');
    expect(fetchMock).not.toHaveBeenCalled();
  });

  test('does not apply cache recovery for dotnetjs resources', () => {
    const loader = createBootResourceLoaderWithCacheRecovery(undefined);

    const result = loader('dotnetjs', 'dotnet.js', '_framework/dotnet.js', 'sha256-mno', 'js-module-dotnet');

    expect(result).toBeUndefined();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  test('does not override default runtime behavior when integrity is absent', () => {
    const loader = createBootResourceLoaderWithCacheRecovery(undefined);

    const result = loader('configuration', 'appsettings.json', '_framework/appsettings.json', '', 'manifest');

    expect(result).toBeUndefined();
    expect(fetchMock).not.toHaveBeenCalled();
  });
});

function createResponse(ok: boolean, contentLength: string | null, byteLength = 1): Response {
  const headers = new Headers();
  if (contentLength !== null) {
    headers.set('content-length', contentLength);
  }

  const arrayBuffer = async () => new ArrayBuffer(byteLength);

  return {
    ok,
    headers,
    arrayBuffer,
    clone: () => ({
      arrayBuffer,
    }),
  } as unknown as Response;
}

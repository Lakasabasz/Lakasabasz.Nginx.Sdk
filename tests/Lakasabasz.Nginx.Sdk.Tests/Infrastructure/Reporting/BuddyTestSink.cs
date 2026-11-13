using System.Collections.Concurrent;

namespace Lakasabasz.Nginx.Sdk.Tests.Infrastructure.Reporting;

public sealed class BuddyTestSink : IAsyncDisposable
{
    public static readonly TimeSpan DefaultFlushInterval = TimeSpan.FromSeconds(5);
    public const int MaxBatchSize = 250;

    private readonly BuddyUnitTestsClient _client;
    private readonly BuddyTestSessionInfo _sessionInfo;
    private readonly TextWriter _log;
    private readonly ConcurrentQueue<BuddyTestCase> _pending = new();
    private readonly SemaphoreSlim _flushLock = new(1, 1);
    private readonly CancellationTokenSource _stop = new();
    private Task? _flushLoop;
    private string? _sessionId;

    public BuddyTestSink(BuddyUnitTestsClient client, BuddyTestSessionInfo sessionInfo, TimeSpan? flushInterval = null, TextWriter? log = null)
    {
        _client = client;
        _sessionInfo = sessionInfo;
        FlushInterval = flushInterval ?? DefaultFlushInterval;
        _log = log ?? Console.Error;
    }

    public TimeSpan FlushInterval { get; }

    public int SentCount { get; private set; }

    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _sessionId = await _client.OpenSessionAsync(_sessionInfo, cancellationToken);
            _flushLoop = FlushPeriodicallyAsync(_stop.Token);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            await _log.WriteLineAsync(Consts.Buddy.CannotOpen(e.Message).AsMemory(), CancellationToken.None);
        }
    }

    public void Add(BuddyTestCase testCase) => _pending.Enqueue(testCase);

    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (_sessionId is null)
            return;

        await _stop.CancelAsync();
        if (_flushLoop is not null)
            await _flushLoop;

        await FlushAsync(cancellationToken);
        try
        {
            await _client.CloseSessionAsync(_sessionId, cancellationToken);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            await _log.WriteLineAsync(Consts.Buddy.CannotClose(e.Message).AsMemory(), CancellationToken.None);
        }

        if (!_pending.IsEmpty)
            await _log.WriteLineAsync(Consts.Buddy.NotReported(_pending.Count).AsMemory(), CancellationToken.None);
        _sessionId = null;
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        if (_sessionId is null)
            return;

        await _flushLock.WaitAsync(cancellationToken);
        try
        {
            while (TakeBatch() is { Count: > 0 } batch)
            {
                try
                {
                    await _client.AddCasesAsync(_sessionId, batch, cancellationToken);
                    SentCount += batch.Count;
                }
                catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
                {
                    foreach (var testCase in batch)
                        _pending.Enqueue(testCase);
                    await _log.WriteLineAsync(Consts.Buddy.SendFailed(batch.Count, e.Message).AsMemory(), CancellationToken.None);
                    return;
                }
            }
        }
        finally
        {
            _flushLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
        _stop.Dispose();
        _flushLock.Dispose();
        _client.Dispose();
    }

    private List<BuddyTestCase> TakeBatch()
    {
        var batch = new List<BuddyTestCase>();
        while (batch.Count < MaxBatchSize && _pending.TryDequeue(out var testCase))
            batch.Add(testCase);
        return batch;
    }

    private async Task FlushPeriodicallyAsync(CancellationToken stop)
    {
        using var timer = new PeriodicTimer(FlushInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stop))
                await FlushAsync(stop);
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        {
        }
    }
}

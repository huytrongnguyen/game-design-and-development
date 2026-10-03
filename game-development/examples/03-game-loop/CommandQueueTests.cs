namespace Course.GameLoop;

public class CommandQueueTests
{
    [Fact]
    public async Task Submit_FromManyConcurrentThreads_AppliesInStrictSequenceOrder()
    {
        const int threadCount = 8;
        const int commandsPerThread = 50;
        const int totalCommands = threadCount * commandsPerThread;

        var clock = new GameClock(TimeSpan.FromMilliseconds(100));
        var queue = new CommandQueue();
        var state = new ZoneState();
        var loop = new ZoneLoop(clock, new SeededRng(1), new TickTimerWheel(), queue, state);

        // Many threads race to submit at the same time. Each command records the
        // sequence number it was assigned, not which thread sent it -- the race is real,
        // but the sequence assignment (Interlocked.Increment inside CommandQueue.Submit)
        // is not.
        var submitters = new Task[threadCount];
        for (int t = 0; t < threadCount; t++)
        {
            submitters[t] = Task.Run(() =>
            {
                for (int i = 0; i < commandsPerThread; i++)
                {
                    queue.Submit(sequence => new RecordSequenceCommand(sequence));
                }
            });
        }

        await Task.WhenAll(submitters);

        // Every submitted command is still sitting in the channel at this point: nothing
        // is applied until Step() drains it, at the start of a tick.
        Assert.Empty(state.AppliedOrder);

        loop.Step();

        Assert.Equal(totalCommands, state.AppliedOrder.Count);

        var expected = new long[totalCommands];
        for (int i = 0; i < totalCommands; i++)
        {
            expected[i] = i + 1; // sequence numbers are assigned starting at 1, with no gaps
        }

        Assert.Equal(expected, state.AppliedOrder);
    }
}

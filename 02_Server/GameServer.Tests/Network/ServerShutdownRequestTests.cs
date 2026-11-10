using Dawnholder.Server.GameServer.Hosting;

namespace Dawnholder.Server.GameServer.Tests.Network;

[Collection("ConsoleSerial")]
public sealed class ServerShutdownRequestTests
{
    static readonly TimeSpan Limit = TimeSpan.FromSeconds(5);

    [Theory]
    [InlineData("\n")]
    [InlineData("")]
    [InlineData("stop\n")]
    public async Task CompletedInput_RequestsShutdown(string input)
    {
        TextReader original = Console.In;
        using StringReader reader = new(input);
        using ServerShutdownRequest request = new();
        try
        {
            Console.SetIn(reader);
            await request.WaitAsync().WaitAsync(Limit);
        }
        finally
        {
            Console.SetIn(original);
        }
    }

    [Fact]
    public async Task OpenInput_ReturnsPendingTask_WithoutHoldingCaller()
    {
        TextReader original = Console.In;
        using BlockingReader reader = new();
        using ServerShutdownRequest request = new();
        TaskCompletionSource<Task> returned = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task? caller = null;
        try
        {
            Console.SetIn(reader);
            // A separate caller lets this test release the reader even if WaitAsync blocks synchronously.
            caller = Task.Run(() => returned.SetResult(request.WaitAsync()));
            Task pending = await returned.Task.WaitAsync(Limit);
            await reader.Entered.Task.WaitAsync(Limit);
            Assert.False(pending.IsCompleted);
            reader.Release.Set();
            await pending.WaitAsync(Limit);
        }
        finally
        {
            try
            {
                reader.Release.Set();
                if (caller is not null)
                    await caller.WaitAsync(Limit);
                if (returned.Task.IsCompletedSuccessfully)
                {
                    Task pending = await returned.Task;
                    await pending.WaitAsync(Limit);
                }
            }
            finally
            {
                Console.SetIn(original);
            }
        }
    }

    [Fact]
    public async Task InputFailure_IsPropagated_InsteadOfBecomingNormalShutdown()
    {
        TextReader original = Console.In;
        IOException failure = new("Injected input read failure.");
        using ThrowingReader reader = new(failure);
        using ServerShutdownRequest request = new();
        try
        {
            Console.SetIn(reader);
            IOException observed = await Assert.ThrowsAsync<IOException>(
                () => request.WaitAsync().WaitAsync(Limit));
            Assert.Same(failure, observed);
        }
        finally
        {
            Console.SetIn(original);
        }
    }

    sealed class BlockingReader : TextReader
    {
        internal readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly ManualResetEventSlim Release = new();

        public override string? ReadLine()
        {
            Entered.SetResult();
            if (!Release.Wait(Limit))
                throw new TimeoutException("Test did not release its input reader.");
            return null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Release.Dispose();
            base.Dispose(disposing);
        }
    }

    sealed class ThrowingReader(IOException failure) : TextReader
    {
        public override string? ReadLine() => throw failure;
    }
}

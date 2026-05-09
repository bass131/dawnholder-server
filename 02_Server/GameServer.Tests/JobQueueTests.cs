using Dawnholder.Server.Network;

namespace GameServer.Tests;

public class JobQueueTests
{
    [Fact]
    public void Push_ExecutesAction_WhenQueueWasEmpty()
    {
        // 첫 Push: 큐가 비어 있으니 Push 호출자가 Flush 책임을 지고
        // 람다가 즉시 동기 실행되어야 한다.
        var queue = new JobQueue();
        int counter = 0;
        queue.Push(() => counter++);
        Assert.Equal(1, counter);
    }

    [Fact]
    public void Push_NestedDuringFlush_ExecutedInSameFlush()
    {
        // 첫 Push의 Flush 안에서 두 번째 Push가 들어와도
        // 두 번째 Push의 호출자는 Flush를 다시 시작하지 않는다 (m_Flush 플래그).
        // 두 작업 모두 첫 Flush 안에서 직렬로 실행됨 — actor 모델의 핵심.
        var queue = new JobQueue();
        int counter = 0;
        queue.Push(() =>
        {
            queue.Push(() => counter++);
        });
        Assert.Equal(1, counter);
    }
}

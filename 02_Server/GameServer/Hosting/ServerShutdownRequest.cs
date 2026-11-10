using System.Runtime.InteropServices;

namespace Dawnholder.Server.GameServer.Hosting;

/// <summary>표준입력 또는 OS 신호를 하나의 정지 요청으로 모으며 호스트 정지는 호출자가 소유한다.</summary>
internal sealed class ServerShutdownRequest : IDisposable
{
    readonly TaskCompletionSource _signalRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly PosixSignalRegistration _interrupt;
    readonly PosixSignalRegistration _terminate;

    public ServerShutdownRequest()
    {
        _interrupt = PosixSignalRegistration.Create(PosixSignal.SIGINT, OnSignal);
        try
        {
            _terminate = PosixSignalRegistration.Create(PosixSignal.SIGTERM, OnSignal);
        }
        catch
        {
            _interrupt.Dispose();
            throw;
        }
    }

    public async Task WaitAsync()
    {
        // Console의 동기 읽기는 취소할 수 없다. 열린 입력이 신호 종료를 막지 않도록
        // 프로세스를 붙들지 않는 스레드 풀에서 읽고 Enter와 EOF 모두 요청으로 취급한다.
        Task inputCompleted = Task.Run(() => Console.ReadLine());
        Task firstRequest = await Task.WhenAny(_signalRequested.Task, inputCompleted);
        await firstRequest;
    }

    public void Dispose()
    {
        _terminate.Dispose();
        _interrupt.Dispose();
    }

    void OnSignal(PosixSignalContext context)
    {
        // 반복 신호도 기본 종료를 취소한다. 비동기 continuation으로 Stop의 정지 예산을
        // 신호 처리기에서 기다리지 않고 Program의 기존 성공/실패 경로 한 곳에서 처리한다.
        context.Cancel = true;
        _signalRequested.TrySetResult();
    }
}

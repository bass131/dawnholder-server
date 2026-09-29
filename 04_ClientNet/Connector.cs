using System.Net;
using System.Net.Sockets;

namespace Dawnholder.Client.Net;

/// <summary>Creates independent, cancelable client connection attempts.</summary>
public class Connector
{
    public IConnectionAttempt BeginConnect(IPEndPoint endPoint)
    {
        if (endPoint == null) throw new ArgumentNullException(nameof(endPoint));
        return new ConnectionAttempt(endPoint);
    }

    /// <summary>Compatibility entry point for headless clients; each attempt owns its factory.</summary>
    public void Connect(IPEndPoint endPoint, Func<ClientSession> sessionFactory, int count = 1)
    {
        if (sessionFactory == null) throw new ArgumentNullException(nameof(sessionFactory));
        for (int i = 0; i < count; i++)
            _ = ActivateLegacyAsync(BeginConnect(endPoint), sessionFactory);
    }

    static async Task ActivateLegacyAsync(IConnectionAttempt attempt, Func<ClientSession> sessionFactory)
    {
        using (attempt)
        {
            Socket? socket = null;
            ClientSession? session = null;
            try
            {
                ConnectionOutcome outcome = await attempt.Completion.ConfigureAwait(false);
                if (outcome.Status != ConnectionStatus.Success || !attempt.TryTakeConnectedSocket(out socket))
                {
                    Console.WriteLine($"[Connector] Connect {outcome.Status}: {outcome.Error?.Message}");
                    return;
                }

                EndPoint endPoint = socket!.RemoteEndPoint!;
                session = sessionFactory();
                session.Start(socket);
                socket = null; // Start takes ownership, including failed activation.
                if (!session.IsDisconnected) session.OnConnected(endPoint);
            }
            catch (Exception error)
            {
                Console.WriteLine($"[Connector] Activation failed: {error}");
                session?.Disconnect();
            }
            finally
            {
                socket?.Dispose();
            }
        }
    }
}

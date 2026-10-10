using System.Net;
using System.Net.Sockets;

namespace Dawnholder.Management.Backend.Tests.Support;

/// <summary>
/// Keeps a free loopback TCP port bound, without listening, until disposal. While it is bound the kernel does not hand
/// the port to another process that asks for a free one. A bound socket is not a listener, so /proc/net/tcp, and with
/// it the backend's port observation, does not show it.
/// </summary>
internal sealed class PortReservation : IDisposable
{
    readonly Socket _socket;

    PortReservation(Socket socket)
    {
        _socket = socket;
        Port = ((IPEndPoint)socket.LocalEndPoint!).Port;
    }

    public int Port { get; }

    public static PortReservation Take()
    {
        while (true)
        {
            Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            PortReservation reservation = new(socket);
            if (!FreePort.IsReserved(reservation.Port))
            {
                return reservation;
            }

            reservation.Dispose();
        }
    }

    public void Dispose() => _socket.Dispose();
}

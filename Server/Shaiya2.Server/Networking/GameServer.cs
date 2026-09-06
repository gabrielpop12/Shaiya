using LiteNetLib;
using Shaiya2.Server.Networking.Sessions;
using Shaiya2.Server.World;
using Shaiya2.Shared.Network;
using System.Diagnostics;

namespace Shaiya2.Server.Networking;

public sealed class GameServer : INetEventListener
{
    private readonly NetManager _netManager;

    private readonly WorldManager
        _worldManager;

    private readonly PacketRouter
        _packetRouter;

    private readonly Dictionary<int, PlayerSession> _sessions = new();

    private int _nextSessionId = 1;


    public GameServer()
    {
        _worldManager =
            new WorldManager();

        _packetRouter =
            new PacketRouter(
                _worldManager
            );

        _netManager = new NetManager(this)
        {
            AutoRecycle = true
        };
    }

    public bool Start(int port)
    {
        Console.WriteLine("[Network] Starting...");

        bool started =
            _netManager.Start(port);

        if (!started)
        {
            Console.WriteLine(
                $"[Network] Failed to start on UDP port {port}."
            );

            return false;
        }

        Console.WriteLine(
            $"[Network] UDP port: {port}"
        );

        Console.WriteLine(
            "[Network] Server listening."
        );

        return true;
    }

    public void Run()
    {
        const double simulationRate =
            20.0;

        const double snapshotRate =
            10.0;

        const double simulationInterval =
            1.0 / simulationRate;

        const double snapshotInterval =
            1.0 / snapshotRate;

        var stopwatch =
            Stopwatch.StartNew();

        double previousTime =
            stopwatch.Elapsed.TotalSeconds;

        double simulationAccumulator =
            0.0;

        double snapshotAccumulator =
            0.0;

        Console.WriteLine(
            $"[World] Simulation={simulationRate} Hz | Snapshots={snapshotRate} Hz"
        );

        while (true)
        {
            _netManager.PollEvents();

            double currentTime =
                stopwatch.Elapsed.TotalSeconds;

            double frameTime =
                currentTime -
                previousTime;

            previousTime =
                currentTime;

            if (frameTime > 0.25)
            {
                frameTime =
                    0.25;
            }

            simulationAccumulator +=
                frameTime;

            snapshotAccumulator +=
                frameTime;

            while (simulationAccumulator >=
                   simulationInterval)
            {
                _worldManager.Tick(
                    (float)simulationInterval
                );

                simulationAccumulator -=
                    simulationInterval;
            }

            if (snapshotAccumulator >=
                snapshotInterval)
            {
                _worldManager.SendWorldSnapshots();

                snapshotAccumulator %=
                    snapshotInterval;
            }

            Thread.Sleep(1);
        }
    }

    public void Stop()
    {
        Console.WriteLine(
            "[Network] Stopping server..."
        );

        _netManager.Stop();
    }

    public void OnPeerConnected(
        NetPeer peer)
    {
        int sessionId =
            _nextSessionId++;

        var session =
            new PlayerSession(
                sessionId,
                peer
            );

        _sessions.Add(
            peer.Id,
            session
        );

        Console.WriteLine(
            $"[Session] Created | SessionId={sessionId} | PeerId={peer.Id}"
        );
    }

    public void OnPeerDisconnected(
        NetPeer peer,
        DisconnectInfo disconnectInfo)
    {
        if (_sessions.TryGetValue(
                peer.Id,
                out PlayerSession? session))
        {
            if (session.WorldPlayer != null)
            {
                _worldManager.RemovePlayer(
                    session.WorldPlayer
                );

                session.WorldPlayer =
                    null;
            }

            _sessions.Remove(
                peer.Id
            );

            Console.WriteLine(
                $"[Session] Removed | SessionId={session.SessionId}"
            );
        }

        Console.WriteLine(
            $"[Network] Client disconnected | PeerId={peer.Id} | Reason={disconnectInfo.Reason}"
        );
    }

    public void OnNetworkReceive(
        NetPeer peer,
        NetPacketReader reader,
        byte channelNumber,
        DeliveryMethod deliveryMethod)
    {
        try
        {
            if (!_sessions.TryGetValue(
                    peer.Id,
                    out PlayerSession? session))
            {
                Console.WriteLine(
                    $"[Network] Packet received from unknown PeerId={peer.Id}"
                );

                peer.Disconnect();

                return;
            }

            PacketId packetId =
                (PacketId)reader.GetUShort();

            Console.WriteLine(
                $"[Network] Packet received | SessionId={session.SessionId} | PacketId={packetId}"
            );

            _packetRouter.Route(
                session,
                packetId,
                reader
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[Network] Packet processing error | PeerId={peer.Id} | {ex.Message}"
            );
        }
    }

    public void OnConnectionRequest(
        ConnectionRequest request)
    {
        request.AcceptIfKey(
            "Shaiya2ConnectionKey"
        );
    }

    public void OnNetworkError(
        System.Net.IPEndPoint endPoint,
        System.Net.Sockets.SocketError socketError)
    {
        Console.WriteLine(
            $"[Network] Error | {socketError} | {endPoint}"
        );
    }

    public void OnNetworkLatencyUpdate(
        NetPeer peer,
        int latency)
    {
    }

    public void OnNetworkReceiveUnconnected(
        System.Net.IPEndPoint remoteEndPoint,
        NetPacketReader reader,
        UnconnectedMessageType messageType)
    {
    }
}
using System;
using System.Collections.Generic;
using System.Text;

namespace Shaiya2.Shared.Network.Packets;

public sealed class ServerHelloPacket
{
    public int SessionId { get; set; }

    public string ServerVersion { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}
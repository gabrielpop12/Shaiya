using System;
using System.Collections.Generic;
using System.Text;

namespace Shaiya2.Shared.Network.Packets;

public sealed class ClientHelloPacket
{
    public string ClientVersion { get; set; } = string.Empty;
}
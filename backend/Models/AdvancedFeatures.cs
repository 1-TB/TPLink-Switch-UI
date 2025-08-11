namespace TPLinkWebUI.Models
{
    // Port Mirroring
    public class PortMirrorEnableRequest
    {
        public bool Enabled { get; set; }
        public int MirrorDestinationPort { get; set; }
    }

    public class PortMirrorConfigRequest
    {
        public int[] SourcePorts { get; set; } = Array.Empty<int>();
        public bool IngressEnabled { get; set; }
        public bool EgressEnabled { get; set; }
    }

    // Port Trunking/LAG
    public class PortTrunkRequest
    {
        public int TrunkId { get; set; }
        public int[] MemberPorts { get; set; } = Array.Empty<int>();
    }

    // Loop Prevention
    public class LoopPreventionRequest
    {
        public bool Enabled { get; set; }
    }

    // QoS Mode
    public class QosModeRequest
    {
        public int Mode { get; set; }
    }

    // Bandwidth Control
    public class BandwidthControlRequest
    {
        public int[] Ports { get; set; } = Array.Empty<int>();
        public int IngressRate { get; set; } // Kbps
        public int EgressRate { get; set; }  // Kbps
    }

    // Port Priority
    public class PortPriorityRequest
    {
        public int[] Ports { get; set; } = Array.Empty<int>();
        public int Priority { get; set; } // 0-7
    }

    // Storm Control
    public class StormControlRequest
    {
        public int[] Ports { get; set; } = Array.Empty<int>();
        public int BroadcastRate { get; set; }
        public int MulticastRate { get; set; }
        public int UnicastRate { get; set; }
    }

    // IGMP Snooping
    public class IgmpSnoopingRequest
    {
        public bool Enabled { get; set; }
    }
}
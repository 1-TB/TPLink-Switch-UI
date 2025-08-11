using System.ComponentModel.DataAnnotations;

namespace TPLinkWebUI.Models.History
{
    public class PortStatisticsHistoryEntry
    {
        [Key]
        public int Id { get; set; }
        
        public int PortNumber { get; set; }
        
        public DateTime Timestamp { get; set; }
        
        // Packet statistics
        public long TxGoodPkt { get; set; }
        public long TxBadPkt { get; set; }
        public long RxGoodPkt { get; set; }
        public long RxBadPkt { get; set; }
        
        // Byte statistics
        public long TxBytes { get; set; }
        public long RxBytes { get; set; }
        
        // Calculated metrics
        public double TxErrorRate => TxGoodPkt + TxBadPkt > 0 ? (double)TxBadPkt / (TxGoodPkt + TxBadPkt) * 100 : 0;
        public double RxErrorRate => RxGoodPkt + RxBadPkt > 0 ? (double)RxBadPkt / (RxGoodPkt + RxBadPkt) * 100 : 0;
        public long TotalTxPkts => TxGoodPkt + TxBadPkt;
        public long TotalRxPkts => RxGoodPkt + RxBadPkt;
        
        public string ChangeType { get; set; } = "STATISTICS_UPDATE";
        
        public string? Notes { get; set; }
        
        public static PortStatisticsHistoryEntry FromPortStatistics(int portNumber, PortStatistics stats, string changeType = "STATISTICS_UPDATE")
        {
            return new PortStatisticsHistoryEntry
            {
                PortNumber = portNumber,
                Timestamp = DateTime.UtcNow,
                TxGoodPkt = stats.TxGoodPkt,
                TxBadPkt = stats.TxBadPkt,
                RxGoodPkt = stats.RxGoodPkt,
                RxBadPkt = stats.RxBadPkt,
                TxBytes = stats.TxBytes,
                RxBytes = stats.RxBytes,
                ChangeType = changeType
            };
        }
        
        public string GetFormattedTxBytes() => FormatBytes(TxBytes);
        public string GetFormattedRxBytes() => FormatBytes(RxBytes);
        
        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
    
    public class PortStatistics
    {
        public long TxGoodPkt { get; set; }
        public long TxBadPkt { get; set; }
        public long RxGoodPkt { get; set; }
        public long RxBadPkt { get; set; }
        public long TxBytes { get; set; }
        public long RxBytes { get; set; }
    }
}
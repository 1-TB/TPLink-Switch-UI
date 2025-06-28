using System.ComponentModel.DataAnnotations;

namespace TPLinkWebUI.Models.History
{
    public class VlanHistoryEntry
    {
        [Key]
        public int Id { get; set; }
        
        public int VlanId { get; set; }
        
        public DateTime Timestamp { get; set; }
        
        public string VlanName { get; set; } = string.Empty;
        
        public string PortMembership { get; set; } = string.Empty; // JSON array of port numbers
        
        public string ChangeType { get; set; } = string.Empty; // "VLAN_CREATED", "VLAN_MODIFIED", "VLAN_DELETED", "PORT_ADDED", "PORT_REMOVED"
        
        public string? PreviousValue { get; set; }
        
        public string? NewValue { get; set; }
        
        public string? Notes { get; set; }
        
        public static VlanHistoryEntry FromVlanInfo(VlanInfo vlanInfo, string changeType, string? previousValue = null, string? newValue = null, string? notes = null)
        {
            return new VlanHistoryEntry
            {
                VlanId = vlanInfo.VlanId,
                Timestamp = DateTime.UtcNow,
                VlanName = $"VLAN {vlanInfo.VlanId}",
                PortMembership = System.Text.Json.JsonSerializer.Serialize(vlanInfo.PortNumbers),
                ChangeType = changeType,
                PreviousValue = previousValue,
                NewValue = newValue,
                Notes = notes
            };
        }
    }
}
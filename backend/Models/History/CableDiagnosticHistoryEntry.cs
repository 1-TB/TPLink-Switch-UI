using System.ComponentModel.DataAnnotations;

namespace TPLinkWebUI.Models.History
{
    public class CableDiagnosticHistoryEntry
    {
        [Key]
        public int Id { get; set; }
        
        public int PortNumber { get; set; }
        
        public DateTime Timestamp { get; set; }
        
        public int State { get; set; }
        
        public string StateDescription { get; set; } = string.Empty;
        
        public int Length { get; set; }
        
        public bool IsHealthy { get; set; }
        
        public bool HasIssue { get; set; }
        
        public bool IsUntested { get; set; }
        
        public bool IsDisconnected { get; set; }
        
        public string? Notes { get; set; }
        
        public string TestTrigger { get; set; } = string.Empty; // "MANUAL", "PERIODIC", "STATUS_CHANGE"
        
        public static CableDiagnosticHistoryEntry FromPortDiagnosticInfo(PortDiagnosticInfo diagnostic, string testTrigger, string? notes = null)
        {
            return new CableDiagnosticHistoryEntry
            {
                PortNumber = diagnostic.PortNumber,
                Timestamp = DateTime.UtcNow,
                State = diagnostic.State,
                StateDescription = diagnostic.StateDescription,
                Length = diagnostic.Length,
                IsHealthy = diagnostic.IsHealthy,
                HasIssue = diagnostic.HasIssue,
                IsUntested = diagnostic.IsUntested,
                IsDisconnected = diagnostic.IsDisconnected,
                Notes = notes,
                TestTrigger = testTrigger
            };
        }
    }
}
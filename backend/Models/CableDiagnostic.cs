using TPLinkWebUI.Services;

namespace TPLinkWebUI.Models
{
    public class CableDiagnosticResponse
    {
        public List<PortDiagnosticInfo> Diagnostics { get; set; } = new();
        public int MaxPorts { get; set; }
        public string RawData { get; set; } = string.Empty;

        public static CableDiagnosticResponse FromTplinkResult(CableDiagnosticResult result)
        {
            var response = new CableDiagnosticResponse
            {
                MaxPorts = result.MaxPorts,
                RawData = result.FormatResults()
            };

            var diagnostics = result.GetPortDiagnostics();
            response.Diagnostics = diagnostics.Select(d => new PortDiagnosticInfo
            {
                PortNumber = d.PortNumber,
                State = d.State,
                StateDescription = d.StateDescription,
                Length = d.Length,
                IsHealthy = d.IsHealthy,
                HasIssue = d.HasIssue,
                IsUntested = d.IsUntested,
                IsDisconnected = d.IsDisconnected
            }).ToList();

            return response;
        }
    }

    public class PortDiagnosticInfo
    {
        public int PortNumber { get; set; }
        public int State { get; set; }
        public string StateDescription { get; set; } = string.Empty;
        public int Length { get; set; }
        public bool IsHealthy { get; set; }
        public bool HasIssue { get; set; }
        public bool IsUntested { get; set; }
        public bool IsDisconnected { get; set; }
    }

    public class CableDiagnosticRequest
    {
        public int[] Ports { get; set; } = Array.Empty<int>();
    }

    public class SinglePortDiagnosticRequest
    {
        public int Port { get; set; }
    }
}
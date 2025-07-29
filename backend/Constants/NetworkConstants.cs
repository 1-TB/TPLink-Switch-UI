using System.Text.RegularExpressions;

namespace TPLinkWebUI.Constants
{
    /// <summary>
    /// Network and protocol related constants for TPLink switch communication
    /// </summary>
    public static class NetworkConstants
    {
        /// <summary>
        /// Default HTTP timeout for general operations (seconds)
        /// </summary>
        public const int DefaultTimeoutSeconds = 60;

        /// <summary>
        /// Connection test timeout (seconds)
        /// </summary>
        public const int ConnectionTestTimeoutSeconds = 5;

        /// <summary>
        /// Login verification timeout (seconds)
        /// </summary>
        public const int LoginVerificationTimeoutSeconds = 10;

        /// <summary>
        /// Session cookie expiration duration (hours)
        /// </summary>
        public const int SessionCookieExpirationHours = 1;

        /// <summary>
        /// Maximum number of ports supported by TPLink switches
        /// </summary>
        public const int MaxSupportedPorts = 48;

        /// <summary>
        /// Minimum valid port number
        /// </summary>
        public const int MinPortNumber = 1;

        /// <summary>
        /// Default maximum ports for fallback when parsing fails
        /// </summary>
        public const int DefaultMaxPorts = 24;

        /// <summary>
        /// Maximum port bitmask size for VLAN operations
        /// </summary>
        public const int MaxPortBitmaskSize = 32;
    }

    /// <summary>
    /// VLAN related constants
    /// </summary>
    public static class VlanConstants
    {
        /// <summary>
        /// Minimum valid VLAN ID
        /// </summary>
        public const int MinVlanId = 1;

        /// <summary>
        /// Maximum valid VLAN ID
        /// </summary>
        public const int MaxVlanId = 4094;
    }

    /// <summary>
    /// Regex patterns for parsing switch responses
    /// </summary>
    public static class RegexPatterns
    {
        /// <summary>
        /// Compiled regex for parsing system info array format
        /// </summary>
        public static readonly Regex SystemInfoArrayPattern = new Regex(
            @"var\s+info_ds\s*=\s*new Array\(([^)]*)\);",
            RegexOptions.Compiled);

        /// <summary>
        /// Compiled regex for parsing system info object format
        /// </summary>
        public static readonly Regex SystemInfoObjectPattern = new Regex(
            @"var\s+info_ds\s*=\s*\{([\s\S]*?)\};",
            RegexOptions.Compiled);

        /// <summary>
        /// Compiled regex for parsing port information
        /// </summary>
        public static readonly Regex PortInfoPattern = new Regex(
            @"var all_info = \{([\s\S]*?)\};",
            RegexOptions.Compiled | RegexOptions.Multiline);

        /// <summary>
        /// Compiled regex for parsing max port number
        /// </summary>
        public static readonly Regex MaxPortPattern = new Regex(
            @"var max_port_num = (\d+);",
            RegexOptions.Compiled);

        /// <summary>
        /// Compiled regex for parsing VLAN state
        /// </summary>
        public static readonly Regex VlanStatePattern = new Regex(
            @"state\s*:\s*(\d+)",
            RegexOptions.Compiled);

        /// <summary>
        /// Compiled regex for parsing VLAN port count
        /// </summary>
        public static readonly Regex VlanPortNumPattern = new Regex(
            @"portNum\s*:\s*(\d+)",
            RegexOptions.Compiled);

        /// <summary>
        /// Compiled regex for parsing VLAN count
        /// </summary>
        public static readonly Regex VlanCountPattern = new Regex(
            @"count\s*:\s*(\d+)",
            RegexOptions.Compiled);

        /// <summary>
        /// Compiled regex for parsing VLAN IDs array
        /// </summary>
        public static readonly Regex VlanIdsPattern = new Regex(
            @"vids\s*:\s*\[\s*([^\]]+)\s*\]",
            RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Compiled regex for parsing VLAN member bitmasks
        /// </summary>
        public static readonly Regex VlanMembersPattern = new Regex(
            @"mbrs\s*:\s*\[\s*([^\]]+)\s*\]",
            RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Compiled regex for parsing cable diagnostic state
        /// </summary>
        public static readonly Regex CableStatePattern = new Regex(
            @"var cablestate=\[([^\]]+)\];",
            RegexOptions.Compiled);

        /// <summary>
        /// Compiled regex for parsing cable diagnostic length
        /// </summary>
        public static readonly Regex CableLengthPattern = new Regex(
            @"var cablelength=\[([^\]]+)\];",
            RegexOptions.Compiled);

        /// <summary>
        /// Compiled regex for parsing max port in cable diagnostics
        /// </summary>
        public static readonly Regex CableMaxPortPattern = new Regex(
            @"var maxPort=(\d+);",
            RegexOptions.Compiled);

        /// <summary>
        /// Creates a compiled regex for parsing JavaScript arrays with dynamic property names
        /// </summary>
        /// <param name="arrayName">Name of the JavaScript array property</param>
        /// <returns>Compiled regex pattern</returns>
        public static Regex CreateJsArrayPattern(string arrayName)
        {
            return new Regex($@"{arrayName}:\s*\[([\d,\s]*)\]", RegexOptions.Compiled);
        }
    }

    /// <summary>
    /// Switch endpoint constants
    /// </summary>
    public static class SwitchEndpoints
    {
        public const string Login = "/logon.cgi";
        public const string SystemInfo = "/SystemInfoRpm.htm";
        public const string PortSettings = "/PortSettingRpm.htm";
        public const string PortConfig = "/port_setting.cgi";
        public const string VlanConfig = "/VlanPortBasicRpm.htm";
        public const string VlanSet = "/pvlanSet.cgi";
        public const string CableDiagnostic = "/cable_diag_get.cgi";
        public const string Reboot = "/reboot.cgi";
    }

    /// <summary>
    /// HTTP response patterns for validation
    /// </summary>
    public static class ResponsePatterns
    {
        public const string LoginCheck = "logon.cgi";
        public const string OperationSuccessful = "Operation successful";
    }
}
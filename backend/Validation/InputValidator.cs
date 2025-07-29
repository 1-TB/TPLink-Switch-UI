using System.Text.RegularExpressions;
using TPLinkWebUI.Constants;

namespace TPLinkWebUI.Validation
{
    /// <summary>
    /// Provides input validation methods for switch operations
    /// </summary>
    public static class InputValidator
    {
        /// <summary>
        /// Compiled regex for validating IP addresses
        /// </summary>
        private static readonly Regex IpAddressPattern = new Regex(
            @"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$",
            RegexOptions.Compiled);

        /// <summary>
        /// Compiled regex for validating hostnames
        /// </summary>
        private static readonly Regex HostnamePattern = new Regex(
            @"^[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?(\.[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?)*$",
            RegexOptions.Compiled);

        /// <summary>
        /// Compiled regex for validating usernames (alphanumeric and common special chars)
        /// </summary>
        private static readonly Regex UsernamePattern = new Regex(
            @"^[a-zA-Z0-9._@-]{1,64}$",
            RegexOptions.Compiled);

        /// <summary>
        /// Validates if a string is a valid IP address or hostname
        /// </summary>
        /// <param name="host">Host to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidHost(string? host)
        {
            if (string.IsNullOrWhiteSpace(host))
                return false;

            // Check if it's a valid IP address
            if (IpAddressPattern.IsMatch(host))
                return true;

            // Check if it's a valid hostname
            if (host.Length <= 253 && HostnamePattern.IsMatch(host))
                return true;

            return false;
        }

        /// <summary>
        /// Validates if a username contains only allowed characters
        /// </summary>
        /// <param name="username">Username to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidUsername(string? username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            return UsernamePattern.IsMatch(username);
        }

        /// <summary>
        /// Validates if a password meets basic security requirements
        /// </summary>
        /// <param name="password">Password to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidPassword(string? password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            // Basic validation - not empty, reasonable length
            return password.Length >= 1 && password.Length <= 128;
        }

        /// <summary>
        /// Validates if a port number is within the valid range
        /// </summary>
        /// <param name="port">Port number to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidPortNumber(int port)
        {
            return port >= NetworkConstants.MinPortNumber && port <= NetworkConstants.MaxSupportedPorts;
        }

        /// <summary>
        /// Validates if a VLAN ID is within the valid range
        /// </summary>
        /// <param name="vlanId">VLAN ID to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidVlanId(int vlanId)
        {
            return vlanId >= VlanConstants.MinVlanId && vlanId <= VlanConstants.MaxVlanId;
        }

        /// <summary>
        /// Validates if an array of port numbers are all valid
        /// </summary>
        /// <param name="ports">Array of port numbers</param>
        /// <returns>True if all ports are valid, false otherwise</returns>
        public static bool AreValidPortNumbers(int[]? ports)
        {
            if (ports == null || ports.Length == 0)
                return false;

            return ports.All(IsValidPortNumber);
        }

        /// <summary>
        /// Validates if an array of VLAN IDs are all valid
        /// </summary>
        /// <param name="vlanIds">Array of VLAN IDs</param>
        /// <returns>True if all VLAN IDs are valid, false otherwise</returns>
        public static bool AreValidVlanIds(int[]? vlanIds)
        {
            if (vlanIds == null || vlanIds.Length == 0)
                return false;

            return vlanIds.All(IsValidVlanId);
        }

        /// <summary>
        /// Sanitizes a string by removing potentially dangerous characters
        /// </summary>
        /// <param name="input">Input string to sanitize</param>
        /// <returns>Sanitized string</returns>
        public static string SanitizeString(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Remove control characters and other potentially dangerous chars
            return Regex.Replace(input, @"[\x00-\x1F\x7F<>""'&\\]", "", RegexOptions.Compiled);
        }

        /// <summary>
        /// Validates and sanitizes login request data
        /// </summary>
        /// <param name="host">Host address</param>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns>Validation result with error message if invalid</returns>
        public static ValidationResult ValidateLoginRequest(string? host, string? username, string? password)
        {
            if (!IsValidHost(host))
            {
                return new ValidationResult(false, "Invalid host address. Must be a valid IP address or hostname.");
            }

            if (!IsValidUsername(username))
            {
                return new ValidationResult(false, "Invalid username. Only alphanumeric characters and ._@- are allowed (max 64 chars).");
            }

            if (!IsValidPassword(password))
            {
                return new ValidationResult(false, "Invalid password. Password must be between 1 and 128 characters.");
            }

            return new ValidationResult(true, "Valid");
        }

        /// <summary>
        /// Validates port configuration request
        /// </summary>
        /// <param name="port">Port number</param>
        /// <param name="speed">Speed setting</param>
        /// <returns>Validation result</returns>
        public static ValidationResult ValidatePortConfigRequest(int port, int speed)
        {
            if (!IsValidPortNumber(port))
            {
                return new ValidationResult(false, $"Invalid port number {port}. Must be between {NetworkConstants.MinPortNumber} and {NetworkConstants.MaxSupportedPorts}.");
            }

            if (speed < 0 || speed > 10)
            {
                return new ValidationResult(false, "Invalid speed setting. Must be between 0 and 10.");
            }

            return new ValidationResult(true, "Valid");
        }

        /// <summary>
        /// Validates VLAN creation request
        /// </summary>
        /// <param name="vlanId">VLAN ID</param>
        /// <param name="ports">Array of port numbers</param>
        /// <returns>Validation result</returns>
        public static ValidationResult ValidateVlanRequest(int vlanId, int[]? ports)
        {
            if (!IsValidVlanId(vlanId))
            {
                return new ValidationResult(false, $"Invalid VLAN ID {vlanId}. Must be between {VlanConstants.MinVlanId} and {VlanConstants.MaxVlanId}.");
            }

            if (!AreValidPortNumbers(ports))
            {
                return new ValidationResult(false, $"Invalid port numbers. All ports must be between {NetworkConstants.MinPortNumber} and {NetworkConstants.MaxSupportedPorts}.");
            }

            return new ValidationResult(true, "Valid");
        }
    }

    /// <summary>
    /// Represents the result of an input validation operation
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Whether the validation passed
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Error message if validation failed
        /// </summary>
        public string Message { get; }

        public ValidationResult(bool isValid, string message)
        {
            IsValid = isValid;
            Message = message;
        }
    }
}
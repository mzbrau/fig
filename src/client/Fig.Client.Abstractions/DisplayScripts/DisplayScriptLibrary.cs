namespace Fig.Client.Abstractions.DisplayScripts;

/// <summary>
/// A library of predefined display scripts that can be used with the <see cref="Fig.Client.Abstractions.Attributes.DisplayScriptAttribute"/>.
/// All scripts use the <c>{{this}}</c> placeholder which is automatically replaced with the setting name
/// at registration time, making them reusable across any setting.
/// </summary>
/// <example>
/// <code>
/// [Setting("Server port")]
/// [DisplayScript(DisplayScriptLibrary.ValidatePort)]
/// public int Port { get; set; } = 8080;
/// </code>
/// </example>
public static class DisplayScriptLibrary
{
    /// <summary>
    /// Converts a millisecond value to a human-readable time string displayed as InformationText.
    /// Supports days, hours, minutes, seconds, and milliseconds.
    /// Applies to: numeric settings (int, long, double).
    /// </summary>
    public const string MillisecondsToHumanReadableTime = @"
var ms = {{this}}.Value;
if (ms == null || ms < 0) {
    {{this}}.InformationText = null;
} else if (ms === 0) {
    {{this}}.InformationText = '0 milliseconds';
} else {
    var parts = [];
    var days = Math.floor(ms / 86400000);
    if (days > 0) parts.push(days + (days === 1 ? ' day' : ' days'));
    var hours = Math.floor((ms % 86400000) / 3600000);
    if (hours > 0) parts.push(hours + (hours === 1 ? ' hour' : ' hours'));
    var mins = Math.floor((ms % 3600000) / 60000);
    if (mins > 0) parts.push(mins + (mins === 1 ? ' minute' : ' minutes'));
    var secs = Math.floor((ms % 60000) / 1000);
    if (secs > 0) parts.push(secs + (secs === 1 ? ' second' : ' seconds'));
    var remainingMs = ms % 1000;
    if (remainingMs > 0) parts.push(remainingMs + ' ms');
    {{this}}.InformationText = parts.length > 0 ? '= ' + parts.join(' ') : null;
}
";

    /// <summary>
    /// Converts a seconds value to a human-readable time string displayed as InformationText.
    /// Supports days, hours, minutes, and seconds.
    /// Applies to: numeric settings (int, long, double).
    /// </summary>
    public const string SecondsToHumanReadableTime = @"
var totalSeconds = {{this}}.Value;
if (totalSeconds == null || totalSeconds < 0) {
    {{this}}.InformationText = null;
} else if (totalSeconds === 0) {
    {{this}}.InformationText = '0 seconds';
} else {
    var parts = [];
    var days = Math.floor(totalSeconds / 86400);
    if (days > 0) parts.push(days + (days === 1 ? ' day' : ' days'));
    var hours = Math.floor((totalSeconds % 86400) / 3600);
    if (hours > 0) parts.push(hours + (hours === 1 ? ' hour' : ' hours'));
    var mins = Math.floor((totalSeconds % 3600) / 60);
    if (mins > 0) parts.push(mins + (mins === 1 ? ' minute' : ' minutes'));
    var secs = totalSeconds % 60;
    if (secs > 0) parts.push(secs + (secs === 1 ? ' second' : ' seconds'));
    {{this}}.InformationText = parts.length > 0 ? '= ' + parts.join(' ') : null;
}
";

    /// <summary>
    /// Validates that the value is a valid IPv4 address (four octets, each 0-255).
    /// Sets IsValid to false with a ValidationExplanation on failure.
    /// Applies to: string settings.
    /// </summary>
    public const string ValidateIpAddress = @"
var ip = {{this}}.Value;
if (!ip || typeof ip !== 'string' || ip.trim() === '') {
    {{this}}.IsValid = false;
    {{this}}.ValidationExplanation = 'IP address is required';
} else {
    var parts = ip.trim().split('.');
    if (parts.length !== 4) {
        {{this}}.IsValid = false;
        {{this}}.ValidationExplanation = 'IP address must have exactly 4 octets (e.g. 192.168.1.1)';
    } else {
        var valid = true;
        for (var i = 0; i < 4; i++) {
            var octet = parts[i];
            if (!/^\d{1,3}$/.test(octet)) { valid = false; break; }
            var num = parseInt(octet, 10);
            if (num < 0 || num > 255) { valid = false; break; }
            if (octet.length > 1 && octet[0] === '0') { valid = false; break; }
        }
        if (valid) {
            {{this}}.IsValid = true;
            {{this}}.ValidationExplanation = '';
        } else {
            {{this}}.IsValid = false;
            {{this}}.ValidationExplanation = 'Each octet must be a number between 0 and 255 with no leading zeros';
        }
    }
}
";

    /// <summary>
    /// Validates that the value is a valid port number (integer between 1 and 65535).
    /// Sets IsValid to false with a ValidationExplanation on failure.
    /// Applies to: numeric settings (int, long).
    /// </summary>
    public const string ValidatePort = @"
var port = {{this}}.Value;
if (port == null) {
    {{this}}.IsValid = false;
    {{this}}.ValidationExplanation = 'Port number is required';
} else if (typeof port !== 'number' || !Number.isInteger(port)) {
    {{this}}.IsValid = false;
    {{this}}.ValidationExplanation = 'Port must be a whole number';
} else if (port < 1 || port > 65535) {
    {{this}}.IsValid = false;
    {{this}}.ValidationExplanation = 'Port must be between 1 and 65535';
} else {
    {{this}}.IsValid = true;
    {{this}}.ValidationExplanation = '';
}
";

    /// <summary>
    /// Validates that the value is a valid Windows filename.
    /// Checks for illegal characters (&lt;&gt;:"/\|?*), reserved names (CON, PRN, NUL, etc.),
    /// trailing dots/spaces, and empty values.
    /// Sets IsValid to false with a ValidationExplanation on failure.
    /// Applies to: string settings.
    /// </summary>
    public const string ValidateWindowsFilename = @"
var filename = {{this}}.Value;
if (!filename || typeof filename !== 'string' || filename.trim() === '') {
    {{this}}.IsValid = false;
    {{this}}.ValidationExplanation = 'Filename is required';
} else if (/[<>:""/\\|?*]/.test(filename)) {
    {{this}}.IsValid = false;
    {{this}}.ValidationExplanation = 'Filename contains illegal characters: < > : "" / \\ | ? *';
} else if (/[\. ]$/.test(filename)) {
    {{this}}.IsValid = false;
    {{this}}.ValidationExplanation = 'Filename must not end with a dot or space';
} else {
    var reserved = ['CON','PRN','AUX','NUL','COM1','COM2','COM3','COM4','COM5','COM6','COM7','COM8','COM9','LPT1','LPT2','LPT3','LPT4','LPT5','LPT6','LPT7','LPT8','LPT9'];
    var nameWithoutExt = filename.split('.')[0].toUpperCase();
    if (reserved.indexOf(nameWithoutExt) >= 0) {
        {{this}}.IsValid = false;
        {{this}}.ValidationExplanation = 'Filename uses a reserved Windows name: ' + nameWithoutExt;
    } else if (filename.length > 255) {
        {{this}}.IsValid = false;
        {{this}}.ValidationExplanation = 'Filename must not exceed 255 characters';
    } else {
        {{this}}.IsValid = true;
        {{this}}.ValidationExplanation = '';
    }
}
";

    /// <summary>
    /// Validates that the value is a valid Linux filename.
    /// Checks for forward slash, null bytes, and maximum length of 255 characters.
    /// Sets IsValid to false with a ValidationExplanation on failure.
    /// Applies to: string settings.
    /// </summary>
    public const string ValidateLinuxFilename = @"
var filename = {{this}}.Value;
if (!filename || typeof filename !== 'string' || filename.trim() === '') {
    {{this}}.IsValid = false;
    {{this}}.ValidationExplanation = 'Filename is required';
} else if (filename.indexOf('/') >= 0) {
    {{this}}.IsValid = false;
    {{this}}.ValidationExplanation = 'Filename must not contain forward slash (/)';
} else if (filename.indexOf('\0') >= 0) {
    {{this}}.IsValid = false;
    {{this}}.ValidationExplanation = 'Filename must not contain null bytes';
} else if (filename.length > 255) {
    {{this}}.IsValid = false;
    {{this}}.ValidationExplanation = 'Filename must not exceed 255 characters';
} else {
    {{this}}.IsValid = true;
    {{this}}.ValidationExplanation = '';
}
";

    /// <summary>
    /// Validates that the value is a valid URL starting with http:// or https://.
    /// Sets IsValid to false with a ValidationExplanation on failure.
    /// Applies to: string settings.
    /// </summary>
    public const string ValidateUrl = @"
var url = {{this}}.Value;
if (!url || typeof url !== 'string' || url.trim() === '') {
    {{this}}.IsValid = false;
    {{this}}.ValidationExplanation = 'URL is required';
} else {
    var pattern = /^https?:\/\/[^\s/$.?#].[^\s]*$/i;
    if (pattern.test(url.trim())) {
        {{this}}.IsValid = true;
        {{this}}.ValidationExplanation = '';
    } else {
        {{this}}.IsValid = false;
        {{this}}.ValidationExplanation = 'URL must start with http:// or https:// and be well-formed';
    }
}
";

    /// <summary>
    /// Validates that the value is a valid email address format.
    /// Uses a practical regex that covers common email patterns.
    /// Sets IsValid to false with a ValidationExplanation on failure.
    /// Applies to: string settings.
    /// </summary>
    public const string ValidateEmail = @"
var email = {{this}}.Value;
if (!email || typeof email !== 'string' || email.trim() === '') {
    {{this}}.IsValid = false;
    {{this}}.ValidationExplanation = 'Email address is required';
} else {
    var pattern = /^[a-zA-Z0-9.!#$%&'*+\/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$/;
    if (pattern.test(email.trim())) {
        {{this}}.IsValid = true;
        {{this}}.ValidationExplanation = '';
    } else {
        {{this}}.IsValid = false;
        {{this}}.ValidationExplanation = 'Invalid email address format';
    }
}
";

    /// <summary>
    /// Validates that the value is valid JSON by attempting to parse it.
    /// Sets IsValid to false with a ValidationExplanation on failure.
    /// Applies to: string settings (typically with MultiLine).
    /// </summary>
    public const string ValidateJson = @"
var jsonStr = {{this}}.Value;
if (!jsonStr || typeof jsonStr !== 'string' || jsonStr.trim() === '') {
    {{this}}.IsValid = false;
    {{this}}.ValidationExplanation = 'JSON string is required';
} else {
    try {
        JSON.parse(jsonStr);
        {{this}}.IsValid = true;
        {{this}}.ValidationExplanation = '';
    } catch (e) {
        {{this}}.IsValid = false;
        {{this}}.ValidationExplanation = 'Invalid JSON: ' + e.message;
    }
}
";

    /// <summary>
    /// Converts a byte count to a human-readable size string displayed as InformationText.
    /// Supports bytes, KB, MB, GB, TB, and PB with up to 2 decimal places.
    /// Applies to: numeric settings (int, long, double).
    /// </summary>
    public const string BytesToHumanReadableSize = @"
var bytes = {{this}}.Value;
if (bytes == null || bytes < 0) {
    {{this}}.InformationText = null;
} else if (bytes === 0) {
    {{this}}.InformationText = '0 bytes';
} else {
    var units = ['bytes', 'KB', 'MB', 'GB', 'TB', 'PB'];
    var unitIndex = 0;
    var size = bytes;
    while (size >= 1024 && unitIndex < units.length - 1) {
        size = size / 1024;
        unitIndex++;
    }
    var display = unitIndex === 0 ? size.toString() : size.toFixed(2).replace(/\.?0+$/, '');
    var unit = units[unitIndex];
    if (unitIndex === 0 && display === '1') unit = 'byte';
    {{this}}.InformationText = '= ' + display + ' ' + unit;
}
";

    /// <summary>
    /// Validates that the value is a valid RFC-1123 hostname.
    /// Labels must be 1-63 characters (alphanumeric and hyphens, not starting/ending with hyphen).
    /// Total length must not exceed 253 characters.
    /// Sets IsValid to false with a ValidationExplanation on failure.
    /// Applies to: string settings.
    /// </summary>
    public const string ValidateHostname = @"
var hostname = {{this}}.Value;
if (!hostname || typeof hostname !== 'string' || hostname.trim() === '') {
    {{this}}.IsValid = false;
    {{this}}.ValidationExplanation = 'Hostname is required';
} else {
    hostname = hostname.trim();
    if (hostname.length > 253) {
        {{this}}.IsValid = false;
        {{this}}.ValidationExplanation = 'Hostname must not exceed 253 characters';
    } else {
        var labels = hostname.split('.');
        var valid = true;
        var reason = '';
        for (var i = 0; i < labels.length; i++) {
            var label = labels[i];
            if (label.length === 0 || label.length > 63) {
                valid = false;
                reason = 'Each label must be 1-63 characters long';
                break;
            }
            if (!/^[a-zA-Z0-9]([a-zA-Z0-9-]*[a-zA-Z0-9])?$/.test(label)) {
                valid = false;
                reason = 'Labels must contain only alphanumeric characters and hyphens, and must not start or end with a hyphen';
                break;
            }
        }
        if (valid) {
            {{this}}.IsValid = true;
            {{this}}.ValidationExplanation = '';
        } else {
            {{this}}.IsValid = false;
            {{this}}.ValidationExplanation = reason;
        }
    }
}
";

    /// <summary>
    /// Validates that the value is valid CIDR notation (e.g., 192.168.1.0/24).
    /// Validates the IPv4 address and that the prefix length is between 0 and 32.
    /// Sets IsValid to false with a ValidationExplanation on failure.
    /// Applies to: string settings.
    /// </summary>
    public const string ValidateCidr = @"
var cidr = {{this}}.Value;
if (!cidr || typeof cidr !== 'string' || cidr.trim() === '') {
    {{this}}.IsValid = false;
    {{this}}.ValidationExplanation = 'CIDR notation is required';
} else {
    cidr = cidr.trim();
    var slashIndex = cidr.indexOf('/');
    if (slashIndex < 0) {
        {{this}}.IsValid = false;
        {{this}}.ValidationExplanation = 'CIDR must contain a / separator (e.g. 192.168.1.0/24)';
    } else {
        var ipPart = cidr.substring(0, slashIndex);
        var prefixPart = cidr.substring(slashIndex + 1);
        var ipParts = ipPart.split('.');
        var ipValid = ipParts.length === 4;
        if (ipValid) {
            for (var i = 0; i < 4; i++) {
                var octet = ipParts[i];
                if (!/^\d{1,3}$/.test(octet)) { ipValid = false; break; }
                var num = parseInt(octet, 10);
                if (num < 0 || num > 255) { ipValid = false; break; }
            }
        }
        if (!ipValid) {
            {{this}}.IsValid = false;
            {{this}}.ValidationExplanation = 'IP address portion is not a valid IPv4 address';
        } else if (!/^\d{1,2}$/.test(prefixPart)) {
            {{this}}.IsValid = false;
            {{this}}.ValidationExplanation = 'Prefix length must be a number between 0 and 32';
        } else {
            var prefix = parseInt(prefixPart, 10);
            if (prefix < 0 || prefix > 32) {
                {{this}}.IsValid = false;
                {{this}}.ValidationExplanation = 'Prefix length must be between 0 and 32';
            } else {
                {{this}}.IsValid = true;
                {{this}}.ValidationExplanation = '';
            }
        }
    }
}
";
}

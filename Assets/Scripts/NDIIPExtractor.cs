using System.Text.RegularExpressions;
using UnityEngine;

public static class NDIIPExtractor
{
    /// <summary>
    /// Extracts IP address from NDI source name
    /// Example: "CAM 1 (HX-Stream-192.168.1.10)" -> "192.168.1.10"
    /// </summary>
    public static string ExtractIPFromNDISource(string ndiSourceName)
    {
        if (string.IsNullOrEmpty(ndiSourceName))
        {
            Debug.LogWarning("[NDI IP Extractor] NDI source name is null or empty");
            return null;
        }

        // Regex pattern to match IP addresses (xxx.xxx.xxx.xxx)
        string ipPattern = @"(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})";
        var match = Regex.Match(ndiSourceName, ipPattern);
        
        if (match.Success)
        {
            string ip = match.Groups[1].Value;
            Debug.Log($"[NDI IP Extractor] Extracted IP '{ip}' from NDI source '{ndiSourceName}'");
            return ip;
        }
        
        Debug.LogWarning($"[NDI IP Extractor] Could not extract IP from NDI source: '{ndiSourceName}'");
        return null;
    }

    /// <summary>
    /// Validates if the extracted IP address is valid
    /// </summary>
    public static bool IsValidIPAddress(string ip)
    {
        if (string.IsNullOrEmpty(ip))
            return false;

        string[] parts = ip.Split('.');
        if (parts.Length != 4)
            return false;

        foreach (string part in parts)
        {
            if (!int.TryParse(part, out int value) || value < 0 || value > 255)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Extracts camera number from NDI source name
    /// Example: "CAM 1 (HX-Stream-192.168.1.10)" -> 1
    /// </summary>
    public static int ExtractCameraNumberFromNDISource(string ndiSourceName)
    {
        if (string.IsNullOrEmpty(ndiSourceName))
            return -1;

        // Look for "CAM X" pattern
        var match = Regex.Match(ndiSourceName, @"CAM\s+(\d+)", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int cameraNumber))
        {
            return cameraNumber;
        }

        // Fallback: look for any number at the beginning
        match = Regex.Match(ndiSourceName, @"^(\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out cameraNumber))
        {
            return cameraNumber;
        }

        Debug.LogWarning($"[NDI IP Extractor] Could not extract camera number from: '{ndiSourceName}'");
        return -1;
    }

    /// <summary>
    /// Creates camera info from NDI source name
    /// </summary>
    public static NDICameraInfo CreateCameraInfoFromNDISource(string ndiSourceName, int fallbackCameraNumber = -1)
    {
        var info = new NDICameraInfo
        {
            NDISourceName = ndiSourceName,
            IPAddress = ExtractIPFromNDISource(ndiSourceName),
            CameraNumber = ExtractCameraNumberFromNDISource(ndiSourceName)
        };

        // Use fallback camera number if extraction failed
        if (info.CameraNumber <= 0 && fallbackCameraNumber > 0)
        {
            info.CameraNumber = fallbackCameraNumber;
            Debug.Log($"[NDI IP Extractor] Using fallback camera number {fallbackCameraNumber} for '{ndiSourceName}'");
        }

        // Validate the extracted data
        info.IsValid = !string.IsNullOrEmpty(info.IPAddress) && 
                      IsValidIPAddress(info.IPAddress) && 
                      info.CameraNumber > 0;

        return info;
    }
}

[System.Serializable]
public class NDICameraInfo
{
    public string NDISourceName;
    public string IPAddress;
    public int CameraNumber;
    public bool IsValid;

    public override string ToString()
    {
        return $"Camera {CameraNumber}: {IPAddress} (NDI: {NDISourceName}) - Valid: {IsValid}";
    }
}
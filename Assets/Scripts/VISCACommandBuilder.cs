using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class VISCACommandBuilder
{
    // Build PTZ movement command
    public static byte[] BuildPTZCommand(int cameraAddress, VISCAProtocol.PTZCommand command, 
        int panSpeed = 5, int tiltSpeed = 5, int panPosition = 0, int tiltPosition = 0)
    {
        if (!VISCAProtocol.IsValidCameraAddress(cameraAddress))
        {
            Debug.LogError($"Invalid camera address: {cameraAddress}");
            return null;
        }

        if (!VISCAProtocol.IsValidPanSpeed(panSpeed) || !VISCAProtocol.IsValidTiltSpeed(tiltSpeed))
        {
            Debug.LogError($"Invalid speed values: pan={panSpeed}, tilt={tiltSpeed}");
            return null;
        }

        string pattern = VISCAProtocol.PTZCommands[command];
        
        // Replace placeholders
        pattern = pattern.Replace("8x", $"8{cameraAddress}");
        pattern = pattern.Replace("VV", $"{panSpeed:X2}");
        pattern = pattern.Replace("WW", $"{tiltSpeed:X2}");
        
        // Handle position commands
        if (command == VISCAProtocol.PTZCommand.AbsolutePosition || 
            command == VISCAProtocol.PTZCommand.RelativePosition)
        {
            if (!VISCAProtocol.IsValidPanPosition(panPosition) || 
                !VISCAProtocol.IsValidTiltPosition(tiltPosition))
            {
                Debug.LogError($"Invalid position values: pan={panPosition}, tilt={tiltPosition}");
                return null;
            }
            
            // Convert positions to VISCA format (4 nibbles each)
            var panBytes = ConvertToVISCAPosition(panPosition);
            var tiltBytes = ConvertToVISCAPosition(tiltPosition);
            
            pattern = pattern.Replace("0Y 0Y 0Y 0Y", $"0{panBytes[0]:X} 0{panBytes[1]:X} 0{panBytes[2]:X} 0{panBytes[3]:X}");
            pattern = pattern.Replace("0Z 0Z 0Z 0Z", $"0{tiltBytes[0]:X} 0{tiltBytes[1]:X} 0{tiltBytes[2]:X} 0{tiltBytes[3]:X}");
        }
        
        return ConvertHexStringToBytes(pattern);
    }

    // Build zoom command
    public static byte[] BuildZoomCommand(int cameraAddress, VISCAProtocol.ZoomCommand command, 
        int speed = 3, int position = 0)
    {
        if (!VISCAProtocol.IsValidCameraAddress(cameraAddress))
        {
            Debug.LogError($"Invalid camera address: {cameraAddress}");
            return null;
        }

        string pattern = VISCAProtocol.ZoomCommands[command];
        pattern = pattern.Replace("8x", $"8{cameraAddress}");
        
        switch (command)
        {
            case VISCAProtocol.ZoomCommand.TeleVariable:
            case VISCAProtocol.ZoomCommand.WideVariable:
                if (!VISCAProtocol.IsValidZoomSpeed(speed))
                {
                    Debug.LogError($"Invalid zoom speed: {speed}");
                    return null;
                }
                pattern = pattern.Replace("p", $"{speed:X}");
                break;
                
            case VISCAProtocol.ZoomCommand.Direct:
                if (!VISCAProtocol.IsValidZoomPosition(position))
                {
                    Debug.LogError($"Invalid zoom position: {position}");
                    return null;
                }
                var posBytes = ConvertToVISCAPosition(position);
                pattern = pattern.Replace("0p 0q 0r 0s", $"0{posBytes[0]:X} 0{posBytes[1]:X} 0{posBytes[2]:X} 0{posBytes[3]:X}");
                break;
                
            case VISCAProtocol.ZoomCommand.DirectWithSpeed:
                if (!VISCAProtocol.IsValidZoomSpeed(speed) || !VISCAProtocol.IsValidZoomPosition(position))
                {
                    Debug.LogError($"Invalid zoom parameters: speed={speed}, position={position}");
                    return null;
                }
                pattern = pattern.Replace("0t", $"0{speed:X}");
                var zoomPosBytes = ConvertToVISCAPosition(position);
                pattern = pattern.Replace("0p 0q 0r 0s", $"0{zoomPosBytes[0]:X} 0{zoomPosBytes[1]:X} 0{zoomPosBytes[2]:X} 0{zoomPosBytes[3]:X}");
                break;
        }
        
        return ConvertHexStringToBytes(pattern);
    }

    // Build focus command
    public static byte[] BuildFocusCommand(int cameraAddress, VISCAProtocol.FocusCommand command, 
        int speed = 3, int position = 0)
    {
        if (!VISCAProtocol.IsValidCameraAddress(cameraAddress))
        {
            Debug.LogError($"Invalid camera address: {cameraAddress}");
            return null;
        }

        string pattern = VISCAProtocol.FocusCommands[command];
        pattern = pattern.Replace("8x", $"8{cameraAddress}");
        pattern = pattern.Replace("81", $"8{cameraAddress}"); // Handle fixed camera address patterns
        
        switch (command)
        {
            case VISCAProtocol.FocusCommand.FarVariable:
            case VISCAProtocol.FocusCommand.NearVariable:
                if (!VISCAProtocol.IsValidFocusSpeed(speed))
                {
                    Debug.LogError($"Invalid focus speed: {speed}");
                    return null;
                }
                pattern = pattern.Replace("p", $"{speed:X}");
                break;
                
            case VISCAProtocol.FocusCommand.Direct:
                if (!VISCAProtocol.IsValidFocusPosition(position))
                {
                    Debug.LogError($"Invalid focus position: {position}");
                    return null;
                }
                var posBytes = ConvertToVISCAPosition(position);
                pattern = pattern.Replace("0p 0q 0r 0s", $"0{posBytes[0]:X} 0{posBytes[1]:X} 0{posBytes[2]:X} 0{posBytes[3]:X}");
                break;
        }
        
        return ConvertHexStringToBytes(pattern);
    }

    // Build preset command
    public static byte[] BuildPresetCommand(int cameraAddress, VISCAProtocol.PresetCommand command, 
        int presetNumber)
    {
        if (!VISCAProtocol.IsValidCameraAddress(cameraAddress))
        {
            Debug.LogError($"Invalid camera address: {cameraAddress}");
            return null;
        }

        if (!VISCAProtocol.IsValidPresetNumber(presetNumber))
        {
            Debug.LogError($"Invalid preset number: {presetNumber}");
            return null;
        }

        string pattern = VISCAProtocol.PresetCommands[command];
        pattern = pattern.Replace("8x", $"8{cameraAddress}");
        pattern = pattern.Replace("0p", $"0{presetNumber:X}");
        
        return ConvertHexStringToBytes(pattern);
    }

    // Build inquiry command
    public static byte[] BuildInquiryCommand(int cameraAddress, string inquiryType)
    {
        if (!VISCAProtocol.IsValidCameraAddress(cameraAddress))
        {
            Debug.LogError($"Invalid camera address: {cameraAddress}");
            return null;
        }

        string pattern = "";
        
        switch (inquiryType.ToLower())
        {
            case "power":
                pattern = "8x 09 04 00 FF";
                break;
            case "zoom":
                pattern = "8x 09 04 47 FF";
                break;
            case "focus":
                pattern = "8x 09 04 48 FF";
                break;
            case "pan_tilt":
                pattern = "8x 09 06 12 FF";
                break;
            default:
                Debug.LogError($"Unknown inquiry type: {inquiryType}");
                return null;
        }
        
        pattern = pattern.Replace("8x", $"8{cameraAddress}");
        return ConvertHexStringToBytes(pattern);
    }

    // Convert integer position to VISCA 4-nibble format
    private static byte[] ConvertToVISCAPosition(int position)
    {
        // Handle negative positions (two's complement for pan/tilt)
        uint unsignedPos = (uint)(position < 0 ? position + 65536 : position);
        
        return new byte[]
        {
            (byte)((unsignedPos >> 12) & 0x0F),
            (byte)((unsignedPos >> 8) & 0x0F),
            (byte)((unsignedPos >> 4) & 0x0F),
            (byte)(unsignedPos & 0x0F)
        };
    }

    // Convert VISCA 4-nibble format back to integer
    public static int ConvertFromVISCAPosition(byte[] nibbles)
    {
        if (nibbles.Length != 4)
        {
            Debug.LogError("VISCA position must be 4 nibbles");
            return 0;
        }

        int position = (nibbles[0] << 12) | (nibbles[1] << 8) | (nibbles[2] << 4) | nibbles[3];
        
        // Handle signed positions (convert from unsigned if needed)
        if (position > 32767)
        {
            position -= 65536;
        }
        
        return position;
    }

    // Convert hex string pattern to byte array
    private static byte[] ConvertHexStringToBytes(string hexString)
    {
        try
        {
            // Split by spaces to get individual hex values
            var hexValues = hexString.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var bytes = new List<byte>();
            
            foreach (var hex in hexValues)
            {
                if (!string.IsNullOrWhiteSpace(hex))
                {
                    // Convert each hex string to byte (e.g., "82" -> 0x82)
                    byte value = Convert.ToByte(hex, 16);
                    bytes.Add(value);
                }
            }
            
            return bytes.ToArray();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to convert hex string to bytes: '{hexString}', Error: {ex.Message}");
            return null;
        }
    }

    // Convert byte array to hex string for debugging
    public static string BytesToHexString(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return "";
            
        return string.Join(" ", bytes.Select(b => b.ToString("X2")));
    }

    // Validate VISCA packet structure
    public static bool ValidatePacket(byte[] packet)
    {
        if (packet == null || packet.Length < 3)
        {
            Debug.LogError("VISCA packet too short");
            return false;
        }

        // Check start byte (8x format)
        if ((packet[0] & 0xF0) != 0x80)
        {
            Debug.LogError($"Invalid VISCA start byte: {packet[0]:X2}");
            return false;
        }

        // Check end byte (must be FF)
        if (packet[packet.Length - 1] != 0xFF)
        {
            Debug.LogError($"Invalid VISCA end byte: {packet[packet.Length - 1]:X2}");
            return false;
        }

        return true;
    }

    // Calculate checksum (if required by camera)
    public static byte CalculateChecksum(byte[] packet)
    {
        byte checksum = 0;
        for (int i = 0; i < packet.Length - 1; i++) // Exclude FF terminator
        {
            checksum ^= packet[i];
        }
        return checksum;
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;

public static class VISCAProtocol
{
    public enum CommandPriority
    {
        PTZMovement = 1,        // Highest priority
        PTZPosition = 2,
        ZoomMovement = 5,
        ZoomPosition = 6,
        ZoomControl = 7,
        FocusAuto = 8,
        FocusPosition = 9,
        FocusControl = 10,
        CameraControl = 11,
        ImageControl = 12,
        SystemResponse = 13,
        Other = 14              // Lowest priority
    }

    public enum PTZCommand
    {
        Stop,
        Up,
        Down,
        Left,
        Right,
        UpLeft,
        UpRight,
        DownLeft,
        DownRight,
        Home,
        Reset,
        AbsolutePosition,
        RelativePosition
    }

    public enum ZoomCommand
    {
        TeleStandard,
        TeleVariable,
        WideStandard,
        WideVariable,
        Stop,
        Direct,
        DirectWithSpeed
    }

    public enum FocusCommand
    {
        Auto,
        Manual,
        OnePushAF,
        FarStandard,
        FarVariable,
        NearStandard,
        NearVariable,
        Direct,
        Stop
    }

    public enum PresetCommand
    {
        Set,
        Recall,
        Reset
    }

    public struct VISCACommandData
    {
        public string FunctionType;
        public string Category;
        public string Action;
        public string HexCommand;
        public string Description;
        public string Parameters;
        public CommandPriority Priority;
    }

    // VISCA command patterns with parameter placeholders
    public static readonly Dictionary<PTZCommand, string> PTZCommands = new Dictionary<PTZCommand, string>
    {
        { PTZCommand.Stop, "8x 01 06 01 VV WW 03 03 FF" },
        { PTZCommand.Up, "8x 01 06 01 VV WW 03 01 FF" },
        { PTZCommand.Down, "8x 01 06 01 VV WW 03 02 FF" },
        { PTZCommand.Left, "8x 01 06 01 VV WW 01 03 FF" },
        { PTZCommand.Right, "8x 01 06 01 VV WW 02 03 FF" },
        { PTZCommand.UpLeft, "8x 01 06 01 VV WW 01 01 FF" },
        { PTZCommand.UpRight, "8x 01 06 01 VV WW 02 01 FF" },
        { PTZCommand.DownLeft, "8x 01 06 01 VV WW 01 02 FF" },
        { PTZCommand.DownRight, "8x 01 06 01 VV WW 02 02 FF" },
        { PTZCommand.Home, "8x 01 06 04 FF" },
        { PTZCommand.Reset, "8x 01 06 05 FF" },
        { PTZCommand.AbsolutePosition, "8x 01 06 02 VV WW 0Y 0Y 0Y 0Y 0Z 0Z 0Z 0Z FF" },
        { PTZCommand.RelativePosition, "8x 01 06 03 VV WW 0Y 0Y 0Y 0Y 0Z 0Z 0Z 0Z FF" }
    };

    public static readonly Dictionary<ZoomCommand, string> ZoomCommands = new Dictionary<ZoomCommand, string>
    {
        { ZoomCommand.TeleStandard, "8x 01 04 07 02 FF" },
        { ZoomCommand.TeleVariable, "8x 01 04 07 2p FF" },
        { ZoomCommand.WideStandard, "8x 01 04 07 03 FF" },
        { ZoomCommand.WideVariable, "8x 01 04 07 3p FF" },
        { ZoomCommand.Stop, "8x 01 04 07 00 FF" },
        { ZoomCommand.Direct, "8x 01 04 47 0p 0q 0r 0s FF" },
        { ZoomCommand.DirectWithSpeed, "8x 0A 04 47 0t 0p 0q 0r 0s FF" }
    };

    public static readonly Dictionary<FocusCommand, string> FocusCommands = new Dictionary<FocusCommand, string>
    {
        { FocusCommand.Auto, "81 01 04 38 02 FF" },
        { FocusCommand.Manual, "81 01 04 38 03 FF" },
        { FocusCommand.OnePushAF, "8x 01 04 18 01 FF" },
        { FocusCommand.FarStandard, "8x 01 04 08 02 FF" },
        { FocusCommand.FarVariable, "81 01 04 08 2p FF" },
        { FocusCommand.NearStandard, "8x 01 04 08 03 FF" },
        { FocusCommand.NearVariable, "81 01 04 08 3p FF" },
        { FocusCommand.Direct, "8x 01 04 48 0p 0q 0r 0s FF" },
        { FocusCommand.Stop, "8x 01 04 08 00 FF" }
    };

    public static readonly Dictionary<PresetCommand, string> PresetCommands = new Dictionary<PresetCommand, string>
    {
        { PresetCommand.Set, "8x 01 04 3F 01 0p FF" },
        { PresetCommand.Recall, "8x 01 04 3F 02 0p FF" },
        { PresetCommand.Reset, "8x 01 04 3F 00 0p FF" }
    };

    // Common parameter ranges
    public const int MIN_PAN_TILT_SPEED = 0x01;
    public const int MAX_PAN_TILT_SPEED = 0x18; // 24 decimal
    public const int MIN_ZOOM_SPEED = 0x0;
    public const int MAX_ZOOM_SPEED = 0x7;
    public const int MIN_FOCUS_SPEED = 0x0;
    public const int MAX_FOCUS_SPEED = 0x7;
    public const int MIN_PRESET_NUMBER = 0x0;
    public const int MAX_PRESET_NUMBER = 0xF; // 15 presets

    // Camera address range
    public const int MIN_CAMERA_ADDRESS = 1;
    public const int MAX_CAMERA_ADDRESS = 7;

    // Pan/Tilt position ranges (varies by camera model)
    public const int MIN_PAN_POSITION = -2448; // Typical range
    public const int MAX_PAN_POSITION = 2448;
    public const int MIN_TILT_POSITION = -432;
    public const int MAX_TILT_POSITION = 1296;

    // Zoom position range
    public const int MIN_ZOOM_POSITION = 0x0;
    public const int MAX_ZOOM_POSITION = 0x4000;

    // Focus position range
    public const int MIN_FOCUS_POSITION = 0x1000;
    public const int MAX_FOCUS_POSITION = 0xF000;

    // Response codes
    public const byte ACK_CODE = 0x40;
    public const byte COMPLETION_CODE = 0x50;
    public const byte ERROR_CODE = 0x60;

    // Error types
    public enum VISCAError
    {
        None = 0x00,
        MessageLength = 0x01,
        Syntax = 0x02,
        CommandBuffer = 0x03,
        CommandCancelled = 0x04,
        NoSocket = 0x05,
        CommandNotExecutable = 0x41
    }

    // Helper methods for validation
    public static bool IsValidCameraAddress(int address)
    {
        return address >= MIN_CAMERA_ADDRESS && address <= MAX_CAMERA_ADDRESS;
    }

    public static bool IsValidPanSpeed(int speed)
    {
        return speed >= MIN_PAN_TILT_SPEED && speed <= MAX_PAN_TILT_SPEED;
    }

    public static bool IsValidTiltSpeed(int speed)
    {
        return speed >= MIN_PAN_TILT_SPEED && speed <= MAX_PAN_TILT_SPEED;
    }

    public static bool IsValidZoomSpeed(int speed)
    {
        return speed >= MIN_ZOOM_SPEED && speed <= MAX_ZOOM_SPEED;
    }

    public static bool IsValidFocusSpeed(int speed)
    {
        return speed >= MIN_FOCUS_SPEED && speed <= MAX_FOCUS_SPEED;
    }

    public static bool IsValidPresetNumber(int presetNumber)
    {
        return presetNumber >= MIN_PRESET_NUMBER && presetNumber <= MAX_PRESET_NUMBER;
    }

    public static bool IsValidPanPosition(int position)
    {
        return position >= MIN_PAN_POSITION && position <= MAX_PAN_POSITION;
    }

    public static bool IsValidTiltPosition(int position)
    {
        return position >= MIN_TILT_POSITION && position <= MAX_TILT_POSITION;
    }

    public static bool IsValidZoomPosition(int position)
    {
        return position >= MIN_ZOOM_POSITION && position <= MAX_ZOOM_POSITION;
    }

    public static bool IsValidFocusPosition(int position)
    {
        return position >= MIN_FOCUS_POSITION && position <= MAX_FOCUS_POSITION;
    }
}

// Data class for camera configuration
[System.Serializable]
public class VISCACameraConfig
{
    public int CameraAddress = 1;
    public string IPAddress = "192.168.1.100";
    public int Port = 52381; // Default VISCA over IP port
    public bool UseSerial = false;
    public string SerialPort = "COM1";
    public int BaudRate = 9600;
    
    [Header("Camera Limits")]
    public int MaxPanSpeed = VISCAProtocol.MAX_PAN_TILT_SPEED;
    public int MaxTiltSpeed = VISCAProtocol.MAX_PAN_TILT_SPEED;
    public int MaxZoomSpeed = VISCAProtocol.MAX_ZOOM_SPEED;
    public int MaxFocusSpeed = VISCAProtocol.MAX_FOCUS_SPEED;
    
    [Header("Position Ranges")]
    public int MinPanPosition = VISCAProtocol.MIN_PAN_POSITION;
    public int MaxPanPosition = VISCAProtocol.MAX_PAN_POSITION;
    public int MinTiltPosition = VISCAProtocol.MIN_TILT_POSITION;
    public int MaxTiltPosition = VISCAProtocol.MAX_TILT_POSITION;
}
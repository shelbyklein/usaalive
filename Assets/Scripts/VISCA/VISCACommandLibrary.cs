using System;
using System.Collections.Generic;

namespace USAALive.VISCA
{
    public static class VISCACommandLibrary
    {
        private static readonly Dictionary<string, VISCACommand> _commands = new Dictionary<string, VISCACommand>();

        static VISCACommandLibrary()
        {
            InitializeCommands();
        }

        private static void InitializeCommands()
        {
            InitializePTZCommands();
            InitializeZoomCommands();
            InitializeFocusCommands();
            InitializeImageControlCommands();
            InitializeCameraControlCommands();
            InitializeSystemCommands();
        }

        #region PTZ Commands (Priority 1-2)

        private static void InitializePTZCommands()
        {
            // PTZ Movement Commands (Priority 1)
            AddCommand("PTZ_Up", VISCAFunctionType.PTZMovement, VISCACommandPriority.PTZMovement,
                "Pan_TiltDrive", "Up", "Move camera up",
                "8x 01 06 01 VV WW 03 01 FF");

            AddCommand("PTZ_Down", VISCAFunctionType.PTZMovement, VISCACommandPriority.PTZMovement,
                "Pan_TiltDrive", "Down", "Move camera down",
                "8x 01 06 01 VV WW 03 02 FF");

            AddCommand("PTZ_Left", VISCAFunctionType.PTZMovement, VISCACommandPriority.PTZMovement,
                "Pan_TiltDrive", "Left", "Move camera left",
                "8x 01 06 01 VV WW 01 03 FF");

            AddCommand("PTZ_Right", VISCAFunctionType.PTZMovement, VISCACommandPriority.PTZMovement,
                "Pan_TiltDrive", "Right", "Move camera right",
                "8x 01 06 01 VV WW 02 03 FF");

            AddCommand("PTZ_UpLeft", VISCAFunctionType.PTZMovement, VISCACommandPriority.PTZMovement,
                "Pan_TiltDrive", "Upleft", "Move camera up-left",
                "8x 01 06 01 VV WW 01 01 FF");

            AddCommand("PTZ_UpRight", VISCAFunctionType.PTZMovement, VISCACommandPriority.PTZMovement,
                "Pan_TiltDrive", "Upright", "Move camera up-right",
                "8x 01 06 01 VV WW 02 01 FF");

            AddCommand("PTZ_DownLeft", VISCAFunctionType.PTZMovement, VISCACommandPriority.PTZMovement,
                "Pan_TiltDrive", "Downleft", "Move camera down-left",
                "8x 01 06 01 VV WW 01 02 FF");

            AddCommand("PTZ_DownRight", VISCAFunctionType.PTZMovement, VISCACommandPriority.PTZMovement,
                "Pan_TiltDrive", "Downright", "Move camera down-right",
                "8x 01 06 01 VV WW 02 02 FF");

            AddCommand("PTZ_Stop", VISCAFunctionType.PTZMovement, VISCACommandPriority.PTZMovement,
                "Pan_TiltDrive", "Stop", "Stop camera movement",
                "8x 01 06 01 VV WW 03 03 FF");

            AddCommand("PTZ_Home", VISCAFunctionType.PTZMovement, VISCACommandPriority.PTZMovement,
                "Pan_TiltDrive", "Home", "Move camera to home position",
                "8x 01 06 04 FF");

            AddCommand("PTZ_Reset", VISCAFunctionType.PTZMovement, VISCACommandPriority.PTZMovement,
                "Pan_TiltDrive", "Reset", "Reset camera position",
                "8x 01 06 05 FF");

            AddCommand("PTZ_AbsolutePosition", VISCAFunctionType.PTZMovement, VISCACommandPriority.PTZMovement,
                "Pan_TiltDrive", "Absolute Position", "Move to absolute pan/tilt position",
                "8x 01 06 02 VV WW 0Y 0Y 0Y 0Y 0Z 0Z 0Z 0Z FF");

            AddCommand("PTZ_RelativePosition", VISCAFunctionType.PTZMovement, VISCACommandPriority.PTZMovement,
                "Pan_TiltDrive", "Relative Position", "Move relative to current position",
                "8x 01 06 03 VV WW 0Y 0Y 0Y 0Y 0Z 0Z 0Z 0Z FF");

            // PTZ Position Commands (Priority 2)
            AddCommand("PTZ_LimitClear", VISCAFunctionType.PTZPosition, VISCACommandPriority.PTZPosition,
                "Pan Tilt_LimitSet", "Clear", "Clear pan/tilt limits",
                "8x 01 06 07 01 0W 07 0F 0F 0F 07 0F 0F 0F FF");
        }

        #endregion

        #region Zoom Commands (Priority 5-7)

        private static void InitializeZoomCommands()
        {
            // Zoom Movement Commands (Priority 5)
            AddCommand("Zoom_TeleStandard", VISCAFunctionType.ZoomMovement, VISCACommandPriority.ZoomMovement,
                "CAM_Zoom", "Tele(Standard)", "Zoom in at standard speed",
                "8x 01 04 07 02 FF");

            AddCommand("Zoom_TeleVariable", VISCAFunctionType.ZoomMovement, VISCACommandPriority.ZoomMovement,
                "CAM_Zoom", "Tele(Variable)", "Zoom in at variable speed",
                "8x 01 04 07 2p FF");

            AddCommand("Zoom_WideStandard", VISCAFunctionType.ZoomMovement, VISCACommandPriority.ZoomMovement,
                "CAM_Zoom", "Wide(Standard)", "Zoom out at standard speed",
                "8x 01 04 07 03 FF");

            AddCommand("Zoom_WideVariable", VISCAFunctionType.ZoomMovement, VISCACommandPriority.ZoomMovement,
                "CAM_Zoom", "Wide(Variable)", "Zoom out at variable speed",
                "8x 01 04 07 3p FF");

            AddCommand("Zoom_Stop", VISCAFunctionType.ZoomMovement, VISCACommandPriority.ZoomMovement,
                "CAM_Zoom", "Stop", "Stop zoom movement",
                "8x 01 04 07 00 FF");

            // Digital Zoom
            AddCommand("DZoom_TeleVariable", VISCAFunctionType.ZoomMovement, VISCACommandPriority.ZoomMovement,
                "CAM_DZoom", "Tele(Variable)", "Digital zoom in",
                "81 01 04 06 2p FF");

            AddCommand("DZoom_WideVariable", VISCAFunctionType.ZoomMovement, VISCACommandPriority.ZoomMovement,
                "CAM_DZoom", "Wide(Variable)", "Digital zoom out",
                "81 01 04 06 3p FF");

            // Zoom Position Commands (Priority 6)
            AddCommand("Zoom_Direct", VISCAFunctionType.ZoomPosition, VISCACommandPriority.ZoomPosition,
                "CAM_Zoom", "Direct", "Set direct zoom position",
                "8x 01 04 47 0p 0q 0r 0s FF");

            AddCommand("Zoom_DirectWithSpeed", VISCAFunctionType.ZoomPosition, VISCACommandPriority.ZoomPosition,
                "CAM_Zoom", "Direct with speed", "Set zoom position with speed",
                "8x 0A 04 47 0t 0p 0q 0r 0s FF");

            AddCommand("DZoom_Direct", VISCAFunctionType.ZoomPosition, VISCACommandPriority.ZoomPosition,
                "CAM_DZoom", "Direct", "Set direct digital zoom position",
                "81 01 04 46 0p 0q 0r 0s FF");

            // Zoom Control Commands (Priority 7)
            AddCommand("Zoom_SeparateMode", VISCAFunctionType.ZoomControl, VISCACommandPriority.ZoomControl,
                "CAM_Zoom", "Separate Mode", "Enable separate zoom mode",
                "81 01 04 36 01 FF");

            AddCommand("SpeedByZoom_Off", VISCAFunctionType.ZoomControl, VISCACommandPriority.ZoomControl,
                "CAM_SpeedByZoom", "Off", "Disable speed by zoom",
                "8x 01 06 A0 03 FF");

            AddCommand("ZoomDisplay_Off", VISCAFunctionType.ZoomControl, VISCACommandPriority.ZoomControl,
                "CAM_ZoomDisplay", "Off", "Turn off zoom display",
                "8x 01 06 C2 03 FF");
        }

        #endregion

        #region Focus Commands (Priority 8-10)

        private static void InitializeFocusCommands()
        {
            // Focus Auto Commands (Priority 8)
            AddCommand("Focus_Auto", VISCAFunctionType.FocusAuto, VISCACommandPriority.FocusAuto,
                "CAM_Focus", "Auto Focus", "Enable auto focus",
                "81 01 04 38 02 FF");

            // Focus Position Commands (Priority 9)
            AddCommand("Focus_Direct", VISCAFunctionType.FocusPosition, VISCACommandPriority.FocusPosition,
                "CAM_Focus", "Direct", "Set direct focus position",
                "8x 01 04 48 0p 0q 0r 0s FF");

            // Focus Control Commands (Priority 10)
            AddCommand("Focus_Manual", VISCAFunctionType.FocusControl, VISCACommandPriority.FocusControl,
                "CAM_Focus", "Manual Focus", "Enable manual focus",
                "81 01 04 38 03 FF");

            AddCommand("Focus_NearStandard", VISCAFunctionType.FocusControl, VISCACommandPriority.FocusControl,
                "CAM_Focus", "Near(Standard)", "Focus near at standard speed",
                "8x 01 04 08 03 FF");

            AddCommand("Focus_NearVariable", VISCAFunctionType.FocusControl, VISCACommandPriority.FocusControl,
                "CAM_Focus", "Near (Variable)", "Focus near at variable speed",
                "81 01 04 08 3p FF");

            AddCommand("Focus_FarStandard", VISCAFunctionType.FocusControl, VISCACommandPriority.FocusControl,
                "CAM_Focus", "Far(Standard)", "Focus far at standard speed",
                "8x 01 04 08 02 FF");

            AddCommand("Focus_FarVariable", VISCAFunctionType.FocusControl, VISCACommandPriority.FocusControl,
                "CAM_Focus", "Far(Variable)", "Focus far at variable speed",
                "81 01 04 08 2p FF");

            AddCommand("Focus_OnePushAF", VISCAFunctionType.FocusControl, VISCACommandPriority.FocusControl,
                "CAM_Focus", "One Push AF", "One push auto focus",
                "8x 01 04 18 01 FF");

            AddCommand("Focus_Stop", VISCAFunctionType.FocusControl, VISCACommandPriority.FocusControl,
                "CAM_Focus", "Stop", "Stop focus movement",
                "8x 01 04 08 00 FF");
        }

        #endregion

        #region Image Control Commands (Priority 12)

        private static void InitializeImageControlCommands()
        {
            // Auto Exposure Modes
            AddCommand("AE_Manual", VISCAFunctionType.ImageControl, VISCACommandPriority.ImageControl,
                "CAM_AE", "Manual", "Manual exposure control",
                "81 01 04 39 03 FF");

            AddCommand("AE_ShutterPriority", VISCAFunctionType.ImageControl, VISCACommandPriority.ImageControl,
                "CAM_AE", "Shutter Priority", "Shutter priority auto exposure",
                "81 01 04 39 0A FF");

            AddCommand("AE_IrisPriority", VISCAFunctionType.ImageControl, VISCACommandPriority.ImageControl,
                "CAM_AE", "Iris Priority", "Iris priority auto exposure",
                "81 01 04 39 0B FF");

            AddCommand("AE_Bright", VISCAFunctionType.ImageControl, VISCACommandPriority.ImageControl,
                "CAM_AE", "Bright", "Bright mode manual control",
                "81 01 04 39 0D FF");

            // White Balance
            AddCommand("WB_Indoor", VISCAFunctionType.ImageControl, VISCACommandPriority.ImageControl,
                "CAM_WB", "Indoor", "Indoor white balance",
                "8x 01 04 35 01 FF");

            AddCommand("WB_Outdoor", VISCAFunctionType.ImageControl, VISCACommandPriority.ImageControl,
                "CAM_WB", "Outdoor", "Outdoor white balance",
                "8x 01 04 35 02 FF");

            AddCommand("WB_OnePush", VISCAFunctionType.ImageControl, VISCACommandPriority.ImageControl,
                "CAM_WB", "One Push", "One push white balance",
                "8x 01 04 35 03 FF");

            AddCommand("WB_Auto", VISCAFunctionType.ImageControl, VISCACommandPriority.ImageControl,
                "CAM_WB", "Auto", "Auto tracking white balance",
                "8x 01 04 35 05 FF");

            AddCommand("WB_Manual", VISCAFunctionType.ImageControl, VISCACommandPriority.ImageControl,
                "CAM_WB", "Manual", "Manual white balance",
                "8x 01 04 35 07 FF");

            // Iris Control
            AddCommand("Iris_Up", VISCAFunctionType.ImageControl, VISCACommandPriority.ImageControl,
                "CAM_Iris", "Up", "Open iris",
                "8x 01 04 0B 02 FF");

            AddCommand("Iris_Down", VISCAFunctionType.ImageControl, VISCACommandPriority.ImageControl,
                "CAM_Iris", "Down", "Close iris",
                "8x 01 04 0B 03 FF");

            AddCommand("Iris_Direct", VISCAFunctionType.ImageControl, VISCACommandPriority.ImageControl,
                "CAM_Iris", "Direct", "Set direct iris value",
                "8x 01 04 4B 00 00 0p 0q FF");

            // Shutter Control
            AddCommand("Shutter_Up", VISCAFunctionType.ImageControl, VISCACommandPriority.ImageControl,
                "CAM_Shutter", "Up", "Increase shutter speed",
                "8x 01 04 0A 02 FF");

            AddCommand("Shutter_Down", VISCAFunctionType.ImageControl, VISCACommandPriority.ImageControl,
                "CAM_Shutter", "Down", "Decrease shutter speed",
                "8x 01 04 0A 03 FF");

            AddCommand("Shutter_Direct", VISCAFunctionType.ImageControl, VISCACommandPriority.ImageControl,
                "CAM_Shutter", "Direct", "Set direct shutter value",
                "8x 01 04 4A 00 00 0p 0q FF");

            // Gain Control
            AddCommand("Gain_Up", VISCAFunctionType.ImageControl, VISCACommandPriority.ImageControl,
                "CAM_Gain", "Up", "Increase gain",
                "8x 01 04 0C 02 FF");

            AddCommand("Gain_Down", VISCAFunctionType.ImageControl, VISCACommandPriority.ImageControl,
                "CAM_Gain", "Down", "Decrease gain",
                "8x 01 04 0C 03 FF");

            AddCommand("Gain_Direct", VISCAFunctionType.ImageControl, VISCACommandPriority.ImageControl,
                "CAM_Gain", "Direct", "Set direct gain value",
                "8x 01 04 4C 00 00 0p 0q FF");

            // Color Control
            AddCommand("RGain_Up", VISCAFunctionType.ImageControl, VISCACommandPriority.ImageControl,
                "CAM_RGain", "Up", "Increase red gain",
                "8x 01 04 03 02 FF");

            AddCommand("RGain_Down", VISCAFunctionType.ImageControl, VISCACommandPriority.ImageControl,
                "CAM_RGain", "Down", "Decrease red gain",
                "8x 01 04 03 03 FF");

            AddCommand("RGain_Direct", VISCAFunctionType.ImageControl, VISCACommandPriority.ImageControl,
                "CAM_RGain", "Direct", "Set direct red gain value",
                "8x 01 04 43 00 00 0p 0q FF");

            AddCommand("BGain_Up", VISCAFunctionType.ImageControl, VISCACommandPriority.ImageControl,
                "CAM_BGain", "Up", "Increase blue gain",
                "8x 01 04 04 02 FF");

            AddCommand("BGain_Down", VISCAFunctionType.ImageControl, VISCACommandPriority.ImageControl,
                "CAM_BGain", "Down", "Decrease blue gain",
                "8x 01 04 04 03 FF");

            AddCommand("BGain_Direct", VISCAFunctionType.ImageControl, VISCACommandPriority.ImageControl,
                "CAM_BGain", "Direct", "Set direct blue gain value",
                "8x 01 04 44 00 00 0p 0q FF");

            // Brightness Control
            AddCommand("Bright_Up", VISCAFunctionType.ImageControl, VISCACommandPriority.ImageControl,
                "CAM_Bright", "Up", "Increase brightness",
                "8x 01 04 0D 02 FF");

            AddCommand("Bright_Down", VISCAFunctionType.ImageControl, VISCACommandPriority.ImageControl,
                "CAM_Bright", "Down", "Decrease brightness",
                "8x 01 04 0D 03 FF");

            AddCommand("Bright_Direct", VISCAFunctionType.ImageControl, VISCACommandPriority.ImageControl,
                "CAM_Bright", "Direct", "Set direct brightness value",
                "8x 01 04 4D 00 00 0p 0q FF");
        }

        #endregion

        #region Camera Control Commands (Priority 11)

        private static void InitializeCameraControlCommands()
        {
            // Power Control
            AddCommand("Power_On", VISCAFunctionType.CameraControl, VISCACommandPriority.CameraControl,
                "CAM_Power", "On", "Turn camera power on",
                "8x 01 04 00 02 FF");

            AddCommand("Power_Off", VISCAFunctionType.CameraControl, VISCACommandPriority.CameraControl,
                "CAM_Power", "Off", "Turn camera power off",
                "8x 01 04 00 03 FF");

            // Memory Presets
            AddCommand("Memory_Set", VISCAFunctionType.CameraControl, VISCACommandPriority.CameraControl,
                "CAM_Memory (preset)", "Set", "Set memory preset",
                "8x 01 04 3F 01 0p FF");

            AddCommand("Memory_Recall", VISCAFunctionType.CameraControl, VISCACommandPriority.CameraControl,
                "CAM_Memory (preset)", "Recall", "Recall memory preset",
                "8x 01 04 3F 02 0p FF");

            AddCommand("Memory_Reset", VISCAFunctionType.CameraControl, VISCACommandPriority.CameraControl,
                "CAM_Memory (preset)", "Reset", "Reset memory preset",
                "8x 01 04 3F 00 0p FF");
        }

        #endregion

        #region System Commands (Priority 13-14)

        private static void InitializeSystemCommands()
        {
            // Inquiry Commands
            AddCommand("PowerInq", VISCAFunctionType.SystemResponse, VISCACommandPriority.SystemResponse,
                "CAM_PowerInq", "", "Inquire power status",
                "8x 09 04 00 FF");

            AddCommand("ZoomPosInq", VISCAFunctionType.SystemResponse, VISCACommandPriority.SystemResponse,
                "CAM_ZoomPosInq", "", "Inquire zoom position",
                "8x 09 04 47 FF");

            AddCommand("FocusPosInq", VISCAFunctionType.SystemResponse, VISCACommandPriority.SystemResponse,
                "CAM_FocusPosInq", "", "Inquire focus position",
                "8x 09 04 48 FF");

            AddCommand("PTZPosInq", VISCAFunctionType.SystemResponse, VISCACommandPriority.SystemResponse,
                "CAM_PTZPosInq", "", "Inquire pan/tilt position",
                "8x 09 06 12 FF");

            AddCommand("AEModeInq", VISCAFunctionType.SystemResponse, VISCACommandPriority.SystemResponse,
                "CAM_AEModeInq", "", "Inquire auto exposure mode",
                "8x 09 04 39 FF");

            AddCommand("FocusModeInq", VISCAFunctionType.SystemResponse, VISCACommandPriority.SystemResponse,
                "CAM_Focus ModeInq", "", "Inquire focus mode",
                "8x 09 04 38 FF");

            AddCommand("WBModeInq", VISCAFunctionType.SystemResponse, VISCACommandPriority.SystemResponse,
                "CAM_WBModeInq", "", "Inquire white balance mode",
                "8x 09 04 35 FF");
        }

        #endregion

        #region Helper Methods

        private static void AddCommand(string key, VISCAFunctionType functionType, VISCACommandPriority priority,
                                     string category, string action, string description, string hexCommand)
        {
            var command = VISCACommand.CreateFromHex(hexCommand, functionType, priority, category, action, description);
            _commands[key] = command;
        }

        public static VISCACommand GetCommand(string commandKey)
        {
            return _commands.TryGetValue(commandKey, out var command) ? command : null;
        }

        public static IEnumerable<string> GetAllCommandKeys()
        {
            return _commands.Keys;
        }

        public static IEnumerable<VISCACommand> GetCommandsByPriority(VISCACommandPriority priority)
        {
            foreach (var command in _commands.Values)
            {
                if (command.Priority == priority)
                    yield return command;
            }
        }

        public static IEnumerable<VISCACommand> GetCommandsByFunctionType(VISCAFunctionType functionType)
        {
            foreach (var command in _commands.Values)
            {
                if (command.FunctionType == functionType)
                    yield return command;
            }
        }

        public static bool HasCommand(string commandKey)
        {
            return _commands.ContainsKey(commandKey);
        }

        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

namespace USAALive.VISCA
{
    public enum VISCACommandPriority
    {
        PTZMovement = 1,
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
        Other = 14
    }

    public enum VISCAFunctionType
    {
        PTZMovement,
        PTZPosition,
        ZoomMovement,
        ZoomPosition,
        ZoomControl,
        FocusAuto,
        FocusPosition,
        FocusControl,
        CameraControl,
        ImageControl,
        SystemResponse,
        Other
    }

    public enum VISCAResponseType
    {
        ACK,
        Completion,
        Error,
        Data
    }

    [Serializable]
    public class VISCACommand
    {
        public VISCAFunctionType FunctionType { get; private set; }
        public VISCACommandPriority Priority { get; private set; }
        public string Category { get; private set; }
        public string Action { get; private set; }
        public string Description { get; private set; }
        public byte[] CommandBytes { get; private set; }
        public string HexCommand { get; private set; }
        public Dictionary<string, object> Parameters { get; private set; }

        public VISCACommand(VISCAFunctionType functionType, VISCACommandPriority priority, 
                           string category, string action, string description, 
                           byte[] commandBytes, Dictionary<string, object> parameters = null)
        {
            FunctionType = functionType;
            Priority = priority;
            Category = category;
            Action = action;
            Description = description;
            CommandBytes = commandBytes;
            HexCommand = BitConverter.ToString(commandBytes).Replace("-", " ");
            Parameters = parameters ?? new Dictionary<string, object>();
        }

        public static VISCACommand CreateFromHex(string hexCommand, VISCAFunctionType functionType, 
                                               VISCACommandPriority priority, string category, 
                                               string action, string description, 
                                               Dictionary<string, object> parameters = null)
        {
            var commandBytes = ParseHexCommand(hexCommand);
            return new VISCACommand(functionType, priority, category, action, description, commandBytes, parameters);
        }

        public static byte[] ParseHexCommand(string hexCommand)
        {
            hexCommand = hexCommand.Replace(" ", "").Replace("x", "");
            var bytes = new List<byte>();
            
            for (int i = 0; i < hexCommand.Length; i += 2)
            {
                if (i + 1 < hexCommand.Length)
                {
                    var hexByte = hexCommand.Substring(i, 2);
                    
                    if (hexByte == "8x" || hexByte == "81")
                    {
                        bytes.Add(0x81);
                    }
                    else if (hexByte == "FF")
                    {
                        bytes.Add(0xFF);
                    }
                    else if (hexByte.All(c => "0123456789ABCDEF".Contains(char.ToUpper(c))))
                    {
                        bytes.Add(Convert.ToByte(hexByte, 16));
                    }
                }
            }
            
            return bytes.ToArray();
        }

        public byte[] BuildCommand(byte cameraAddress = 0x81, Dictionary<string, object> runtimeParameters = null)
        {
            var command = new List<byte>(CommandBytes);
            
            if (command.Count > 0 && (command[0] == 0x81 || command[0] == 0x8F))
            {
                command[0] = cameraAddress;
            }
            
            if (runtimeParameters != null)
            {
                ApplyParameters(command, runtimeParameters);
            }
            
            return command.ToArray();
        }

        private void ApplyParameters(List<byte> command, Dictionary<string, object> parameters)
        {
            foreach (var param in parameters)
            {
                switch (param.Key.ToLower())
                {
                    case "speed":
                    case "panspeed":
                    case "vv":
                        if (param.Value is byte speed && speed <= 0x18)
                        {
                            ReplaceParameterInCommand(command, "VV", speed);
                        }
                        break;
                        
                    case "tiltspeed":
                    case "ww":
                        if (param.Value is byte tiltSpeed && tiltSpeed <= 0x17)
                        {
                            ReplaceParameterInCommand(command, "WW", tiltSpeed);
                        }
                        break;
                        
                    case "zoomspeed":
                    case "p":
                        if (param.Value is byte zoomSpeed && zoomSpeed <= 0x07)
                        {
                            ReplaceParameterInCommand(command, "p", zoomSpeed);
                        }
                        break;
                        
                    case "panposition":
                        if (param.Value is ushort panPos)
                        {
                            var bytes = BitConverter.GetBytes(panPos);
                            if (BitConverter.IsLittleEndian)
                                Array.Reverse(bytes);
                            ReplaceParameterInCommand(command, "YYYY", bytes);
                        }
                        break;
                        
                    case "tiltposition":
                        if (param.Value is ushort tiltPos)
                        {
                            var bytes = BitConverter.GetBytes(tiltPos);
                            if (BitConverter.IsLittleEndian)
                                Array.Reverse(bytes);
                            ReplaceParameterInCommand(command, "ZZZZ", bytes);
                        }
                        break;
                }
            }
        }

        private void ReplaceParameterInCommand(List<byte> command, string parameter, byte value)
        {
            for (int i = 0; i < command.Count; i++)
            {
                if (command[i] == 0x00 && parameter.Length == 1)
                {
                    command[i] = value;
                    break;
                }
            }
        }

        private void ReplaceParameterInCommand(List<byte> command, string parameter, byte[] values)
        {
            int paramIndex = 0;
            for (int i = 0; i < command.Count && paramIndex < values.Length; i++)
            {
                if (command[i] == 0x00)
                {
                    command[i] = values[paramIndex++];
                }
            }
        }

        public override string ToString()
        {
            return $"{FunctionType} - {Action}: {BitConverter.ToString(CommandBytes)} (Priority: {Priority})";
        }
    }

    [Serializable]
    public class VISCAResponse
    {
        public VISCAResponseType Type { get; private set; }
        public byte[] ResponseBytes { get; private set; }
        public string HexResponse { get; private set; }
        public bool IsError { get; private set; }
        public string ErrorMessage { get; private set; }
        public Dictionary<string, object> Data { get; private set; }

        public VISCAResponse(byte[] responseBytes)
        {
            ResponseBytes = responseBytes;
            HexResponse = BitConverter.ToString(responseBytes).Replace("-", " ");
            Data = new Dictionary<string, object>();
            ParseResponse();
        }

        private void ParseResponse()
        {
            if (ResponseBytes.Length < 3)
            {
                Type = VISCAResponseType.Error;
                IsError = true;
                ErrorMessage = "Invalid response length";
                return;
            }

            var header = ResponseBytes[0];
            var command = ResponseBytes[1];
            var subCommand = ResponseBytes[2];

            if ((header & 0xF0) == 0x90)
            {
                if (command == 0x41)
                {
                    Type = VISCAResponseType.ACK;
                }
                else if (command == 0x51)
                {
                    Type = VISCAResponseType.Completion;
                }
                else if (command == 0x60)
                {
                    Type = VISCAResponseType.Error;
                    IsError = true;
                    ErrorMessage = GetErrorMessage(subCommand);
                }
            }
            else if ((header & 0xF0) == 0x50)
            {
                Type = VISCAResponseType.Data;
                ParseDataResponse();
            }
            else
            {
                Type = VISCAResponseType.Error;
                IsError = true;
                ErrorMessage = "Unknown response format";
            }
        }

        private string GetErrorMessage(byte errorCode)
        {
            return errorCode switch
            {
                0x01 => "Message length error",
                0x02 => "Syntax error",
                0x03 => "Command buffer full",
                0x04 => "Command cancelled",
                0x05 => "No socket (to be cancelled)",
                0x41 => "Command not executable",
                _ => $"Unknown error code: 0x{errorCode:X2}"
            };
        }

        private void ParseDataResponse()
        {
            if (ResponseBytes.Length >= 4)
            {
                var dataBytes = new byte[ResponseBytes.Length - 3];
                Array.Copy(ResponseBytes, 2, dataBytes, 0, dataBytes.Length);
                Data["raw"] = dataBytes;
            }
        }

        public override string ToString()
        {
            return IsError ? $"Error: {ErrorMessage}" : $"{Type}: {HexResponse}";
        }
    }
}
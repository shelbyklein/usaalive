# Technology Scope

## Core Technologies

### Unity Platform
- **Version**: 2022.3.15f1 LTS
- **Target Platforms**: Windows, macOS, Linux
- **Build Pipeline**: Unity Editor built-in
- **Scripting Backend**: Mono/.NET Standard 2.1

### UI Framework
- **Primary**: UI Toolkit (Unity's modern UI system)
- **Layout**: UXML documents for structure
- **Styling**: USS stylesheets for theming
- **Logic**: C# with VisualElement manipulation
- **Responsive Design**: Flex-based layouts

### Input System
- **Package**: Unity Input System 1.7.0
- **Architecture**: Input Action Assets with Action Maps
- **Device Support**: 
  - Gamepad controllers (Xbox, PlayStation, generic)
  - Keyboard input
  - Custom controller hardware
- **Features**: Variable speed control, input buffering, device switching

### Video Integration
- **NDI Support**: KlakNDI plugin 2.1.4
- **Video Protocols**: NDI (Network Device Interface)
- **Streaming**: Real-time video over IP networks
- **Integration**: Embedded video feed in controller UI

### Communication Protocols
- **Primary**: VISCA over TCP/IP
- **Command Structure**: Hex-based packets (8x address + command + FF)
- **Transport**: TCP sockets for reliable delivery
- **Error Handling**: ACK/NACK response processing
- **Command Queuing**: Priority-based execution (1-14 levels)

## Development Tools

### IDE and Debugging
- **Primary IDE**: Unity Editor with Visual Studio/Rider integration
- **Version Control**: Git with GitHub
- **Code Analysis**: Unity's built-in tools
- **Profiling**: Unity Profiler for performance monitoring

### Package Management
- **Unity Package Manager**: Official Unity packages
- **Scoped Registries**: Keijiro registry for KlakNDI
- **Custom Packages**: Local development packages as needed

## Networking Architecture

### Camera Communication
- **Protocol**: VISCA over TCP/IP (typically port 52381)
- **Connection Management**: Persistent TCP connections
- **Discovery**: Manual IP configuration (future: auto-discovery)
- **Reliability**: Connection monitoring and automatic reconnection

### NDI Video Streaming
- **Protocol**: NDI over Ethernet
- **Discovery**: NDI network discovery service
- **Latency**: Real-time/low-latency streaming
- **Quality**: Configurable resolution and bitrate

## Constraints and Limitations

### Performance Targets
- **Frame Rate**: 60 FPS UI responsiveness
- **Input Latency**: <50ms for camera commands
- **Video Latency**: <100ms for NDI streams
- **Memory**: <512MB baseline usage

### Hardware Requirements
- **Minimum**: Intel i5 equivalent, 8GB RAM, DirectX 11
- **Recommended**: Intel i7 equivalent, 16GB RAM, dedicated GPU
- **Network**: Gigabit Ethernet for optimal NDI performance

### Platform Support
- **Primary**: Windows 10/11
- **Secondary**: macOS 10.15+, Ubuntu 20.04+
- **Mobile**: Not planned (complex control interface unsuitable)

## Security Considerations
- **Network**: Secure camera authentication where supported
- **Configuration**: Encrypted credential storage
- **Communication**: TLS support where available
- **Input Validation**: Sanitize all network inputs

## Future Technology Considerations
- **WebRTC**: Potential future video streaming alternative
- **ONVIF**: Additional camera protocol support
- **Cloud Integration**: Remote camera access capabilities
- **Mobile Companion**: Simplified mobile control interface
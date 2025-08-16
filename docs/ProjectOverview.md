# USAALive - IP Camera VISCA Controller

## Project Description
USAALive is a Unity-based application designed to control IP cameras that use the VISCA (Video System Control Architecture) protocol. The application provides an intuitive controller interface for camera operators to manipulate various camera settings and functions remotely.

## Core Features

### Multiview Display System
- **Grid-Based Layout**: Configurable 2x2, 3x3, 4x4, and custom grid arrangements
- **Simultaneous Camera Feeds**: Real-time display of all connected cameras via NDI streams
- **Active Camera Management**: Visual indicators for selected/active cameras with quick switching
- **Individual Camera Status**: Per-camera connection status, labels, and health indicators
- **Preview/Program Mode**: Professional switching workflow with preview and program outputs
- **Fullscreen Toggle**: Quick switch between grid view and fullscreen single camera
- **Performance Optimization**: Efficient rendering of multiple video streams with quality scaling

### Camera Control Capabilities
- **PTZ (Pan/Tilt/Zoom) Operations**
  - 8-directional movement control (up, down, left, right, diagonals)
  - Absolute and relative positioning
  - Variable speed control
  - Home positioning and reset functions

- **Zoom Control**
  - Standard and variable speed zoom (tele/wide)
  - Direct zoom positioning
  - Digital zoom support (separate mode)
  - Speed-by-zoom functionality

- **Focus Management**
  - Auto Focus and Manual Focus modes
  - Near/Far focus adjustment with variable speed
  - Direct focus positioning
  - One Push Auto Focus

- **Image Control**
  - Multiple Auto Exposure modes (Manual, Iris Priority, Shutter Priority, Bright)
  - White Balance settings (Indoor, Outdoor, ATW, Manual, One Push)
  - Iris, Shutter, and Gain control
  - Brightness and color adjustments (R/B Gain)

- **Camera Management**
  - Power control
  - Memory preset storage and recall
  - System configuration

### Technical Integration
- **NDI Support**: Integration with KlakNDI plugin for network video streaming and multiview display
- **Multiview Rendering**: Advanced video grid system supporting 4-16+ simultaneous camera feeds
- **VISCA Protocol**: Full implementation of 161 VISCA commands across 13 categories
- **Network Communication**: IP-based camera control for remote operation
- **Unity Platform**: Cross-platform deployment capabilities
- **Performance Engine**: Optimized rendering pipeline for multiple high-quality video streams

## VISCA Protocol Implementation

### Command Structure
The application implements the complete VISCA command set with:
- **161 total commands** organized by priority (1-14)
- **Hex-based communication** (8x address prefix, FF terminator)
- **13 functional categories** covering all camera operations
- **Priority-based execution** (PTZ = highest priority)

### Command Categories by Priority
1. **PTZ Movement** (Priority 1): Core camera positioning
2. **Zoom Operations** (Priority 5-6): Lens control
3. **Focus Control** (Priority 8-10): Image clarity management
4. **Image Settings** (Priority 12): Exposure and color control
5. **System Functions** (Priority 13-14): Camera management and responses

## Technology Stack
- **Platform**: Unity 2022.3.15f1 LTS
- **Language**: C#
- **UI Framework**: UI Toolkit (modern Unity UI system)
- **Input System**: Unity Input System 1.7.0 for controller support
- **Networking**: IP camera communication
- **Video Integration**: NDI via KlakNDI plugin (2.1.4)
- **Protocol**: VISCA over IP

## Development Architecture

### Unity Project Structure
- Component-based GameObject architecture
- UI Toolkit-based controller interface for camera operations
- Network communication layer for VISCA commands
- NDI integration for video streaming
- Error handling and camera response management

### UI Toolkit Architecture
- **UXML Documents**: Define UI layout and element hierarchy
- **USS Stylesheets**: Handle visual styling and responsive design
- **C# Controllers**: Manage UI logic, data binding, and event handling
- **VisualElement Manipulation**: Dynamic UI updates and state management

### Input System Architecture
- **Input Action Assets**: Define control mappings for camera operations
- **Action Maps**: Organize controls by context (PTZ, Focus, Zoom, Image)
- **Device Support**: Gamepad, keyboard, and custom controller compatibility
- **Variable Input**: Analog input for smooth camera movement control
- **Input Buffering**: Queue commands for reliable camera communication

### Key Implementation Areas
1. **VISCA Command Engine**: Core protocol implementation
2. **Multiview Display Engine**: Grid-based video rendering and camera feed management
3. **UI Controller**: User interface for camera control and multiview layout
4. **Input Handler**: Unity Input System integration for device control
5. **Network Layer**: IP communication with cameras
6. **NDI Integration**: Video streaming capabilities and multiview rendering
7. **Camera Management**: Device discovery, connection handling, and status monitoring

## Target Use Cases
- **Live Event Production**: Real-time multi-camera control and monitoring for broadcasts with multiview operator displays
- **Security Operations**: Comprehensive surveillance with simultaneous camera feed monitoring
- **Content Creation**: Multi-camera video production with real-time switching and preview capabilities
- **Educational/Training**: Camera operation training with visual feedback from multiple camera perspectives
- **Studio Production**: Professional broadcasting environments requiring multiview monitoring and camera selection

## Next Steps for Development
1. Implement core VISCA command communication
2. Design and build multiview display system with grid layouts
3. Create controller UI interface with multiview integration
4. Integrate NDI plugin for video streaming and multiview rendering
5. Add camera discovery and connection management
6. Implement camera switching and preview/program workflow
7. Implement error handling and response processing
8. Test with actual VISCA-compatible IP cameras and multiple NDI sources
9. Optimize performance for multiple simultaneous video streams
10. Add advanced multiview features (labels, status indicators, custom layouts)
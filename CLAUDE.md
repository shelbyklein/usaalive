# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview
USAALive is a Unity application for controlling IP cameras via the VISCA protocol with NDI integration. The app provides a controller interface for manipulating camera settings like pan/tilt/zoom, focus, exposure, and other image parameters.

## Unity Project Structure
- **Unity Version**: 2022.3.15f1 (LTS)
- **Main Scene**: Assets/Scenes/SampleScene.unity
- **VISCA Protocol Resources**: Resources/VISCA Protocol/ contains command reference CSVs
- **Package Dependencies**: Unity 2D feature set, Input System (1.7.0), KlakNDI (2.1.4)

## Key Technologies
- **VISCA Protocol**: Camera control standard for PTZ and image settings
- **NDI Integration**: Using KlakNDI plugin (https://github.com/keijiro/KlakNDI) for network video
- **Unity 2022.3 LTS**: Game engine platform
- **UI Toolkit**: Modern UI framework for interface development
- **Unity Input System**: Modern input handling for controllers and devices
- **C# Scripting**: Primary development language

## VISCA Protocol Implementation
The project includes comprehensive VISCA command references:
- 161 total commands across 13 function categories
- Priority levels from 1-14 (PTZ movement = highest priority)
- Command categories: PTZ Movement/Position, Zoom Control/Movement/Position, Focus Control/Auto/Position, Image Control, Camera Control, System Response
- Hex command format: 8x prefix for camera address, FF terminator

### Key VISCA Command Categories:
1. **PTZ Movement** (Priority 1): Pan/Tilt operations, absolute/relative positioning
2. **Zoom Movement** (Priority 5): Tele/Wide zoom with variable speed
3. **Focus Control** (Priority 8-10): Auto/Manual focus, near/far adjustment
4. **Image Control** (Priority 12): AE modes, iris, shutter, white balance, gain
5. **Camera Control** (Priority 11): Power, memory presets
6. **System Response** (Priority 13): ACK/completion messages, error handling

## Development Commands
Since this is a Unity project, use Unity Editor for development:
- Open project in Unity 2022.3.15f1
- Build via Unity Editor (File > Build Settings)
- No external build tools or package managers beyond Unity Package Manager

## Architecture Notes
- Standard Unity GameObject/Component architecture
- UI Toolkit for modern, responsive controller interface
- Network communication for IP camera control
- NDI plugin integration for video streaming
- VISCA protocol implementation will need serial/TCP communication

### UI Toolkit Implementation
- Use UXML files for UI layout and structure
- USS files for styling and theming
- C# scripts with VisualElement manipulation for dynamic behavior
- Event handling through UI Toolkit's event system
- Consider responsive design for different screen sizes

### Input System Integration
- Create Input Action Assets for camera control mappings
- Support multiple input devices (gamepad, keyboard, custom controllers)
- Map PTZ controls to analog sticks/directional inputs
- Implement variable speed control based on input magnitude
- Handle both continuous (hold) and discrete (press) input patterns

## Development Guidelines
- Follow Unity C# coding conventions
- Use Unity's component-based architecture
- Implement VISCA commands based on provided CSV references
- Test with actual IP cameras supporting VISCA protocol
- Consider camera response times and error handling for network communication
- Use UI Toolkit best practices:
  - Separate layout (UXML) from styling (USS) from logic (C#)
  - Use data binding where appropriate for dynamic content
  - Implement proper event handling and cleanup
  - Follow UI Toolkit naming conventions for USS classes and UXML elements
- Use Input System best practices:
  - Create reusable Input Action Assets for different control schemes
  - Implement proper input event subscription/unsubscription
  - Use Input System's PlayerInput component for device management
  - Consider input buffering for camera command queuing
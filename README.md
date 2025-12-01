# BleHeartRate-OscSender
Read Heart rate value from BLE device.
A tool sends heart‑rate data from a BLE device to an OSC server.

## Overview
This application reads heart‑rate data from a BLE device and sends the data to an OSC server.(e.g. VRChat)
* The BLE device must implement the **Heart Rate Service** (e.g. COOSPO HW807/HW9, IGPSPORT HR70, etc.).
* The data sent consists of:
  * Heart‑rate in **Hz** (beats per second).
  * A value normalized to the range **0–1** based on a user‑specified minimum and maximum heart‑rate.
* When the application starts, it automatically reconnects to the BLE device that was connected during the previous session.

# Credits / Third-Party Assets
This project uses the following third-party libraries and assets:

## uOSC
- Repository: https://github.com/hecomi/uOSC  
- Author: hecomi  
- License: MIT License  
- This package is referenced via Unity's Package Manager.  
  A local copy of the license is not included; please refer to the license at the repository above.

## Noto Sans JP
- Homepage: https://fonts.google.com/noto  
- Author: Google / Adobe  
- License: SIL Open Font License 1.1 (OFL)  
- A copy of the OFL license is included as `OFL.txt` in this project.

## BleWinrtDll (Forked Version)
- Original Repository: https://github.com/adabru/BleWinrtDll  
- Forked Repository: https://github.com/shun1053/BleWinrtDll  
- Author (Original): adabru  
- License: MIT License  
- This project uses a custom-built DLL compiled from my forked version of the library.  
  Modifications (if any) are documented in the forked repository.  
- The original license terms apply; a copy of the MIT License is available in the original repository.

# QRNG Unity Demo
A simple Unity demo that uses the QRNG endpoint of the [Quantum API](https://github.com/DavidJGrimsley/quantum-api) to generate random numbers in 4 different use cases. 

## Run the demo

Open `Assets/QRNG/Scenes/QRNG.unity` in Unity 6000.6.1f1. Select the `QuantumApiManager` GameObject in the Hierarchy. For direct mode, leave **Backend Proxy Mode** off and enter your own key in **API Key (Direct Mode)**. For proxy mode, enable it and enter a compatible backend proxy URL; keep the upstream API key on that server. The manager also holds the IBM backend and profile names used by the hardware card. Then press Play. The four cards use the same client.

Direct mode uses the hosted API URL. Proxy mode uses the configured URL with `/v1` appended when needed. Do not commit a scene after entering an API key; Unity serializes Inspector fields into scene files, and a distributed build does not keep them secret.

The plugin is [Quantum API Unity package v1.1.0](https://github.com/DavidJGrimsley/quantum-api/releases/tag/quantumapi-unity-v1.1.0), vendored in `Assets/QuantumApi` so this project opens without a machine-specific package path.
## C# vs Visual Scripting
The plugin currently only supports C# scripts but I'm thinking of making it work with visual scripting.
## Simulator vs Hardware
3 of the use cases use simulator, one uses hardware. If we add visual scripting then I'd like to make it work with both simulator and hardware.

At that point the top two uses would be simulator only, one C# and one visual script. The bottom two uses would be hardware only, one C# and one visual script. Same functionality, just refactoring.

# QRNG Unity Demo
A simple Unity demo that uses the QRNG endpoint of the [Quantum API](https://github.com/DavidJGrimsley/quantum-api) to generate random numbers in 4 different use cases. 

## Run the demo

Open `Assets/QRNG/Scenes/QRNG.unity` in Unity 6000.6.1f1. Select the `QuantumApiManager` GameObject in the Hierarchy. For direct mode, leave **Backend Proxy Mode** off and enter your own key in **API Key**, then press Play. The four cards use the same client. The manager also has **Check Health** and **Request Random (0-1)** context-menu actions for quick Console checks.

The production API URL is fixed inside the plugin source. Do not commit a scene after entering an API key; Unity serializes Inspector fields into scene files. A distributed game should use a backend proxy or credentials appropriate for a public client.

The plugin source is maintained in `sdk/unity` of the Quantum API repository and copied into `Assets/QuantumApi` here so this project opens without a machine-specific package path.
## C# vs Visual Scripting
The plugin currently only supports C# scripts but I'm thinking of making it work with visual scripting.
## Simulator vs Hardware
3 of the use cases use simulator, one uses hardware. If we add visual scripting then I'd like to make it work with both simulator and hardware.

At that point the top two uses would be simulator only, one C# and one visual script. The bottom two uses would be hardware only, one C# and one visual script. Same functionality, just refactoring.

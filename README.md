# QRNG Unity Demo
A simple Unity demo that uses the QRNG endpoint of the [Quantum API](https://github.com/DavidJGrimsley/quantum-api) to generate random numbers in 4 different use cases. 
## C# vs Visual Scripting
The plugin currently only supports C# scripts but I'm thinking of making it work with visual scripting.
## Simulator vs Hardware
3 of the use cases use simulator, one uses hardware. If we add visual scripting then I'd like to make it work with both simulator and hardware.

At that point the top two uses would be simulator only, one C# and one visual script. The bottom two uses would be hardware only, one C# and one visual script. Same functionality, just refactoring.
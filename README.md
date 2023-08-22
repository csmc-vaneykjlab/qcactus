# QCactus
QCactus is a C# desktop application for quickly consolidating QC metrics for proteomics data.

## Technical Requirements
This application is written in C# and intended to run as a Windows Desktop Application.
- .NET runtime (https://dotnet.microsoft.com/en-us/download/dotnet-framework)
- Oracle Java (or OpenJDK https://openjdk.org/)
- For Development:
  - Visual C++ Redistributable
  - Visual Studio 2023
 
## Current Version
The current version focuses on providing a wrapper of sorts for the following workflow:
1] Import and check raw files for integrity
2] Provide ID-free metrics based on raw files w/ plots
3] Provide ID-based summary of raw files via MSFragger w/plots
4] Export an example PDF report of derived QC information

## License
Please note the software falls under the MIT license but the QCactus name is trademark of Cedars-Sinai and the cactus logo is copyright of Zachary Dwight (both requiring permission for use).

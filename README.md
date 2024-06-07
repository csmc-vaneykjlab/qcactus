<h1><img src="ThermoDust/images/cactus86.png" width="75" > QCactus </h1> 
QCactus is a C# desktop application for quickly consolidating QC metrics for proteomics data.  

## Technical Requirements
This application is written in C# and intended to run as a Windows Desktop Application.
- .NET runtime (https://dotnet.microsoft.com/en-us/download/dotnet-framework)
- Oracle Java (or OpenJDK https://openjdk.org/)
- For Development:
  - Visual C++ Redistributable
  - Visual Studio 2023
  - The project is named 'ThermoDust' in this repo

NOTE I: this version is not our internal production version but rather a community version.  Can't give everything away ;)
NOTE II: there is no installer only because we are still working on various parts to push out based on feedback
 
## Features
The current version focuses on providing a wrapper of sorts for the following workflow:
- Import and check raw files for integrity
- Provide ID-free metrics based on raw files w/ plots
- Provide ID-based summary of raw files via MSFragger w/plots
- Export an example PDF report of derived QC information

## License
This software falls under the MIT license (attached).  The 'QCactus' name is trademark of Cedars-Sinai.

## About Us
Cedars-Sinai Precision Biomarker Laboratories (PBL) provides complete proteomics solutions across biomarker discovery, validation and CAP/CLIA-certified laboratory testing capabilities.  Our customized workflows allow for the quick translation from protein discovery to clinically validated targeted panels.

https://www.cs-pbl.com/



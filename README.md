<h1><img src="ThermoDust/images/cactus86.png" width="60"> QCactus</h1>

**QCactus** is a Windows desktop application for quickly consolidating QC metrics for proteomics data. Built at [Cedars-Sinai Precision Biomarker Laboratories](https://www.cs-pbl.com/).

---

## Installation

### Option A — Installer (recommended)

1. Download **`QCactus_Setup.exe`** from the [Releases](../../releases) page
   > *(New releases are built automatically by GitHub Actions when a version tag is pushed. You can also grab the latest build from the [Actions tab](../../actions) → most recent run → `QCactus_Setup` artifact.)*
2. Run the installer — no admin rights required
3. Launch QCactus from the Start Menu or Desktop shortcut
4. On first launch, QCactus will auto-detect bundled tools. If MSFragger isn't found, open **Tools → Settings** to configure paths

### Option B — Build from Source

See [Building from Source](#building-from-source) below.

---

## Requirements

| Requirement | Notes |
|---|---|
| Windows 10 / 11 | 64-bit |
| Java (OpenJDK 17+) | Required for MSFragger peptide search — [download here](https://openjdk.org/) |
| .NET 6 runtime | **Bundled** in the installer — no separate install needed |

> **Java note:** After installing Java, make sure `java` is on your PATH (open a Command Prompt and run `java -version` to confirm). If not, use **Tools → Settings** to point QCactus directly to your `java.exe`.

---

## First Run

When QCactus launches for the first time it automatically looks for MSFragger and fragger.params in the `msfragger\` folder next to the executable. If found, paths are configured automatically.

If the status bar shows **⚠ MSFragger not configured**, open **Tools → Settings**:

```
Tools → Settings
  ├─ Java executable    e.g.  java   (if on PATH)  or  C:\jdk\bin\java.exe
  ├─ MSFragger JAR      e.g.  C:\Program Files\QCactus\msfragger\MSFragger-3.8.jar
  ├─ fragger.params     e.g.  C:\Program Files\QCactus\msfragger\fragger.params
  └─ Output directory   e.g.  C:\Users\You\AppData\Local\QCactus   (default)
```

Click **Detect Defaults** to auto-fill from the installation directory, then **Save**.

---

## Features

| Step | What it does |
|---|---|
| **Import** | Load `.raw` files — auto-sorted into samples, blanks, and HeLa QC controls |
| **ID-free QC** | TIC, base peaks, MS1/MS2 scan intensity extracted directly from raw files |
| **ID-based QC** | Runs MSFragger to search peptides against a human FASTA; reports protein/peptide counts |
| **Group comparison** | Overlay plots for up to 4 sample groups (A/B/C/D) |
| **PDF report** | Export a multi-page QC report with all plots and statistics |

---

## Building from Source

### Prerequisites
- [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
- [Inno Setup 6](https://jrsoftware.org/isinfo.php) *(for building the installer)*
- Visual Studio 2022 *(optional — for IDE development)*

### Build & run
```bat
dotnet build ThermoDust\ThermoDust.csproj
dotnet run --project ThermoDust\ThermoDust.csproj
```

### Build installer
```bat
build_installer.bat
```
This will:
1. `dotnet publish` — self-contained win-x64 build → `publish\`
2. Compile `installer\QCactus.iss` with Inno Setup
3. Output: `installer\Output\QCactus_Setup.exe`

### Development layout
```
qcactus/
├── ThermoDust/              C# WinForms project (the app)
│   ├── Form1.cs             Main window — QC logic, plots, PDF export
│   ├── SettingsForm.cs      Tools → Settings dialog
│   ├── Properties/
│   │   ├── Settings.settings   User-scoped app settings (paths)
│   │   └── Settings.Designer.cs
│   └── ThermoDust.csproj
├── DLLS/                    Thermo Fisher + Bruker external DLLs
├── msfragger/               MSFragger JAR, fragger.params, FASTA database
├── installer/
│   └── QCactus.iss          Inno Setup script
├── build_installer.bat      One-click build + package
└── README.md
```

---

## Updating the FASTA database

The bundled database (`msfragger/2023-04-13-HUMAN_SANTOSH.fasta`) is a human proteome snapshot. To use a different database:

1. Download your FASTA from [UniProt](https://www.uniprot.org/) or [NCBI](https://www.ncbi.nlm.nih.gov/)
2. Update `fragger.params`: change the `database_name` line to point to your new file
3. In **Tools → Settings**, update the `fragger.params` path if you moved it

---

## License

MIT — see [LICENSE](LICENSE). The **QCactus** name is a trademark of Cedars-Sinai.

---

## About

Cedars-Sinai Precision Biomarker Laboratories (PBL) provides complete proteomics solutions across biomarker discovery, validation, and CAP/CLIA-certified laboratory testing. Our customized workflows allow for quick translation from protein discovery to clinically validated targeted panels.

🌵 [cs-pbl.com](https://www.cs-pbl.com/)

# MobileLinesManager - WPF Desktop Application

## Overview
MobileLinesManager is a production-ready WPF desktop application built with C# .NET 8.0 and Entity Framework Core with SQLite. This application manages mobile phone lines with features for tracking assignments, categories, operators, and generating reports. It includes Arabic right-to-left (RTL) language support.

## Important Note
This is a **Windows-only WPF desktop application** that requires Windows to run the GUI. On Replit's Linux environment, the project can be built and validated, but the application cannot be executed as WPF requires Windows.

## Project Architecture

### Technology Stack
- **.NET 8.0** with WPF (Windows Presentation Foundation)
- **Entity Framework Core 8.0** with SQLite database
- **Dependency Injection** using Microsoft.Extensions.DependencyInjection
- **MVVM Pattern** (Model-View-ViewModel)

### Key Dependencies
- **CsvHelper** (30.0.1) - CSV import/export functionality
- **ZXing.Net** (0.16.9) - QR code generation and scanning
- **QuestPDF** (2024.3.0) - PDF report generation
- **ClosedXML** (0.102.1) - Excel report generation
- **BCrypt.Net-Next** (4.0.3) - Password hashing and security

### Project Structure
```
MobileLinesManager/
├── Commands/          # MVVM command implementations
├── Converters/        # XAML value converters
├── Data/             # Database context and initialization
├── Models/           # Entity models (Line, User, Category, Operator, etc.)
├── Resources/        # XAML resource dictionaries (styles, converters)
├── Services/         # Business logic services
│   ├── Alert         # Line alert monitoring
│   ├── Backup        # Database backup functionality
│   ├── Import        # CSV import service
│   ├── QR            # QR code operations
│   └── Report        # PDF/Excel report generation
├── ViewModels/       # MVVM ViewModels
└── Views/           # XAML views and code-behind
```

### Features
1. **Line Management** - Track mobile phone lines with details
2. **Assignment Tracking** - Monitor line assignments with audit logs
3. **Category Organization** - Organize lines by categories
4. **Operator Management** - Manage mobile operators
5. **Dashboard** - Overview with statistics
6. **Reports** - Generate PDF and Excel reports
7. **QR Code Support** - Generate and scan QR codes for lines
8. **Import/Export** - CSV import and Excel export
9. **Alerts** - Automated alerts for line status changes
10. **Backup** - Database backup and restore functionality
11. **Audit Trail** - Complete audit logging system

## Development Setup

### Prerequisites (Windows Required for GUI)
- Windows OS
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code with C# extension

### On Replit (Linux Environment)
The project has been set up with:
- ✅ .NET 8.0 SDK installed
- ✅ NuGet packages restored
- ✅ Build validation completed successfully
- ⚠️ GUI execution not possible (requires Windows)

### Build Commands
```bash
# Restore packages
dotnet restore

# Build project
dotnet build

# Build in Release mode
dotnet build -c Release
```

### Database
- Uses **SQLite** database
- Database file: `MobileLinesManager.db` (created on first run)
- Entity Framework Core migrations included

## Recent Changes
- **2025-11-01**: Project imported to Replit
  - Installed .NET 8.0 SDK
  - Restored all NuGet packages
  - Validated build successfully
  - Added comprehensive .gitignore for .NET projects

## User Preferences
- None specified yet

## Notes
- The application includes Arabic language support with RTL layout
- Build warnings related to nullable reference types are expected and non-critical
- The project uses dependency injection pattern with ServiceLocator
- Database is initialized automatically on first run

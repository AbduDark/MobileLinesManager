# MobileLinesManager - WPF Desktop Application

## Overview
MobileLinesManager is a production-ready WPF desktop application built with C# .NET 8.0 and Entity Framework Core with SQLite. This application manages mobile phone lines across four Egyptian telecom operators (Vodafone, Etisalat, We, Orange) using a **group-based system**. Each group can contain up to 50 lines with features for tracking assignments, delivery, and generating comprehensive reports. The application includes Arabic right-to-left (RTL) language support.

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
- **AForge.Video** (2.2.5) - Webcam video capture for QR scanning
- **AForge.Video.DirectShow** (2.2.5) - DirectShow support for AForge

### Project Structure
```
MobileLinesManager/
├── Commands/          # MVVM command implementations
├── Converters/        # XAML value converters
├── Data/             # Database context and initialization
├── Models/           # Entity models (Line, User, Group, Operator, etc.)
├── Resources/        # XAML resource dictionaries (styles, converters)
├── Services/         # Business logic services
│   ├── Alert         # Line alert monitoring (now Group-based)
│   ├── Backup        # Database backup functionality
│   ├── Import        # CSV import service (Group-based)
│   ├── QR            # QR code operations (Group-based)
│   └── Report        # PDF/Excel report generation (Group-based)
├── ViewModels/       # MVVM ViewModels
│   └── GroupsViewModel.cs  # New ViewModel for Groups management
└── Views/           # XAML views and code-behind
    └── GroupsView.xaml     # New View for Groups management
```

### Features
1. **Group-Based Line Management** - Organize mobile phone lines into groups (up to 50 lines per group)
2. **Four Telecom Operators** - Support for Vodafone, Etisalat, We, and Orange with brand-specific colors
3. **Group Types** - CashWallet (60-day validity), NoWallet, Suspended, Prepaid
4. **Delivery Tracking** - Track delivery status for CashWallet groups
5. **Assignment Tracking** - Monitor line assignments with audit logs
6. **Dashboard** - Overview with statistics (Group-based)
7. **Reports** - Generate PDF and Excel reports (Group-based)
8. **QR Code Support** - Generate and scan QR codes for lines (Group-based)
9. **Import/Export** - CSV import and Excel export (Group-based)
10. **Alerts** - Automated alerts for group validity changes (60-day periods for CashWallet)
11. **Backup** - Database backup and restore functionality
12. **Audit Trail** - Complete audit logging system

### Operator-Specific UI Colors
- **Vodafone**: Red (#E60000)
- **Etisalat**: Green (#008000)
- **We**: Purple (#8B008B)
- **Orange**: Orange (#FF8C00)

## Development Setup

### Prerequisites (Windows Required for GUI)
- Windows OS
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code with C# extension

### On Replit (Linux Environment)
The project has been set up with:
- ✅ .NET 8.0 SDK installed (version 8.0.412)
- ✅ NuGet packages restored (including AForge dependencies)
- ✅ Build validation completed successfully (0 errors)
- ✅ All compilation errors fixed
- ✅ Import completed successfully
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
- **Group** model with relationships to Operator and Lines

## Recent Changes
- **2025-11-01**: Project successfully imported and fixed on Replit
  - Installed .NET 8.0 SDK (version 8.0.412)
  - Fixed all compilation errors:
    * Updated OperatorViewModel to use Groups instead of Categories
    * Updated ReportsViewModel to use Groups instead of Categories
    * Fixed QRService async method to properly return Line type
    * Fixed QRScannerWindow to use event-driven frame capture (removed non-existent GetCurrentFrame)
    * Added AForge.Video and AForge.Video.DirectShow packages for webcam support
    * Updated DashboardViewModel to properly map operator Groups
  - Build validation completed successfully (0 errors, only nullable reference warnings)
  - Created validate-build workflow for continuous validation
  - Import completed successfully

- **2025-11-01**: Complete refactoring from Category-based to Group-based system
  - Updated all services: ImportService, AlertService, QRService, DashboardViewModel
  - Rewrote ReportService (simplified from 574 lines) with Group-based PDF/Excel exports
  - Added Groups navigation property to Operator model
  - Updated AppDbContext with Group relationships
  - Created GroupsView.xaml with operator-specific colors and logos
  - Created GroupsViewModel.cs for Groups management
  - Updated MainWindow.xaml with operator logos (Vodafone, Etisalat, We, Orange)
  - Updated LinesViewModel, AssignViewModel to use Groups

## User Preferences
- Prefer Group-based architecture over Category-based
- Support for Egyptian telecom operators with brand-specific UI
- Arabic language support with RTL layout

## Notes
- The application includes Arabic language support with RTL layout
- Build warnings (194) related to nullable reference types are expected and non-critical
- The project uses dependency injection pattern with ServiceLocator
- Database is initialized automatically on first run
- GroupsView DataContext is wired through ServiceLocator in code-behind
- Each group supports up to 50 lines
- CashWallet groups have 60-day validity periods with delivery tracking

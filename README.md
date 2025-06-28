# TP-Link Switch WebUI

A modern web interface for managing TP-Link switches, built with React and ASP.NET Core 9.0. This application provides a comprehensive web-based management interface for TP-Link managed switches, offering real-time monitoring and configuration capabilities.

## Features

### 🔐 **Authentication & Security**
- Secure login with switch credentials
- Session management with cookie persistence
- Automatic session renewal

### 📊 **System Monitoring**
- Real-time system information display
- Device details (name, MAC, IP, firmware version)
- Network configuration monitoring

### 🔌 **Port Management**
- View all port statuses and configurations
- Enable/disable individual ports
- Monitor port speeds and link status
- Flow control configuration
- Real-time port status updates

### 🌐 **VLAN Configuration**
- View port-based VLAN settings
- VLAN membership display
- Port assignment visualization

### 🔍 **Cable Diagnostics**
- Test cable integrity for all ports
- Cable length measurement
- Fault detection (open, short, cross-cable)
- Comprehensive diagnostic reporting

### 🎨 **Modern User Interface**
- Dark/light theme support
- Responsive design for all devices
- Tabbed interface for easy navigation
- Real-time data refresh
- Intuitive controls and feedback

### ⚡ **Switch Management**
- Remote switch reboot functionality
- Configuration management
- Status monitoring

## Quick Start

### Prerequisites
- .NET 9.0 SDK
- Node.js 18+ and npm
- TP-Link managed switch (for real device testing)

### Installation
1. Clone the repository:
   ```bash
   git clone https://github.com/your-username/tplink-webui.git
   cd tplink-webui
   ```

2. Install dependencies:
   ```bash
   # Backend
   cd backend && dotnet restore && cd ..
   
   # Frontend
   cd frontend && npm install && cd ..
   ```

3. Start the application:
   ```bash
   # Terminal 1 - Start Backend
   ./start-backend.sh

   # Terminal 2 - Start Frontend  
   ./start-frontend.sh
   ```

4. Navigate to: **http://localhost:5173**

## Backend
- **Framework**: ASP .NET Core 9.0
- **Logging**: Serilog with structured logging to files
- **Structure**:
  - `Program.cs` — app startup and service registration
  - `Controllers/SwitchController.cs` — REST API endpoints
  - `Services/SwitchService.cs` — business logic and switch operations
  - `Services/TplinkClient.cs` — TP-Link switch communication
  - `Models/` — data models and request/response objects

### Features
- **Enhanced Logging**: Structured logging with Serilog
  - Daily rotating log files in `logs/` directory
  - Separate error logs for troubleshooting
  - Request/response logging with performance metrics
  - Detailed operation logging for switch communications
- **Session Management**: Persistent authentication with automatic renewal
- **Error Handling**: Comprehensive error handling with detailed logging
- **Performance Monitoring**: Timing logs for all operations

### Setup & Run
```bash
cd backend
dotnet restore
dotnet run --urls="http://localhost:5000"
```

### Log Files
- `logs/tplink-webui-YYYY-MM-DD.log` - General application logs
- `logs/errors-YYYY-MM-DD.log` - Error and warning logs

## Frontend
- **Framework**: React + Vite + TypeScript
- **Styling**: Tailwind CSS with dark mode
- **UI Library**: shadcn/ui, Lucide icons, Framer Motion

### Setup & Run
```bash
cd frontend
npm install
npm run dev
```

Proxy API calls to backend by editing `vite.config.js` if needed.

## 📁 Project Structure
```
├── backend/                          # .NET Core API
│   ├── Controllers/                  # REST API controllers
│   ├── Services/                     # Business logic services
│   ├── Models/                       # Data models
│   ├── logs/                         # Application log files
│   └── appsettings.json             # Configuration
├── frontend/                         # React application
│   ├── src/
│   │   ├── components/              # React components
│   │   └── lib/                     # Utilities
│   └── package.json                 # Dependencies
├── docs/                            # Documentation
├── .gitignore                       # Git ignore rules
├── LICENSE                          # MIT License
└── README.md                        # This file
```

## 🔒 Security

This application includes comprehensive security features:
- Secure credential storage and session management
- Structured logging without sensitive data exposure
- CORS protection and input validation
- See [SECURITY.md](SECURITY.md) for detailed security information

## 🤝 Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

### Quick Development Setup
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📋 Environment Configuration

Copy `.env.example` to `.env` and configure as needed:
```bash
cp .env.example .env
```

For production deployment, see `backend/appsettings.Production.json.example`.

## 🐛 Troubleshooting

### Common Issues
- **Connection refused**: Check if switch IP is correct and accessible
- **Login failed**: Verify switch credentials
- **CORS errors**: Ensure frontend/backend URLs are configured correctly
- **Build errors**: Run `dotnet restore` and `npm install`

### Debug Information
- Check log files in `logs/` directory
- Enable debug logging in `appsettings.Development.json`
- Use browser developer tools for frontend issues
- See [TROUBLESHOOTING.md](TROUBLESHOOTING.md) for detailed help

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- TP-Link for their switch management interfaces
- The .NET and React communities for excellent frameworks
- Contributors and testers who help improve this project
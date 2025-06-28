# TP-Link Switch WebUI

A modern web interface for managing TP-Link switches, built with React and ASP.NET Core 9.0. This application provides a better web-based management interface for TP-Link managed switches.

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
- TP-Link managed switch

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

## 📋 Environment Configuration

Copy `.env.example` to `.env` and configure as needed:
```bash
cp .env.example .env
```

For production deployment, see `backend/appsettings.Production.json.example`.


## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

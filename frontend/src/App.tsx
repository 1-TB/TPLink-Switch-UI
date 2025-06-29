import { useEffect, useState } from 'react';
import { Button } from './components/ui/button';
import { Sun, Moon, Power, RefreshCw, LogOut } from 'lucide-react';
import { motion } from 'framer-motion';
import SystemInfoCard from './components/SystemInfoCard';
import LoginForm from './components/LoginForm';
import PortInfoCard from './components/PortInfoCard';
import SwitchPortLayout from './components/SwitchPortLayout';
import VlanInfoCard from './components/VlanInfoCard';
import DiagnosticsCard from './components/DiagnosticsCard';
import HistoryDashboard from './components/HistoryDashboard';
import { SetupWizard } from './components/SetupWizard';
import { UserLogin } from './components/UserLogin';

interface SystemInfo {
  deviceName: string;
  macAddress: string;
  ipAddress: string;
  subnetMask: string;
  gateway: string;
  firmwareVersion: string;
  hardwareVersion: string;
  systemUptime: string;
}

interface PortInfo {
  portNumber: number;
  status: string;
  speedConfig: string;
  speedActual: string;
  flowControlConfig: string;
  flowControlActual: string;
  trunk: string;
  isEnabled: boolean;
  isConnected: boolean;
}

interface VlanInfo {
  vlanId: number;
  memberPorts: string;
  portNumbers: number[];
}

interface VlanConfigResponse {
  isEnabled: boolean;
  totalPorts: number;
  vlanCount: number;
  vlans: VlanInfo[];
}

interface PortDiagnostic {
  portNumber: number;
  state: number;
  stateDescription: string;
  length: number;
  isHealthy: boolean;
  hasIssue: boolean;
  isUntested: boolean;
  isDisconnected: boolean;
}

interface User {
  id: number;
  username: string;
  email: string;
  firstName?: string;
  lastName?: string;
  role: string;
  lastLoginAt?: string;
}

type AppState = 'loading' | 'setup' | 'login' | 'dashboard';

function App() {
  const [dark, setDark] = useState(true);
  const [appState, setAppState] = useState<AppState>('loading');
  const [currentUser, setCurrentUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [sysInfo, setSysInfo] = useState<SystemInfo | null>(null);
  const [portInfo, setPortInfo] = useState<PortInfo[]>([]);
  const [vlanConfig, setVlanConfig] = useState<VlanConfigResponse | null>(null);
  const [diagnostics, setDiagnostics] = useState<PortDiagnostic[]>([]);
  const [activeTab, setActiveTab] = useState('overview');
  const [selectedPort, setSelectedPort] = useState<number | undefined>();

  useEffect(() => {
    document.documentElement.classList.toggle('dark', dark);
  }, [dark]);

  useEffect(() => {
    checkApplicationState();
  }, []);

  const checkApplicationState = async () => {
    try {
      // First check if initial setup is required
      const setupResponse = await fetch('/api/auth/setup/required');
      const setupData = await setupResponse.json();
      
      if (setupData.setupRequired) {
        setAppState('setup');
        return;
      }

      // Check if user is already authenticated
      const userResponse = await fetch('/api/auth/me');
      if (userResponse.ok) {
        const userData = await userResponse.json();
        if (userData.success && userData.user) {
          setCurrentUser(userData.user);
          setAppState('dashboard');
          await fetchAllData();
          return;
        }
      }

      // User needs to log in
      setAppState('login');
    } catch (error) {
      console.error('Error checking application state:', error);
      setAppState('login');
    }
  };

  const handleSetupComplete = () => {
    setAppState('login');
  };

  const handleLoginSuccess = async () => {
    try {
      const userResponse = await fetch('/api/auth/me');
      const userData = await userResponse.json();
      
      if (userData.success && userData.user) {
        setCurrentUser(userData.user);
        setAppState('dashboard');
        await fetchAllData();
      }
    } catch (error) {
      console.error('Error after login:', error);
    }
  };

  const handleLogout = async () => {
    try {
      await fetch('/api/auth/logout', { method: 'POST' });
    } catch (error) {
      console.error('Logout error:', error);
    } finally {
      setCurrentUser(null);
      setAppState('login');
      setSysInfo(null);
      setPortInfo([]);
      setVlanConfig(null);
      setDiagnostics([]);
    }
  };

  const fetchSystemInfo = async () => {
    try {
      const response = await fetch('/api/systeminfo');
      if (!response.ok) {
        if (response.status === 401 || response.status === 403) {
          handleLogout();
          return;
        }
        const errorData = await response.json();
        throw new Error(errorData.error || `HTTP ${response.status}`);
      }
      const data = await response.json();
      setSysInfo(data.data || data);
    } catch (error) {
      console.error('Failed to fetch system info:', error);
    }
  };

const fetchPortInfo = async () => {
  try {
    const response = await fetch('/api/ports');
    if (!response.ok) {
      if (response.status === 401 || response.status === 403) {
        handleLogout();
        return;
      }
      const err = await response.json();
      throw new Error(err.error || `HTTP ${response.status}`);
    }

    const payload = await response.json();
    let portsArray: PortInfo[] = [];

    if (Array.isArray(payload.ports)) {
      portsArray = payload.ports;
    } else if (payload.data && Array.isArray(payload.data.ports)) {
      portsArray = payload.data.ports;
    } else {
      console.warn('Unexpected /api/ports response shape:', payload);
      portsArray = [];
    }

    setPortInfo(portsArray);
  } catch (error) {
    console.error('Failed to fetch port info:', error);
  }
};


  const fetchVlanConfig = async () => {
    try {
      const response = await fetch('/api/vlans');
      if (!response.ok) {
        if (response.status === 401 || response.status === 403) {
          handleLogout();
          return;
        }
        const errorData = await response.json();
        throw new Error(errorData.error || `HTTP ${response.status}`);
      }
      const data = await response.json();
      setVlanConfig(data.data || data);
    } catch (error) {
      console.error('Failed to fetch VLAN config:', error);
    }
  };

  const fetchAllData = async () => {
    setIsLoading(true);
    try {
      await Promise.all([
        fetchSystemInfo(),
        fetchPortInfo(),
        fetchVlanConfig()
      ]);
    } finally {
      setIsLoading(false);
    }
  };

  const handleRefresh = () => {
    fetchAllData();
  };

  const handleConfigurePort = async (portNumber: number, enabled: boolean) => {
    try {
      const response = await fetch('/api/ports/configure', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          portNumber,
          enabled
        })
      });

      if (!response.ok) {
        if (response.status === 401 || response.status === 403) {
          handleLogout();
          return;
        }
        const errorData = await response.json();
        throw new Error(errorData.message || `HTTP ${response.status}`);
      }

      await fetchPortInfo();
    } catch (error) {
      console.error('Failed to configure port:', error);
      alert('Failed to configure port: ' + error);
    }
  };

 const handleRunDiagnostics = async (portNumbers: number[]) => {
  try {
    const response = await fetch('/api/diagnostics/cable', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ ports: portNumbers })
    });

    if (!response.ok) {
      if (response.status === 401 || response.status === 403) {
        handleLogout();
        return;
      }
      const err = await response.json();
      throw new Error(err.message || `HTTP ${response.status}`);
    }

    const payload = await response.json();
    // pick out the diagnostics array
    let diagArray: PortDiagnostic[] = [];

    if (Array.isArray(payload.diagnostics)) {
      diagArray = payload.diagnostics;
    } else if (payload.data && Array.isArray(payload.data.diagnostics)) {
      diagArray = payload.data.diagnostics;
    } else {
      console.warn('Unexpected /api/diagnostics response shape:', payload);
    }

    setDiagnostics(diagArray);
    setActiveTab('diagnostics');
  } catch (error) {
    console.error('Failed to run diagnostics:', error);
    alert('Failed to run diagnostics: ' + error);
  }
};

  const handleRunSinglePortDiagnostic = async (portNumber: number) => {
    await handleRunDiagnostics([portNumber]);
  };

  const handleCreateVlan = async (vlanId: number, memberPorts: number[]) => {
    try {
      const response = await fetch('/api/vlans/create', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          vlanId,
          memberPorts
        })
      });

      if (!response.ok) {
        if (response.status === 401 || response.status === 403) {
          handleLogout();
          return;
        }
        const errorData = await response.json();
        throw new Error(errorData.message || `HTTP ${response.status}`);
      }

      await fetchVlanConfig();
    } catch (error) {
      console.error('Failed to create VLAN:', error);
      alert('Failed to create VLAN: ' + error);
    }
  };

  const handleDeleteVlans = async (vlanIds: number[]) => {
    try {
      const response = await fetch('/api/vlans/delete', {
        method: 'DELETE',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ vlanIds })
      });

      if (!response.ok) {
        if (response.status === 401 || response.status === 403) {
          handleLogout();
          return;
        }
        const errorData = await response.json();
        throw new Error(errorData.message || `HTTP ${response.status}`);
      }

      await fetchVlanConfig();
    } catch (error) {
      console.error('Failed to delete VLANs:', error);
      alert('Failed to delete VLANs: ' + error);
    }
  };

  const handleReboot = async () => {
    if (!confirm('Are you sure you want to reboot the switch? This will temporarily disconnect all ports.')) {
      return;
    }

    try {
      const response = await fetch('/api/reboot', {
        method: 'POST'
      });

      if (!response.ok) {
        if (response.status === 401 || response.status === 403) {
          handleLogout();
          return;
        }
        const errorData = await response.json();
        throw new Error(errorData.message || `HTTP ${response.status}`);
      }

      alert('Switch reboot initiated. The switch will be unavailable for a few minutes.');
    } catch (error) {
      console.error('Failed to reboot switch:', error);
      alert('Failed to reboot switch: ' + error);
    }
  };

  // Show loading screen while checking application state
  if (appState === 'loading') {
    return (
      <div className="min-h-screen bg-gray-50 dark:bg-gray-900 flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
          <p className="text-gray-600 dark:text-gray-300">Loading...</p>
        </div>
      </div>
    );
  }

  // Show setup wizard if initial setup is required
  if (appState === 'setup') {
    return <SetupWizard onSetupComplete={handleSetupComplete} />;
  }

  // Show login form if user needs to authenticate
  if (appState === 'login') {
    return <UserLogin onLoginSuccess={handleLoginSuccess} />;
  }

  // Show main dashboard
  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      <div className="container mx-auto p-4 sm:p-6">
        <div className="flex flex-col sm:flex-row sm:justify-between sm:items-center mb-6 sm:mb-8 gap-4">
          <div className="flex-1">
            <h1 className="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white">
              TP-Link Switch Manager
            </h1>
            {currentUser && (
              <p className="text-gray-600 dark:text-gray-300 mt-1 text-sm sm:text-base">
                Welcome back, {currentUser.firstName || currentUser.username}
              </p>
            )}
          </div>
          
          <div className="flex items-center gap-2 sm:space-x-4 flex-wrap">
            <Button
              variant="outline"
              size="sm"
              onClick={handleRefresh}
              disabled={isLoading}
              className="flex-1 sm:flex-none min-w-0"
            >
              <RefreshCw className={`w-4 h-4 sm:mr-2 ${isLoading ? 'animate-spin' : ''}`} />
              <span className="hidden sm:inline">Refresh</span>
            </Button>
            
            <Button
              variant="outline"
              size="sm"
              onClick={handleReboot}
              className="flex-1 sm:flex-none min-w-0"
            >
              <Power className="w-4 h-4 sm:mr-2" />
              <span className="hidden sm:inline">Reboot</span>
            </Button>
            
            <Button
              variant="outline"
              size="sm"
              onClick={() => setDark(!dark)}
              className="px-3"
            >
              {dark ? <Sun className="w-4 h-4" /> : <Moon className="w-4 h-4" />}
            </Button>
            
            <Button
              variant="outline"
              size="sm"
              onClick={handleLogout}
              className="flex-1 sm:flex-none min-w-0"
            >
              <LogOut className="w-4 h-4 sm:mr-2" />
              <span className="hidden sm:inline">Logout</span>
            </Button>
          </div>
        </div>

        <div className="mb-6 sm:mb-8">
          <nav className="flex flex-wrap gap-1 bg-white dark:bg-gray-800 p-1 rounded-lg shadow-sm overflow-x-auto">
            {[
              { id: 'overview', label: 'Overview' },
              { id: 'ports', label: 'Ports' },
              { id: 'vlans', label: 'VLANs' },
              { id: 'diagnostics', label: 'Diagnostics' },
              { id: 'history', label: 'History' }
            ].map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`px-3 sm:px-4 py-2 rounded-md text-xs sm:text-sm font-medium transition-colors whitespace-nowrap ${
                  activeTab === tab.id
                    ? 'bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300'
                    : 'text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200'
                }`}
              >
                {tab.label}
              </button>
            ))}
          </nav>
        </div>

        <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
          {activeTab === 'overview' && (
            <div className="space-y-6">
              {sysInfo && <SystemInfoCard info={sysInfo} />}
              <SwitchPortLayout 
                ports={portInfo} 
                vlanConfig={vlanConfig}
                onConfigurePort={handleConfigurePort}
                onRunDiagnostic={handleRunSinglePortDiagnostic}
              />
            </div>
          )}
          
          {activeTab === 'ports' && (
            <PortInfoCard 
              ports={portInfo} 
              onConfigurePort={handleConfigurePort}
              onRunDiagnostics={handleRunDiagnostics}
              onViewHistory={(portNumber) => {
                setSelectedPort(portNumber);
                setActiveTab('history');
              }}
            />
          )}
          
          {activeTab === 'vlans' && vlanConfig && (
            <VlanInfoCard 
              vlanConfig={vlanConfig} 
              onCreateVlan={handleCreateVlan}
              onDeleteVlans={handleDeleteVlans}
              onRefresh={fetchVlanConfig}
            />
          )}
          
          {activeTab === 'diagnostics' && (
            <DiagnosticsCard diagnostics={diagnostics} />
          )}
          
          {activeTab === 'history' && (
            <HistoryDashboard selectedPort={selectedPort} />
          )}
        </motion.div>
      </div>
    </div>
  );
}

export default App;
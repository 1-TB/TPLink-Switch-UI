import { useEffect, useState } from 'react';
import { Button } from './components/ui/button';
import { Sun, Moon, Power, RefreshCw } from 'lucide-react';
import { motion } from 'framer-motion';
import SystemInfoCard from './components/SystemInfoCard';
import LoginForm from './components/LoginForm';
import PortInfoCard from './components/PortInfoCard';
import SwitchPortLayout from './components/SwitchPortLayout';
import VlanInfoCard from './components/VlanInfoCard';
import DiagnosticsCard from './components/DiagnosticsCard';

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

function App() {
  const [dark, setDark] = useState(true);
  const [isLoggedIn, setIsLoggedIn] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [sysInfo, setSysInfo] = useState<SystemInfo | null>(null);
  const [portInfo, setPortInfo] = useState<PortInfo[]>([]);
  const [vlanConfig, setVlanConfig] = useState<VlanConfigResponse | null>(null);
  const [diagnostics, setDiagnostics] = useState<PortDiagnostic[]>([]);
  const [activeTab, setActiveTab] = useState('overview');

  useEffect(() => {
    document.documentElement.classList.toggle('dark', dark);
  }, [dark]);

  const handleLogin = async (credentials: { host: string; username: string; password: string }) => {
    setIsLoading(true);
    try {
      // First test basic connectivity
      const testResponse = await fetch('/api/test-connection', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(credentials),
      });
      
      const testResult = await testResponse.json();
      if (!testResult.success) {
        alert('Connection Test Failed: ' + testResult.message);
        return;
      }

      // If connectivity test passes, proceed with login
      const response = await fetch('/api/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(credentials),
      });
      
      const result = await response.json();
      if (result.success) {
        setIsLoggedIn(true);
        await fetchAllData();
      } else {
        alert('Login failed: ' + result.message);
      }
    } catch (error) {
      console.error('Login failed:', error);
      if (error instanceof TypeError && error.message.includes('fetch')) {
        alert('Cannot connect to the backend server. Please make sure the backend is running.');
      } else {
        alert('Login failed: ' + error);
      }
    } finally {
      setIsLoading(false);
    }
  };

  const fetchSystemInfo = async () => {
    try {
      const response = await fetch('/api/systeminfo');
      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.error || `HTTP ${response.status}`);
      }
      const data = await response.json();
      setSysInfo(data);
    } catch (error) {
      console.error('Failed to fetch system info:', error);
      // Don't show alert for system info failures, just log them
      setSysInfo(null);
    }
  };

  const fetchPortInfo = async () => {
    try {
      const response = await fetch('/api/ports');
      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.error || `HTTP ${response.status}`);
      }
      const data = await response.json();
      setPortInfo(data.ports || []);
    } catch (error) {
      console.error('Failed to fetch port info:', error);
      setPortInfo([]);
    }
  };

  const fetchVlanConfig = async () => {
    try {
      const response = await fetch('/api/vlans');
      const data = await response.json();
      setVlanConfig(data);
    } catch (error) {
      console.error('Failed to fetch VLAN config:', error);
    }
  };

  const fetchAllData = async () => {
    await Promise.all([
      fetchSystemInfo(),
      fetchPortInfo(),
      fetchVlanConfig(),
    ]);
  };

  const handleConfigurePort = async (port: number, enable: boolean) => {
    try {
      const response = await fetch('/api/ports/configure', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ port, enable, speed: 1, flowControl: false }),
      });
      
      const result = await response.json();
      if (result.success) {
        await fetchPortInfo(); // Refresh port info
      } else {
        alert('Port configuration failed: ' + result.message);
      }
    } catch (error) {
      console.error('Port configuration failed:', error);
      alert('Port configuration failed: ' + error);
    }
  };

  const handleRunDiagnostics = async (ports: number[]) => {
    try {
      setIsLoading(true);
      const response = await fetch('/api/diagnostics/cable', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ ports }),
      });
      
      const data = await response.json();
      setDiagnostics(data.diagnostics || []);
      setActiveTab('diagnostics');
    } catch (error) {
      console.error('Cable diagnostics failed:', error);
      alert('Cable diagnostics failed: ' + error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleRunSinglePortDiagnostic = async (port: number) => {
    try {
      setIsLoading(true);
      const response = await fetch(`/api/diagnostics/cable/port/${port}`, {
        method: 'POST',
      });
      
      const data = await response.json();
      setDiagnostics(data.diagnostics || []);
      setActiveTab('diagnostics');
    } catch (error) {
      console.error('Cable diagnostic failed:', error);
      alert('Cable diagnostic failed: ' + error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleCreateVlan = async (vlanId: number, ports: number[]) => {
    try {
      const response = await fetch('/api/vlans/create', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ vlanId, ports }),
      });
      
      const result = await response.json();
      if (!result.success) {
        throw new Error(result.message);
      }
    } catch (error) {
      console.error('VLAN creation failed:', error);
      throw error;
    }
  };

  const handleDeleteVlans = async (vlanIds: number[]) => {
    try {
      const response = await fetch('/api/vlans/delete', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ vlanIds }),
      });
      
      const result = await response.json();
      if (!result.success) {
        throw new Error(result.message);
      }
    } catch (error) {
      console.error('VLAN deletion failed:', error);
      throw error;
    }
  };

  const handleReboot = async () => {
    if (confirm('Are you sure you want to reboot the switch? This will disconnect all users.')) {
      try {
        const response = await fetch('/api/reboot', { method: 'POST' });
        const result = await response.json();
        if (result.success) {
          alert('Switch reboot initiated. You will be disconnected.');
          setIsLoggedIn(false);
        } else {
          alert('Reboot failed: ' + result.message);
        }
      } catch (error) {
        console.error('Reboot failed:', error);
        alert('Reboot failed: ' + error);
      }
    }
  };

  if (!isLoggedIn) {
    return (
      <div className="min-h-screen bg-background text-foreground p-6 flex items-center justify-center">
        <div className="w-full max-w-md">
          <div className="text-center mb-8">
            <h1 className="text-3xl font-bold mb-2">TP-Link Switch Manager</h1>
            <p className="text-muted-foreground">Connect to your TP-Link switch to manage it</p>
          </div>
          <LoginForm onLogin={handleLogin} isLoading={isLoading} />
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-background text-foreground p-6">
      <header className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold">TP-Link Switch Manager</h1>
        <div className="flex items-center gap-2">
          <Button variant="outline" onClick={() => setDark(!dark)}>
            {dark ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
          </Button>
          <Button variant="outline" onClick={fetchAllData} disabled={isLoading}>
            <RefreshCw className={`h-4 w-4 ${isLoading ? 'animate-spin' : ''}`} />
          </Button>
          <Button variant="destructive" onClick={handleReboot}>
            <Power className="h-4 w-4" />
          </Button>
        </div>
      </header>

      {/* Navigation Tabs */}
      <div className="flex space-x-1 mb-6 border-b">
        {[
          { id: 'overview', label: 'Overview' },
          { id: 'ports', label: 'Port List' },
          { id: 'switch', label: 'Switch View' },
          { id: 'vlans', label: 'VLANs' },
          { id: 'diagnostics', label: 'Diagnostics' },
        ].map((tab) => (
          <button
            key={tab.id}
            onClick={() => setActiveTab(tab.id)}
            className={`px-4 py-2 font-medium text-sm border-b-2 transition-colors ${
              activeTab === tab.id
                ? 'border-primary text-primary'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            }`}
          >
            {tab.label}
          </button>
        ))}
      </div>

      <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
        {activeTab === 'overview' && sysInfo && (
          <SystemInfoCard info={sysInfo} />
        )}
        
        {activeTab === 'ports' && (
          <PortInfoCard 
            ports={portInfo} 
            onConfigurePort={handleConfigurePort}
            onRunDiagnostics={handleRunDiagnostics}
          />
        )}
        
        {activeTab === 'switch' && (
          <SwitchPortLayout 
            ports={portInfo} 
            onConfigurePort={handleConfigurePort}
            onRunDiagnostic={handleRunSinglePortDiagnostic}
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
      </motion.div>
    </div>
  );
}

export default App;
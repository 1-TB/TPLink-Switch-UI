import { useState } from 'react';
import { Button } from './ui/button';
import { RefreshCw, Power, Sun, Moon, LogOut } from 'lucide-react';
import { motion } from 'framer-motion';
import SystemInfoCard from './SystemInfoCard';
import PortInfoCard from './PortInfoCard';
import SwitchPortLayout from './SwitchPortLayout';
import VlanInfoCard from './VlanInfoCard';
import DiagnosticsCard from './DiagnosticsCard';
import HistoryDashboard from './HistoryDashboard';
import { ApiState } from '../lib/useApi';

interface User {
  id: number;
  username: string;
  email: string;
  firstName?: string;
  lastName?: string;
  role: string;
  lastLoginAt?: string;
}

interface DashboardProps {
  currentUser: User;
  apiState: ApiState;
  dark: boolean;
  onToggleDark: () => void;
  onRefresh: () => void;
  onReboot: () => void;
  onLogout: () => void;
  onConfigurePort: (portNumber: number, enabled: boolean) => void;
  onRunDiagnostics: (portNumbers: number[]) => void;
  onRunSinglePortDiagnostic: (portNumber: number) => void;
  onCreateVlan: (vlanId: number, vlanName: string, taggedPorts: number[], untaggedPorts: number[]) => void;
  onDeleteVlans: (vlanIds: number[]) => void;
  onRefreshVlans: () => void;
}

export const Dashboard: React.FC<DashboardProps> = ({
  currentUser,
  apiState,
  dark,
  onToggleDark,
  onRefresh,
  onReboot,
  onLogout,
  onConfigurePort,
  onRunDiagnostics,
  onRunSinglePortDiagnostic,
  onCreateVlan,
  onDeleteVlans,
  onRefreshVlans,
}) => {
  const [activeTab, setActiveTab] = useState('overview');
  const [selectedPort, setSelectedPort] = useState<number | undefined>();

  const { sysInfo, portInfo, vlanConfig, diagnostics, isLoading } = apiState;

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      <div className="container mx-auto p-4 sm:p-6">
        {/* Header */}
        <div className="flex flex-col sm:flex-row sm:justify-between sm:items-center mb-6 sm:mb-8 gap-4">
          <div className="flex-1">
            <h1 className="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white">
              TP-Link Switch Manager
            </h1>
            <p className="text-gray-600 dark:text-gray-300 mt-1 text-sm sm:text-base">
              Welcome back, {currentUser.firstName || currentUser.username}
            </p>
          </div>
          
          <div className="flex items-center gap-2 sm:space-x-4 flex-wrap">
            <Button
              variant="outline"
              size="sm"
              onClick={onRefresh}
              disabled={isLoading}
              className="flex-1 sm:flex-none min-w-0"
            >
              <RefreshCw className={`w-4 h-4 sm:mr-2 ${isLoading ? 'animate-spin' : ''}`} />
              <span className="hidden sm:inline">Refresh</span>
            </Button>
            
            <Button
              variant="outline"
              size="sm"
              onClick={onReboot}
              className="flex-1 sm:flex-none min-w-0"
            >
              <Power className="w-4 h-4 sm:mr-2" />
              <span className="hidden sm:inline">Reboot</span>
            </Button>
            
            <Button
              variant="outline"
              size="sm"
              onClick={onToggleDark}
              className="px-3"
            >
              {dark ? <Sun className="w-4 h-4" /> : <Moon className="w-4 h-4" />}
            </Button>
            
            <Button
              variant="outline"
              size="sm"
              onClick={onLogout}
              className="flex-1 sm:flex-none min-w-0"
            >
              <LogOut className="w-4 h-4 sm:mr-2" />
              <span className="hidden sm:inline">Logout</span>
            </Button>
          </div>
        </div>

        {/* Navigation Tabs */}
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

        {/* Tab Content */}
        <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
          {activeTab === 'overview' && (
            <div className="space-y-6">
              {sysInfo && <SystemInfoCard info={sysInfo} />}
              <SwitchPortLayout 
                ports={portInfo} 
                vlanConfig={vlanConfig}
                onConfigurePort={onConfigurePort}
                onRunDiagnostic={onRunSinglePortDiagnostic}
              />
            </div>
          )}
          
          {activeTab === 'ports' && (
            <PortInfoCard 
              ports={portInfo} 
              onConfigurePort={onConfigurePort}
              onRunDiagnostics={onRunDiagnostics}
              onViewHistory={(portNumber) => {
                setSelectedPort(portNumber);
                setActiveTab('history');
              }}
            />
          )}
          
          {activeTab === 'vlans' && vlanConfig && (
            <VlanInfoCard 
              vlanConfig={vlanConfig} 
              onCreateVlan={onCreateVlan}
              onDeleteVlans={onDeleteVlans}
              onRefresh={onRefreshVlans}
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
};
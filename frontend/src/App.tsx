import { useEffect, useState } from 'react';
import { SetupWizard } from './components/SetupWizard';
import { UserLogin } from './components/UserLogin';
import { Dashboard } from './components/Dashboard';
import { useAuth } from './lib/useAuth';
import { useApi } from './lib/useApi';

function App() {
  const [dark, setDark] = useState(true);
  const auth = useAuth();
  const api = useApi();

  useEffect(() => {
    document.documentElement.classList.toggle('dark', dark);
  }, [dark]);

  useEffect(() => {
    auth.checkApplicationState().then((user) => {
      if (user) {
        api.fetchAllData(auth.handleLogout);
      }
    });
  }, []);

  const handleLoginSuccess = async () => {
    const user = await auth.handleLoginSuccess();
    if (user) {
      api.fetchAllData(auth.handleLogout);
    }
  };

  const handleRefresh = () => {
    api.fetchAllData(auth.handleLogout);
  };

  const handleReboot = async () => {
    if (!confirm('Are you sure you want to reboot the switch? This will temporarily disconnect all ports.')) {
      return;
    }
    api.rebootSwitch(auth.handleLogout);
  };

  const handleConfigurePort = (portNumber: number, enabled: boolean) => {
    api.configurePort(portNumber, enabled, auth.handleLogout);
  };

  const handleRunDiagnostics = (portNumbers: number[]) => {
    api.runDiagnostics(portNumbers, auth.handleLogout);
  };

  const handleRunSinglePortDiagnostic = (portNumber: number) => {
    api.runDiagnostics([portNumber], auth.handleLogout);
  };

  const handleCreateVlan = (vlanId: number, vlanName: string, taggedPorts: number[], untaggedPorts: number[]) => {
    api.createVlan(vlanId, vlanName, taggedPorts, untaggedPorts, auth.handleLogout);
  };

  const handleDeleteVlans = (vlanIds: number[]) => {
    api.deleteVlans(vlanIds, auth.handleLogout);
  };

  const handleRefreshVlans = () => {
    api.fetchVlanConfig(auth.handleLogout);
  };

  // System Management handlers
  const handleSetSystemName = (name: string) => {
    api.setSystemName(name, auth.handleLogout);
  };

  const handleConfigureIpSettings = (config: any) => {
    api.configureIpSettings(config, auth.handleLogout);
  };

  const handleFactoryReset = () => {
    api.factoryReset(auth.handleLogout);
  };

  const handleSaveConfiguration = () => {
    api.saveConfiguration(auth.handleLogout);
  };

  const handleControlLed = (enabled: boolean) => {
    api.controlLed(enabled, auth.handleLogout);
  };

  const handleUpdateUserAccount = (accountData: any) => {
    api.updateUserAccount(accountData, auth.handleLogout);
  };

  // Advanced Networking handlers
  const handleClearPortStatistics = (ports: number[]) => {
    api.clearPortStatistics(ports, auth.handleLogout);
  };

  const handleConfigureMirroring = (config: any) => {
    api.configureMirroring(config, auth.handleLogout);
  };

  const handleConfigureTrunking = (config: any) => {
    api.configureTrunking(config, auth.handleLogout);
  };

  const handleConfigureLoopPrevention = (config: any) => {
    api.configureLoopPrevention(config, auth.handleLogout);
  };

  // QoS Management handlers
  const handleConfigureQosMode = (mode: string) => {
    api.configureQosMode(mode, auth.handleLogout);
  };

  const handleConfigureBandwidthControl = (config: any) => {
    api.configureBandwidthControl(config, auth.handleLogout);
  };

  const handleConfigurePortPriority = (config: any) => {
    api.configurePortPriority(config, auth.handleLogout);
  };

  const handleConfigureStormControl = (config: any) => {
    api.configureStormControl(config, auth.handleLogout);
  };

  // PoE Management handlers
  const handleConfigurePoEGlobal = (config: any) => {
    api.configurePoEGlobal(config, auth.handleLogout);
  };

  const handleConfigurePoEPort = (config: any) => {
    api.configurePoEPort(config, auth.handleLogout);
  };

  // Configuration Management handlers
  const handleBackupConfiguration = () => {
    api.backupConfiguration(auth.handleLogout);
  };

  const handleRestoreConfiguration = (file: File) => {
    api.restoreConfiguration(file, auth.handleLogout);
  };

  const handleUpgradeFirmware = (file: File) => {
    api.upgradeFirmware(file, auth.handleLogout);
  };

  const handleConfigureIgmpSnooping = (config: any) => {
    api.configureIgmpSnooping(config, auth.handleLogout);
  };

  // Show loading screen while checking application state
  if (auth.appState === 'loading') {
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
  if (auth.appState === 'setup') {
    return <SetupWizard onSetupComplete={auth.handleSetupComplete} />;
  }

  // Show login form if user needs to authenticate
  if (auth.appState === 'login') {
    return <UserLogin onLoginSuccess={handleLoginSuccess} />;
  }

  // Show main dashboard
  if (auth.currentUser) {
    return (
      <Dashboard
        currentUser={auth.currentUser}
        apiState={api}
        dark={dark}
        onToggleDark={() => setDark(!dark)}
        onRefresh={handleRefresh}
        onReboot={handleReboot}
        onLogout={auth.handleLogout}
        onConfigurePort={handleConfigurePort}
        onRunDiagnostics={handleRunDiagnostics}
        onRunSinglePortDiagnostic={handleRunSinglePortDiagnostic}
        onCreateVlan={handleCreateVlan}
        onDeleteVlans={handleDeleteVlans}
        onRefreshVlans={handleRefreshVlans}
        // System Management
        onSetSystemName={handleSetSystemName}
        onConfigureIpSettings={handleConfigureIpSettings}
        onFactoryReset={handleFactoryReset}
        onSaveConfiguration={handleSaveConfiguration}
        onControlLed={handleControlLed}
        onUpdateUserAccount={handleUpdateUserAccount}
        // Advanced Networking
        onClearPortStatistics={handleClearPortStatistics}
        onConfigureMirroring={handleConfigureMirroring}
        onConfigureTrunking={handleConfigureTrunking}
        onConfigureLoopPrevention={handleConfigureLoopPrevention}
        // QoS Management
        onConfigureQosMode={handleConfigureQosMode}
        onConfigureBandwidthControl={handleConfigureBandwidthControl}
        onConfigurePortPriority={handleConfigurePortPriority}
        onConfigureStormControl={handleConfigureStormControl}
        // PoE Management
        onConfigurePoEGlobal={handleConfigurePoEGlobal}
        onConfigurePoEPort={handleConfigurePoEPort}
        // Configuration Management
        onBackupConfiguration={handleBackupConfiguration}
        onRestoreConfiguration={handleRestoreConfiguration}
        onUpgradeFirmware={handleUpgradeFirmware}
        onConfigureIgmpSnooping={handleConfigureIgmpSnooping}
      />
    );
  }

  return null;
}

export default App;
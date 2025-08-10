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
      />
    );
  }

  return null;
}

export default App;
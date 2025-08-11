import { useState } from 'react';
import { Button } from './ui/button';
import { Input } from './ui/input';
import { Label } from './ui/label';
import { Card, CardContent, CardHeader, CardTitle } from './ui/card';
import { Switch } from './ui/switch';
import { AlertTriangle, Save, Settings, User, Wifi, RotateCcw } from 'lucide-react';
import { ApiState } from '../lib/useApi';

interface SystemManagementProps {
  apiState: ApiState;
  onSetSystemName: (name: string) => void;
  onConfigureIpSettings: (config: any) => void;
  onFactoryReset: () => void;
  onSaveConfiguration: () => void;
  onControlLed: (enabled: boolean) => void;
  onUpdateUserAccount: (accountData: any) => void;
}

export const SystemManagement: React.FC<SystemManagementProps> = ({
  apiState,
  onSetSystemName,
  onConfigureIpSettings,
  onFactoryReset,
  onSaveConfiguration,
  onControlLed,
  onUpdateUserAccount,
}) => {
  const [systemName, setSystemName] = useState('');
  const [ipConfig, setIpConfig] = useState({
    dhcpEnabled: true,
    ipAddress: '',
    subnetMask: '',
    gateway: '',
  });
  const [ledEnabled, setLedEnabled] = useState(true);
  const [userAccount, setUserAccount] = useState({
    username: '',
    oldPassword: '',
    newPassword: '',
    confirmPassword: '',
  });

  const handleSetSystemName = () => {
    if (!systemName.trim()) {
      alert('Please enter a system name');
      return;
    }
    onSetSystemName(systemName);
    setSystemName('');
  };

  const handleConfigureIP = () => {
    if (!ipConfig.dhcpEnabled) {
      if (!ipConfig.ipAddress || !ipConfig.subnetMask || !ipConfig.gateway) {
        alert('Please fill in all static IP fields');
        return;
      }
    }
    onConfigureIpSettings(ipConfig);
  };

  const handleFactoryReset = () => {
    if (confirm('Are you sure you want to perform a factory reset? This will erase all configuration and restart the switch with default settings.')) {
      onFactoryReset();
    }
  };

  const handleUpdateUserAccount = () => {
    if (!userAccount.username || !userAccount.oldPassword || !userAccount.newPassword) {
      alert('Please fill in all required fields');
      return;
    }
    if (userAccount.newPassword !== userAccount.confirmPassword) {
      alert('New password and confirmation do not match');
      return;
    }
    onUpdateUserAccount(userAccount);
    setUserAccount({
      username: '',
      oldPassword: '',
      newPassword: '',
      confirmPassword: '',
    });
  };

  return (
    <div className="space-y-6">
      {/* System Name Configuration */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Settings className="w-5 h-5" />
            System Name Configuration
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex gap-4">
            <div className="flex-1">
              <Label htmlFor="systemName">System Name</Label>
              <Input
                id="systemName"
                value={systemName}
                onChange={(e) => setSystemName(e.target.value)}
                placeholder="Enter system name"
                className="mt-1"
              />
            </div>
            <div className="flex items-end">
              <Button onClick={handleSetSystemName} disabled={apiState.isLoading}>
                <Settings className="w-4 h-4 mr-2" />
                Update Name
              </Button>
            </div>
          </div>
          {apiState.sysInfo && (
            <p className="text-sm text-gray-600 dark:text-gray-400">
              Current: {apiState.sysInfo.deviceName}
            </p>
          )}
        </CardContent>
      </Card>

      {/* IP Configuration */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Wifi className="w-5 h-5" />
            IP Configuration
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center space-x-2">
            <Switch
              checked={ipConfig.dhcpEnabled}
              onCheckedChange={(checked) => setIpConfig(prev => ({ ...prev, dhcpEnabled: checked }))}
            />
            <Label>Use DHCP</Label>
          </div>
          
          {!ipConfig.dhcpEnabled && (
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div>
                <Label htmlFor="ipAddress">IP Address</Label>
                <Input
                  id="ipAddress"
                  value={ipConfig.ipAddress}
                  onChange={(e) => setIpConfig(prev => ({ ...prev, ipAddress: e.target.value }))}
                  placeholder="192.168.1.100"
                  className="mt-1"
                />
              </div>
              <div>
                <Label htmlFor="subnetMask">Subnet Mask</Label>
                <Input
                  id="subnetMask"
                  value={ipConfig.subnetMask}
                  onChange={(e) => setIpConfig(prev => ({ ...prev, subnetMask: e.target.value }))}
                  placeholder="255.255.255.0"
                  className="mt-1"
                />
              </div>
              <div>
                <Label htmlFor="gateway">Gateway</Label>
                <Input
                  id="gateway"
                  value={ipConfig.gateway}
                  onChange={(e) => setIpConfig(prev => ({ ...prev, gateway: e.target.value }))}
                  placeholder="192.168.1.1"
                  className="mt-1"
                />
              </div>
            </div>
          )}
          
          <Button onClick={handleConfigureIP} disabled={apiState.isLoading}>
            <Wifi className="w-4 h-4 mr-2" />
            Update IP Settings
          </Button>
          
          {apiState.sysInfo && (
            <div className="text-sm text-gray-600 dark:text-gray-400">
              <p>Current IP: {apiState.sysInfo.ipAddress}</p>
              <p>Subnet Mask: {apiState.sysInfo.subnetMask}</p>
              <p>Gateway: {apiState.sysInfo.gateway}</p>
            </div>
          )}
        </CardContent>
      </Card>

      {/* LED Control */}
      <Card>
        <CardHeader>
          <CardTitle>LED Control</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <Label>Front Panel LEDs</Label>
              <p className="text-sm text-gray-600 dark:text-gray-400">
                Control the front panel LED indicators
              </p>
            </div>
            <Switch
              checked={ledEnabled}
              onCheckedChange={(checked) => {
                setLedEnabled(checked);
                onControlLed(checked);
              }}
            />
          </div>
        </CardContent>
      </Card>

      {/* User Account Management */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <User className="w-5 h-5" />
            User Account Management
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <Label htmlFor="username">Username</Label>
              <Input
                id="username"
                value={userAccount.username}
                onChange={(e) => setUserAccount(prev => ({ ...prev, username: e.target.value }))}
                placeholder="Enter username"
                className="mt-1"
              />
            </div>
            <div>
              <Label htmlFor="oldPassword">Current Password</Label>
              <Input
                id="oldPassword"
                type="password"
                value={userAccount.oldPassword}
                onChange={(e) => setUserAccount(prev => ({ ...prev, oldPassword: e.target.value }))}
                placeholder="Enter current password"
                className="mt-1"
              />
            </div>
            <div>
              <Label htmlFor="newPassword">New Password</Label>
              <Input
                id="newPassword"
                type="password"
                value={userAccount.newPassword}
                onChange={(e) => setUserAccount(prev => ({ ...prev, newPassword: e.target.value }))}
                placeholder="Enter new password"
                className="mt-1"
              />
            </div>
            <div>
              <Label htmlFor="confirmPassword">Confirm New Password</Label>
              <Input
                id="confirmPassword"
                type="password"
                value={userAccount.confirmPassword}
                onChange={(e) => setUserAccount(prev => ({ ...prev, confirmPassword: e.target.value }))}
                placeholder="Confirm new password"
                className="mt-1"
              />
            </div>
          </div>
          <Button onClick={handleUpdateUserAccount} disabled={apiState.isLoading}>
            <User className="w-4 h-4 mr-2" />
            Update Account
          </Button>
        </CardContent>
      </Card>

      {/* Configuration Actions */}
      <Card>
        <CardHeader>
          <CardTitle>Configuration Actions</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex flex-wrap gap-4">
            <Button onClick={onSaveConfiguration} disabled={apiState.isLoading}>
              <Save className="w-4 h-4 mr-2" />
              Save Configuration
            </Button>
            <Button 
              onClick={handleFactoryReset} 
              variant="destructive" 
              disabled={apiState.isLoading}
            >
              <RotateCcw className="w-4 h-4 mr-2" />
              Factory Reset
            </Button>
          </div>
          <div className="flex items-start gap-2 p-4 bg-yellow-50 dark:bg-yellow-900/20 rounded-lg">
            <AlertTriangle className="w-5 h-5 text-yellow-600 dark:text-yellow-400 mt-0.5" />
            <div className="text-sm">
              <p className="font-medium text-yellow-800 dark:text-yellow-300">Important:</p>
              <p className="text-yellow-700 dark:text-yellow-400">
                Factory reset will erase all configurations and restart the switch with default settings. 
                Save your current configuration before proceeding.
              </p>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
};
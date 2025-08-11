import { useState, useRef } from 'react';
import { Button } from './ui/button';
import { Label } from './ui/label';
import { Card, CardContent, CardHeader, CardTitle } from './ui/card';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from './ui/select';
import { Download, Upload, FileCode, AlertTriangle } from 'lucide-react';
import { ApiState } from '../lib/useApi';

interface ConfigManagementProps {
  apiState: ApiState;
  onBackupConfiguration: () => void;
  onRestoreConfiguration: (file: File) => void;
  onUpgradeFirmware: (file: File) => void;
  onConfigureIgmpSnooping: (config: any) => void;
}

export const ConfigManagement: React.FC<ConfigManagementProps> = ({
  apiState,
  onBackupConfiguration,
  onRestoreConfiguration,
  onUpgradeFirmware,
  onConfigureIgmpSnooping,
}) => {
  const [igmpConfig, setIgmpConfig] = useState({
    enabled: false,
    version: 'v2',
    queryInterval: 125,
    maxResponseTime: 10,
  });
  const configFileRef = useRef<HTMLInputElement>(null);
  const firmwareFileRef = useRef<HTMLInputElement>(null);

  const handleBackup = () => {
    onBackupConfiguration();
  };

  const handleConfigRestore = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      if (!file.name.endsWith('.cfg')) {
        alert('Please select a valid configuration file (.cfg)');
        return;
      }
      if (confirm('Are you sure you want to restore this configuration? This will overwrite current settings and restart the switch.')) {
        onRestoreConfiguration(file);
      }
    }
  };

  const handleFirmwareUpgrade = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      if (!file.name.endsWith('.bin')) {
        alert('Please select a valid firmware file (.bin)');
        return;
      }
      if (confirm('Are you sure you want to upgrade the firmware? This process may take several minutes and the switch will restart.')) {
        onUpgradeFirmware(file);
      }
    }
  };

  const handleConfigureIgmp = () => {
    onConfigureIgmpSnooping(igmpConfig);
  };

  return (
    <div className="space-y-6">
      {/* Configuration Backup & Restore */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <FileCode className="w-5 h-5" />
            Configuration Management
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-3">
              <h4 className="font-medium">Backup Configuration</h4>
              <p className="text-sm text-gray-600 dark:text-gray-400">
                Download the current switch configuration as a .cfg file for backup purposes.
              </p>
              <Button onClick={handleBackup} disabled={apiState.isLoading} className="w-full">
                <Download className="w-4 h-4 mr-2" />
                Download Configuration
              </Button>
            </div>

            <div className="space-y-3">
              <h4 className="font-medium">Restore Configuration</h4>
              <p className="text-sm text-gray-600 dark:text-gray-400">
                Upload and restore a previously saved configuration file.
              </p>
              <input
                type="file"
                ref={configFileRef}
                onChange={handleConfigRestore}
                accept=".cfg"
                className="hidden"
              />
              <Button 
                onClick={() => configFileRef.current?.click()} 
                disabled={apiState.isLoading}
                variant="outline"
                className="w-full"
              >
                <Upload className="w-4 h-4 mr-2" />
                Upload Configuration
              </Button>
            </div>
          </div>

          <div className="flex items-start gap-2 p-4 bg-yellow-50 dark:bg-yellow-900/20 rounded-lg">
            <AlertTriangle className="w-5 h-5 text-yellow-600 dark:text-yellow-400 mt-0.5" />
            <div className="text-sm">
              <p className="font-medium text-yellow-800 dark:text-yellow-300">Important:</p>
              <p className="text-yellow-700 dark:text-yellow-400">
                Restoring a configuration will overwrite all current settings and restart the switch. 
                Make sure to backup your current configuration before proceeding.
              </p>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Firmware Upgrade */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Upload className="w-5 h-5" />
            Firmware Upgrade
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-3">
            <p className="text-sm text-gray-600 dark:text-gray-400">
              Upload a new firmware file (.bin) to upgrade the switch firmware. 
              This process may take several minutes and will restart the switch automatically.
            </p>
            
            {apiState.sysInfo && (
              <div className="p-3 bg-gray-50 dark:bg-gray-800 rounded-lg">
                <p className="text-sm">
                  <strong>Current Firmware:</strong> {apiState.sysInfo.firmwareVersion}
                </p>
                <p className="text-sm">
                  <strong>Hardware:</strong> {apiState.sysInfo.hardwareVersion}
                </p>
              </div>
            )}

            <input
              type="file"
              ref={firmwareFileRef}
              onChange={handleFirmwareUpgrade}
              accept=".bin"
              className="hidden"
            />
            <Button 
              onClick={() => firmwareFileRef.current?.click()} 
              disabled={apiState.isLoading}
              variant="outline"
              className="w-full"
            >
              <Upload className="w-4 h-4 mr-2" />
              Upload Firmware File
            </Button>
          </div>

          <div className="flex items-start gap-2 p-4 bg-red-50 dark:bg-red-900/20 rounded-lg">
            <AlertTriangle className="w-5 h-5 text-red-600 dark:text-red-400 mt-0.5" />
            <div className="text-sm">
              <p className="font-medium text-red-800 dark:text-red-300">Warning:</p>
              <p className="text-red-700 dark:text-red-400">
                Firmware upgrade is a critical operation. Do not power off the switch during the upgrade process. 
                Ensure you have the correct firmware file for your switch model before proceeding.
              </p>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* IGMP Snooping Configuration */}
      <Card>
        <CardHeader>
          <CardTitle>IGMP Snooping Configuration</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center space-x-2">
            <input
              type="checkbox"
              id="igmpEnabled"
              checked={igmpConfig.enabled}
              onChange={(e) => setIgmpConfig(prev => ({ ...prev, enabled: e.target.checked }))}
            />
            <Label htmlFor="igmpEnabled">Enable IGMP Snooping</Label>
          </div>

          {igmpConfig.enabled && (
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div>
                <Label htmlFor="igmpVersion">IGMP Version</Label>
                <Select
                  value={igmpConfig.version}
                  onValueChange={(value) => setIgmpConfig(prev => ({ ...prev, version: value }))}
                >
                  <SelectTrigger className="mt-1">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="v1">IGMP v1</SelectItem>
                    <SelectItem value="v2">IGMP v2</SelectItem>
                    <SelectItem value="v3">IGMP v3</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div>
                <Label htmlFor="queryInterval">Query Interval (seconds)</Label>
                <Select
                  value={igmpConfig.queryInterval.toString()}
                  onValueChange={(value) => setIgmpConfig(prev => ({ ...prev, queryInterval: parseInt(value) }))}
                >
                  <SelectTrigger className="mt-1">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="60">60</SelectItem>
                    <SelectItem value="125">125</SelectItem>
                    <SelectItem value="250">250</SelectItem>
                    <SelectItem value="300">300</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div>
                <Label htmlFor="maxResponseTime">Max Response Time (seconds)</Label>
                <Select
                  value={igmpConfig.maxResponseTime.toString()}
                  onValueChange={(value) => setIgmpConfig(prev => ({ ...prev, maxResponseTime: parseInt(value) }))}
                >
                  <SelectTrigger className="mt-1">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="5">5</SelectItem>
                    <SelectItem value="10">10</SelectItem>
                    <SelectItem value="15">15</SelectItem>
                    <SelectItem value="20">20</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
          )}

          <Button onClick={handleConfigureIgmp} disabled={apiState.isLoading}>
            Configure IGMP Snooping
          </Button>

          <div className="p-3 bg-green-50 dark:bg-green-900/20 rounded-lg">
            <p className="text-sm text-green-800 dark:text-green-300">
              IGMP Snooping optimizes multicast traffic by forwarding multicast streams only to ports that have requested them, 
              reducing unnecessary network traffic and improving performance.
            </p>
          </div>
        </CardContent>
      </Card>
    </div>
  );
};
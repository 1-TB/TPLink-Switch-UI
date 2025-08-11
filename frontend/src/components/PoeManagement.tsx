import { useState } from 'react';
import { Button } from './ui/button';
import { Input } from './ui/input';
import { Label } from './ui/label';
import { Card, CardContent, CardHeader, CardTitle } from './ui/card';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from './ui/select';
import { Badge } from './ui/badge';
import { Zap, Settings, Power } from 'lucide-react';
import { ApiState } from '../lib/useApi';

interface PoeManagementProps {
  apiState: ApiState;
  onConfigurePoEGlobal: (config: any) => void;
  onConfigurePoEPort: (config: any) => void;
}

export const PoeManagement: React.FC<PoeManagementProps> = ({
  apiState,
  onConfigurePoEGlobal,
  onConfigurePoEPort,
}) => {
  const [globalConfig, setGlobalConfig] = useState({
    enabled: true,
    powerBudget: 370, // Watts
    managementMode: 'auto',
  });
  const [portConfig, setPortConfig] = useState({
    ports: [] as number[],
    state: 2, // 0=disabled, 1=enabled, 2=auto
    powerLimit: 15, // Watts
    priority: 'low',
  });

  const totalPorts = apiState.portInfo?.length || 8;
  const portNumbers = Array.from({ length: totalPorts }, (_, i) => i + 1);

  const togglePortSelection = (port: number) => {
    if (portConfig.ports.includes(port)) {
      setPortConfig(prev => ({ ...prev, ports: prev.ports.filter(p => p !== port) }));
    } else {
      setPortConfig(prev => ({ ...prev, ports: [...prev.ports, port] }));
    }
  };

  const handleConfigureGlobal = () => {
    onConfigurePoEGlobal(globalConfig);
  };

  const handleConfigurePort = () => {
    if (portConfig.ports.length === 0) {
      alert('Please select ports for PoE configuration');
      return;
    }
    onConfigurePoEPort(portConfig);
  };

  const getStateLabel = (state: number) => {
    switch (state) {
      case 0: return 'Disabled';
      case 1: return 'Enabled';
      case 2: return 'Auto';
      default: return 'Auto';
    }
  };

  const getPowerClassInfo = (powerLimit: number) => {
    if (powerLimit <= 4) return { class: '1', description: 'Class 1 - Up to 4W' };
    if (powerLimit <= 7) return { class: '2', description: 'Class 2 - Up to 7W' };
    if (powerLimit <= 15.4) return { class: '3', description: 'Class 3 - Up to 15.4W' };
    if (powerLimit <= 30) return { class: '4', description: 'Class 4 - Up to 30W' };
    return { class: '4+', description: 'High Power - Up to 90W' };
  };

  return (
    <div className="space-y-6">
      {/* PoE Global Configuration */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Settings className="w-5 h-5" />
            Global PoE Configuration
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <Label htmlFor="powerBudget">Power Budget (Watts)</Label>
              <Input
                id="powerBudget"
                type="number"
                min="100"
                max="740"
                step="10"
                value={globalConfig.powerBudget}
                onChange={(e) => 
                  setGlobalConfig(prev => ({ ...prev, powerBudget: parseInt(e.target.value) || 370 }))
                }
                className="mt-1"
              />
              <p className="text-xs text-gray-600 dark:text-gray-400 mt-1">
                Total power budget for all PoE ports
              </p>
            </div>
            <div>
              <Label htmlFor="managementMode">Management Mode</Label>
              <Select
                value={globalConfig.managementMode}
                onValueChange={(value) => setGlobalConfig(prev => ({ ...prev, managementMode: value }))}
              >
                <SelectTrigger className="mt-1">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="auto">Auto</SelectItem>
                  <SelectItem value="manual">Manual</SelectItem>
                  <SelectItem value="classification">Classification</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="flex items-center space-x-2">
            <input
              type="checkbox"
              id="poeEnabled"
              checked={globalConfig.enabled}
              onChange={(e) => setGlobalConfig(prev => ({ ...prev, enabled: e.target.checked }))}
            />
            <Label htmlFor="poeEnabled">Enable PoE System</Label>
          </div>

          <Button onClick={handleConfigureGlobal} disabled={apiState.isLoading}>
            <Settings className="w-4 h-4 mr-2" />
            Configure Global PoE Settings
          </Button>

          <div className="p-3 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
            <p className="text-sm text-blue-800 dark:text-blue-300">
              <strong>Auto:</strong> Automatic power allocation based on device requirements.<br />
              <strong>Manual:</strong> Manual power limit configuration per port.<br />
              <strong>Classification:</strong> Power allocation based on IEEE 802.3af/at classification.
            </p>
          </div>
        </CardContent>
      </Card>

      {/* Per-Port PoE Configuration */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Zap className="w-5 h-5" />
            Per-Port PoE Configuration
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div>
            <Label className="text-sm font-medium">Select Ports</Label>
            <div className="grid grid-cols-4 gap-2 mt-2">
              {portNumbers.map(port => (
                <Button
                  key={port}
                  variant={portConfig.ports.includes(port) ? "default" : "outline"}
                  size="sm"
                  onClick={() => togglePortSelection(port)}
                  className="h-8"
                >
                  {port}
                </Button>
              ))}
            </div>
            {portConfig.ports.length > 0 && (
              <div className="mt-2 flex flex-wrap gap-1">
                {portConfig.ports.map(port => (
                  <Badge key={port} variant="secondary" className="text-xs">
                    Port {port}
                  </Badge>
                ))}
              </div>
            )}
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <Label htmlFor="poeState">PoE State</Label>
              <Select
                value={portConfig.state.toString()}
                onValueChange={(value) => setPortConfig(prev => ({ ...prev, state: parseInt(value) }))}
              >
                <SelectTrigger className="mt-1">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="0">Disabled</SelectItem>
                  <SelectItem value="1">Enabled</SelectItem>
                  <SelectItem value="2">Auto</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div>
              <Label htmlFor="powerLimit">Power Limit (Watts)</Label>
              <Input
                id="powerLimit"
                type="number"
                min="4"
                max="90"
                step="0.1"
                value={portConfig.powerLimit}
                onChange={(e) => 
                  setPortConfig(prev => ({ ...prev, powerLimit: parseFloat(e.target.value) || 15 }))
                }
                className="mt-1"
              />
              <div className="mt-1">
                <Badge variant="secondary" className="text-xs">
                  {getPowerClassInfo(portConfig.powerLimit).description}
                </Badge>
              </div>
            </div>
            <div>
              <Label htmlFor="priority">Priority</Label>
              <Select
                value={portConfig.priority}
                onValueChange={(value) => setPortConfig(prev => ({ ...prev, priority: value }))}
              >
                <SelectTrigger className="mt-1">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="low">Low</SelectItem>
                  <SelectItem value="medium">Medium</SelectItem>
                  <SelectItem value="high">High</SelectItem>
                  <SelectItem value="critical">Critical</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          <Button onClick={handleConfigurePort} disabled={apiState.isLoading}>
            <Zap className="w-4 h-4 mr-2" />
            Configure Port PoE Settings
          </Button>

          <div className="p-3 bg-green-50 dark:bg-green-900/20 rounded-lg">
            <p className="text-sm text-green-800 dark:text-green-300">
              Configure individual port PoE settings. Higher priority ports get power preference during power budget constraints.
            </p>
          </div>
        </CardContent>
      </Card>

      {/* PoE Status Information */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Power className="w-5 h-5" />
            PoE Power Classes
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-2">
              <h4 className="font-medium">IEEE 802.3af (PoE)</h4>
              <div className="space-y-1 text-sm">
                <div className="flex justify-between">
                  <span>Class 0:</span>
                  <span>0.44 - 12.95W</span>
                </div>
                <div className="flex justify-between">
                  <span>Class 1:</span>
                  <span>0.44 - 3.84W</span>
                </div>
                <div className="flex justify-between">
                  <span>Class 2:</span>
                  <span>3.84 - 6.49W</span>
                </div>
                <div className="flex justify-between">
                  <span>Class 3:</span>
                  <span>6.49 - 12.95W</span>
                </div>
              </div>
            </div>
            <div className="space-y-2">
              <h4 className="font-medium">IEEE 802.3at (PoE+)</h4>
              <div className="space-y-1 text-sm">
                <div className="flex justify-between">
                  <span>Class 4:</span>
                  <span>12.95 - 25.5W</span>
                </div>
                <div className="flex justify-between">
                  <span>Type 1:</span>
                  <span>Up to 15.4W</span>
                </div>
                <div className="flex justify-between">
                  <span>Type 2:</span>
                  <span>Up to 30W</span>
                </div>
                <div className="flex justify-between">
                  <span>High Power:</span>
                  <span>Up to 90W</span>
                </div>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
};
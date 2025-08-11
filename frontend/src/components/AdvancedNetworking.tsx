import { useState } from 'react';
import { Button } from './ui/button';
import { Input } from './ui/input';
import { Label } from './ui/label';
import { Card, CardContent, CardHeader, CardTitle } from './ui/card';
import { Switch } from './ui/switch';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from './ui/select';
import { Badge } from './ui/badge';
import { Network, Shield, BarChart3, Trash2 } from 'lucide-react';
import { ApiState } from '../lib/useApi';

interface AdvancedNetworkingProps {
  apiState: ApiState;
  onClearPortStatistics: (ports: number[]) => void;
  onConfigureMirroring: (config: any) => void;
  onConfigureTrunking: (config: any) => void;
  onConfigureLoopPrevention: (config: any) => void;
}

export const AdvancedNetworking: React.FC<AdvancedNetworkingProps> = ({
  apiState,
  onClearPortStatistics,
  onConfigureMirroring,
  onConfigureTrunking,
  onConfigureLoopPrevention,
}) => {
  const [selectedPorts, setSelectedPorts] = useState<number[]>([]);
  const [mirroringConfig, setMirroringConfig] = useState({
    enabled: false,
    mirrorDestinationPort: 1,
    mirrorPorts: [] as number[],
  });
  const [trunkingConfig, setTrunkingConfig] = useState({
    trunkId: 1,
    ports: [] as number[],
    enabled: true,
  });
  const [loopPreventionConfig, setLoopPreventionConfig] = useState({
    enabled: true,
    ports: [] as number[],
    action: 'block',
  });

  const totalPorts = apiState.portInfo?.length || 8;
  const portNumbers = Array.from({ length: totalPorts }, (_, i) => i + 1);

  const togglePortSelection = (port: number, currentSelection: number[], setter: (ports: number[]) => void) => {
    if (currentSelection.includes(port)) {
      setter(currentSelection.filter(p => p !== port));
    } else {
      setter([...currentSelection, port]);
    }
  };

  const handleClearStatistics = () => {
    if (selectedPorts.length === 0) {
      alert('Please select ports to clear statistics for');
      return;
    }
    onClearPortStatistics(selectedPorts);
    setSelectedPorts([]);
  };

  const handleConfigureMirroring = () => {
    if (mirroringConfig.enabled && mirroringConfig.mirrorPorts.length === 0) {
      alert('Please select source ports for mirroring');
      return;
    }
    if (mirroringConfig.enabled && mirroringConfig.mirrorPorts.includes(mirroringConfig.mirrorDestinationPort)) {
      alert('Destination port cannot be the same as source port');
      return;
    }
    onConfigureMirroring(mirroringConfig);
  };

  const handleConfigureTrunking = () => {
    if (trunkingConfig.ports.length < 2) {
      alert('Please select at least 2 ports for trunking');
      return;
    }
    onConfigureTrunking(trunkingConfig);
  };

  const handleConfigureLoopPrevention = () => {
    onConfigureLoopPrevention(loopPreventionConfig);
  };

  const PortSelector = ({ 
    label, 
    selectedPorts, 
    onChange, 
    excludePorts = [] 
  }: { 
    label: string; 
    selectedPorts: number[]; 
    onChange: (ports: number[]) => void;
    excludePorts?: number[];
  }) => (
    <div>
      <Label className="text-sm font-medium">{label}</Label>
      <div className="grid grid-cols-4 gap-2 mt-2">
        {portNumbers.filter(port => !excludePorts.includes(port)).map(port => (
          <Button
            key={port}
            variant={selectedPorts.includes(port) ? "default" : "outline"}
            size="sm"
            onClick={() => togglePortSelection(port, selectedPorts, onChange)}
            className="h-8"
          >
            {port}
          </Button>
        ))}
      </div>
      {selectedPorts.length > 0 && (
        <div className="mt-2 flex flex-wrap gap-1">
          {selectedPorts.map(port => (
            <Badge key={port} variant="secondary" className="text-xs">
              Port {port}
            </Badge>
          ))}
        </div>
      )}
    </div>
  );

  return (
    <div className="space-y-6">
      {/* Port Statistics Management */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <BarChart3 className="w-5 h-5" />
            Port Statistics Management
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <PortSelector
            label="Select Ports to Clear Statistics"
            selectedPorts={selectedPorts}
            onChange={setSelectedPorts}
          />
          <Button 
            onClick={handleClearStatistics} 
            disabled={apiState.isLoading || selectedPorts.length === 0}
            variant="destructive"
          >
            <Trash2 className="w-4 h-4 mr-2" />
            Clear Statistics for Selected Ports
          </Button>
          <p className="text-sm text-gray-600 dark:text-gray-400">
            This will reset all traffic counters for the selected ports.
          </p>
        </CardContent>
      </Card>

      {/* Port Mirroring */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Network className="w-5 h-5" />
            Port Mirroring
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center space-x-2">
            <Switch
              checked={mirroringConfig.enabled}
              onCheckedChange={(checked) => 
                setMirroringConfig(prev => ({ ...prev, enabled: checked }))
              }
            />
            <Label>Enable Port Mirroring</Label>
          </div>

          {mirroringConfig.enabled && (
            <div className="space-y-4">
              <div>
                <Label htmlFor="mirrorDestination">Mirror Destination Port</Label>
                <Select
                  value={mirroringConfig.mirrorDestinationPort.toString()}
                  onValueChange={(value) => 
                    setMirroringConfig(prev => ({ ...prev, mirrorDestinationPort: parseInt(value) }))
                  }
                >
                  <SelectTrigger className="mt-1">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {portNumbers.map(port => (
                      <SelectItem key={port} value={port.toString()}>
                        Port {port}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <p className="text-xs text-gray-600 dark:text-gray-400 mt-1">
                  Mirrored traffic will be sent to this port
                </p>
              </div>

              <PortSelector
                label="Source Ports to Mirror"
                selectedPorts={mirroringConfig.mirrorPorts}
                onChange={(ports) => setMirroringConfig(prev => ({ ...prev, mirrorPorts: ports }))}
                excludePorts={[mirroringConfig.mirrorDestinationPort]}
              />
            </div>
          )}

          <Button onClick={handleConfigureMirroring} disabled={apiState.isLoading}>
            <Network className="w-4 h-4 mr-2" />
            {mirroringConfig.enabled ? 'Configure' : 'Disable'} Port Mirroring
          </Button>

          <div className="p-3 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
            <p className="text-sm text-blue-800 dark:text-blue-300">
              Port mirroring copies traffic from source ports to a destination port for monitoring and analysis.
            </p>
          </div>
        </CardContent>
      </Card>

      {/* Port Trunking/LAG */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Network className="w-5 h-5" />
            Port Trunking (Link Aggregation)
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <Label htmlFor="trunkId">Trunk Group ID</Label>
              <Input
                id="trunkId"
                type="number"
                min="1"
                max="8"
                value={trunkingConfig.trunkId}
                onChange={(e) => 
                  setTrunkingConfig(prev => ({ ...prev, trunkId: parseInt(e.target.value) || 1 }))
                }
                className="mt-1"
              />
            </div>
            <div className="flex items-center space-x-2">
              <Switch
                checked={trunkingConfig.enabled}
                onCheckedChange={(checked) => 
                  setTrunkingConfig(prev => ({ ...prev, enabled: checked }))
                }
              />
              <Label>Enable Trunk Group</Label>
            </div>
          </div>

          <PortSelector
            label="Select Ports for Trunk Group"
            selectedPorts={trunkingConfig.ports}
            onChange={(ports) => setTrunkingConfig(prev => ({ ...prev, ports }))}
          />

          <Button 
            onClick={handleConfigureTrunking} 
            disabled={apiState.isLoading || trunkingConfig.ports.length < 2}
          >
            <Network className="w-4 h-4 mr-2" />
            Configure Port Trunking
          </Button>

          <div className="p-3 bg-green-50 dark:bg-green-900/20 rounded-lg">
            <p className="text-sm text-green-800 dark:text-green-300">
              Port trunking combines multiple ports into a single logical link for increased bandwidth and redundancy.
            </p>
          </div>
        </CardContent>
      </Card>

      {/* Loop Prevention */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Shield className="w-5 h-5" />
            Loop Prevention
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center space-x-2">
            <Switch
              checked={loopPreventionConfig.enabled}
              onCheckedChange={(checked) => 
                setLoopPreventionConfig(prev => ({ ...prev, enabled: checked }))
              }
            />
            <Label>Enable Loop Prevention</Label>
          </div>

          {loopPreventionConfig.enabled && (
            <div className="space-y-4">
              <div>
                <Label htmlFor="loopAction">Action on Loop Detection</Label>
                <Select
                  value={loopPreventionConfig.action}
                  onValueChange={(value) => 
                    setLoopPreventionConfig(prev => ({ ...prev, action: value }))
                  }
                >
                  <SelectTrigger className="mt-1">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="block">Block Port</SelectItem>
                    <SelectItem value="shutdown">Shutdown Port</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <PortSelector
                label="Ports to Monitor for Loops"
                selectedPorts={loopPreventionConfig.ports}
                onChange={(ports) => setLoopPreventionConfig(prev => ({ ...prev, ports }))}
              />
            </div>
          )}

          <Button onClick={handleConfigureLoopPrevention} disabled={apiState.isLoading}>
            <Shield className="w-4 h-4 mr-2" />
            Configure Loop Prevention
          </Button>

          <div className="p-3 bg-orange-50 dark:bg-orange-900/20 rounded-lg">
            <p className="text-sm text-orange-800 dark:text-orange-300">
              Loop prevention detects and prevents network loops that can cause broadcast storms and network instability.
            </p>
          </div>
        </CardContent>
      </Card>
    </div>
  );
};
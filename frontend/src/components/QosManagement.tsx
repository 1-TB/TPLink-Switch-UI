import { useState } from 'react';
import { Button } from './ui/button';
import { Input } from './ui/input';
import { Label } from './ui/label';
import { Card, CardContent, CardHeader, CardTitle } from './ui/card';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from './ui/select';
import { Badge } from './ui/badge';
import { Slider } from './ui/slider';
import { Gauge, Zap, Shield, Clock } from 'lucide-react';
import { ApiState } from '../lib/useApi';

interface QosManagementProps {
  apiState: ApiState;
  onConfigureQosMode: (mode: string) => void;
  onConfigureBandwidthControl: (config: any) => void;
  onConfigurePortPriority: (config: any) => void;
  onConfigureStormControl: (config: any) => void;
}

export const QosManagement: React.FC<QosManagementProps> = ({
  apiState,
  onConfigureQosMode,
  onConfigureBandwidthControl,
  onConfigurePortPriority,
  onConfigureStormControl,
}) => {
  const [qosMode, setQosMode] = useState('port-based');
  const [bandwidthConfig, setBandwidthConfig] = useState({
    ports: [] as number[],
    ingressRate: 100,
    egressRate: 100,
    unit: 'Mbps',
  });
  const [priorityConfig, setPriorityConfig] = useState({
    ports: [] as number[],
    priority: 4,
  });
  const [stormConfig, setStormConfig] = useState({
    ports: [] as number[],
    broadcastEnabled: true,
    multicastEnabled: true,
    unicastEnabled: false,
    rate: 100,
    unit: 'pps',
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

  const handleConfigureQosMode = () => {
    onConfigureQosMode(qosMode);
  };

  const handleConfigureBandwidth = () => {
    if (bandwidthConfig.ports.length === 0) {
      alert('Please select ports for bandwidth control');
      return;
    }
    const config = {
      ...bandwidthConfig,
      ingressRate: bandwidthConfig.unit === 'Mbps' ? bandwidthConfig.ingressRate * 1000 : bandwidthConfig.ingressRate,
      egressRate: bandwidthConfig.unit === 'Mbps' ? bandwidthConfig.egressRate * 1000 : bandwidthConfig.egressRate,
    };
    onConfigureBandwidthControl(config);
  };

  const handleConfigurePriority = () => {
    if (priorityConfig.ports.length === 0) {
      alert('Please select ports for priority configuration');
      return;
    }
    onConfigurePortPriority(priorityConfig);
  };

  const handleConfigureStormControl = () => {
    if (stormConfig.ports.length === 0) {
      alert('Please select ports for storm control');
      return;
    }
    onConfigureStormControl(stormConfig);
  };

  const PortSelector = ({ 
    label, 
    selectedPorts, 
    onChange 
  }: { 
    label: string; 
    selectedPorts: number[]; 
    onChange: (ports: number[]) => void;
  }) => (
    <div>
      <Label className="text-sm font-medium">{label}</Label>
      <div className="grid grid-cols-4 gap-2 mt-2">
        {portNumbers.map(port => (
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

  const getPriorityLabel = (priority: number) => {
    const labels = ['Lowest', 'Low', 'Below Normal', 'Normal', 'Above Normal', 'High', 'Higher', 'Highest'];
    return labels[priority] || 'Normal';
  };

  return (
    <div className="space-y-6">
      {/* QoS Mode Configuration */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Gauge className="w-5 h-5" />
            QoS Mode Configuration
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div>
            <Label htmlFor="qosMode">QoS Mode</Label>
            <Select value={qosMode} onValueChange={setQosMode}>
              <SelectTrigger className="mt-1">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="port-based">Port-based Priority</SelectItem>
                <SelectItem value="dscp">DSCP Priority</SelectItem>
                <SelectItem value="cos">CoS Priority</SelectItem>
                <SelectItem value="weighted">Weighted Round Robin</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <Button onClick={handleConfigureQosMode} disabled={apiState.isLoading}>
            <Gauge className="w-4 h-4 mr-2" />
            Configure QoS Mode
          </Button>

          <div className="p-3 bg-blue-50 dark:bg-blue-900/20 rounded-lg">
            <p className="text-sm text-blue-800 dark:text-blue-300">
              <strong>Port-based:</strong> Priority based on ingress port. <br />
              <strong>DSCP:</strong> Priority based on DSCP field in IP header. <br />
              <strong>CoS:</strong> Priority based on Class of Service in VLAN tag. <br />
              <strong>Weighted:</strong> Time-sliced priority scheduling.
            </p>
          </div>
        </CardContent>
      </Card>

      {/* Bandwidth Control */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Zap className="w-5 h-5" />
            Bandwidth Control
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <PortSelector
            label="Select Ports for Bandwidth Control"
            selectedPorts={bandwidthConfig.ports}
            onChange={(ports) => setBandwidthConfig(prev => ({ ...prev, ports }))}
          />

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <Label htmlFor="ingressRate">Ingress Rate</Label>
              <Input
                id="ingressRate"
                type="number"
                min="1"
                max={bandwidthConfig.unit === 'Mbps' ? '1000' : '1000000'}
                value={bandwidthConfig.ingressRate}
                onChange={(e) => 
                  setBandwidthConfig(prev => ({ ...prev, ingressRate: parseInt(e.target.value) || 1 }))
                }
                className="mt-1"
              />
            </div>
            <div>
              <Label htmlFor="egressRate">Egress Rate</Label>
              <Input
                id="egressRate"
                type="number"
                min="1"
                max={bandwidthConfig.unit === 'Mbps' ? '1000' : '1000000'}
                value={bandwidthConfig.egressRate}
                onChange={(e) => 
                  setBandwidthConfig(prev => ({ ...prev, egressRate: parseInt(e.target.value) || 1 }))
                }
                className="mt-1"
              />
            </div>
            <div>
              <Label htmlFor="bandwidthUnit">Unit</Label>
              <Select
                value={bandwidthConfig.unit}
                onValueChange={(value) => setBandwidthConfig(prev => ({ ...prev, unit: value }))}
              >
                <SelectTrigger className="mt-1">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Kbps">Kbps</SelectItem>
                  <SelectItem value="Mbps">Mbps</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          <Button onClick={handleConfigureBandwidth} disabled={apiState.isLoading}>
            <Zap className="w-4 h-4 mr-2" />
            Configure Bandwidth Control
          </Button>

          <div className="p-3 bg-green-50 dark:bg-green-900/20 rounded-lg">
            <p className="text-sm text-green-800 dark:text-green-300">
              Rate limiting controls the maximum bandwidth available to selected ports for ingress (incoming) and egress (outgoing) traffic.
            </p>
          </div>
        </CardContent>
      </Card>

      {/* Port Priority */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Clock className="w-5 h-5" />
            Port Priority Configuration
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <PortSelector
            label="Select Ports for Priority Configuration"
            selectedPorts={priorityConfig.ports}
            onChange={(ports) => setPriorityConfig(prev => ({ ...prev, ports }))}
          />

          <div>
            <Label>Priority Level: {priorityConfig.priority} - {getPriorityLabel(priorityConfig.priority)}</Label>
            <div className="mt-2">
              <Slider
                value={[priorityConfig.priority]}
                onValueChange={(value) => setPriorityConfig(prev => ({ ...prev, priority: value[0] }))}
                max={7}
                min={0}
                step={1}
                className="w-full"
              />
              <div className="flex justify-between text-xs text-gray-500 mt-1">
                <span>0 (Lowest)</span>
                <span>7 (Highest)</span>
              </div>
            </div>
          </div>

          <Button onClick={handleConfigurePriority} disabled={apiState.isLoading}>
            <Clock className="w-4 h-4 mr-2" />
            Configure Port Priority
          </Button>

          <div className="p-3 bg-purple-50 dark:bg-purple-900/20 rounded-lg">
            <p className="text-sm text-purple-800 dark:text-purple-300">
              Higher priority ports get preferential treatment during network congestion. Priority 7 is highest, 0 is lowest.
            </p>
          </div>
        </CardContent>
      </Card>

      {/* Storm Control */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Shield className="w-5 h-5" />
            Storm Control
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <PortSelector
            label="Select Ports for Storm Control"
            selectedPorts={stormConfig.ports}
            onChange={(ports) => setStormConfig(prev => ({ ...prev, ports }))}
          />

          <div>
            <Label className="text-sm font-medium">Storm Types to Control</Label>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mt-2">
              <div className="flex items-center space-x-2">
                <input
                  type="checkbox"
                  id="broadcast"
                  checked={stormConfig.broadcastEnabled}
                  onChange={(e) => setStormConfig(prev => ({ ...prev, broadcastEnabled: e.target.checked }))}
                />
                <Label htmlFor="broadcast" className="text-sm">Broadcast</Label>
              </div>
              <div className="flex items-center space-x-2">
                <input
                  type="checkbox"
                  id="multicast"
                  checked={stormConfig.multicastEnabled}
                  onChange={(e) => setStormConfig(prev => ({ ...prev, multicastEnabled: e.target.checked }))}
                />
                <Label htmlFor="multicast" className="text-sm">Multicast</Label>
              </div>
              <div className="flex items-center space-x-2">
                <input
                  type="checkbox"
                  id="unicast"
                  checked={stormConfig.unicastEnabled}
                  onChange={(e) => setStormConfig(prev => ({ ...prev, unicastEnabled: e.target.checked }))}
                />
                <Label htmlFor="unicast" className="text-sm">Unknown Unicast</Label>
              </div>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <Label htmlFor="stormRate">Storm Control Rate</Label>
              <Input
                id="stormRate"
                type="number"
                min="1"
                max={stormConfig.unit === 'pps' ? '1000000' : '1000'}
                value={stormConfig.rate}
                onChange={(e) => 
                  setStormConfig(prev => ({ ...prev, rate: parseInt(e.target.value) || 1 }))
                }
                className="mt-1"
              />
            </div>
            <div>
              <Label htmlFor="stormUnit">Unit</Label>
              <Select
                value={stormConfig.unit}
                onValueChange={(value) => setStormConfig(prev => ({ ...prev, unit: value }))}
              >
                <SelectTrigger className="mt-1">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="pps">Packets per second (pps)</SelectItem>
                  <SelectItem value="kbps">Kilobits per second (kbps)</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          <Button onClick={handleConfigureStormControl} disabled={apiState.isLoading}>
            <Shield className="w-4 h-4 mr-2" />
            Configure Storm Control
          </Button>

          <div className="p-3 bg-orange-50 dark:bg-orange-900/20 rounded-lg">
            <p className="text-sm text-orange-800 dark:text-orange-300">
              Storm control prevents broadcast, multicast, or unknown unicast storms from overwhelming the network by limiting their transmission rate.
            </p>
          </div>
        </CardContent>
      </Card>
    </div>
  );
};
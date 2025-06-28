import { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from './ui/card';
import { Button } from './ui/button';
import { Activity, Zap, AlertTriangle } from 'lucide-react';

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

interface SwitchPortLayoutProps {
  ports: PortInfo[];
  vlanConfig?: VlanConfigResponse | null;
  onConfigurePort: (port: number, enable: boolean) => Promise<void>;
  onRunDiagnostic: (port: number) => Promise<void>;
}

export default function SwitchPortLayout({ ports, vlanConfig, onConfigurePort, onRunDiagnostic }: SwitchPortLayoutProps) {
  const [selectedPort, setSelectedPort] = useState<number | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const getPortColor = (port: PortInfo) => {
    if (!port.isEnabled) return 'bg-gray-400';
    if (!port.isConnected) return 'bg-gray-500';
    
    // Check speed for color coding
    if (port.speedActual.includes('1000') || port.speedActual.includes('1G')) {
      return 'bg-green-500';
    } else if (port.speedActual.includes('100')) {
      return 'bg-orange-500';
    } else if (port.speedActual.includes('10')) {
      return 'bg-yellow-500';
    }
    
    return 'bg-gray-500';
  };

  const getPortIcon = (port: PortInfo) => {
    if (!port.isEnabled) return null;
    if (!port.isConnected) return null;
    
    if (port.speedActual.includes('1000') || port.speedActual.includes('1G')) {
      return <Zap className="h-3 w-3 text-white" />;
    } else if (port.speedActual.includes('100') || port.speedActual.includes('10')) {
      return <Activity className="h-3 w-3 text-white" />;
    }
    
    return null;
  };

  const handlePortClick = (port: PortInfo) => {
    setSelectedPort(port.portNumber);
  };

  const handleTogglePort = async (port: number, currentlyEnabled: boolean) => {
    setIsLoading(true);
    try {
      await onConfigurePort(port, !currentlyEnabled);
    } finally {
      setIsLoading(false);
    }
  };

  const handleRunDiagnostic = async (port: number) => {
    setIsLoading(true);
    try {
      await onRunDiagnostic(port);
    } finally {
      setIsLoading(false);
    }
  };

  const renderEthernetPort = (port: PortInfo) => {
    const isSelected = selectedPort === port.portNumber;
    
    return (
      <div
        key={port.portNumber}
        className={`relative cursor-pointer transition-all duration-200 group ${
          isSelected ? 'scale-110 z-10' : ''
        }`}
        onClick={() => handlePortClick(port)}
      >
        {/* Ethernet port shape */}
        <div className={`
          w-12 h-8 rounded-sm border-2 border-gray-300 relative
          ${getPortColor(port)} 
          ${isSelected ? 'ring-2 ring-blue-500 ring-offset-2' : ''}
          shadow-sm hover:shadow-md transition-shadow
        `}>
          {/* Port number label */}
          <div className="absolute -top-5 left-1/2 transform -translate-x-1/2 text-xs font-medium text-foreground">
            {port.portNumber}
          </div>
          
          {/* Status icon */}
          <div className="absolute inset-0 flex items-center justify-center">
            {getPortIcon(port)}
          </div>
          
          {/* LED indicators */}
          <div className="absolute top-1 right-1 flex flex-col gap-0.5">
            <div className={`w-1 h-1 rounded-full ${port.isEnabled ? 'bg-blue-400' : 'bg-gray-600'}`} />
            <div className={`w-1 h-1 rounded-full ${port.isConnected ? 'bg-green-400' : 'bg-red-400'}`} />
          </div>
          
          {/* Ethernet connector notch */}
          <div className="absolute bottom-0 left-1/2 transform -translate-x-1/2 w-3 h-1 bg-gray-600 rounded-t-sm" />
        </div>
        
        {/* Hover tooltip */}
        <div className="absolute -top-16 left-1/2 transform -translate-x-1/2 bg-black text-white text-xs px-2 py-1 rounded opacity-0 group-hover:opacity-100 transition-opacity duration-200 pointer-events-none whitespace-nowrap z-20">
          Port {port.portNumber}: {port.isConnected ? port.speedActual : 'Down'} ({port.status})
          {(() => {
            const portVlans = getPortVlans(port.portNumber);
            if (portVlans.length > 0) {
              return (
                <>
                  <br />
                  VLANs: {portVlans.map(v => v.vlanId).join(', ')}
                </>
              );
            }
            return null;
          })()}
        </div>
      </div>
    );
  };

  const selectedPortInfo = ports.find(p => p.portNumber === selectedPort);
  
  // Get VLANs for the selected port
  const getPortVlans = (portNumber: number) => {
    if (!vlanConfig || !vlanConfig.vlans) return [];
    return vlanConfig.vlans.filter(vlan => vlan.portNumbers.includes(portNumber));
  };

  // Arrange ports in 12x2 layout for 24-port switch
  const topRowPorts = ports.filter(p => p.portNumber <= 12);
  const bottomRowPorts = ports.filter(p => p.portNumber > 12 && p.portNumber <= 24);

  return (
    <div className="space-y-6">
      {/* Switch Visual Layout */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <div className="w-3 h-3 bg-blue-500 rounded-full animate-pulse" />
            24-Port Gigabit Switch
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="bg-gray-100 dark:bg-gray-800 p-6 rounded-lg">
            {/* Switch body */}
            <div className="bg-gray-200 dark:bg-gray-700 p-4 rounded-lg shadow-inner">
              {/* Top row (ports 1-12) */}
              <div className="flex justify-center gap-2 mb-8">
                {topRowPorts.map(port => renderEthernetPort(port))}
              </div>
              
              {/* Bottom row (ports 13-24) */}
              <div className="flex justify-center gap-2">
                {bottomRowPorts.map(port => renderEthernetPort(port))}
              </div>
            </div>
            
            {/* Legend */}
            <div className="mt-6 flex flex-wrap gap-4 text-sm">
              <div className="flex items-center gap-2">
                <div className="w-4 h-3 bg-green-500 rounded-sm" />
                <span>1000 Mbps</span>
              </div>
              <div className="flex items-center gap-2">
                <div className="w-4 h-3 bg-orange-500 rounded-sm" />
                <span>100 Mbps</span>
              </div>
              <div className="flex items-center gap-2">
                <div className="w-4 h-3 bg-yellow-500 rounded-sm" />
                <span>10 Mbps</span>
              </div>
              <div className="flex items-center gap-2">
                <div className="w-4 h-3 bg-gray-500 rounded-sm" />
                <span>Link Down</span>
              </div>
              <div className="flex items-center gap-2">
                <div className="w-4 h-3 bg-gray-400 rounded-sm" />
                <span>Disabled</span>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Port Details Panel */}
      {selectedPortInfo && (
        <Card>
          <CardHeader>
            <CardTitle>Port {selectedPortInfo.portNumber} Details</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-4">
              <div>
                <span className="text-sm font-medium text-muted-foreground">Status</span>
                <div className={`font-medium ${selectedPortInfo.isEnabled ? 'text-green-600' : 'text-red-500'}`}>
                  {selectedPortInfo.status}
                </div>
              </div>
              <div>
                <span className="text-sm font-medium text-muted-foreground">Link Speed</span>
                <div className="font-medium">{selectedPortInfo.speedActual}</div>
              </div>
              <div>
                <span className="text-sm font-medium text-muted-foreground">Configured Speed</span>
                <div className="font-medium">{selectedPortInfo.speedConfig}</div>
              </div>
              <div>
                <span className="text-sm font-medium text-muted-foreground">Flow Control</span>
                <div className="font-medium">{selectedPortInfo.flowControlActual}</div>
              </div>
            </div>
            
            {selectedPortInfo.trunk && (
              <div className="mb-4">
                <span className="text-sm font-medium text-muted-foreground">Trunk</span>
                <div className="font-medium">{selectedPortInfo.trunk}</div>
              </div>
            )}
            
            {/* VLAN Information */}
            <div className="mb-4">
              <span className="text-sm font-medium text-muted-foreground">VLANs</span>
              <div className="mt-1">
                {(() => {
                  const portVlans = getPortVlans(selectedPortInfo.portNumber);
                  if (portVlans.length === 0) {
                    return <span className="text-muted-foreground text-sm">No VLANs configured</span>;
                  }
                  return (
                    <div className="flex flex-wrap gap-1">
                      {portVlans.map(vlan => (
                        <span key={vlan.vlanId} className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200">
                          VLAN {vlan.vlanId}
                        </span>
                      ))}
                    </div>
                  );
                })()}
              </div>
            </div>
            
            <div className="flex gap-2">
              <Button
                onClick={() => handleTogglePort(selectedPortInfo.portNumber, selectedPortInfo.isEnabled)}
                variant={selectedPortInfo.isEnabled ? "destructive" : "default"}
                disabled={isLoading}
              >
                {selectedPortInfo.isEnabled ? 'Disable Port' : 'Enable Port'}
              </Button>
              <Button
                onClick={() => handleRunDiagnostic(selectedPortInfo.portNumber)}
                variant="outline"
                disabled={isLoading}
              >
                <AlertTriangle className="h-4 w-4 mr-1" />
                Run Diagnostic
              </Button>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
import { Card, CardContent, CardHeader, CardTitle } from './ui/card';
import { Button } from './ui/button';
import { History } from 'lucide-react';

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

interface PortInfoCardProps {
  ports: PortInfo[];
  onConfigurePort: (port: number, enable: boolean) => Promise<void>;
  onRunDiagnostics: (ports: number[]) => Promise<void>;
  onViewHistory?: (portNumber: number) => void;
}

export default function PortInfoCard({ ports, onConfigurePort, onRunDiagnostics, onViewHistory }: PortInfoCardProps) {
  const handleTogglePort = async (port: number, currentlyEnabled: boolean) => {
    await onConfigurePort(port, !currentlyEnabled);
  };

  const handleRunDiagnostics = async () => {
    const allPorts = ports.map(p => p.portNumber);
    await onRunDiagnostics(allPorts);
  };

  const getStatusColor = (port: PortInfo) => {
    if (!port.isEnabled) return 'text-gray-500';
    if (port.isConnected) return 'text-green-600';
    return 'text-red-500';
  };

  const getStatusIcon = (port: PortInfo) => {
    if (!port.isEnabled) return '⚫';
    if (port.isConnected) return '🟢';
    return '🔴';
  };

  return (
    <Card className="mt-4">
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle>Port Information</CardTitle>
        <Button onClick={handleRunDiagnostics} variant="outline" size="sm">
          Run Cable Diagnostics
        </Button>
      </CardHeader>
      <CardContent>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
          {ports.map((port) => (
            <div
              key={port.portNumber}
              className="border rounded-lg p-3 space-y-2"
            >
              <div className="flex items-center justify-between">
                <span className="font-medium">Port {port.portNumber}</span>
                <span className={getStatusColor(port)}>
                  {getStatusIcon(port)}
                </span>
              </div>
              
              <div className="text-sm space-y-1">
                <div>Status: <span className={getStatusColor(port)}>{port.status}</span></div>
                <div>Speed: {port.speedActual}</div>
                {port.trunk && <div>Trunk: {port.trunk}</div>}
              </div>
              
              <div className="flex gap-1">
                <Button
                  size="sm"
                  variant={port.isEnabled ? "destructive" : "default"}
                  onClick={() => handleTogglePort(port.portNumber, port.isEnabled)}
                  className="flex-1"
                >
                  {port.isEnabled ? 'Disable' : 'Enable'}
                </Button>
                {onViewHistory && (
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => onViewHistory(port.portNumber)}
                    className="px-2"
                    title="View History"
                  >
                    <History className="h-3 w-3" />
                  </Button>
                )}
              </div>
            </div>
          ))}
        </div>
        
        {ports.length === 0 && (
          <div className="text-center text-muted-foreground py-8">
            No port information available
          </div>
        )}
      </CardContent>
    </Card>
  );
}
import { Card, CardContent, CardHeader, CardTitle } from './ui/card';

interface PortDiagnostic {
  portNumber: number;
  state: number;
  stateDescription: string;
  length: number;
  isHealthy: boolean;
  hasIssue: boolean;
  isUntested: boolean;
  isDisconnected: boolean;
}

interface DiagnosticsCardProps {
  diagnostics: PortDiagnostic[];
}

export default function DiagnosticsCard({ diagnostics }: DiagnosticsCardProps) {
  const getStatusColor = (diagnostic: PortDiagnostic) => {
    if (diagnostic.isHealthy) return 'text-green-600';
    if (diagnostic.hasIssue) return 'text-red-500';
    if (diagnostic.isDisconnected) return 'text-gray-500';
    return 'text-yellow-500';
  };

  const getStatusIcon = (diagnostic: PortDiagnostic) => {
    if (diagnostic.isHealthy) return '✅';
    if (diagnostic.hasIssue) return '❌';
    if (diagnostic.isDisconnected) return '🔌';
    return '❓';
  };

  return (
    <Card className="mt-4">
      <CardHeader>
        <CardTitle>Cable Diagnostics Results</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
          {diagnostics.map((diagnostic) => (
            <div
              key={diagnostic.portNumber}
              className="border rounded-lg p-3 space-y-2"
            >
              <div className="flex items-center justify-between">
                <span className="font-medium">Port {diagnostic.portNumber}</span>
                <span className={getStatusColor(diagnostic)}>
                  {getStatusIcon(diagnostic)}
                </span>
              </div>
              
              <div className="text-sm space-y-1">
                <div>
                  Status: <span className={getStatusColor(diagnostic)}>
                    {diagnostic.stateDescription}
                  </span>
                </div>
                {diagnostic.length >= 0 && (
                  <div>Length: {diagnostic.length}m</div>
                )}
              </div>
            </div>
          ))}
        </div>
        
        {diagnostics.length === 0 && (
          <div className="text-center text-muted-foreground py-8">
            No diagnostic results available
          </div>
        )}
      </CardContent>
    </Card>
  );
}
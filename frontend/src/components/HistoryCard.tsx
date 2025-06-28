import { useState, useEffect } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from './ui/card';
import { Button } from './ui/button';
import { Clock, Activity, Zap, Settings, Network, RefreshCw, ChevronDown, ChevronUp } from 'lucide-react';

interface PortHistoryEntry {
  id: number;
  portNumber: number;
  timestamp: string;
  status: string;
  speedConfig: string;
  speedActual: string;
  flowControlConfig: string;
  flowControlActual: string;
  trunk: string;
  changeType: string;
  previousValue?: string;
  newValue?: string;
  notes?: string;
}

interface CableDiagnosticHistoryEntry {
  id: number;
  portNumber: number;
  timestamp: string;
  state: number;
  stateDescription: string;
  length: number;
  isHealthy: boolean;
  hasIssue: boolean;
  isUntested: boolean;
  isDisconnected: boolean;
  testTrigger: string;
  notes?: string;
}

interface SystemInfoHistoryEntry {
  id: number;
  timestamp: string;
  deviceName: string;
  macAddress: string;
  ipAddress: string;
  subnetMask: string;
  gateway: string;
  firmwareVersion: string;
  hardwareVersion: string;
  systemUptime: string;
  changeType: string;
  notes?: string;
}

interface VlanHistoryEntry {
  id: number;
  vlanId: number;
  timestamp: string;
  vlanName: string;
  portMembership: string;
  changeType: string;
  previousValue?: string;
  newValue?: string;
  notes?: string;
}

interface HistorySummary {
  period: {
    since: string;
    until: string;
  };
  counts: {
    portChanges: number;
    cableTests: number;
    systemUpdates: number;
    vlanChanges: number;
  };
  recentActivity: {
    portChanges: PortHistoryEntry[];
    cableTests: CableDiagnosticHistoryEntry[];
    vlanChanges: VlanHistoryEntry[];
  };
  portStats: Array<{
    portNumber: number;
    changeCount: number;
  }>;
}

interface HistoryCardProps {
  selectedPort?: number;
}

export default function HistoryCard({ selectedPort }: HistoryCardProps) {
  const [activeTab, setActiveTab] = useState('summary');
  const [portHistory, setPortHistory] = useState<PortHistoryEntry[]>([]);
  const [cableHistory, setCableHistory] = useState<CableDiagnosticHistoryEntry[]>([]);
  const [systemHistory, setSystemHistory] = useState<SystemInfoHistoryEntry[]>([]);
  const [vlanHistory, setVlanHistory] = useState<VlanHistoryEntry[]>([]);
  const [summary, setSummary] = useState<HistorySummary | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [expandedEntries, setExpandedEntries] = useState<Set<string>>(new Set());
  const [sinceDays, setSinceDays] = useState(7);

  useEffect(() => {
    fetchData();
  }, [selectedPort, sinceDays]);

  const fetchData = async () => {
    setIsLoading(true);
    try {
      const since = new Date();
      since.setDate(since.getDate() - sinceDays);
      const sinceParam = since.toISOString();

      const promises = [
        fetch(selectedPort ? `/api/history/ports/${selectedPort}?since=${sinceParam}&limit=100` : `/api/history/ports?since=${sinceParam}&limit=100`),
        fetch(selectedPort ? `/api/history/cable-diagnostics/${selectedPort}?since=${sinceParam}&limit=100` : `/api/history/cable-diagnostics?since=${sinceParam}&limit=100`),
        fetch(`/api/history/system-info?since=${sinceParam}&limit=100`),
        fetch(`/api/history/vlans?since=${sinceParam}&limit=100`),
        fetch(`/api/history/summary?since=${sinceParam}`)
      ];

      const [portRes, cableRes, systemRes, vlanRes, summaryRes] = await Promise.all(promises);

      if (portRes.ok) setPortHistory(await portRes.json());
      if (cableRes.ok) setCableHistory(await cableRes.json());
      if (systemRes.ok) setSystemHistory(await systemRes.json());
      if (vlanRes.ok) setVlanHistory(await vlanRes.json());
      if (summaryRes.ok) setSummary(await summaryRes.json());
    } catch (error) {
      console.error('Failed to fetch history:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const formatTimestamp = (timestamp: string) => {
    return new Date(timestamp).toLocaleString();
  };

  const getChangeTypeIcon = (changeType: string) => {
    switch (changeType) {
      case 'CONFIG_CHANGE':
        return <Settings className="h-4 w-4 text-blue-500" />;
      case 'STATUS_CHANGE':
        return <Activity className="h-4 w-4 text-yellow-500" />;
      case 'VLAN_CREATED':
      case 'VLAN_DELETED':
      case 'VLAN_MODIFIED':
        return <Network className="h-4 w-4 text-green-500" />;
      case 'MANUAL':
        return <Zap className="h-4 w-4 text-purple-500" />;
      default:
        return <Clock className="h-4 w-4 text-gray-500" />;
    }
  };

  const toggleExpanded = (entryId: string) => {
    const newExpanded = new Set(expandedEntries);
    if (newExpanded.has(entryId)) {
      newExpanded.delete(entryId);
    } else {
      newExpanded.add(entryId);
    }
    setExpandedEntries(newExpanded);
  };

  const renderPortHistory = () => (
    <div className="space-y-3">
      {portHistory.map((entry) => {
        const entryId = `port-${entry.id}`;
        const isExpanded = expandedEntries.has(entryId);
        const hasDetails = entry.previousValue || entry.newValue || entry.notes;

        return (
          <div key={entry.id} className="border rounded-lg p-4 bg-card">
            <div className="flex items-start justify-between">
              <div className="flex items-center space-x-3">
                {getChangeTypeIcon(entry.changeType)}
                <div>
                  <div className="font-medium">
                    Port {entry.portNumber} - {entry.changeType.replace('_', ' ')}
                  </div>
                  <div className="text-sm text-muted-foreground">
                    {formatTimestamp(entry.timestamp)}
                  </div>
                  <div className="text-sm mt-1">
                    Status: {entry.status} | Speed: {entry.speedActual} | Flow Control: {entry.flowControlActual}
                  </div>
                </div>
              </div>
              {hasDetails && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => toggleExpanded(entryId)}
                >
                  {isExpanded ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
                </Button>
              )}
            </div>
            
            {isExpanded && hasDetails && (
              <div className="mt-3 pt-3 border-t space-y-2">
                {entry.previousValue && (
                  <div className="text-sm">
                    <span className="font-medium">Previous: </span>
                    <span className="text-muted-foreground">{entry.previousValue}</span>
                  </div>
                )}
                {entry.newValue && (
                  <div className="text-sm">
                    <span className="font-medium">New: </span>
                    <span className="text-muted-foreground">{entry.newValue}</span>
                  </div>
                )}
                {entry.notes && (
                  <div className="text-sm">
                    <span className="font-medium">Notes: </span>
                    <span className="text-muted-foreground">{entry.notes}</span>
                  </div>
                )}
              </div>
            )}
          </div>
        );
      })}
      {portHistory.length === 0 && (
        <div className="text-center text-muted-foreground py-8">
          No port history found for the selected period.
        </div>
      )}
    </div>
  );

  const renderCableHistory = () => (
    <div className="space-y-3">
      {cableHistory.map((entry) => (
        <div key={entry.id} className="border rounded-lg p-4 bg-card">
          <div className="flex items-center space-x-3">
            {getChangeTypeIcon(entry.testTrigger)}
            <div className="flex-1">
              <div className="font-medium">
                Port {entry.portNumber} Cable Test - {entry.testTrigger}
              </div>
              <div className="text-sm text-muted-foreground">
                {formatTimestamp(entry.timestamp)}
              </div>
              <div className="text-sm mt-1 flex items-center space-x-4">
                <span className={`px-2 py-1 rounded text-xs ${
                  entry.isHealthy ? 'bg-green-100 text-green-700' :
                  entry.hasIssue ? 'bg-red-100 text-red-700' :
                  entry.isDisconnected ? 'bg-gray-100 text-gray-700' :
                  'bg-yellow-100 text-yellow-700'
                }`}>
                  {entry.stateDescription}
                </span>
                {entry.length > 0 && (
                  <span className="text-muted-foreground">Length: {entry.length}m</span>
                )}
              </div>
              {entry.notes && (
                <div className="text-sm text-muted-foreground mt-2">
                  {entry.notes}
                </div>
              )}
            </div>
          </div>
        </div>
      ))}
      {cableHistory.length === 0 && (
        <div className="text-center text-muted-foreground py-8">
          No cable diagnostic history found for the selected period.
        </div>
      )}
    </div>
  );

  const renderSystemHistory = () => (
    <div className="space-y-3">
      {systemHistory.map((entry) => (
        <div key={entry.id} className="border rounded-lg p-4 bg-card">
          <div className="flex items-center space-x-3">
            {getChangeTypeIcon(entry.changeType)}
            <div>
              <div className="font-medium">
                System Info - {entry.changeType.replace('_', ' ')}
              </div>
              <div className="text-sm text-muted-foreground">
                {formatTimestamp(entry.timestamp)}
              </div>
              <div className="text-sm mt-1">
                {entry.deviceName} | FW: {entry.firmwareVersion} | IP: {entry.ipAddress}
              </div>
              {entry.notes && (
                <div className="text-sm text-muted-foreground mt-2">
                  {entry.notes}
                </div>
              )}
            </div>
          </div>
        </div>
      ))}
      {systemHistory.length === 0 && (
        <div className="text-center text-muted-foreground py-8">
          No system history found for the selected period.
        </div>
      )}
    </div>
  );

  const renderVlanHistory = () => (
    <div className="space-y-3">
      {vlanHistory.map((entry) => {
        const entryId = `vlan-${entry.id}`;
        const isExpanded = expandedEntries.has(entryId);
        const hasDetails = entry.previousValue || entry.newValue || entry.notes;

        return (
          <div key={entry.id} className="border rounded-lg p-4 bg-card">
            <div className="flex items-start justify-between">
              <div className="flex items-center space-x-3">
                {getChangeTypeIcon(entry.changeType)}
                <div>
                  <div className="font-medium">
                    {entry.vlanName} - {entry.changeType.replace('_', ' ')}
                  </div>
                  <div className="text-sm text-muted-foreground">
                    {formatTimestamp(entry.timestamp)}
                  </div>
                  <div className="text-sm mt-1">
                    Ports: {JSON.parse(entry.portMembership).join(', ') || 'None'}
                  </div>
                </div>
              </div>
              {hasDetails && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => toggleExpanded(entryId)}
                >
                  {isExpanded ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
                </Button>
              )}
            </div>
            
            {isExpanded && hasDetails && (
              <div className="mt-3 pt-3 border-t space-y-2">
                {entry.previousValue && (
                  <div className="text-sm">
                    <span className="font-medium">Previous: </span>
                    <span className="text-muted-foreground">{entry.previousValue}</span>
                  </div>
                )}
                {entry.newValue && (
                  <div className="text-sm">
                    <span className="font-medium">New: </span>
                    <span className="text-muted-foreground">{entry.newValue}</span>
                  </div>
                )}
                {entry.notes && (
                  <div className="text-sm">
                    <span className="font-medium">Notes: </span>
                    <span className="text-muted-foreground">{entry.notes}</span>
                  </div>
                )}
              </div>
            )}
          </div>
        );
      })}
      {vlanHistory.length === 0 && (
        <div className="text-center text-muted-foreground py-8">
          No VLAN history found for the selected period.
        </div>
      )}
    </div>
  );

  const renderSummary = () => (
    <div className="space-y-6">
      {summary && (
        <>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div className="bg-card p-4 rounded-lg border">
              <div className="text-2xl font-bold text-blue-600">{summary.counts.portChanges}</div>
              <div className="text-sm text-muted-foreground">Port Changes</div>
            </div>
            <div className="bg-card p-4 rounded-lg border">
              <div className="text-2xl font-bold text-purple-600">{summary.counts.cableTests}</div>
              <div className="text-sm text-muted-foreground">Cable Tests</div>
            </div>
            <div className="bg-card p-4 rounded-lg border">
              <div className="text-2xl font-bold text-green-600">{summary.counts.vlanChanges}</div>
              <div className="text-sm text-muted-foreground">VLAN Changes</div>
            </div>
            <div className="bg-card p-4 rounded-lg border">
              <div className="text-2xl font-bold text-orange-600">{summary.counts.systemUpdates}</div>
              <div className="text-sm text-muted-foreground">System Updates</div>
            </div>
          </div>

          {summary.portStats.length > 0 && (
            <div className="bg-card p-4 rounded-lg border">
              <h3 className="font-medium mb-3">Most Active Ports</h3>
              <div className="space-y-2">
                {summary.portStats.slice(0, 5).map((stat) => (
                  <div key={stat.portNumber} className="flex justify-between items-center">
                    <span className="text-sm">Port {stat.portNumber}</span>
                    <span className="text-sm font-medium">{stat.changeCount} changes</span>
                  </div>
                ))}
              </div>
            </div>
          )}

          <div className="grid md:grid-cols-2 gap-4">
            {summary.recentActivity.portChanges.length > 0 && (
              <div className="bg-card p-4 rounded-lg border">
                <h3 className="font-medium mb-3">Recent Port Changes</h3>
                <div className="space-y-2">
                  {summary.recentActivity.portChanges.slice(0, 3).map((change) => (
                    <div key={change.id} className="text-sm">
                      <div className="font-medium">Port {change.portNumber}</div>
                      <div className="text-muted-foreground">
                        {formatTimestamp(change.timestamp)}
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {summary.recentActivity.cableTests.length > 0 && (
              <div className="bg-card p-4 rounded-lg border">
                <h3 className="font-medium mb-3">Recent Cable Tests</h3>
                <div className="space-y-2">
                  {summary.recentActivity.cableTests.slice(0, 3).map((test) => (
                    <div key={test.id} className="text-sm">
                      <div className="font-medium">Port {test.portNumber}</div>
                      <div className="text-muted-foreground">
                        {formatTimestamp(test.timestamp)} - {test.stateDescription}
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        </>
      )}
    </div>
  );

  return (
    <Card>
      <CardHeader>
        <div className="flex justify-between items-center">
          <div>
            <CardTitle className="flex items-center space-x-2">
              <Clock className="h-5 w-5" />
              <span>History & Logs</span>
              {selectedPort && <span className="text-sm font-normal">(Port {selectedPort})</span>}
            </CardTitle>
            <CardDescription>
              View historical data and configuration changes
            </CardDescription>
          </div>
          <div className="flex items-center space-x-2">
            <select
              value={sinceDays}
              onChange={(e) => setSinceDays(Number(e.target.value))}
              className="px-3 py-1 border rounded text-sm"
            >
              <option value={1}>Last 24 hours</option>
              <option value={7}>Last 7 days</option>
              <option value={30}>Last 30 days</option>
              <option value={90}>Last 90 days</option>
            </select>
            <Button variant="outline" size="sm" onClick={fetchData} disabled={isLoading}>
              <RefreshCw className={`h-4 w-4 ${isLoading ? 'animate-spin' : ''}`} />
            </Button>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <div className="flex space-x-1 mb-4 border-b">
          {[
            { id: 'summary', label: 'Summary' },
            { id: 'ports', label: 'Port Changes' },
            { id: 'cables', label: 'Cable Tests' },
            { id: 'vlans', label: 'VLAN Changes' },
            { id: 'system', label: 'System' },
          ].map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`px-3 py-2 text-sm font-medium border-b-2 transition-colors ${
                activeTab === tab.id
                  ? 'border-primary text-primary'
                  : 'border-transparent text-muted-foreground hover:text-foreground'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>

        <div className="max-h-96 overflow-y-auto">
          {activeTab === 'summary' && renderSummary()}
          {activeTab === 'ports' && renderPortHistory()}
          {activeTab === 'cables' && renderCableHistory()}
          {activeTab === 'vlans' && renderVlanHistory()}
          {activeTab === 'system' && renderSystemHistory()}
        </div>
      </CardContent>
    </Card>
  );
}
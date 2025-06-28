import { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from './ui/card';
import { Button } from './ui/button';
import { Trash2, Plus, X } from 'lucide-react';

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

interface VlanInfoCardProps {
  vlanConfig: VlanConfigResponse;
  onCreateVlan: (vlanId: number, ports: number[]) => Promise<void>;
  onDeleteVlans: (vlanIds: number[]) => Promise<void>;
  onRefresh: () => Promise<void>;
}

export default function VlanInfoCard({ vlanConfig, onCreateVlan, onDeleteVlans, onRefresh }: VlanInfoCardProps) {
  const [selectedVlans, setSelectedVlans] = useState<number[]>([]);
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [newVlanId, setNewVlanId] = useState('');
  const [selectedPorts, setSelectedPorts] = useState<number[]>([]);
  const [isLoading, setIsLoading] = useState(false);

  const handleVlanSelection = (vlanId: number) => {
    setSelectedVlans(prev => 
      prev.includes(vlanId) 
        ? prev.filter(id => id !== vlanId)
        : [...prev, vlanId]
    );
  };

  const handlePortSelection = (port: number) => {
    setSelectedPorts(prev => 
      prev.includes(port) 
        ? prev.filter(p => p !== port)
        : [...prev, port]
    );
  };

  const handleDeleteSelected = async () => {
    if (selectedVlans.length === 0) return;
    
    if (confirm(`Are you sure you want to delete VLAN(s) ${selectedVlans.join(', ')}?`)) {
      setIsLoading(true);
      try {
        await onDeleteVlans(selectedVlans);
        setSelectedVlans([]);
        await onRefresh();
      } finally {
        setIsLoading(false);
      }
    }
  };

  const handleCreateVlan = async () => {
    const vlanId = parseInt(newVlanId);
    if (!vlanId || vlanId < 1 || vlanId > 4094) {
      alert('Please enter a valid VLAN ID (1-4094)');
      return;
    }
    
    if (selectedPorts.length === 0) {
      alert('Please select at least one port');
      return;
    }

    setIsLoading(true);
    try {
      await onCreateVlan(vlanId, selectedPorts);
      setShowCreateForm(false);
      setNewVlanId('');
      setSelectedPorts([]);
      await onRefresh();
    } finally {
      setIsLoading(false);
    }
  };

  const renderPortSelector = () => {
    const ports = Array.from({ length: vlanConfig.totalPorts }, (_, i) => i + 1);
    
    return (
      <div className="grid grid-cols-8 gap-2 mt-4">
        {ports.map(port => (
          <button
            key={port}
            onClick={() => handlePortSelection(port)}
            className={`p-2 text-sm border rounded ${
              selectedPorts.includes(port)
                ? 'bg-primary text-primary-foreground'
                : 'bg-background hover:bg-accent'
            }`}
          >
            {port}
          </button>
        ))}
      </div>
    );
  };
  return (
    <Card className="mt-4">
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle>VLAN Configuration</CardTitle>
        <div className="flex gap-2">
          {vlanConfig.isEnabled && (
            <>
              <Button 
                onClick={() => setShowCreateForm(true)} 
                size="sm"
                disabled={isLoading}
              >
                <Plus className="h-4 w-4 mr-1" />
                Add VLAN
              </Button>
              {selectedVlans.length > 0 && (
                <Button 
                  onClick={handleDeleteSelected} 
                  variant="destructive" 
                  size="sm"
                  disabled={isLoading}
                >
                  <Trash2 className="h-4 w-4 mr-1" />
                  Delete ({selectedVlans.length})
                </Button>
              )}
            </>
          )}
        </div>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          <div className="grid grid-cols-2 md:grid-cols-3 gap-4 text-sm">
            <div>
              <span className="font-medium">Status:</span>{' '}
              <span className={vlanConfig.isEnabled ? 'text-green-600' : 'text-red-500'}>
                {vlanConfig.isEnabled ? 'Enabled' : 'Disabled'}
              </span>
            </div>
            <div>
              <span className="font-medium">Total Ports:</span> {vlanConfig.totalPorts}
            </div>
            <div>
              <span className="font-medium">VLAN Count:</span> {vlanConfig.vlanCount}
            </div>
          </div>

          {showCreateForm && (
            <div className="border rounded-lg p-4 space-y-4">
              <div className="flex items-center justify-between">
                <h4 className="font-medium">Create New VLAN</h4>
                <Button 
                  onClick={() => setShowCreateForm(false)} 
                  variant="ghost" 
                  size="sm"
                >
                  <X className="h-4 w-4" />
                </Button>
              </div>
              
              <div>
                <label className="block text-sm font-medium mb-1">VLAN ID (1-4094)</label>
                <input
                  type="number"
                  min="1"
                  max="4094"
                  value={newVlanId}
                  onChange={(e) => setNewVlanId(e.target.value)}
                  className="w-full px-3 py-2 border border-input rounded-md bg-background"
                  placeholder="Enter VLAN ID"
                />
              </div>
              
              <div>
                <label className="block text-sm font-medium mb-1">
                  Select Ports ({selectedPorts.length} selected)
                </label>
                {renderPortSelector()}
              </div>
              
              <div className="flex gap-2">
                <Button onClick={handleCreateVlan} disabled={isLoading}>
                  Create VLAN
                </Button>
                <Button 
                  onClick={() => setSelectedPorts([])} 
                  variant="outline"
                  disabled={isLoading}
                >
                  Clear Selection
                </Button>
              </div>
            </div>
          )}

          {vlanConfig.isEnabled && vlanConfig.vlans.length > 0 ? (
            <div className="space-y-3">
              <h4 className="font-medium">Configured VLANs:</h4>
              <div className="space-y-2">
                {vlanConfig.vlans.map((vlan) => (
                  <div
                    key={vlan.vlanId}
                    className={`flex items-center justify-between p-3 border rounded-lg cursor-pointer transition-colors ${
                      selectedVlans.includes(vlan.vlanId) 
                        ? 'bg-primary/10 border-primary' 
                        : 'hover:bg-accent'
                    }`}
                    onClick={() => handleVlanSelection(vlan.vlanId)}
                  >
                    <div className="flex items-center gap-3">
                      <input
                        type="checkbox"
                        checked={selectedVlans.includes(vlan.vlanId)}
                        onChange={() => handleVlanSelection(vlan.vlanId)}
                        className="rounded"
                      />
                      <span className="font-medium">VLAN {vlan.vlanId}</span>
                    </div>
                    <div className="text-sm text-muted-foreground">
                      Ports: {vlan.memberPorts || 'None'}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          ) : (
            <div className="text-center text-muted-foreground py-4">
              {vlanConfig.isEnabled ? 'No VLANs configured' : 'VLAN is disabled'}
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
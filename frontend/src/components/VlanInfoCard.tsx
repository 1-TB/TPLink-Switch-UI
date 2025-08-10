import { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from './ui/card';
import { Button } from './ui/button';
import { Trash2, Plus, X, Tag, Hash } from 'lucide-react';

interface VlanInfo {
  vlanId: number;
  vlanName: string;
  memberPorts: string;
  portNumbers: number[];
  taggedPorts: number[];
  untaggedPorts: number[];
}

interface VlanConfigResponse {
  isEnabled: boolean;
  totalPorts: number;
  vlanCount: number;
  vlans: VlanInfo[];
}

interface VlanInfoCardProps {
  vlanConfig: VlanConfigResponse;
  onCreateVlan: (vlanId: number, vlanName: string, taggedPorts: number[], untaggedPorts: number[]) => Promise<void>;
  onDeleteVlans: (vlanIds: number[]) => Promise<void>;
  onRefresh: () => Promise<void>;
}

export default function VlanInfoCard({ vlanConfig, onCreateVlan, onDeleteVlans, onRefresh }: VlanInfoCardProps) {
  const [selectedVlans, setSelectedVlans] = useState<number[]>([]);
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [newVlanId, setNewVlanId] = useState('');
  const [newVlanName, setNewVlanName] = useState('');
  const [taggedPorts, setTaggedPorts] = useState<number[]>([]);
  const [untaggedPorts, setUntaggedPorts] = useState<number[]>([]);
  const [isLoading, setIsLoading] = useState(false);

  const handleVlanSelection = (vlanId: number) => {
    setSelectedVlans(prev => 
      prev.includes(vlanId) 
        ? prev.filter(id => id !== vlanId)
        : [...prev, vlanId]
    );
  };

  const handlePortSelection = (port: number, type: 'tagged' | 'untagged') => {
    if (type === 'tagged') {
      setTaggedPorts(prev => 
        prev.includes(port) 
          ? prev.filter(p => p !== port)
          : [...prev.filter(p => p !== port), port] // Remove from untagged if exists, then add to tagged
      );
      setUntaggedPorts(prev => prev.filter(p => p !== port)); // Remove from untagged
    } else {
      setUntaggedPorts(prev => 
        prev.includes(port) 
          ? prev.filter(p => p !== port)
          : [...prev.filter(p => p !== port), port] // Remove from tagged if exists, then add to untagged
      );
      setTaggedPorts(prev => prev.filter(p => p !== port)); // Remove from tagged
    }
  };

  const getPortType = (port: number): 'tagged' | 'untagged' | 'none' => {
    if (taggedPorts.includes(port)) return 'tagged';
    if (untaggedPorts.includes(port)) return 'untagged';
    return 'none';
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
    
    if (taggedPorts.length === 0 && untaggedPorts.length === 0) {
      alert('Please select at least one port (tagged or untagged)');
      return;
    }

    const vlanName = newVlanName.trim() || `VLAN${vlanId}`;

    setIsLoading(true);
    try {
      await onCreateVlan(vlanId, vlanName, taggedPorts, untaggedPorts);
      setShowCreateForm(false);
      setNewVlanId('');
      setNewVlanName('');
      setTaggedPorts([]);
      setUntaggedPorts([]);
      await onRefresh();
    } finally {
      setIsLoading(false);
    }
  };

  const renderPortSelector = () => {
    const ports = Array.from({ length: vlanConfig.totalPorts }, (_, i) => i + 1);
    
    return (
      <div className="space-y-4">
        <div className="flex gap-4 text-sm text-muted-foreground">
          <div className="flex items-center gap-2">
            <div className="w-4 h-4 bg-green-500 rounded"></div>
            <span>Tagged</span>
          </div>
          <div className="flex items-center gap-2">
            <div className="w-4 h-4 bg-blue-500 rounded"></div>
            <span>Untagged</span>
          </div>
          <div className="flex items-center gap-2">
            <div className="w-4 h-4 border border-gray-300 rounded"></div>
            <span>Not Member</span>
          </div>
        </div>
        
        <div className="grid grid-cols-6 sm:grid-cols-8 lg:grid-cols-12 gap-1 sm:gap-2">
          {ports.map(port => {
            const portType = getPortType(port);
            let buttonClass = 'p-1 sm:p-2 text-xs sm:text-sm border rounded transition-colors ';
            
            if (portType === 'tagged') {
              buttonClass += 'bg-green-500 text-white border-green-600';
            } else if (portType === 'untagged') {
              buttonClass += 'bg-blue-500 text-white border-blue-600';
            } else {
              buttonClass += 'bg-background hover:bg-accent border-gray-300';
            }
            
            return (
              <div key={port} className="relative">
                <button
                  onClick={() => {
                    const currentType = getPortType(port);
                    if (currentType === 'none') {
                      handlePortSelection(port, 'untagged'); // Default to untagged
                    } else if (currentType === 'untagged') {
                      handlePortSelection(port, 'tagged'); // Switch to tagged
                    } else {
                      // Remove from both (cycle back to none)
                      setTaggedPorts(prev => prev.filter(p => p !== port));
                      setUntaggedPorts(prev => prev.filter(p => p !== port));
                    }
                  }}
                  className={buttonClass}
                  title={`Port ${port} - Click to cycle: None → Untagged → Tagged → None`}
                >
                  {port}
                </button>
                {portType === 'tagged' && (
                  <Tag className="absolute -top-1 -right-1 w-3 h-3 text-green-600" />
                )}
                {portType === 'untagged' && (
                  <Hash className="absolute -top-1 -right-1 w-3 h-3 text-blue-600" />
                )}
              </div>
            );
          })}
        </div>
        
        <div className="text-sm text-muted-foreground">
          Tagged ports: {taggedPorts.length > 0 ? taggedPorts.join(', ') : 'None'} | 
          Untagged ports: {untaggedPorts.length > 0 ? untaggedPorts.join(', ') : 'None'}
        </div>
      </div>
    );
  };
  return (
    <Card className="mt-4">
      <CardHeader className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <CardTitle className="text-lg sm:text-xl">802.1Q VLAN Configuration</CardTitle>
        <div className="flex flex-col sm:flex-row gap-2 w-full sm:w-auto">
          {vlanConfig.isEnabled && (
            <>
              <Button 
                onClick={() => setShowCreateForm(true)} 
                size="sm"
                disabled={isLoading}
                className="w-full sm:w-auto"
              >
                <Plus className="h-4 w-4 sm:mr-1" />
                <span className="hidden sm:inline">Add VLAN</span>
                <span className="sm:hidden">Add</span>
              </Button>
              {selectedVlans.length > 0 && (
                <Button 
                  onClick={handleDeleteSelected} 
                  variant="destructive" 
                  size="sm"
                  disabled={isLoading}
                  className="w-full sm:w-auto"
                >
                  <Trash2 className="h-4 w-4 sm:mr-1" />
                  Delete ({selectedVlans.length})
                </Button>
              )}
            </>
          )}
        </div>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 text-sm">
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
                <h4 className="font-medium">Create New 802.1Q VLAN</h4>
                <Button 
                  onClick={() => setShowCreateForm(false)} 
                  variant="ghost" 
                  size="sm"
                >
                  <X className="h-4 w-4" />
                </Button>
              </div>
              
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
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
                  <label className="block text-sm font-medium mb-1">VLAN Name (Optional)</label>
                  <input
                    type="text"
                    value={newVlanName}
                    onChange={(e) => setNewVlanName(e.target.value)}
                    className="w-full px-3 py-2 border border-input rounded-md bg-background"
                    placeholder="Enter VLAN name"
                    maxLength={32}
                  />
                </div>
              </div>
              
              <div>
                <label className="block text-sm font-medium mb-1">
                  Select Ports (Tagged: {taggedPorts.length}, Untagged: {untaggedPorts.length})
                </label>
                <div className="text-xs text-muted-foreground mb-2">
                  Click ports to cycle: None → Untagged → Tagged → None
                </div>
                {renderPortSelector()}
              </div>
              
              <div className="flex flex-col sm:flex-row gap-2">
                <Button onClick={handleCreateVlan} disabled={isLoading} className="flex-1 sm:flex-none">
                  Create VLAN
                </Button>
                <Button 
                  onClick={() => {
                    setTaggedPorts([]);
                    setUntaggedPorts([]);
                  }} 
                  variant="outline"
                  disabled={isLoading}
                  className="flex-1 sm:flex-none"
                >
                  Clear Ports
                </Button>
              </div>
            </div>
          )}

          {vlanConfig.isEnabled && vlanConfig.vlans.length > 0 ? (
            <div className="space-y-3">
              <h4 className="font-medium">Configured 802.1Q VLANs:</h4>
              <div className="space-y-2">
                {vlanConfig.vlans.map((vlan) => (
                  <div
                    key={vlan.vlanId}
                    className={`p-3 border rounded-lg cursor-pointer transition-colors ${
                      selectedVlans.includes(vlan.vlanId) 
                        ? 'bg-primary/10 border-primary' 
                        : 'hover:bg-accent'
                    }`}
                    onClick={() => handleVlanSelection(vlan.vlanId)}
                  >
                    <div className="flex items-center gap-3 mb-2">
                      <input
                        type="checkbox"
                        checked={selectedVlans.includes(vlan.vlanId)}
                        onChange={() => handleVlanSelection(vlan.vlanId)}
                        className="rounded"
                      />
                      <div className="flex flex-col sm:flex-row sm:items-center gap-1 sm:gap-3">
                        <span className="font-medium">VLAN {vlan.vlanId}</span>
                        {vlan.vlanName && vlan.vlanName !== `VLAN${vlan.vlanId}` && (
                          <span className="text-sm text-muted-foreground">({vlan.vlanName})</span>
                        )}
                      </div>
                    </div>
                    
                    <div className="space-y-1 text-sm">
                      {vlan.taggedPorts && vlan.taggedPorts.length > 0 && (
                        <div className="flex items-center gap-2">
                          <div className="flex items-center gap-1">
                            <Tag className="w-3 h-3 text-green-600" />
                            <span className="text-green-600 font-medium">Tagged:</span>
                          </div>
                          <span className="text-muted-foreground">
                            {vlan.taggedPorts.join(', ')}
                          </span>
                        </div>
                      )}
                      
                      {vlan.untaggedPorts && vlan.untaggedPorts.length > 0 && (
                        <div className="flex items-center gap-2">
                          <div className="flex items-center gap-1">
                            <Hash className="w-3 h-3 text-blue-600" />
                            <span className="text-blue-600 font-medium">Untagged:</span>
                          </div>
                          <span className="text-muted-foreground">
                            {vlan.untaggedPorts.join(', ')}
                          </span>
                        </div>
                      )}
                      
                      {(!vlan.taggedPorts || vlan.taggedPorts.length === 0) && 
                       (!vlan.untaggedPorts || vlan.untaggedPorts.length === 0) && (
                        <div className="text-muted-foreground italic">No ports configured</div>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          ) : (
            <div className="text-center text-muted-foreground py-4">
              {vlanConfig.isEnabled ? 'No VLANs configured' : '802.1Q VLAN is disabled'}
            </div>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
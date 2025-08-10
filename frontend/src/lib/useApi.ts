import { useState, useCallback } from 'react';
import { errorHandler, withErrorHandling } from './errorHandler';
import { useToast } from '../components/ToastProvider';

interface SystemInfo {
  deviceName: string;
  macAddress: string;
  ipAddress: string;
  subnetMask: string;
  gateway: string;
  firmwareVersion: string;
  hardwareVersion: string;
  systemUptime: string;
}

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

export interface ApiState {
  sysInfo: SystemInfo | null;
  portInfo: PortInfo[];
  vlanConfig: VlanConfigResponse | null;
  diagnostics: PortDiagnostic[];
  isLoading: boolean;
}

export const useApi = () => {
  const [apiState, setApiState] = useState<ApiState>({
    sysInfo: null,
    portInfo: [],
    vlanConfig: null,
    diagnostics: [],
    isLoading: false,
  });

  const { showSuccess, showError } = useToast();

  const checkAuthAndLogout = useCallback((response: Response, onLogout: () => void) => {
    if (response.status === 401 || response.status === 403) {
      onLogout();
      return true;
    }
    return false;
  }, []);

  const fetchSystemInfo = useCallback(async (onLogout: () => void) => {
    return withErrorHandling(async () => {
      const response = await fetch('/api/systeminfo');
      if (!response.ok) {
        if (checkAuthAndLogout(response, onLogout)) return;
        const errorData = await response.json();
        throw new Error(errorData.error || `HTTP ${response.status}`);
      }
      const data = await response.json();
      setApiState(prev => ({ ...prev, sysInfo: data.data || data }));
      return data.data || data;
    }, 'System Info Fetch');
  }, [checkAuthAndLogout]);

  const fetchPortInfo = useCallback(async (onLogout: () => void) => {
    return withErrorHandling(async () => {
      const response = await fetch('/api/ports');
      if (!response.ok) {
        if (checkAuthAndLogout(response, onLogout)) return;
        const err = await response.json();
        throw new Error(err.error || `HTTP ${response.status}`);
      }

      const payload = await response.json();
      let portsArray: PortInfo[] = [];

      if (Array.isArray(payload.ports)) {
        portsArray = payload.ports;
      } else if (payload.data && Array.isArray(payload.data.ports)) {
        portsArray = payload.data.ports;
      }

      setApiState(prev => ({ ...prev, portInfo: portsArray }));
      return portsArray;
    }, 'Port Info Fetch');
  }, [checkAuthAndLogout]);

  const fetchVlanConfig = useCallback(async (onLogout: () => void) => {
    return withErrorHandling(async () => {
      const response = await fetch('/api/vlans');
      if (!response.ok) {
        if (checkAuthAndLogout(response, onLogout)) return;
        const errorData = await response.json();
        throw new Error(errorData.error || `HTTP ${response.status}`);
      }
      const data = await response.json();
      setApiState(prev => ({ ...prev, vlanConfig: data.data || data }));
      return data.data || data;
    }, 'VLAN Config Fetch');
  }, [checkAuthAndLogout]);

  const fetchAllData = useCallback(async (onLogout: () => void) => {
    setApiState(prev => ({ ...prev, isLoading: true }));
    try {
      await Promise.allSettled([
        fetchSystemInfo(onLogout),
        fetchPortInfo(onLogout),
        fetchVlanConfig(onLogout)
      ]);
    } finally {
      setApiState(prev => ({ ...prev, isLoading: false }));
    }
  }, [fetchSystemInfo, fetchPortInfo, fetchVlanConfig]);

  const configurePort = useCallback(async (portNumber: number, enabled: boolean, onLogout: () => void) => {
    return withErrorHandling(async () => {
      const response = await fetch('/api/ports/configure', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          Port: portNumber,
          Enable: enabled
        })
      });

      if (!response.ok) {
        if (checkAuthAndLogout(response, onLogout)) return;
        const errorData = await response.json();
        throw new Error(errorData.message || `HTTP ${response.status}`);
      }

      showSuccess(`Port ${portNumber} ${enabled ? 'enabled' : 'disabled'} successfully`);
      await fetchPortInfo(onLogout);
    }, 'Port Configuration');
  }, [checkAuthAndLogout, showSuccess, fetchPortInfo]);

  const runDiagnostics = useCallback(async (portNumbers: number[], onLogout: () => void) => {
    return withErrorHandling(async () => {
      const response = await fetch('/api/diagnostics/cable', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ ports: portNumbers })
      });

      if (!response.ok) {
        if (checkAuthAndLogout(response, onLogout)) return;
        const err = await response.json();
        throw new Error(err.message || `HTTP ${response.status}`);
      }

      const payload = await response.json();
      let diagArray: PortDiagnostic[] = [];

      if (Array.isArray(payload.diagnostics)) {
        diagArray = payload.diagnostics;
      } else if (payload.data && Array.isArray(payload.data.diagnostics)) {
        diagArray = payload.data.diagnostics;
      }

      setApiState(prev => ({ ...prev, diagnostics: diagArray }));
      showSuccess(`Cable diagnostics completed for ${portNumbers.length} port(s)`);
      return diagArray;
    }, 'Cable Diagnostics');
  }, [checkAuthAndLogout, showSuccess]);

  const createVlan = useCallback(async (vlanId: number, vlanName: string, taggedPorts: number[], untaggedPorts: number[], onLogout: () => void) => {
    return withErrorHandling(async () => {
      const response = await fetch('/api/vlans/create', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ 
          vlanId, 
          vlanName,
          taggedPorts, 
          untaggedPorts,
          // Include legacy ports field for backwards compatibility
          ports: untaggedPorts
        })
      });

      if (!response.ok) {
        if (checkAuthAndLogout(response, onLogout)) return;
        const errorData = await response.json();
        throw new Error(errorData.message || `HTTP ${response.status}`);
      }

      const vlanDisplayName = vlanName ? `${vlanId} (${vlanName})` : vlanId.toString();
      showSuccess(`VLAN ${vlanDisplayName} created successfully`);
      await fetchVlanConfig(onLogout);
    }, 'VLAN Creation');
  }, [checkAuthAndLogout, showSuccess, fetchVlanConfig]);

  const deleteVlans = useCallback(async (vlanIds: number[], onLogout: () => void) => {
    return withErrorHandling(async () => {
      const response = await fetch('/api/vlans/delete', {
        method: 'DELETE',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ vlanIds })
      });

      if (!response.ok) {
        if (checkAuthAndLogout(response, onLogout)) return;
        const errorData = await response.json();
        throw new Error(errorData.message || `HTTP ${response.status}`);
      }

      showSuccess(`${vlanIds.length} VLAN(s) deleted successfully`);
      await fetchVlanConfig(onLogout);
    }, 'VLAN Deletion');
  }, [checkAuthAndLogout, showSuccess, fetchVlanConfig]);

  const rebootSwitch = useCallback(async (onLogout: () => void) => {
    return withErrorHandling(async () => {
      const response = await fetch('/api/reboot', { method: 'POST' });

      if (!response.ok) {
        if (checkAuthAndLogout(response, onLogout)) return;
        const errorData = await response.json();
        throw new Error(errorData.message || `HTTP ${response.status}`);
      }

      showSuccess('Switch reboot initiated. The switch will be unavailable for a few minutes.');
    }, 'Switch Reboot');
  }, [checkAuthAndLogout, showSuccess]);

  return {
    ...apiState,
    fetchAllData,
    fetchSystemInfo,
    fetchPortInfo,
    fetchVlanConfig,
    configurePort,
    runDiagnostics,
    createVlan,
    deleteVlans,
    rebootSwitch,
  };
};
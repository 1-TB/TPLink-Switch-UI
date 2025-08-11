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

  // System Management API calls
  const setSystemName = useCallback(async (systemName: string, onLogout: () => void) => {
    return withErrorHandling(async () => {
      const response = await fetch('/api/system/name', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ systemName })
      });

      if (!response.ok) {
        if (checkAuthAndLogout(response, onLogout)) return;
        const errorData = await response.json();
        throw new Error(errorData.message || `HTTP ${response.status}`);
      }

      showSuccess('System name updated successfully');
      await fetchSystemInfo(onLogout);
    }, 'System Name Update');
  }, [checkAuthAndLogout, showSuccess, fetchSystemInfo]);

  const configureIpSettings = useCallback(async (config: any, onLogout: () => void) => {
    return withErrorHandling(async () => {
      const response = await fetch('/api/system/ip-config', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(config)
      });

      if (!response.ok) {
        if (checkAuthAndLogout(response, onLogout)) return;
        const errorData = await response.json();
        throw new Error(errorData.message || `HTTP ${response.status}`);
      }

      showSuccess('IP configuration updated successfully');
      await fetchSystemInfo(onLogout);
    }, 'IP Configuration');
  }, [checkAuthAndLogout, showSuccess, fetchSystemInfo]);

  const factoryReset = useCallback(async (onLogout: () => void) => {
    return withErrorHandling(async () => {
      const response = await fetch('/api/system/factory-reset', { method: 'POST' });

      if (!response.ok) {
        if (checkAuthAndLogout(response, onLogout)) return;
        const errorData = await response.json();
        throw new Error(errorData.message || `HTTP ${response.status}`);
      }

      showSuccess('Factory reset initiated. The switch will restart with default settings.');
    }, 'Factory Reset');
  }, [checkAuthAndLogout, showSuccess]);

  const saveConfiguration = useCallback(async (onLogout: () => void) => {
    return withErrorHandling(async () => {
      const response = await fetch('/api/system/save-config', { method: 'POST' });

      if (!response.ok) {
        if (checkAuthAndLogout(response, onLogout)) return;
        const errorData = await response.json();
        throw new Error(errorData.message || `HTTP ${response.status}`);
      }

      showSuccess('Configuration saved successfully');
    }, 'Save Configuration');
  }, [checkAuthAndLogout, showSuccess]);

  const controlLed = useCallback(async (enabled: boolean, onLogout: () => void) => {
    return withErrorHandling(async () => {
      const response = await fetch('/api/system/led-control', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ enabled })
      });

      if (!response.ok) {
        if (checkAuthAndLogout(response, onLogout)) return;
        const errorData = await response.json();
        throw new Error(errorData.message || `HTTP ${response.status}`);
      }

      showSuccess(`LED ${enabled ? 'enabled' : 'disabled'} successfully`);
    }, 'LED Control');
  }, [checkAuthAndLogout, showSuccess]);

  const updateUserAccount = useCallback(async (accountData: any, onLogout: () => void) => {
    return withErrorHandling(async () => {
      const response = await fetch('/api/system/user-account', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(accountData)
      });

      if (!response.ok) {
        if (checkAuthAndLogout(response, onLogout)) return;
        const errorData = await response.json();
        throw new Error(errorData.message || `HTTP ${response.status}`);
      }

      showSuccess('User account updated successfully');
    }, 'User Account Update');
  }, [checkAuthAndLogout, showSuccess]);

  // Advanced Networking API calls
  const clearPortStatistics = useCallback(async (ports: number[], onLogout: () => void) => {
    return withErrorHandling(async () => {
      const response = await fetch('/api/ports/clear-statistics', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ ports })
      });

      if (!response.ok) {
        if (checkAuthAndLogout(response, onLogout)) return;
        const errorData = await response.json();
        throw new Error(errorData.message || `HTTP ${response.status}`);
      }

      showSuccess('Port statistics cleared successfully');
      await fetchPortInfo(onLogout);
    }, 'Clear Port Statistics');
  }, [checkAuthAndLogout, showSuccess, fetchPortInfo]);

  const configureMirroring = useCallback(async (config: any, onLogout: () => void) => {
    return withErrorHandling(async () => {
      const enableResponse = await fetch('/api/mirroring/enable', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ enabled: config.enabled, mirrorDestinationPort: config.mirrorDestinationPort })
      });

      if (!enableResponse.ok) {
        if (checkAuthAndLogout(enableResponse, onLogout)) return;
        const errorData = await enableResponse.json();
        throw new Error(errorData.message || `HTTP ${enableResponse.status}`);
      }

      if (config.enabled && config.mirrorPorts?.length > 0) {
        const configResponse = await fetch('/api/mirroring/configure', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ mirrorPorts: config.mirrorPorts })
        });

        if (!configResponse.ok) {
          if (checkAuthAndLogout(configResponse, onLogout)) return;
          const errorData = await configResponse.json();
          throw new Error(errorData.message || `HTTP ${configResponse.status}`);
        }
      }

      showSuccess(`Port mirroring ${config.enabled ? 'enabled' : 'disabled'} successfully`);
    }, 'Port Mirroring Configuration');
  }, [checkAuthAndLogout, showSuccess]);

  const configureTrunking = useCallback(async (config: any, onLogout: () => void) => {
    return withErrorHandling(async () => {
      const response = await fetch('/api/trunking/configure', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(config)
      });

      if (!response.ok) {
        if (checkAuthAndLogout(response, onLogout)) return;
        const errorData = await response.json();
        throw new Error(errorData.message || `HTTP ${response.status}`);
      }

      showSuccess('Port trunking configured successfully');
      await fetchPortInfo(onLogout);
    }, 'Port Trunking Configuration');
  }, [checkAuthAndLogout, showSuccess, fetchPortInfo]);

  const configureLoopPrevention = useCallback(async (config: any, onLogout: () => void) => {
    return withErrorHandling(async () => {
      const response = await fetch('/api/loop-prevention', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(config)
      });

      if (!response.ok) {
        if (checkAuthAndLogout(response, onLogout)) return;
        const errorData = await response.json();
        throw new Error(errorData.message || `HTTP ${response.status}`);
      }

      showSuccess('Loop prevention configured successfully');
    }, 'Loop Prevention Configuration');
  }, [checkAuthAndLogout, showSuccess]);

  // QoS Management API calls
  const configureQosMode = useCallback(async (mode: string, onLogout: () => void) => {
    return withErrorHandling(async () => {
      const response = await fetch('/api/qos/mode', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ mode })
      });

      if (!response.ok) {
        if (checkAuthAndLogout(response, onLogout)) return;
        const errorData = await response.json();
        throw new Error(errorData.message || `HTTP ${response.status}`);
      }

      showSuccess('QoS mode configured successfully');
    }, 'QoS Mode Configuration');
  }, [checkAuthAndLogout, showSuccess]);

  const configureBandwidthControl = useCallback(async (config: any, onLogout: () => void) => {
    return withErrorHandling(async () => {
      const response = await fetch('/api/qos/bandwidth-control', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(config)
      });

      if (!response.ok) {
        if (checkAuthAndLogout(response, onLogout)) return;
        const errorData = await response.json();
        throw new Error(errorData.message || `HTTP ${response.status}`);
      }

      showSuccess('Bandwidth control configured successfully');
    }, 'Bandwidth Control Configuration');
  }, [checkAuthAndLogout, showSuccess]);

  const configurePortPriority = useCallback(async (config: any, onLogout: () => void) => {
    return withErrorHandling(async () => {
      const response = await fetch('/api/qos/port-priority', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(config)
      });

      if (!response.ok) {
        if (checkAuthAndLogout(response, onLogout)) return;
        const errorData = await response.json();
        throw new Error(errorData.message || `HTTP ${response.status}`);
      }

      showSuccess('Port priority configured successfully');
    }, 'Port Priority Configuration');
  }, [checkAuthAndLogout, showSuccess]);

  const configureStormControl = useCallback(async (config: any, onLogout: () => void) => {
    return withErrorHandling(async () => {
      const response = await fetch('/api/qos/storm-control', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(config)
      });

      if (!response.ok) {
        if (checkAuthAndLogout(response, onLogout)) return;
        const errorData = await response.json();
        throw new Error(errorData.message || `HTTP ${response.status}`);
      }

      showSuccess('Storm control configured successfully');
    }, 'Storm Control Configuration');
  }, [checkAuthAndLogout, showSuccess]);

  // PoE Management API calls
  const configurePoEGlobal = useCallback(async (config: any, onLogout: () => void) => {
    return withErrorHandling(async () => {
      const response = await fetch('/api/poe/global-config', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(config)
      });

      if (!response.ok) {
        if (checkAuthAndLogout(response, onLogout)) return;
        const errorData = await response.json();
        throw new Error(errorData.message || `HTTP ${response.status}`);
      }

      showSuccess('PoE global configuration updated successfully');
    }, 'PoE Global Configuration');
  }, [checkAuthAndLogout, showSuccess]);

  const configurePoEPort = useCallback(async (config: any, onLogout: () => void) => {
    return withErrorHandling(async () => {
      const response = await fetch('/api/poe/port-config', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(config)
      });

      if (!response.ok) {
        if (checkAuthAndLogout(response, onLogout)) return;
        const errorData = await response.json();
        throw new Error(errorData.message || `HTTP ${response.status}`);
      }

      showSuccess('PoE port configuration updated successfully');
    }, 'PoE Port Configuration');
  }, [checkAuthAndLogout, showSuccess]);

  // Configuration Management API calls
  const backupConfiguration = useCallback(async (onLogout: () => void) => {
    return withErrorHandling(async () => {
      const response = await fetch('/api/config/backup', { method: 'POST' });

      if (!response.ok) {
        if (checkAuthAndLogout(response, onLogout)) return;
        const errorData = await response.json();
        throw new Error(errorData.message || `HTTP ${response.status}`);
      }

      // Create download link
      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `switch-config-${new Date().toISOString().split('T')[0]}.cfg`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      window.URL.revokeObjectURL(url);

      showSuccess('Configuration backup downloaded successfully');
    }, 'Configuration Backup');
  }, [checkAuthAndLogout, showSuccess]);

  const restoreConfiguration = useCallback(async (file: File, onLogout: () => void) => {
    return withErrorHandling(async () => {
      const formData = new FormData();
      formData.append('file', file);

      const response = await fetch('/api/config/restore', {
        method: 'POST',
        body: formData
      });

      if (!response.ok) {
        if (checkAuthAndLogout(response, onLogout)) return;
        const errorData = await response.json();
        throw new Error(errorData.message || `HTTP ${response.status}`);
      }

      showSuccess('Configuration restored successfully. Switch will restart.');
    }, 'Configuration Restore');
  }, [checkAuthAndLogout, showSuccess]);

  const upgradeFirmware = useCallback(async (file: File, onLogout: () => void) => {
    return withErrorHandling(async () => {
      const formData = new FormData();
      formData.append('file', file);

      const response = await fetch('/api/firmware/upgrade', {
        method: 'POST',
        body: formData
      });

      if (!response.ok) {
        if (checkAuthAndLogout(response, onLogout)) return;
        const errorData = await response.json();
        throw new Error(errorData.message || `HTTP ${response.status}`);
      }

      showSuccess('Firmware upgrade initiated. This may take several minutes.');
    }, 'Firmware Upgrade');
  }, [checkAuthAndLogout, showSuccess]);

  // Protocol Support API calls
  const configureIgmpSnooping = useCallback(async (config: any, onLogout: () => void) => {
    return withErrorHandling(async () => {
      const response = await fetch('/api/igmp-snooping', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(config)
      });

      if (!response.ok) {
        if (checkAuthAndLogout(response, onLogout)) return;
        const errorData = await response.json();
        throw new Error(errorData.message || `HTTP ${response.status}`);
      }

      showSuccess('IGMP snooping configured successfully');
    }, 'IGMP Snooping Configuration');
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
    // System Management
    setSystemName,
    configureIpSettings,
    factoryReset,
    saveConfiguration,
    controlLed,
    updateUserAccount,
    // Advanced Networking
    clearPortStatistics,
    configureMirroring,
    configureTrunking,
    configureLoopPrevention,
    // QoS Management
    configureQosMode,
    configureBandwidthControl,
    configurePortPriority,
    configureStormControl,
    // PoE Management
    configurePoEGlobal,
    configurePoEPort,
    // Configuration Management
    backupConfiguration,
    restoreConfiguration,
    upgradeFirmware,
    // Protocol Support
    configureIgmpSnooping,
  };
};
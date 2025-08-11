# TP-Link Switch API Endpoints - Usage Guide

This document provides examples of how to use the newly implemented API endpoints for comprehensive TP-Link switch management.

## Authentication

All endpoints require authentication. First, login to get a session:

```bash
curl -X POST "http://localhost:5000/api/login" \
  -H "Content-Type: application/json" \
  -d '{
    "host": "192.168.1.100",
    "username": "admin", 
    "password": "admin"
  }'
```

## System Management

### Set System Name
```bash
curl -X POST "http://localhost:5000/api/system/name" \
  -H "Content-Type: application/json" \
  -d '{"systemName": "MainSwitch"}'
```

### Configure IP Settings
```bash
# Enable DHCP
curl -X POST "http://localhost:5000/api/system/ip-config" \
  -H "Content-Type: application/json" \
  -d '{"dhcpEnabled": true}'

# Set Static IP
curl -X POST "http://localhost:5000/api/system/ip-config" \
  -H "Content-Type: application/json" \
  -d '{
    "dhcpEnabled": false,
    "ipAddress": "192.168.1.100",
    "subnetMask": "255.255.255.0",
    "gateway": "192.168.1.1"
  }'
```

### LED Control
```bash
# Turn LEDs off
curl -X POST "http://localhost:5000/api/system/led-control" \
  -H "Content-Type: application/json" \
  -d '{"ledEnabled": false}'
```

### Save Configuration
```bash
curl -X POST "http://localhost:5000/api/system/save-config"
```

### Reboot Switch
```bash
# Reboot with config save
curl -X POST "http://localhost:5000/api/reboot" \
  -H "Content-Type: application/json" \
  -d '{"saveConfig": true}'
```

## Port Management

### Clear Port Statistics
```bash
curl -X POST "http://localhost:5000/api/ports/clear-statistics"
```

## Advanced Features

### Port Mirroring
```bash
# Enable port mirroring (port 1 as destination)
curl -X POST "http://localhost:5000/api/mirroring/enable" \
  -H "Content-Type: application/json" \
  -d '{
    "enabled": true,
    "mirrorDestinationPort": 1
  }'

# Configure mirroring (mirror ports 2,3,4 to destination)
curl -X POST "http://localhost:5000/api/mirroring/configure" \
  -H "Content-Type: application/json" \
  -d '{
    "sourcePorts": [2, 3, 4],
    "ingressEnabled": true,
    "egressEnabled": true
  }'
```

### Port Trunking/LAG
```bash
# Create LAG 1 with ports 5-8
curl -X POST "http://localhost:5000/api/trunking/configure" \
  -H "Content-Type: application/json" \
  -d '{
    "trunkId": 1,
    "memberPorts": [5, 6, 7, 8]
  }'
```

### Loop Prevention
```bash
curl -X POST "http://localhost:5000/api/loop-prevention" \
  -H "Content-Type: application/json" \
  -d '{"enabled": true}'
```

## QoS Configuration

### Set QoS Mode
```bash
curl -X POST "http://localhost:5000/api/qos/mode" \
  -H "Content-Type: application/json" \
  -d '{"mode": 1}'
```

### Bandwidth Control
```bash
# Limit ports 1-4 to 100Mbps ingress, 50Mbps egress
curl -X POST "http://localhost:5000/api/qos/bandwidth-control" \
  -H "Content-Type: application/json" \
  -d '{
    "ports": [1, 2, 3, 4],
    "ingressRate": 100000,
    "egressRate": 50000
  }'
```

### Port Priority
```bash
# Set ports 1-2 to high priority (7)
curl -X POST "http://localhost:5000/api/qos/port-priority" \
  -H "Content-Type: application/json" \
  -d '{
    "ports": [1, 2],
    "priority": 7
  }'
```

### Storm Control
```bash
# Configure storm control on ports 1-8
curl -X POST "http://localhost:5000/api/qos/storm-control" \
  -H "Content-Type: application/json" \
  -d '{
    "ports": [1, 2, 3, 4, 5, 6, 7, 8],
    "broadcastRate": 1000,
    "multicastRate": 1000,
    "unicastRate": 1000
  }'
```

## PoE Management (PoE-enabled switches only)

### Global PoE Configuration
```bash
# Set system power limit to 150W
curl -X POST "http://localhost:5000/api/poe/global-config" \
  -H "Content-Type: application/json" \
  -d '{"powerLimit": 150.0}'
```

### PoE Port Configuration
```bash
# Enable PoE on ports 1-4 with high priority and auto power limit
curl -X POST "http://localhost:5000/api/poe/port-config" \
  -H "Content-Type: application/json" \
  -d '{
    "ports": [1, 2, 3, 4],
    "state": 2,
    "priority": 1,
    "powerLimit": 1
  }'

# Set manual power limit for port 5
curl -X POST "http://localhost:5000/api/poe/port-config" \
  -H "Content-Type: application/json" \
  -d '{
    "ports": [5],
    "state": 2,
    "priority": 2,
    "powerLimit": 6,
    "manualPowerLimit": 15.4
  }'
```

## Configuration Management

### Backup Configuration
```bash
# Download configuration file
curl -X POST "http://localhost:5000/api/config/backup" \
  --output "switch-config-backup.cfg"
```

### Restore Configuration
```bash
# Upload configuration file
curl -X POST "http://localhost:5000/api/config/restore" \
  -F "configFile=@switch-config-backup.cfg"
```

### Firmware Upgrade
```bash
# Upload firmware file (WARNING: This can brick your switch if wrong firmware)
curl -X POST "http://localhost:5000/api/firmware/upgrade" \
  -F "firmwareFile=@firmware-update.bin"
```

## IGMP Snooping

### Enable IGMP Snooping
```bash
curl -X POST "http://localhost:5000/api/igmp-snooping" \
  -H "Content-Type: application/json" \
  -d '{"enabled": true}'
```

## Response Format

All endpoints return JSON responses in this format:

**Success:**
```json
{
  "success": true,
  "message": "Operation completed successfully"
}
```

**Error:**
```json
{
  "success": false,
  "message": "Error description"
}
```

## PoE Power Class Reference

When configuring PoE ports:
- `powerLimit: 1` - Auto
- `powerLimit: 2` - Class 1 (4.0W)
- `powerLimit: 3` - Class 2 (7.0W)
- `powerLimit: 4` - Class 3 (15.4W)
- `powerLimit: 5` - Class 4 (30.0W)
- `powerLimit: 6` - Manual (specify `manualPowerLimit`)

## Error Handling

- All endpoints require prior authentication via `/api/login`
- Invalid parameters will return HTTP 400 with error details
- Network/switch communication errors return HTTP 500
- Authentication errors return HTTP 401
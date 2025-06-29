import { useState, useEffect, useMemo } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from './ui/card';
import { Button } from './ui/button';
import { 
  Clock, Activity, Zap, Settings, Network, RefreshCw, Search, Filter,
  TrendingUp, AlertTriangle, CheckCircle, Calendar, BarChart3, 
  Eye, Download, ChevronDown, ChevronUp
} from 'lucide-react';

// Enhanced type definitions
interface BaseHistoryEntry {
  id: number;
  timestamp: string;
  changeType: string;
  notes?: string;
}

interface PortHistoryEntry extends BaseHistoryEntry {
  portNumber: number;
  status: string;
  speedConfig: string;
  speedActual: string;
  flowControlConfig: string;
  flowControlActual: string;
  trunk: string;
  previousValue?: string;
  newValue?: string;
}

interface CableDiagnosticHistoryEntry extends BaseHistoryEntry {
  portNumber: number;
  state: number;
  stateDescription: string;
  length: number;
  isHealthy: boolean;
  hasIssue: boolean;
  isUntested: boolean;
  isDisconnected: boolean;
  testTrigger: string;
}

interface SystemInfoHistoryEntry extends BaseHistoryEntry {
  deviceName: string;
  macAddress: string;
  ipAddress: string;
  subnetMask: string;
  gateway: string;
  firmwareVersion: string;
  hardwareVersion: string;
  systemUptime: string;
}

interface VlanHistoryEntry extends BaseHistoryEntry {
  vlanId: number;
  vlanName: string;
  portMembership: string;
  previousValue?: string;
  newValue?: string;
}

interface HistoryAnalytics {
  totalEvents: number;
  portActivity: Array<{ portNumber: number; eventCount: number; lastActivity: string }>;
  changeTypeDistribution: Array<{ type: string; count: number; percentage: number }>;
  timelineData: Array<{ date: string; count: number }>;
  criticalEvents: Array<{ type: string; count: number; severity: 'high' | 'medium' | 'low' }>;
  healthScore: number;
}

interface FilterOptions {
  dateRange: { start: string; end: string };
  eventTypes: string[];
  portNumbers: number[];
  severity: string[];
  searchQuery: string;
}

interface HistoryDashboardProps {
  selectedPort?: number;
}

const defaultFilters: FilterOptions = {
  dateRange: { start: '', end: '' },
  eventTypes: [],
  portNumbers: [],
  severity: [],
  searchQuery: ''
};

export default function HistoryDashboard({ selectedPort }: HistoryDashboardProps) {
  // State management
  const [activeView, setActiveView] = useState<'overview' | 'timeline' | 'analytics' | 'events'>('overview');
  const [isLoading, setIsLoading] = useState(false);
  const [filters, setFilters] = useState<FilterOptions>(defaultFilters);
  const [showFilters, setShowFilters] = useState(false);
  
  // Data state
  const [allEvents, setAllEvents] = useState<(PortHistoryEntry | CableDiagnosticHistoryEntry | SystemInfoHistoryEntry | VlanHistoryEntry)[]>([]);
  const [analytics, setAnalytics] = useState<HistoryAnalytics | null>(null);
  
  // Pagination and display
  const [currentPage, setCurrentPage] = useState(1);
  const [itemsPerPage] = useState(20);
  const [expandedEvents, setExpandedEvents] = useState<Set<string>>(new Set());

  useEffect(() => {
    fetchHistoryData();
  }, [selectedPort, filters]);

  const fetchHistoryData = async () => {
    setIsLoading(true);
    try {
      const queryParams = new URLSearchParams();
      
      if (filters.dateRange.start) queryParams.append('since', filters.dateRange.start);
      if (filters.dateRange.end) queryParams.append('until', filters.dateRange.end);
      if (selectedPort) queryParams.append('port', selectedPort.toString());
      
      const endpoints = [
        `/api/history/ports?${queryParams}`,
        `/api/history/cable-diagnostics?${queryParams}`,
        `/api/history/system-info?${queryParams}`,
        `/api/history/vlans?${queryParams}`
      ];

      const responses = await Promise.all(endpoints.map(url => fetch(url)));
      const data = await Promise.all(responses.map(res => res.ok ? res.json() : []));
      
      // Combine and sort all events
      const combinedEvents = [
        ...data[0].map((e: PortHistoryEntry) => ({ ...e, eventType: 'port' })),
        ...data[1].map((e: CableDiagnosticHistoryEntry) => ({ ...e, eventType: 'cable' })),
        ...data[2].map((e: SystemInfoHistoryEntry) => ({ ...e, eventType: 'system' })),
        ...data[3].map((e: VlanHistoryEntry) => ({ ...e, eventType: 'vlan' }))
      ].sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime());
      
      setAllEvents(combinedEvents);
      generateAnalytics(combinedEvents);
    } catch (error) {
      console.error('Failed to fetch history data:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const generateAnalytics = (events: any[]) => {
    const now = new Date();
    const dayAgo = new Date(now.getTime() - 24 * 60 * 60 * 1000);
    const weekAgo = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);

    // Port activity analysis
    const portActivity = events
      .filter(e => 'portNumber' in e)
      .reduce((acc, event) => {
        const port = event.portNumber;
        if (!acc[port]) acc[port] = { count: 0, lastActivity: event.timestamp };
        acc[port].count++;
        if (new Date(event.timestamp) > new Date(acc[port].lastActivity)) {
          acc[port].lastActivity = event.timestamp;
        }
        return acc;
      }, {} as Record<number, { count: number; lastActivity: string }>);

    // Change type distribution
    const changeTypes = events.reduce((acc, event) => {
      const type = event.changeType || event.testTrigger || 'Unknown';
      acc[type] = (acc[type] || 0) + 1;
      return acc;
    }, {} as Record<string, number>);

    const totalEvents = events.length;
    const changeTypeDistribution = Object.entries(changeTypes).map(([type, count]) => ({
      type,
      count,
      percentage: Math.round((count / totalEvents) * 100)
    }));

    // Timeline data (last 30 days)
    const timelineData = Array.from({ length: 30 }, (_, i) => {
      const date = new Date(now.getTime() - i * 24 * 60 * 60 * 1000);
      const dateStr = date.toISOString().split('T')[0];
      const count = events.filter(e => e.timestamp.startsWith(dateStr)).length;
      return { date: dateStr, count };
    }).reverse();

    // Critical events analysis
    const criticalEvents = [
      { 
        type: 'Port Down', 
        count: events.filter(e => 'status' in e && e.status === 'Down').length,
        severity: 'high' as const
      },
      { 
        type: 'Cable Issues', 
        count: events.filter(e => 'hasIssue' in e && e.hasIssue).length,
        severity: 'high' as const
      },
      { 
        type: 'Config Changes', 
        count: events.filter(e => e.changeType === 'CONFIG_CHANGE').length,
        severity: 'medium' as const
      }
    ];

    // Health score calculation
    const recentEvents = events.filter(e => new Date(e.timestamp) > weekAgo);
    const errorEvents = recentEvents.filter(e => 
      ('hasIssue' in e && e.hasIssue) || 
      ('status' in e && e.status === 'Down')
    );
    const healthScore = Math.max(0, 100 - (errorEvents.length / recentEvents.length) * 100) || 100;

    setAnalytics({
      totalEvents,
      portActivity: Object.entries(portActivity).map(([port, data]) => ({
        portNumber: parseInt(port),
        eventCount: data.count,
        lastActivity: data.lastActivity
      })),
      changeTypeDistribution,
      timelineData,
      criticalEvents,
      healthScore: Math.round(healthScore)
    });
  };

  const filteredEvents = useMemo(() => {
    return allEvents.filter(event => {
      // Search query filter
      if (filters.searchQuery) {
        const query = filters.searchQuery.toLowerCase();
        const searchableText = [
          event.changeType,
          'portNumber' in event ? `port ${event.portNumber}` : '',
          'deviceName' in event ? event.deviceName : '',
          'vlanName' in event ? event.vlanName : '',
          event.notes || ''
        ].join(' ').toLowerCase();
        
        if (!searchableText.includes(query)) return false;
      }

      // Event type filter
      if (filters.eventTypes.length > 0) {
        const eventType = 'testTrigger' in event ? 'cable' : 
                          'portNumber' in event ? 'port' :
                          'deviceName' in event ? 'system' : 'vlan';
        if (!filters.eventTypes.includes(eventType)) return false;
      }

      // Port filter
      if (filters.portNumbers.length > 0 && 'portNumber' in event) {
        if (!filters.portNumbers.includes(event.portNumber)) return false;
      }

      return true;
    });
  }, [allEvents, filters]);

  const paginatedEvents = useMemo(() => {
    const startIndex = (currentPage - 1) * itemsPerPage;
    return filteredEvents.slice(startIndex, startIndex + itemsPerPage);
  }, [filteredEvents, currentPage, itemsPerPage]);

  const totalPages = Math.ceil(filteredEvents.length / itemsPerPage);

  const formatTimestamp = (timestamp: string) => {
    const date = new Date(timestamp);
    const now = new Date();
    const diffHours = Math.floor((now.getTime() - date.getTime()) / (1000 * 60 * 60));
    
    if (diffHours < 1) return 'Just now';
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffHours < 48) return 'Yesterday';
    return date.toLocaleDateString();
  };

  const getEventIcon = (event: any) => {
    if ('testTrigger' in event) return <Zap className="h-4 w-4 text-purple-500" />;
    if ('deviceName' in event) return <Settings className="h-4 w-4 text-blue-500" />;
    if ('vlanName' in event) return <Network className="h-4 w-4 text-green-500" />;
    return <Activity className="h-4 w-4 text-orange-500" />;
  };

  const getEventSeverity = (event: any) => {
    if ('hasIssue' in event && event.hasIssue) return 'high';
    if ('status' in event && event.status === 'Down') return 'high';
    if (event.changeType === 'CONFIG_CHANGE') return 'medium';
    return 'low';
  };

  const renderOverview = () => (
    <div className="space-y-6">
      {analytics && (
        <>
          {/* Key Metrics */}
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <Card>
              <CardContent className="p-4">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm text-muted-foreground">Total Events</p>
                    <p className="text-2xl font-bold">{analytics.totalEvents}</p>
                  </div>
                  <BarChart3 className="h-8 w-8 text-blue-500" />
                </div>
              </CardContent>
            </Card>
            
            <Card>
              <CardContent className="p-4">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm text-muted-foreground">Health Score</p>
                    <p className={`text-2xl font-bold ${analytics.healthScore >= 90 ? 'text-green-500' : 
                      analytics.healthScore >= 70 ? 'text-yellow-500' : 'text-red-500'}`}>
                      {analytics.healthScore}%
                    </p>
                  </div>
                  <CheckCircle className={`h-8 w-8 ${analytics.healthScore >= 90 ? 'text-green-500' : 
                    analytics.healthScore >= 70 ? 'text-yellow-500' : 'text-red-500'}`} />
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardContent className="p-4">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm text-muted-foreground">Critical Events</p>
                    <p className="text-2xl font-bold text-red-500">
                      {analytics.criticalEvents.find(e => e.severity === 'high')?.count || 0}
                    </p>
                  </div>
                  <AlertTriangle className="h-8 w-8 text-red-500" />
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardContent className="p-4">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm text-muted-foreground">Active Ports</p>
                    <p className="text-2xl font-bold">{analytics.portActivity.length}</p>
                  </div>
                  <Network className="h-8 w-8 text-green-500" />
                </div>
              </CardContent>
            </Card>
          </div>

          {/* Recent Activity & Top Ports */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Most Active Ports</CardTitle>
                <CardDescription>Ports with the most events in selected period</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-3">
                  {analytics.portActivity.slice(0, 5).map((port) => (
                    <div key={port.portNumber} className="flex items-center justify-between p-3 bg-muted/50 rounded-lg">
                      <div className="flex items-center space-x-3">
                        <div className="h-10 w-10 bg-primary/10 rounded-full flex items-center justify-center">
                          <span className="text-sm font-medium">{port.portNumber}</span>
                        </div>
                        <div>
                          <p className="font-medium">Port {port.portNumber}</p>
                          <p className="text-sm text-muted-foreground">
                            Last activity: {formatTimestamp(port.lastActivity)}
                          </p>
                        </div>
                      </div>
                      <div className="text-right">
                        <p className="font-medium">{port.eventCount}</p>
                        <p className="text-sm text-muted-foreground">events</p>
                      </div>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Event Types</CardTitle>
                <CardDescription>Distribution of event types</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-3">
                  {analytics.changeTypeDistribution.slice(0, 5).map((item) => (
                    <div key={item.type} className="flex items-center justify-between">
                      <div className="flex items-center space-x-3">
                        <div className="h-3 w-3 bg-primary rounded-full"></div>
                        <span className="text-sm">{item.type.replace('_', ' ')}</span>
                      </div>
                      <div className="flex items-center space-x-2">
                        <span className="text-sm font-medium">{item.count}</span>
                        <span className="text-xs text-muted-foreground">({item.percentage}%)</span>
                      </div>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          </div>
        </>
      )}
    </div>
  );

  const renderEventsList = () => (
    <div className="space-y-3">
      {paginatedEvents.map((event) => {
        const eventId = `event-${event.id}`;
        const isExpanded = expandedEvents.has(eventId);
        const severity = getEventSeverity(event);
        
        return (
          <Card key={event.id} className={`transition-all ${
            severity === 'high' ? 'border-red-200 bg-red-50/50' :
            severity === 'medium' ? 'border-yellow-200 bg-yellow-50/50' :
            'border-gray-200'
          }`}>
            <CardContent className="p-4">
              <div className="flex items-start justify-between">
                <div className="flex items-start space-x-3">
                  {getEventIcon(event)}
                  <div className="flex-1">
                    <div className="flex items-center space-x-2">
                      <h4 className="font-medium">
                        {'portNumber' in event && `Port ${event.portNumber} - `}
                        {'deviceName' in event && 'System Update'}
                        {'vlanName' in event && event.vlanName}
                        {'testTrigger' in event && 'Cable Test'}
                      </h4>
                      {severity === 'high' && (
                        <AlertTriangle className="h-4 w-4 text-red-500" />
                      )}
                    </div>
                    <p className="text-sm text-muted-foreground mt-1">
                      {formatTimestamp(event.timestamp)} • {event.changeType || 'testTrigger' in event ? event.testTrigger : 'Event'}
                    </p>
                    <div className="mt-2 text-sm">
                      {'status' in event && <span className="mr-4">Status: {event.status}</span>}
                      {'isHealthy' in event && (
                        <span className={`px-2 py-1 rounded text-xs ${
                          event.isHealthy ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'
                        }`}>
                          {event.stateDescription}
                        </span>
                      )}
                    </div>
                  </div>
                </div>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => {
                    const newExpanded = new Set(expandedEvents);
                    if (newExpanded.has(eventId)) {
                      newExpanded.delete(eventId);
                    } else {
                      newExpanded.add(eventId);
                    }
                    setExpandedEvents(newExpanded);
                  }}
                >
                  {isExpanded ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
                </Button>
              </div>
              
              {isExpanded && (
                <div className="mt-4 pt-4 border-t space-y-2 text-sm">
                  {'previousValue' in event && event.previousValue && (
                    <div><strong>Previous:</strong> {event.previousValue}</div>
                  )}
                  {'newValue' in event && event.newValue && (
                    <div><strong>New:</strong> {event.newValue}</div>
                  )}
                  {'length' in event && event.length > 0 && (
                    <div><strong>Cable Length:</strong> {event.length}m</div>
                  )}
                  {event.notes && (
                    <div><strong>Notes:</strong> {event.notes}</div>
                  )}
                </div>
              )}
            </CardContent>
          </Card>
        );
      })}
      
      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-center space-x-2 mt-6">
          <Button
            variant="outline"
            size="sm"
            onClick={() => setCurrentPage(Math.max(1, currentPage - 1))}
            disabled={currentPage === 1}
          >
            Previous
          </Button>
          <span className="text-sm text-muted-foreground">
            Page {currentPage} of {totalPages}
          </span>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setCurrentPage(Math.min(totalPages, currentPage + 1))}
            disabled={currentPage === totalPages}
          >
            Next
          </Button>
        </div>
      )}
    </div>
  );

  return (
    <div className="space-y-6">
      {/* Header */}
      <Card>
        <CardHeader>
          <div className="flex justify-between items-center">
            <div>
              <CardTitle className="flex items-center space-x-2">
                <Clock className="h-5 w-5" />
                <span>Network History Dashboard</span>
                {selectedPort && <span className="text-sm font-normal">(Port {selectedPort})</span>}
              </CardTitle>
              <CardDescription>
                Comprehensive analysis of network events and changes
              </CardDescription>
            </div>
            <div className="flex items-center space-x-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => setShowFilters(!showFilters)}
              >
                <Filter className="h-4 w-4 mr-2" />
                Filters
              </Button>
              <Button variant="outline" size="sm" onClick={fetchHistoryData} disabled={isLoading}>
                <RefreshCw className={`h-4 w-4 ${isLoading ? 'animate-spin' : ''}`} />
              </Button>
            </div>
          </div>
        </CardHeader>
        
        {/* Filters */}
        {showFilters && (
          <CardContent className="pt-0">
            <div className="grid grid-cols-1 md:grid-cols-4 gap-4 p-4 bg-muted/50 rounded-lg">
              <div>
                <label className="text-sm font-medium">Search</label>
                <div className="relative mt-1">
                  <Search className="h-4 w-4 absolute left-3 top-3 text-muted-foreground" />
                  <input
                    type="text"
                    placeholder="Search events..."
                    className="pl-10 w-full px-3 py-2 border rounded-md text-sm"
                    value={filters.searchQuery}
                    onChange={(e) => setFilters({...filters, searchQuery: e.target.value})}
                  />
                </div>
              </div>
              <div>
                <label className="text-sm font-medium">Date Range</label>
                <select
                  className="w-full mt-1 px-3 py-2 border rounded-md text-sm"
                  onChange={(e) => {
                    const days = parseInt(e.target.value);
                    const end = new Date().toISOString();
                    const start = new Date(Date.now() - days * 24 * 60 * 60 * 1000).toISOString();
                    setFilters({...filters, dateRange: { start, end }});
                  }}
                >
                  <option value="7">Last 7 days</option>
                  <option value="30">Last 30 days</option>
                  <option value="90">Last 90 days</option>
                  <option value="365">Last year</option>
                </select>
              </div>
            </div>
          </CardContent>
        )}
      </Card>

      {/* Navigation Tabs */}
      <Card>
        <CardContent className="p-0">
          <div className="flex border-b">
            {[
              { id: 'overview', label: 'Overview', icon: BarChart3 },
              { id: 'events', label: 'All Events', icon: Activity },
              { id: 'timeline', label: 'Timeline', icon: Calendar },
              { id: 'analytics', label: 'Analytics', icon: TrendingUp }
            ].map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveView(tab.id as any)}
                className={`flex items-center space-x-2 px-6 py-4 text-sm font-medium border-b-2 transition-colors ${
                  activeView === tab.id
                    ? 'border-primary text-primary bg-primary/5'
                    : 'border-transparent text-muted-foreground hover:text-foreground hover:bg-muted/50'
                }`}
              >
                <tab.icon className="h-4 w-4" />
                <span>{tab.label}</span>
              </button>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Content */}
      <div>
        {activeView === 'overview' && renderOverview()}
        {activeView === 'events' && renderEventsList()}
        {activeView === 'timeline' && (
          <Card>
            <CardContent className="p-6">
              <div className="text-center py-8 text-muted-foreground">
                Timeline view coming soon - will show interactive timeline of events
              </div>
            </CardContent>
          </Card>
        )}
        {activeView === 'analytics' && (
          <Card>
            <CardContent className="p-6">
              <div className="text-center py-8 text-muted-foreground">
                Advanced analytics coming soon - will show trend analysis and predictions
              </div>
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  );
}
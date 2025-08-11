import { useState, useEffect, useMemo } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from './ui/card';
import { Button } from './ui/button';
import { 
  Clock, Activity, Zap, Settings, Network, RefreshCw, Search, Filter,
  TrendingUp, AlertTriangle, CheckCircle, Calendar, BarChart3, 
  Eye, Download, ChevronDown, ChevronUp, Users, Shield, Wifi, WifiOff,
  Database, PieChart, Target, Lightbulb
} from 'lucide-react';
import { errorHandler } from '../lib/errorHandler';
import { useToast } from './ToastProvider';

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
  userId?: number;
  username?: string;
  downtimeDuration?: string;
  lastUpTime?: string;
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

interface SwitchConnectivityHistoryEntry extends BaseHistoryEntry {
  isReachable: boolean;
  ipAddress?: string;
  responseTimeMs?: number;
  errorMessage?: string;
  downtimeDuration?: string;
}

interface PortStatisticsHistoryEntry extends BaseHistoryEntry {
  portNumber: number;
  txGoodPkt: number;
  txBadPkt: number;
  rxGoodPkt: number;
  rxBadPkt: number;
  txBytes: number;
  rxBytes: number;
  txErrorRate: number;
  rxErrorRate: number;
}

interface UserActivityHistoryEntry extends BaseHistoryEntry {
  userId?: number;
  username: string;
  actionType: string;
  description: string;
  targetEntity?: string;
  previousValue?: string;
  newValue?: string;
  ipAddress?: string;
  userAgent?: string;
  isSuccess: boolean;
  errorMessage?: string;
}

interface NetworkHealthAnalysis {
  analysisDate: string;
  periodStart: string;
  periodEnd: string;
  overallHealthScore: number;
  portHealthScores: Record<number, number>;
  connectivityReliability: number;
  userActivitySummary: UserActivitySummary;
  recommendations: Recommendation[];
}

interface UserActivitySummary {
  totalActions: number;
  configChanges: number;
  systemCommands: number;
  failedActions: number;
  activeUsers: number;
  mostActiveUser?: string;
}

interface Recommendation {
  type: string;
  priority: number;
  title: string;
  description: string;
  affectedEntity?: string;
  confidence: number;
  createdAt: string;
}

interface PortAnalytics {
  portNumber: number;
  analysisDate: string;
  periodStart: string;
  periodEnd: string;
  uptimePercentage: number;
  totalDowntime: string;
  averageDowntimeDuration?: string;
  errorRate: number;
  activityLevel: string;
  lastStatusChange?: string;
  statusChanges: number;
  configChanges: number;
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
  // Hooks
  const { showError } = useToast();
  
  // State management
  const [activeView, setActiveView] = useState<'overview' | 'timeline' | 'analytics' | 'events' | 'recommendations' | 'connectivity'>('overview');
  const [isLoading, setIsLoading] = useState(false);
  const [filters, setFilters] = useState<FilterOptions>(defaultFilters);
  const [showFilters, setShowFilters] = useState(false);
  
  // Data state
  const [allEvents, setAllEvents] = useState<any[]>([]);
  const [analytics, setAnalytics] = useState<HistoryAnalytics | null>(null);
  const [networkAnalysis, setNetworkAnalysis] = useState<NetworkHealthAnalysis | null>(null);
  const [recommendations, setRecommendations] = useState<Recommendation[]>([]);
  const [connectivityHistory, setConnectivityHistory] = useState<SwitchConnectivityHistoryEntry[]>([]);
  const [portStatistics, setPortStatistics] = useState<PortStatisticsHistoryEntry[]>([]);
  const [userActivity, setUserActivity] = useState<UserActivityHistoryEntry[]>([]);
  
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
        `/api/history/vlans?${queryParams}`,
        `/api/history/switch-connectivity?${queryParams}`,
        `/api/history/port-statistics?${queryParams}`,
        `/api/history/user-activity?${queryParams}`,
        `/api/history/analytics?${queryParams}`,
        `/api/history/recommendations`
      ];

      const responses = await Promise.all(endpoints.map(url => 
        fetch(url).then(res => res.ok ? res.json() : [])
      ));
      
      // Combine and sort all events
      const combinedEvents = [
        ...responses[0].map((e: PortHistoryEntry) => ({ ...e, eventType: 'port' })),
        ...responses[1].map((e: CableDiagnosticHistoryEntry) => ({ ...e, eventType: 'cable' })),
        ...responses[2].map((e: SystemInfoHistoryEntry) => ({ ...e, eventType: 'system' })),
        ...responses[3].map((e: VlanHistoryEntry) => ({ ...e, eventType: 'vlan' })),
        ...responses[4].map((e: SwitchConnectivityHistoryEntry) => ({ ...e, eventType: 'connectivity' })),
        ...responses[5].map((e: PortStatisticsHistoryEntry) => ({ ...e, eventType: 'statistics' })),
        ...responses[6].map((e: UserActivityHistoryEntry) => ({ ...e, eventType: 'user-activity' }))
      ].sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime());
      
      setAllEvents(combinedEvents);
      setConnectivityHistory(responses[4]);
      setPortStatistics(responses[5]);
      setUserActivity(responses[6]);
      setNetworkAnalysis(responses[7]);
      setRecommendations(responses[8]);
      
      generateAnalytics(combinedEvents);
    } catch (error) {
      const message = errorHandler.handleApiError(error, 'History Data Fetch');
      showError(`Failed to fetch history data: ${message}`);
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
    if ('isReachable' in event) return event.isReachable 
      ? <Wifi className="h-4 w-4 text-green-500" /> 
      : <WifiOff className="h-4 w-4 text-red-500" />;
    if ('txGoodPkt' in event) return <Database className="h-4 w-4 text-indigo-500" />;
    if ('actionType' in event) return <Users className="h-4 w-4 text-cyan-500" />;
    return <Activity className="h-4 w-4 text-orange-500" />;
  };

  const getEventSeverity = (event: any) => {
    if ('hasIssue' in event && event.hasIssue) return 'high';
    if ('status' in event && event.status === 'Down') return 'high';
    if ('isReachable' in event && !event.isReachable) return 'high';
    if ('isSuccess' in event && !event.isSuccess) return 'high';
    if (event.changeType === 'CONFIG_CHANGE') return 'medium';
    if ('actionType' in event && event.actionType === 'CONFIG_CHANGE') return 'medium';
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
                    <p className="text-sm text-muted-foreground">Switch Status</p>
                    <p className={`text-2xl font-bold ${
                      connectivityHistory.length > 0 && connectivityHistory[0].isReachable 
                        ? 'text-green-500' : 'text-red-500'
                    }`}>
                      {connectivityHistory.length > 0 
                        ? (connectivityHistory[0].isReachable ? 'Online' : 'Offline')
                        : 'Unknown'}
                    </p>
                  </div>
                  {connectivityHistory.length > 0 && connectivityHistory[0].isReachable ? (
                    <Wifi className="h-8 w-8 text-green-500" />
                  ) : (
                    <WifiOff className="h-8 w-8 text-red-500" />
                  )}
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardContent className="p-4">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm text-muted-foreground">User Actions</p>
                    <p className="text-2xl font-bold">{userActivity.length}</p>
                    <p className="text-xs text-muted-foreground">
                      {userActivity.filter(u => !u.isSuccess).length} failed
                    </p>
                  </div>
                  <Users className="h-8 w-8 text-blue-500" />
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

  // Group events by date for timeline display - moved to component level
  const eventsByDate = useMemo(() => {
    const grouped = filteredEvents.reduce((acc, event) => {
      const date = new Date(event.timestamp).toDateString();
      if (!acc[date]) acc[date] = [];
      acc[date].push(event);
      return acc;
    }, {} as Record<string, typeof filteredEvents>);
    
    return Object.entries(grouped)
      .sort(([a], [b]) => new Date(b).getTime() - new Date(a).getTime())
      .slice(0, 30); // Show last 30 days
  }, [filteredEvents]);

  const renderTimeline = () => {

    return (
      <div className="space-y-6">
        {eventsByDate.map(([date, events]) => (
          <Card key={date}>
            <CardHeader className="pb-3">
              <CardTitle className="text-lg flex items-center space-x-2">
                <Calendar className="h-5 w-5 text-blue-500" />
                <span>{new Date(date).toLocaleDateString('en-US', { 
                  weekday: 'long', 
                  year: 'numeric', 
                  month: 'long', 
                  day: 'numeric' 
                })}</span>
                <span className="text-sm font-normal bg-blue-100 text-blue-700 px-2 py-1 rounded-full">
                  {events.length} events
                </span>
              </CardTitle>
            </CardHeader>
            <CardContent className="pt-0">
              <div className="relative">
                {/* Timeline line */}
                <div className="absolute left-6 top-0 bottom-0 w-0.5 bg-gray-200"></div>
                
                <div className="space-y-4">
                  {events.map((event, index) => {
                    const severity = getEventSeverity(event);
                    const eventTime = new Date(event.timestamp).toLocaleTimeString('en-US', {
                      hour: '2-digit',
                      minute: '2-digit'
                    });
                    
                    return (
                      <div key={event.id} className="relative flex items-start space-x-4">
                        {/* Timeline dot */}
                        <div className={`relative z-10 flex items-center justify-center w-12 h-12 rounded-full border-4 border-white ${
                          severity === 'high' ? 'bg-red-100' :
                          severity === 'medium' ? 'bg-yellow-100' :
                          'bg-green-100'
                        }`}>
                          {getEventIcon(event)}
                        </div>
                        
                        {/* Event content */}
                        <div className="flex-1 min-w-0 pb-4">
                          <div className="flex items-center justify-between">
                            <div className="flex items-center space-x-2">
                              <h4 className="font-medium text-gray-900">
                                {'portNumber' in event && `Port ${event.portNumber} - `}
                                {'deviceName' in event && 'System Update'}
                                {'vlanName' in event && event.vlanName}
                                {'testTrigger' in event && 'Cable Test'}
                                {'isReachable' in event && (event.isReachable ? 'Switch Reconnected' : 'Switch Disconnected')}
                                {'txGoodPkt' in event && `Port ${event.portNumber} Statistics`}
                                {'actionType' in event && event.description}
                              </h4>
                              {severity === 'high' && (
                                <AlertTriangle className="h-4 w-4 text-red-500" />
                              )}
                            </div>
                            <span className="text-sm text-gray-500 font-mono">{eventTime}</span>
                          </div>
                          <p className="text-sm text-gray-600 mt-1">
                            {event.changeType || ('testTrigger' in event ? event.testTrigger : 'actionType' in event ? event.actionType : 'Event')}
                            {'username' in event && event.username && ` by ${event.username}`}
                          </p>
                          
                          {/* Enhanced status display */}
                          {'status' in event && event.status && (
                            <div className="mt-2">
                              <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                                event.status === 'Up' ? 'bg-green-100 text-green-800' :
                                event.status === 'Down' ? 'bg-red-100 text-red-800' :
                                'bg-gray-100 text-gray-800'
                              }`}>
                                {event.status}
                              </span>
                              {'downtimeDuration' in event && event.downtimeDuration && (
                                <span className="ml-2 text-sm text-orange-600">
                                  Down for {event.downtimeDuration}
                                </span>
                              )}
                            </div>
                          )}
                          
                          {/* Connectivity status */}
                          {'isReachable' in event && (
                            <div className="mt-2">
                              <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                                event.isReachable ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'
                              }`}>
                                {event.isReachable ? 'Reachable' : 'Unreachable'}
                              </span>
                              {event.downtimeDuration && (
                                <span className="ml-2 text-sm text-orange-600">
                                  Outage: {event.downtimeDuration}
                                </span>
                              )}
                            </div>
                          )}
                          
                          {/* Cable health status */}
                          {'isHealthy' in event && (
                            <div className="mt-2">
                              <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                                event.isHealthy ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'
                              }`}>
                                {event.stateDescription}
                              </span>
                            </div>
                          )}
                          
                          {/* User activity status */}
                          {'isSuccess' in event && (
                            <div className="mt-2">
                              <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                                event.isSuccess ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'
                              }`}>
                                {event.isSuccess ? 'Success' : 'Failed'}
                              </span>
                              {'targetEntity' in event && event.targetEntity && (
                                <span className="ml-2 text-sm text-blue-600">
                                  Target: {event.targetEntity}
                                </span>
                              )}
                            </div>
                          )}
                          
                          {/* Port statistics summary */}
                          {'txGoodPkt' in event && (
                            <div className="mt-2 grid grid-cols-2 gap-2 text-xs">
                              <div className="bg-green-50 p-2 rounded">
                                <span className="text-green-700">TX: {event.txGoodPkt.toLocaleString()} good</span>
                              </div>
                              <div className="bg-blue-50 p-2 rounded">
                                <span className="text-blue-700">RX: {event.rxGoodPkt.toLocaleString()} good</span>
                              </div>
                            </div>
                          )}
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            </CardContent>
          </Card>
        ))}
        
        {eventsByDate.length === 0 && (
          <Card>
            <CardContent className="p-12 text-center">
              <Calendar className="h-12 w-12 text-gray-400 mx-auto mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">No Events Found</h3>
              <p className="text-gray-600">No events match your current filters.</p>
            </CardContent>
          </Card>
        )}
      </div>
    );
  };

  const renderAnalytics = () => {
    if (!analytics) {
      return (
        <Card>
          <CardContent className="p-6">
            <div className="text-center py-8 text-muted-foreground">
              Loading analytics data...
            </div>
          </CardContent>
        </Card>
      );
    }

    return (
      <div className="space-y-6">
        {/* Timeline Chart */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center space-x-2">
              <TrendingUp className="h-5 w-5 text-blue-500" />
              <span>Activity Timeline (Last 30 Days)</span>
            </CardTitle>
            <CardDescription>Daily event count over time</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="h-64">
              <div className="flex items-end justify-between h-full space-x-1">
                {analytics.timelineData.map((day, index) => {
                  const maxCount = Math.max(...analytics.timelineData.map(d => d.count));
                  const height = maxCount > 0 ? (day.count / maxCount) * 100 : 0;
                  
                  return (
                    <div key={index} className="flex-1 flex flex-col items-center group relative">
                      <div className="flex-1 flex items-end">
                        <div 
                          className="w-full bg-blue-500 hover:bg-blue-600 transition-colors rounded-t min-h-[2px]"
                          style={{ height: `${height}%` }}
                          title={`${new Date(day.date).toLocaleDateString()}: ${day.count} events`}
                        ></div>
                      </div>
                      <div className="text-xs text-gray-500 mt-2 transform -rotate-45 origin-left whitespace-nowrap">
                        {new Date(day.date).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
                      </div>
                      {/* Tooltip */}
                      <div className="absolute bottom-full mb-2 left-1/2 transform -translate-x-1/2 bg-gray-900 text-white text-xs rounded px-2 py-1 opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none">
                        {new Date(day.date).toLocaleDateString()}<br/>
                        {day.count} events
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Event Distribution and Port Activity */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Event Type Distribution */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center space-x-2">
                <BarChart3 className="h-5 w-5 text-green-500" />
                <span>Event Type Distribution</span>
              </CardTitle>
              <CardDescription>Breakdown of event types</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {analytics.changeTypeDistribution.slice(0, 8).map((item, index) => {
                  const colors = [
                    'bg-blue-500', 'bg-green-500', 'bg-purple-500', 'bg-orange-500',
                    'bg-red-500', 'bg-yellow-500', 'bg-pink-500', 'bg-indigo-500'
                  ];
                  const color = colors[index % colors.length];
                  
                  return (
                    <div key={item.type} className="flex items-center space-x-3">
                      <div className={`w-4 h-4 rounded ${color}`}></div>
                      <div className="flex-1">
                        <div className="flex justify-between items-center mb-1">
                          <span className="text-sm font-medium">{item.type.replace('_', ' ')}</span>
                          <span className="text-sm text-gray-600">{item.count} ({item.percentage}%)</span>
                        </div>
                        <div className="w-full bg-gray-200 rounded-full h-2">
                          <div 
                            className={`h-2 rounded-full ${color}`}
                            style={{ width: `${item.percentage}%` }}
                          ></div>
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
            </CardContent>
          </Card>

          {/* Port Activity Heatmap */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center space-x-2">
                <Network className="h-5 w-5 text-orange-500" />
                <span>Port Activity Heatmap</span>
              </CardTitle>
              <CardDescription>Event frequency by port</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                {analytics.portActivity.slice(0, 10).map((port) => {
                  const maxActivity = Math.max(...analytics.portActivity.map(p => p.eventCount));
                  const intensity = maxActivity > 0 ? (port.eventCount / maxActivity) * 100 : 0;
                  
                  return (
                    <div key={port.portNumber} className="flex items-center space-x-3">
                      <div className="w-16 text-sm font-medium text-right">
                        Port {port.portNumber}
                      </div>
                      <div className="flex-1">
                        <div className="flex justify-between items-center mb-1">
                          <div className="w-full bg-gray-200 rounded-full h-6 relative">
                            <div 
                              className={`h-6 rounded-full transition-all ${
                                intensity > 75 ? 'bg-red-500' :
                                intensity > 50 ? 'bg-orange-500' :
                                intensity > 25 ? 'bg-yellow-500' :
                                'bg-green-500'
                              }`}
                              style={{ width: `${Math.max(intensity, 5)}%` }}
                            ></div>
                            <span className="absolute inset-0 flex items-center justify-center text-xs font-medium text-gray-700">
                              {port.eventCount} events
                            </span>
                          </div>
                        </div>
                        <p className="text-xs text-gray-500">
                          Last activity: {formatTimestamp(port.lastActivity)}
                        </p>
                      </div>
                    </div>
                  );
                })}
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Critical Events Summary */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center space-x-2">
              <AlertTriangle className="h-5 w-5 text-red-500" />
              <span>Critical Events Analysis</span>
            </CardTitle>
            <CardDescription>Summary of high-priority events</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              {analytics.criticalEvents.map((event) => (
                <div key={event.type} className={`p-4 rounded-lg border ${
                  event.severity === 'high' ? 'border-red-200 bg-red-50' :
                  event.severity === 'medium' ? 'border-yellow-200 bg-yellow-50' :
                  'border-gray-200 bg-gray-50'
                }`}>
                  <div className="flex items-center justify-between">
                    <div>
                      <h4 className="font-medium text-gray-900">{event.type}</h4>
                      <p className={`text-2xl font-bold ${
                        event.severity === 'high' ? 'text-red-600' :
                        event.severity === 'medium' ? 'text-yellow-600' :
                        'text-gray-600'
                      }`}>
                        {event.count}
                      </p>
                    </div>
                    <div className={`p-2 rounded-full ${
                      event.severity === 'high' ? 'bg-red-100' :
                      event.severity === 'medium' ? 'bg-yellow-100' :
                      'bg-gray-100'
                    }`}>
                      {event.severity === 'high' ? (
                        <AlertTriangle className="h-6 w-6 text-red-500" />
                      ) : event.severity === 'medium' ? (
                        <Activity className="h-6 w-6 text-yellow-500" />
                      ) : (
                        <CheckCircle className="h-6 w-6 text-gray-500" />
                      )}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>
    );
  };

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
                        {'isReachable' in event && (event.isReachable ? 'Switch Reconnected' : 'Switch Disconnected')}
                        {'txGoodPkt' in event && `Port ${event.portNumber} Statistics`}
                        {'actionType' in event && event.description}
                      </h4>
                      {severity === 'high' && (
                        <AlertTriangle className="h-4 w-4 text-red-500" />
                      )}
                    </div>
                    <p className="text-sm text-muted-foreground mt-1">
                      {formatTimestamp(event.timestamp)} • {event.changeType || ('testTrigger' in event ? event.testTrigger : 'actionType' in event ? event.actionType : 'Event')}
                      {'username' in event && event.username && ` • by ${event.username}`}
                    </p>
                    <div className="mt-2 text-sm">
                      {'status' in event && <span className="mr-4">Status: {event.status}</span>}
                      {'isReachable' in event && (
                        <span className={`mr-4 ${event.isReachable ? 'text-green-600' : 'text-red-600'}`}>
                          {event.isReachable ? 'Reachable' : 'Unreachable'}
                        </span>
                      )}
                      {'isHealthy' in event && (
                        <span className={`px-2 py-1 rounded text-xs ${
                          event.isHealthy ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'
                        }`}>
                          {event.stateDescription}
                        </span>
                      )}
                      {'isSuccess' in event && (
                        <span className={`px-2 py-1 rounded text-xs ${
                          event.isSuccess ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'
                        }`}>
                          {event.isSuccess ? 'Success' : 'Failed'}
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
                  {'downtimeDuration' in event && event.downtimeDuration && (
                    <div><strong>Downtime Duration:</strong> {event.downtimeDuration}</div>
                  )}
                  {'targetEntity' in event && event.targetEntity && (
                    <div><strong>Target:</strong> {event.targetEntity}</div>
                  )}
                  {'ipAddress' in event && event.ipAddress && (
                    <div><strong>IP Address:</strong> {event.ipAddress}</div>
                  )}
                  {'responseTimeMs' in event && event.responseTimeMs && (
                    <div><strong>Response Time:</strong> {event.responseTimeMs}ms</div>
                  )}
                  {'errorMessage' in event && event.errorMessage && (
                    <div><strong>Error:</strong> <span className="text-red-600">{event.errorMessage}</span></div>
                  )}
                  {'txGoodPkt' in event && (
                    <div className="grid grid-cols-2 gap-4">
                      <div>
                        <strong>TX Packets:</strong><br/>
                        Good: {event.txGoodPkt.toLocaleString()}<br/>
                        Bad: {event.txBadPkt.toLocaleString()}<br/>
                        Error Rate: {event.txErrorRate.toFixed(2)}%
                      </div>
                      <div>
                        <strong>RX Packets:</strong><br/>
                        Good: {event.rxGoodPkt.toLocaleString()}<br/>
                        Bad: {event.rxBadPkt.toLocaleString()}<br/>
                        Error Rate: {event.rxErrorRate.toFixed(2)}%
                      </div>
                    </div>
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

  const renderRecommendations = () => {
    if (!networkAnalysis) {
      return (
        <Card>
          <CardContent className="p-6">
            <div className="text-center py-8 text-muted-foreground">
              Loading AI insights and recommendations...
            </div>
          </CardContent>
        </Card>
      );
    }

    return (
      <div className="space-y-6">
        {/* Health Overview */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-muted-foreground">Network Health</p>
                  <p className={`text-3xl font-bold ${
                    networkAnalysis.overallHealthScore >= 90 ? 'text-green-500' :
                    networkAnalysis.overallHealthScore >= 70 ? 'text-yellow-500' : 'text-red-500'
                  }`}>
                    {Math.round(networkAnalysis.overallHealthScore)}%
                  </p>
                </div>
                <Shield className={`h-8 w-8 ${
                  networkAnalysis.overallHealthScore >= 90 ? 'text-green-500' :
                  networkAnalysis.overallHealthScore >= 70 ? 'text-yellow-500' : 'text-red-500'
                }`} />
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-muted-foreground">Connectivity</p>
                  <p className={`text-3xl font-bold ${
                    networkAnalysis.connectivityReliability >= 95 ? 'text-green-500' :
                    networkAnalysis.connectivityReliability >= 90 ? 'text-yellow-500' : 'text-red-500'
                  }`}>
                    {Math.round(networkAnalysis.connectivityReliability)}%
                  </p>
                </div>
                <Wifi className={`h-8 w-8 ${
                  networkAnalysis.connectivityReliability >= 95 ? 'text-green-500' :
                  networkAnalysis.connectivityReliability >= 90 ? 'text-yellow-500' : 'text-red-500'
                }`} />
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-muted-foreground">Active Users</p>
                  <p className="text-3xl font-bold">{networkAnalysis.userActivitySummary.activeUsers}</p>
                  <p className="text-sm text-muted-foreground">
                    {networkAnalysis.userActivitySummary.totalActions} actions
                  </p>
                </div>
                <Users className="h-8 w-8 text-blue-500" />
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Recommendations */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center space-x-2">
              <Lightbulb className="h-5 w-5 text-yellow-500" />
              <span>AI-Powered Recommendations</span>
            </CardTitle>
            <CardDescription>Machine learning insights and suggestions for network optimization</CardDescription>
          </CardHeader>
          <CardContent>
            {recommendations.length === 0 ? (
              <div className="text-center py-8">
                <CheckCircle className="h-16 w-16 text-green-500 mx-auto mb-4" />
                <h3 className="text-lg font-medium text-gray-900 mb-2">All Systems Operating Optimally</h3>
                <p className="text-gray-600">No recommendations at this time. Your network is performing well!</p>
              </div>
            ) : (
              <div className="space-y-4">
                {recommendations.map((rec, index) => (
                  <div
                    key={index}
                    className={`p-4 rounded-lg border-l-4 ${
                      rec.priority >= 3 ? 'border-red-500 bg-red-50' :
                      rec.priority >= 2 ? 'border-yellow-500 bg-yellow-50' :
                      'border-blue-500 bg-blue-50'
                    }`}
                  >
                    <div className="flex items-start justify-between">
                      <div className="flex-1">
                        <div className="flex items-center space-x-2 mb-2">
                          <h4 className="font-medium text-gray-900">{rec.title}</h4>
                          <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                            rec.priority >= 3 ? 'bg-red-100 text-red-800' :
                            rec.priority >= 2 ? 'bg-yellow-100 text-yellow-800' :
                            'bg-blue-100 text-blue-800'
                          }`}>
                            {rec.priority >= 3 ? 'High' : rec.priority >= 2 ? 'Medium' : 'Low'} Priority
                          </span>
                        </div>
                        <p className="text-gray-700 mb-2">{rec.description}</p>
                        <div className="flex items-center justify-between text-sm text-gray-500">
                          <span>Affects: {rec.affectedEntity || 'System'}</span>
                          <span>Confidence: {Math.round(rec.confidence * 100)}%</span>
                        </div>
                      </div>
                      <Target className={`h-5 w-5 ml-4 ${
                        rec.priority >= 3 ? 'text-red-500' :
                        rec.priority >= 2 ? 'text-yellow-500' :
                        'text-blue-500'
                      }`} />
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>

        {/* User Activity Summary */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center space-x-2">
              <Users className="h-5 w-5 text-blue-500" />
              <span>User Activity Analysis</span>
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <div className="text-center">
                <p className="text-2xl font-bold text-blue-600">{networkAnalysis.userActivitySummary.configChanges}</p>
                <p className="text-sm text-muted-foreground">Config Changes</p>
              </div>
              <div className="text-center">
                <p className="text-2xl font-bold text-green-600">{networkAnalysis.userActivitySummary.systemCommands}</p>
                <p className="text-sm text-muted-foreground">System Commands</p>
              </div>
              <div className="text-center">
                <p className="text-2xl font-bold text-red-600">{networkAnalysis.userActivitySummary.failedActions}</p>
                <p className="text-sm text-muted-foreground">Failed Actions</p>
              </div>
              <div className="text-center">
                <p className="text-2xl font-bold text-purple-600">{networkAnalysis.userActivitySummary.activeUsers}</p>
                <p className="text-sm text-muted-foreground">Active Users</p>
              </div>
            </div>
            {networkAnalysis.userActivitySummary.mostActiveUser && (
              <div className="mt-4 p-3 bg-muted/50 rounded-lg">
                <p className="text-sm">
                  <strong>Most Active User:</strong> {networkAnalysis.userActivitySummary.mostActiveUser}
                </p>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    );
  };

  const renderConnectivity = () => (
    <div className="space-y-6">
      {/* Connectivity Status */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center space-x-2">
            <Wifi className="h-5 w-5 text-green-500" />
            <span>Switch Connectivity Status</span>
          </CardTitle>
          <CardDescription>Real-time and historical switch reachability information</CardDescription>
        </CardHeader>
        <CardContent>
          {connectivityHistory.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground">
              No connectivity data available
            </div>
          ) : (
            <div className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div className="flex items-center space-x-3 p-4 bg-green-50 rounded-lg">
                  <Wifi className="h-8 w-8 text-green-500" />
                  <div>
                    <p className="font-medium">Current Status</p>
                    <p className={`text-sm ${
                      connectivityHistory[0]?.isReachable ? 'text-green-600' : 'text-red-600'
                    }`}>
                      {connectivityHistory[0]?.isReachable ? 'Reachable' : 'Unreachable'}
                    </p>
                  </div>
                </div>
                
                <div className="flex items-center space-x-3 p-4 bg-blue-50 rounded-lg">
                  <Clock className="h-8 w-8 text-blue-500" />
                  <div>
                    <p className="font-medium">Last Check</p>
                    <p className="text-sm text-blue-600">
                      {connectivityHistory[0] ? formatTimestamp(connectivityHistory[0].timestamp) : 'Never'}
                    </p>
                  </div>
                </div>

                <div className="flex items-center space-x-3 p-4 bg-purple-50 rounded-lg">
                  <TrendingUp className="h-8 w-8 text-purple-500" />
                  <div>
                    <p className="font-medium">Reliability</p>
                    <p className="text-sm text-purple-600">
                      {networkAnalysis ? `${Math.round(networkAnalysis.connectivityReliability)}%` : 'N/A'}
                    </p>
                  </div>
                </div>
              </div>

              {/* Connectivity Timeline */}
              <div className="space-y-3">
                <h4 className="font-medium">Recent Connectivity Events</h4>
                {connectivityHistory.slice(0, 10).map((event, index) => (
                  <div key={index} className="flex items-center space-x-4 p-3 border rounded-lg">
                    {event.isReachable ? (
                      <Wifi className="h-5 w-5 text-green-500" />
                    ) : (
                      <WifiOff className="h-5 w-5 text-red-500" />
                    )}
                    <div className="flex-1">
                      <div className="flex items-center justify-between">
                        <p className={`font-medium ${event.isReachable ? 'text-green-700' : 'text-red-700'}`}>
                          {event.isReachable ? 'Switch Reachable' : 'Switch Unreachable'}
                        </p>
                        <span className="text-sm text-muted-foreground">
                          {formatTimestamp(event.timestamp)}
                        </span>
                      </div>
                      {event.responseTimeMs && (
                        <p className="text-sm text-muted-foreground">
                          Response time: {event.responseTimeMs}ms
                        </p>
                      )}
                      {event.errorMessage && (
                        <p className="text-sm text-red-600">
                          Error: {event.errorMessage}
                        </p>
                      )}
                      {event.downtimeDuration && (
                        <p className="text-sm text-orange-600">
                          Downtime: {event.downtimeDuration}
                        </p>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Port Statistics */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center space-x-2">
            <Database className="h-5 w-5 text-indigo-500" />
            <span>Port Traffic Statistics</span>
          </CardTitle>
          <CardDescription>Packet transmission and error statistics for network ports</CardDescription>
        </CardHeader>
        <CardContent>
          {portStatistics.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground">
              No port statistics available
            </div>
          ) : (
            <div className="space-y-4">
              {portStatistics
                .filter(stat => selectedPort ? stat.portNumber === selectedPort : true)
                .slice(0, 10)
                .map((stat, index) => (
                <div key={index} className="p-4 border rounded-lg">
                  <div className="flex items-center justify-between mb-3">
                    <h4 className="font-medium">Port {stat.portNumber}</h4>
                    <span className="text-sm text-muted-foreground">
                      {formatTimestamp(stat.timestamp)}
                    </span>
                  </div>
                  
                  <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
                    <div className="text-center p-2 bg-green-50 rounded">
                      <p className="font-medium text-green-700">TX Good</p>
                      <p className="text-green-600">{stat.txGoodPkt.toLocaleString()}</p>
                    </div>
                    <div className="text-center p-2 bg-red-50 rounded">
                      <p className="font-medium text-red-700">TX Bad</p>
                      <p className="text-red-600">{stat.txBadPkt.toLocaleString()}</p>
                    </div>
                    <div className="text-center p-2 bg-blue-50 rounded">
                      <p className="font-medium text-blue-700">RX Good</p>
                      <p className="text-blue-600">{stat.rxGoodPkt.toLocaleString()}</p>
                    </div>
                    <div className="text-center p-2 bg-orange-50 rounded">
                      <p className="font-medium text-orange-700">RX Bad</p>
                      <p className="text-orange-600">{stat.rxBadPkt.toLocaleString()}</p>
                    </div>
                  </div>
                  
                  <div className="mt-3 grid grid-cols-2 gap-4 text-sm">
                    <div>
                      <p className="text-muted-foreground">TX Error Rate</p>
                      <p className={`font-medium ${stat.txErrorRate > 1 ? 'text-red-600' : 'text-green-600'}`}>
                        {stat.txErrorRate.toFixed(2)}%
                      </p>
                    </div>
                    <div>
                      <p className="text-muted-foreground">RX Error Rate</p>
                      <p className={`font-medium ${stat.rxErrorRate > 1 ? 'text-red-600' : 'text-green-600'}`}>
                        {stat.rxErrorRate.toFixed(2)}%
                      </p>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
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
          <div className="flex border-b overflow-x-auto">
            {[
              { id: 'overview', label: 'Overview', icon: BarChart3 },
              { id: 'events', label: 'All Events', icon: Activity },
              { id: 'timeline', label: 'Timeline', icon: Calendar },
              { id: 'analytics', label: 'Analytics', icon: TrendingUp },
              { id: 'recommendations', label: 'AI Insights', icon: Lightbulb },
              { id: 'connectivity', label: 'Connectivity', icon: Wifi }
            ].map((tab) => (
              <button
                key={tab.id}
                onClick={() => setActiveView(tab.id as any)}
                className={`flex items-center space-x-2 px-6 py-4 text-sm font-medium border-b-2 transition-colors whitespace-nowrap ${
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
        {activeView === 'timeline' && renderTimeline()}
        {activeView === 'analytics' && renderAnalytics()}
        {activeView === 'recommendations' && renderRecommendations()}
        {activeView === 'connectivity' && renderConnectivity()}
      </div>
    </div>
  );
}
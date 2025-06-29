import React, { useState } from 'react';
import { Button } from './ui/button';
import { Card } from './ui/card';
import { AlertCircle, CheckCircle, Loader2, Network, User, Shield } from 'lucide-react';

interface SetupWizardProps {
  onSetupComplete: () => void;
}

interface SwitchCredentials {
  host: string;
  username: string;
  password: string;
}

interface UserAccount {
  username: string;
  email: string;
  password: string;
  confirmPassword: string;
  firstName: string;
  lastName: string;
}

type SetupStep = 'welcome' | 'switch' | 'account' | 'testing' | 'complete';

export const SetupWizard: React.FC<SetupWizardProps> = ({ onSetupComplete }) => {
  const [currentStep, setCurrentStep] = useState<SetupStep>('welcome');
  const [switchCredentials, setSwitchCredentials] = useState<SwitchCredentials>({
    host: '',
    username: 'admin',
    password: ''
  });
  const [userAccount, setUserAccount] = useState<UserAccount>({
    username: '',
    email: '',
    password: '',
    confirmPassword: '',
    firstName: '',
    lastName: ''
  });
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [testingStep, setTestingStep] = useState<'switch' | 'account' | 'complete'>('switch');

  const handleSwitchTest = async () => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/test-connection', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(switchCredentials),
      });

      const data = await response.json();
      if (!data.success) {
        throw new Error(data.message || 'Switch connection failed');
      }

      setCurrentStep('account');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Switch connection failed');
    } finally {
      setIsLoading(false);
    }
  };

  const handleSetupComplete = async () => {
    if (userAccount.password !== userAccount.confirmPassword) {
      setError('Passwords do not match');
      return;
    }

    setIsLoading(true);
    setError(null);
    setCurrentStep('testing');
    setTestingStep('switch');

    try {
      // Submit initial setup
      const setupData = {
        switchHost: switchCredentials.host,
        switchUsername: switchCredentials.username,
        switchPassword: switchCredentials.password,
        userAccount: {
          username: userAccount.username,
          email: userAccount.email,
          password: userAccount.password,
          firstName: userAccount.firstName,
          lastName: userAccount.lastName
        }
      };

      setTestingStep('account');
      const response = await fetch('/api/auth/setup', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(setupData),
      });

      const data = await response.json();
      if (!data.success) {
        throw new Error(data.message || 'Setup failed');
      }

      setTestingStep('complete');
      setTimeout(() => {
        setCurrentStep('complete');
      }, 1500);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Setup failed');
      setCurrentStep('account');
    } finally {
      setIsLoading(false);
    }
  };

  const renderWelcomeStep = () => (
    <div className="text-center space-y-6">
      <div className="w-16 h-16 bg-blue-100 dark:bg-blue-900 rounded-full flex items-center justify-center mx-auto">
        <Network className="w-8 h-8 text-blue-600 dark:text-blue-400" />
      </div>
      <div>
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-2">
          Welcome to TP-Link WebUI
        </h2>
        <p className="text-gray-600 dark:text-gray-300">
          Let's get your switch management system set up. This wizard will guide you through
          connecting to your TP-Link switch and creating your administrator account.
        </p>
      </div>
      <div className="bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg p-4">
        <div className="flex items-start">
          <AlertCircle className="w-5 h-5 text-yellow-600 dark:text-yellow-400 mt-0.5 mr-3 flex-shrink-0" />
          <div className="text-sm">
            <p className="text-yellow-800 dark:text-yellow-200 font-medium">Before you begin:</p>
            <ul className="mt-2 text-yellow-700 dark:text-yellow-300 list-disc list-inside space-y-1">
              <li>Ensure your TP-Link switch is powered on and connected to the network</li>
              <li>Have your switch's IP address and admin credentials ready</li>
              <li>Make sure you can access the switch's web interface</li>
            </ul>
          </div>
        </div>
      </div>
      <Button
        onClick={() => setCurrentStep('switch')}
        className="w-full"
        size="lg"
      >
        Get Started
      </Button>
    </div>
  );

  const renderSwitchStep = () => (
    <div className="space-y-6">
      <div className="text-center">
        <div className="w-16 h-16 bg-green-100 dark:bg-green-900 rounded-full flex items-center justify-center mx-auto mb-4">
          <Network className="w-8 h-8 text-green-600 dark:text-green-400" />
        </div>
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-2">
          Connect to Your Switch
        </h2>
        <p className="text-gray-600 dark:text-gray-300">
          Enter your TP-Link switch connection details below.
        </p>
      </div>

      {error && (
        <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4">
          <div className="flex items-center">
            <AlertCircle className="w-5 h-5 text-red-600 dark:text-red-400 mr-3" />
            <p className="text-red-800 dark:text-red-200 text-sm">{error}</p>
          </div>
        </div>
      )}

      <div className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
            Switch IP Address
          </label>
          <input
            type="text"
            value={switchCredentials.host}
            onChange={(e) => setSwitchCredentials({ ...switchCredentials, host: e.target.value })}
            placeholder="192.168.1.1"
            className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
            Username
          </label>
          <input
            type="text"
            value={switchCredentials.username}
            onChange={(e) => setSwitchCredentials({ ...switchCredentials, username: e.target.value })}
            className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
            Password
          </label>
          <input
            type="password"
            value={switchCredentials.password}
            onChange={(e) => setSwitchCredentials({ ...switchCredentials, password: e.target.value })}
            className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          />
        </div>
      </div>

      <div className="flex space-x-4">
        <Button
          variant="outline"
          onClick={() => setCurrentStep('welcome')}
          className="flex-1"
        >
          Back
        </Button>
        <Button
          onClick={handleSwitchTest}
          disabled={isLoading || !switchCredentials.host || !switchCredentials.username || !switchCredentials.password}
          className="flex-1"
        >
          {isLoading ? (
            <>
              <Loader2 className="w-4 h-4 mr-2 animate-spin" />
              Testing Connection...
            </>
          ) : (
            'Test Connection'
          )}
        </Button>
      </div>
    </div>
  );

  const renderAccountStep = () => (
    <div className="space-y-6">
      <div className="text-center">
        <div className="w-16 h-16 bg-purple-100 dark:bg-purple-900 rounded-full flex items-center justify-center mx-auto mb-4">
          <User className="w-8 h-8 text-purple-600 dark:text-purple-400" />
        </div>
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-2">
          Create Admin Account
        </h2>
        <p className="text-gray-600 dark:text-gray-300">
          Create your administrator account to manage the switch interface.
        </p>
      </div>

      {error && (
        <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4">
          <div className="flex items-center">
            <AlertCircle className="w-5 h-5 text-red-600 dark:text-red-400 mr-3" />
            <p className="text-red-800 dark:text-red-200 text-sm">{error}</p>
          </div>
        </div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
            First Name
          </label>
          <input
            type="text"
            value={userAccount.firstName}
            onChange={(e) => setUserAccount({ ...userAccount, firstName: e.target.value })}
            className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
            Last Name
          </label>
          <input
            type="text"
            value={userAccount.lastName}
            onChange={(e) => setUserAccount({ ...userAccount, lastName: e.target.value })}
            className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          />
        </div>
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
          Username *
        </label>
        <input
          type="text"
          value={userAccount.username}
          onChange={(e) => setUserAccount({ ...userAccount, username: e.target.value })}
          className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-transparent"
        />
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
          Email Address *
        </label>
        <input
          type="email"
          value={userAccount.email}
          onChange={(e) => setUserAccount({ ...userAccount, email: e.target.value })}
          className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-transparent"
        />
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
            Password *
          </label>
          <input
            type="password"
            value={userAccount.password}
            onChange={(e) => setUserAccount({ ...userAccount, password: e.target.value })}
            className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
            Confirm Password *
          </label>
          <input
            type="password"
            value={userAccount.confirmPassword}
            onChange={(e) => setUserAccount({ ...userAccount, confirmPassword: e.target.value })}
            className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          />
        </div>
      </div>

      <div className="flex space-x-4">
        <Button
          variant="outline"
          onClick={() => setCurrentStep('switch')}
          className="flex-1"
        >
          Back
        </Button>
        <Button
          onClick={handleSetupComplete}
          disabled={isLoading || !userAccount.username || !userAccount.email || !userAccount.password || !userAccount.confirmPassword}
          className="flex-1"
        >
          {isLoading ? (
            <>
              <Loader2 className="w-4 h-4 mr-2 animate-spin" />
              Setting Up...
            </>
          ) : (
            'Complete Setup'
          )}
        </Button>
      </div>
    </div>
  );

  const renderTestingStep = () => (
    <div className="text-center space-y-6">
      <div className="w-16 h-16 bg-blue-100 dark:bg-blue-900 rounded-full flex items-center justify-center mx-auto">
        <Loader2 className="w-8 h-8 text-blue-600 dark:text-blue-400 animate-spin" />
      </div>
      <div>
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-2">
          Setting Up Your System
        </h2>
        <p className="text-gray-600 dark:text-gray-300">
          Please wait while we configure your switch management system...
        </p>
      </div>

      <div className="space-y-3">
        <div className="flex items-center justify-center space-x-3">
          {testingStep === 'switch' ? (
            <Loader2 className="w-5 h-5 text-blue-600 animate-spin" />
          ) : (
            <CheckCircle className="w-5 h-5 text-green-600" />
          )}
          <span className={testingStep === 'switch' ? 'text-blue-600' : 'text-green-600'}>
            Testing switch connection...
          </span>
        </div>
        
        <div className="flex items-center justify-center space-x-3">
          {testingStep === 'account' ? (
            <Loader2 className="w-5 h-5 text-blue-600 animate-spin" />
          ) : testingStep === 'complete' ? (
            <CheckCircle className="w-5 h-5 text-green-600" />
          ) : (
            <div className="w-5 h-5 border-2 border-gray-300 rounded-full" />
          )}
          <span className={
            testingStep === 'account' ? 'text-blue-600' : 
            testingStep === 'complete' ? 'text-green-600' : 
            'text-gray-500'
          }>
            Creating administrator account...
          </span>
        </div>
        
        <div className="flex items-center justify-center space-x-3">
          {testingStep === 'complete' ? (
            <CheckCircle className="w-5 h-5 text-green-600" />
          ) : (
            <div className="w-5 h-5 border-2 border-gray-300 rounded-full" />
          )}
          <span className={testingStep === 'complete' ? 'text-green-600' : 'text-gray-500'}>
            Finalizing setup...
          </span>
        </div>
      </div>
    </div>
  );

  const renderCompleteStep = () => (
    <div className="text-center space-y-6">
      <div className="w-16 h-16 bg-green-100 dark:bg-green-900 rounded-full flex items-center justify-center mx-auto">
        <CheckCircle className="w-8 h-8 text-green-600 dark:text-green-400" />
      </div>
      <div>
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-2">
          Setup Complete!
        </h2>
        <p className="text-gray-600 dark:text-gray-300">
          Your TP-Link switch management system has been successfully configured.
          You can now monitor and manage your switch.
        </p>
      </div>
      <div className="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg p-4">
        <div className="flex items-start">
          <Shield className="w-5 h-5 text-green-600 dark:text-green-400 mt-0.5 mr-3 flex-shrink-0" />
          <div className="text-sm">
            <p className="text-green-800 dark:text-green-200 font-medium">Security Notice:</p>
            <p className="mt-1 text-green-700 dark:text-green-300">
              Your administrator account has been created with full access to the system.
              Consider creating additional user accounts with limited permissions for other team members.
            </p>
          </div>
        </div>
      </div>
      <Button
        onClick={onSetupComplete}
        className="w-full"
        size="lg"
      >
        Continue to Dashboard
      </Button>
    </div>
  );

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900 flex items-center justify-center px-4">
      <div className="max-w-2xl w-full">
        <Card className="p-8">
          {currentStep === 'welcome' && renderWelcomeStep()}
          {currentStep === 'switch' && renderSwitchStep()}
          {currentStep === 'account' && renderAccountStep()}
          {currentStep === 'testing' && renderTestingStep()}
          {currentStep === 'complete' && renderCompleteStep()}
        </Card>
      </div>
    </div>
  );
};
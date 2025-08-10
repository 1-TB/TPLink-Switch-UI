import { useState, useCallback } from 'react';
import { errorHandler } from './errorHandler';

interface User {
  id: number;
  username: string;
  email: string;
  firstName?: string;
  lastName?: string;
  role: string;
  lastLoginAt?: string;
}

type AppState = 'loading' | 'setup' | 'login' | 'dashboard';

export const useAuth = () => {
  const [appState, setAppState] = useState<AppState>('loading');
  const [currentUser, setCurrentUser] = useState<User | null>(null);

  const checkApplicationState = useCallback(async () => {
    try {
      // First check if initial setup is required
      const setupResponse = await fetch('/api/auth/setup/required');
      const setupData = await setupResponse.json();
      
      if (setupData.setupRequired) {
        setAppState('setup');
        return;
      }

      // Check if user is already authenticated
      const userResponse = await fetch('/api/auth/me');
      if (userResponse.ok) {
        const userData = await userResponse.json();
        if (userData.success && userData.user) {
          setCurrentUser(userData.user);
          setAppState('dashboard');
          return userData.user;
        }
      }

      // User needs to log in
      setAppState('login');
      return null;
    } catch (error) {
      errorHandler.handleApiError(error, 'Application State Check');
      setAppState('login');
      return null;
    }
  }, []);

  const handleSetupComplete = useCallback(() => {
    setAppState('login');
  }, []);

  const handleLoginSuccess = useCallback(async () => {
    try {
      const userResponse = await fetch('/api/auth/me');
      const userData = await userResponse.json();
      
      if (userData.success && userData.user) {
        setCurrentUser(userData.user);
        setAppState('dashboard');
        return userData.user;
      }
    } catch (error) {
      errorHandler.handleApiError(error, 'Login Success Handler');
    }
    return null;
  }, []);

  const handleLogout = useCallback(async () => {
    try {
      await fetch('/api/auth/logout', { method: 'POST' });
    } catch (error) {
      errorHandler.handleApiError(error, 'Logout');
    } finally {
      setCurrentUser(null);
      setAppState('login');
    }
  }, []);

  return {
    appState,
    currentUser,
    checkApplicationState,
    handleSetupComplete,
    handleLoginSuccess,
    handleLogout,
  };
};
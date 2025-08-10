/**
 * Centralized error handling utility
 */

export interface ErrorInfo {
  message: string;
  code?: string;
  timestamp: Date;
  context?: string;
}

class ErrorHandler {
  private static instance: ErrorHandler;
  private errorQueue: ErrorInfo[] = [];
  
  static getInstance(): ErrorHandler {
    if (!ErrorHandler.instance) {
      ErrorHandler.instance = new ErrorHandler();
    }
    return ErrorHandler.instance;
  }

  /**
   * Log error for debugging (only in development)
   */
  private logError(error: ErrorInfo): void {
    if (import.meta.env.DEV) {
      console.error(`[${error.context || 'App'}] ${error.message}`, {
        code: error.code,
        timestamp: error.timestamp
      });
    }
  }

  /**
   * Handle application errors with proper logging and user feedback
   */
  handleError(message: string, context?: string, originalError?: unknown): void {
    const errorInfo: ErrorInfo = {
      message,
      context,
      timestamp: new Date(),
      code: originalError instanceof Error ? originalError.name : undefined
    };

    this.logError(errorInfo);
    this.errorQueue.push(errorInfo);

    // Keep only last 50 errors to prevent memory leaks
    if (this.errorQueue.length > 50) {
      this.errorQueue.shift();
    }
  }

  /**
   * Handle API errors with proper error extraction
   */
  handleApiError(error: unknown, context: string): string {
    let message = 'An unexpected error occurred';
    
    if (error instanceof Error) {
      message = error.message;
    } else if (typeof error === 'string') {
      message = error;
    } else if (error && typeof error === 'object' && 'message' in error) {
      message = String(error.message);
    }

    this.handleError(message, context, error);
    return message;
  }

  /**
   * Get recent errors for debugging
   */
  getRecentErrors(): ErrorInfo[] {
    return [...this.errorQueue];
  }

  /**
   * Clear error queue
   */
  clearErrors(): void {
    this.errorQueue = [];
  }
}

export const errorHandler = ErrorHandler.getInstance();

/**
 * Wrapper for async operations with error handling
 */
export async function withErrorHandling<T>(
  operation: () => Promise<T>,
  context: string,
  fallback?: T
): Promise<T | undefined> {
  try {
    return await operation();
  } catch (error) {
    errorHandler.handleApiError(error, context);
    return fallback;
  }
}

/**
 * Handle warnings (only shown in development)
 */
export function handleWarning(message: string, context?: string): void {
  if (import.meta.env.DEV) {
    console.warn(`[${context || 'App'}] ${message}`);
  }
}
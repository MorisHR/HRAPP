import { Injectable, signal, computed } from '@angular/core';

/**
 * Service to track read/unread state of activity logs
 * Persists state in localStorage for cross-session tracking
 */
@Injectable({
  providedIn: 'root'
})
export class ActivityReadTrackerService {
  private readonly STORAGE_KEY = 'hrms_activity_read_ids';
  private readonly MAX_STORED_IDS = 1000; // Prevent unlimited growth

  private readActivityIds = signal<Set<string>>(this.loadFromStorage());

  // Computed values
  readonly unreadCount = computed(() => {
    // This would be calculated by comparing against all activities
    // For now, just tracking what's been read
    return 0; // Will be set by components
  });

  constructor() {
    // Auto-save to localStorage when read IDs change
    this.setupAutoSave();
  }

  /**
   * Mark an activity as read
   */
  markAsRead(activityId: string): void {
    const currentIds = this.readActivityIds();
    if (!currentIds.has(activityId)) {
      const newIds = new Set(currentIds);
      newIds.add(activityId);

      // Limit stored IDs to prevent excessive storage
      if (newIds.size > this.MAX_STORED_IDS) {
        this.pruneOldestEntries(newIds);
      }

      this.readActivityIds.set(newIds);
    }
  }

  /**
   * Mark multiple activities as read
   */
  markMultipleAsRead(activityIds: string[]): void {
    const currentIds = this.readActivityIds();
    const newIds = new Set(currentIds);
    let hasChanges = false;

    activityIds.forEach(id => {
      if (!newIds.has(id)) {
        newIds.add(id);
        hasChanges = true;
      }
    });

    if (hasChanges) {
      if (newIds.size > this.MAX_STORED_IDS) {
        this.pruneOldestEntries(newIds);
      }
      this.readActivityIds.set(newIds);
    }
  }

  /**
   * Check if an activity has been read
   */
  isRead(activityId: string): boolean {
    return this.readActivityIds().has(activityId);
  }

  /**
   * Mark all current activities as read
   */
  markAllAsRead(activityIds: string[]): void {
    this.markMultipleAsRead(activityIds);
  }

  /**
   * Clear all read tracking (reset)
   */
  clearAllRead(): void {
    this.readActivityIds.set(new Set());
  }

  /**
   * Remove specific activity from read tracking
   */
  markAsUnread(activityId: string): void {
    const currentIds = this.readActivityIds();
    if (currentIds.has(activityId)) {
      const newIds = new Set(currentIds);
      newIds.delete(activityId);
      this.readActivityIds.set(newIds);
    }
  }

  /**
   * Get count of unread activities from a list
   */
  getUnreadCount(allActivityIds: string[]): number {
    const readIds = this.readActivityIds();
    return allActivityIds.filter(id => !readIds.has(id)).length;
  }

  /**
   * Load read IDs from localStorage
   */
  private loadFromStorage(): Set<string> {
    try {
      const stored = localStorage.getItem(this.STORAGE_KEY);
      if (stored) {
        const ids = JSON.parse(stored);
        return new Set(Array.isArray(ids) ? ids : []);
      }
    } catch (error) {
      console.warn('Failed to load read activity IDs from localStorage:', error);
    }
    return new Set();
  }

  /**
   * Save read IDs to localStorage
   */
  private saveToStorage(): void {
    try {
      const ids = Array.from(this.readActivityIds());
      localStorage.setItem(this.STORAGE_KEY, JSON.stringify(ids));
    } catch (error) {
      console.warn('Failed to save read activity IDs to localStorage:', error);
    }
  }

  /**
   * Setup auto-save effect
   */
  private setupAutoSave(): void {
    // Effect will run whenever readActivityIds changes
    let previousValue = this.readActivityIds();

    setInterval(() => {
      const currentValue = this.readActivityIds();
      if (currentValue !== previousValue) {
        this.saveToStorage();
        previousValue = currentValue;
      }
    }, 1000); // Debounce saves to every second
  }

  /**
   * Prune oldest entries when exceeding max limit
   * Uses FIFO strategy - removes first entries
   */
  private pruneOldestEntries(ids: Set<string>): void {
    const idsArray = Array.from(ids);
    const toKeep = idsArray.slice(-this.MAX_STORED_IDS);
    ids.clear();
    toKeep.forEach(id => ids.add(id));
  }

  /**
   * Get all read activity IDs (for debugging)
   */
  getAllReadIds(): string[] {
    return Array.from(this.readActivityIds());
  }

  /**
   * Get statistics
   */
  getStats(): { totalRead: number; storageSize: number } {
    const readIds = this.readActivityIds();
    return {
      totalRead: readIds.size,
      storageSize: new Blob([JSON.stringify(Array.from(readIds))]).size
    };
  }
}

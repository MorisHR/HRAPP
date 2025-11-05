import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface EmployeeDraft {
  id: string;
  formDataJson: string;
  draftName: string;
  completionPercentage: number;
  createdBy: string;
  createdByName: string;
  createdAt: Date;
  lastEditedBy?: string;
  lastEditedByName?: string;
  lastEditedAt: Date;
  expiresAt: Date;
  daysUntilExpiry: number;
  isExpired: boolean;
}

export interface SaveDraftRequest {
  id?: string;
  formDataJson: string;
  draftName: string;
  completionPercentage: number;
}

export interface SaveDraftResponse {
  success: boolean;
  message: string;
  data: EmployeeDraft;
}

export interface DraftsResponse {
  success: boolean;
  data: EmployeeDraft[];
  count: number;
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class EmployeeDraftService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/employee-drafts`;

  /**
   * Get all drafts for current tenant
   */
  getAllDrafts(): Observable<DraftsResponse> {
    return this.http.get<DraftsResponse>(this.apiUrl);
  }

  /**
   * Get specific draft by ID
   */
  getDraftById(id: string): Observable<{ success: boolean; data: EmployeeDraft; message: string }> {
    return this.http.get<{ success: boolean; data: EmployeeDraft; message: string }>(`${this.apiUrl}/${id}`);
  }

  /**
   * Save draft (create or update)
   * Used for both manual save and auto-save
   */
  saveDraft(request: SaveDraftRequest): Observable<SaveDraftResponse> {
    return this.http.post<SaveDraftResponse>(this.apiUrl, request);
  }

  /**
   * Finalize draft - convert to employee
   */
  finalizeDraft(draftId: string): Observable<{ success: boolean; message: string; data: any }> {
    return this.http.post<{ success: boolean; message: string; data: any }>(
      `${this.apiUrl}/${draftId}/finalize`,
      {}
    );
  }

  /**
   * Delete draft
   */
  deleteDraft(id: string): Observable<{ success: boolean; message: string }> {
    return this.http.delete<{ success: boolean; message: string }>(`${this.apiUrl}/${id}`);
  }

  /**
   * localStorage helper methods for offline auto-save
   */

  // Save draft to localStorage for instant feedback
  saveToLocalStorage(draftId: string, formData: any): void {
    const key = `employee_draft_${draftId}`;
    localStorage.setItem(key, JSON.stringify({
      ...formData,
      _savedAt: new Date().toISOString()
    }));
  }

  // Load draft from localStorage
  loadFromLocalStorage(draftId: string): any | null {
    const key = `employee_draft_${draftId}`;
    const data = localStorage.getItem(key);
    return data ? JSON.parse(data) : null;
  }

  // Clear draft from localStorage
  clearFromLocalStorage(draftId: string): void {
    const key = `employee_draft_${draftId}`;
    localStorage.removeItem(key);
  }

  // List all local drafts
  listLocalDrafts(): Array<{ key: string; data: any }> {
    const drafts: Array<{ key: string; data: any }> = [];
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (key && key.startsWith('employee_draft_')) {
        const data = localStorage.getItem(key);
        if (data) {
          drafts.push({ key, data: JSON.parse(data) });
        }
      }
    }
    return drafts;
  }
}

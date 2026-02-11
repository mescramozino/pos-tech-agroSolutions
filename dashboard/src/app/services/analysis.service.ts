import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

const BASE = '/api/analysis';

export interface Reading {
  id: string;
  plotId: string;
  type: string;
  value: number;
  timestamp: string;
}
export interface PlotStatus {
  plotId: string;
  status: string;
  message: string;
}
export interface Alert {
  id: string;
  plotId: string;
  type: string;
  message: string;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class AnalysisService {
  constructor(private http: HttpClient) {}

  getReadings(plotId: string, from?: string, to?: string, type?: string): Observable<Reading[]> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    if (type) params = params.set('type', type);
    return this.http.get<Reading[]>(`${BASE}/plots/${plotId}/readings`, { params });
  }

  getPlotStatus(plotId: string): Observable<PlotStatus> {
    return this.http.get<PlotStatus>(`${BASE}/plots/${plotId}/status`);
  }

  getAlerts(plotId?: string, from?: string): Observable<Alert[]> {
    let params = new HttpParams();
    if (plotId) params = params.set('plotId', plotId);
    if (from) params = params.set('from', from);
    return this.http.get<Alert[]>(`${BASE}/alerts`, { params });
  }
}

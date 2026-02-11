import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

const BASE = '/api/properties';

export interface Property {
  id: string;
  producerId: string;
  name: string;
  location: string | null;
  createdAt: string;
}
export interface Plot {
  id: string;
  propertyId: string;
  name: string;
  culture: string;
  createdAt: string;
}
export interface CreatePropertyRequest {
  name: string;
  location?: string | null;
}
export interface UpdatePropertyRequest extends CreatePropertyRequest {}
export interface CreatePlotRequest {
  name: string;
  culture: string;
}
export interface UpdatePlotRequest extends CreatePlotRequest {}

@Injectable({ providedIn: 'root' })
export class PropertiesService {
  constructor(private http: HttpClient) {}

  getProperties(): Observable<Property[]> {
    return this.http.get<Property[]>(BASE);
  }
  getProperty(id: string): Observable<Property> {
    return this.http.get<Property>(`${BASE}/${id}`);
  }
  createProperty(body: CreatePropertyRequest): Observable<Property> {
    return this.http.post<Property>(BASE, body);
  }
  updateProperty(id: string, body: UpdatePropertyRequest): Observable<void> {
    return this.http.put<void>(`${BASE}/${id}`, body);
  }
  deleteProperty(id: string): Observable<void> {
    return this.http.delete<void>(`${BASE}/${id}`);
  }
  getPlots(propertyId: string): Observable<Plot[]> {
    return this.http.get<Plot[]>(`${BASE}/${propertyId}/plots`);
  }
  getPlot(id: string): Observable<Plot> {
    return this.http.get<Plot>(`/api/plots/${id}`);
  }
  createPlot(propertyId: string, body: CreatePlotRequest): Observable<Plot> {
    return this.http.post<Plot>(`${BASE}/${propertyId}/plots`, body);
  }
  updatePlot(id: string, body: UpdatePlotRequest): Observable<void> {
    return this.http.put<void>(`/api/plots/${id}`, body);
  }
  deletePlot(id: string): Observable<void> {
    return this.http.delete<void>(`/api/plots/${id}`);
  }
}

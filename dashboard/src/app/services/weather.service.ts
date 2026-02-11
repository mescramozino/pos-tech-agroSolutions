import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

const BASE = '/api/weather';

export interface DailyForecast {
  date: string;
  maxTempC: number;
  minTempC: number;
  precipitationMm: number;
}

export interface WeatherForecast {
  location: string;
  temperatureC: number;
  humidityPercent: number;
  precipitationMm: number;
  weatherCode: number;
  daily: DailyForecast[];
}

@Injectable({ providedIn: 'root' })
export class WeatherService {
  constructor(private http: HttpClient) {}

  getForecast(city?: string, lat?: number, lon?: number): Observable<WeatherForecast> {
    let params = new HttpParams();
    if (city) params = params.set('city', city);
    if (lat != null) params = params.set('lat', lat);
    if (lon != null) params = params.set('lon', lon);
    return this.http.get<WeatherForecast>(`${BASE}/forecast`, { params });
  }
}

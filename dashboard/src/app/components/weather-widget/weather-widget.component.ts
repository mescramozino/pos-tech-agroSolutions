import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe, DecimalPipe } from '@angular/common';
import { WeatherService, WeatherForecast } from '../../services/weather.service';

@Component({
  selector: 'app-weather-widget',
  standalone: true,
  imports: [FormsModule, DatePipe, DecimalPipe],
  template: `
    <div class="weather-widget card">
      <div class="weather-header">
        <h3>Previsão do tempo</h3>
        <div class="weather-search">
          <input type="text" [(ngModel)]="cityInput" placeholder="Cidade (ex: São Paulo)" (keyup.enter)="search()" />
          <button class="btn btn-primary" (click)="search()">Buscar</button>
        </div>
      </div>
      @if (loading()) {
        <p>Carregando...</p>
      }
      @if (error()) {
        <p class="status-drought">{{ error() }}</p>
      }
      @if (forecast(); as f) {
        <div class="weather-current">
          <span class="weather-location">{{ f.location }}</span>
          <span class="weather-temp">{{ f.temperatureC | number:'1.1-1' }} °C</span>
          <span class="weather-meta">Umidade {{ f.humidityPercent }}% · Precip. {{ f.precipitationMm | number:'1.1-1' }} mm</span>
        </div>
        <div class="weather-daily">
          @for (d of f.daily; track d.date) {
            <div class="weather-day">
              <span class="weather-date">{{ d.date | date:'EEE dd/MM' }}</span>
              <span class="weather-minmax">{{ d.minTempC | number:'0.0-0' }}° / {{ d.maxTempC | number:'0.0-0' }}°</span>
              <span class="weather-precip">{{ d.precipitationMm | number:'0.1-1' }} mm</span>
            </div>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .weather-widget { max-width: 420px; }
    .weather-header { display: flex; flex-wrap: wrap; align-items: center; justify-content: space-between; gap: 0.5rem; margin-bottom: 0.75rem; }
    .weather-header h3 { margin: 0; font-size: 1rem; }
    .weather-search { display: flex; gap: 0.5rem; }
    .weather-search input { width: 140px; padding: 0.35rem 0.5rem; border: 1px solid #ccc; border-radius: 4px; font-size: 0.85rem; }
    .weather-current { display: flex; flex-direction: column; gap: 0.25rem; padding: 0.5rem 0; border-bottom: 1px solid #eee; }
    .weather-location { font-weight: 600; color: #2e7d32; }
    .weather-temp { font-size: 1.5rem; font-weight: 700; }
    .weather-meta { font-size: 0.85rem; color: #666; }
    .weather-daily { display: flex; flex-direction: column; gap: 0.35rem; margin-top: 0.5rem; }
    .weather-day { display: flex; justify-content: space-between; align-items: center; font-size: 0.9rem; }
    .weather-date { min-width: 80px; }
    .weather-minmax { font-weight: 500; }
    .weather-precip { color: #1565c0; }
  `],
})
export class WeatherWidgetComponent implements OnInit {
  forecast = signal<WeatherForecast | null>(null);
  loading = signal(false);
  error = signal('');
  cityInput = '';

  constructor(private weather: WeatherService) {}

  ngOnInit() {
    this.load('');
  }

  search() {
    this.load(this.cityInput.trim());
  }

  private load(city: string) {
    this.loading.set(true);
    this.error.set('');
    const req = city ? this.weather.getForecast(city) : this.weather.getForecast();
    req.subscribe({
      next: (f) => {
        this.forecast.set(f);
        if (!city) this.cityInput = f.location;
      },
      error: () => {
        this.error.set('Não foi possível carregar a previsão.');
        this.forecast.set(null);
      },
      complete: () => this.loading.set(false),
    });
  }
}

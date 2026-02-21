import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe, DecimalPipe } from '@angular/common';
import { WeatherService, WeatherForecast } from '../../services/weather.service';

@Component({
  selector: 'app-weather-widget',
  standalone: true,
  imports: [FormsModule, DatePipe, DecimalPipe],
  templateUrl: './weather-widget.component.html',
  styleUrls: ['./weather-widget.component.css'],
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

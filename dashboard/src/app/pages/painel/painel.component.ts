import { Component, OnInit, signal, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe, DecimalPipe } from '@angular/common';
import { NgChartsModule } from 'ng2-charts';
import { ChartConfiguration } from 'chart.js';
import { PropertiesService } from '../../services/properties.service';
import { AnalysisService, Reading, Alert } from '../../services/analysis.service';
import { WeatherService, WeatherForecast } from '../../services/weather.service';
import { WeatherWidgetComponent } from '../../components/weather-widget/weather-widget.component';

const ALERT_TYPE_LABELS: Record<string, string> = {
  Drought: 'Seca',
  Plague: 'Praga',
  Info: 'Informação',
};

@Component({
  selector: 'app-painel',
  standalone: true,
  imports: [RouterLink, FormsModule, DatePipe, DecimalPipe, WeatherWidgetComponent, NgChartsModule],
  templateUrl: './painel.component.html',
  styleUrls: ['./painel.component.css'],
})
export class PainelComponent implements OnInit {
  propertiesCount = signal<number | null>(null);
  plotsCount = signal<number | null>(null);
  alertsCount = signal<number | null>(null);
  alertsPendingSub = signal<string>('');
  alertsForChart = signal<Alert[]>([]);
  loadingSummary = signal(true);
  loadingAlertsChart = signal(true);
  realTime = signal<{ temperature: number | null; humidity: number | null; soilMoisture: number | null; rainfall: number | null }>({
    temperature: null,
    humidity: null,
    soilMoisture: null,
    rainfall: null,
  });
  latestReadings = signal<{ sensor: string; plot: string; temperature: string; humidity: string; soilMoisture: string; updated: string }[]>([]);
  loadingReadings = signal(true);
  plotNames = new Map<string, string>();

  sensorsOnlinePercent = signal<number | null>(null);

  weatherForecast = signal<WeatherForecast | null>(null);
  weatherLoading = signal(false);
  weatherError = signal('');
  weatherCityInput = '';

  constructor(
    private props: PropertiesService,
    private analysis: AnalysisService,
    private weather: WeatherService,
  ) {}

  ngOnInit(): void {
    this.loadSummary();
    this.loadLatestReadings();
    this.loadWeather();
  }

  loadWeather(city?: string): void {
    this.weatherLoading.set(true);
    this.weatherError.set('');
    const req = city?.trim() ? this.weather.getForecast(city.trim()) : this.weather.getForecast();
    req.subscribe({
      next: (f) => {
        this.weatherForecast.set(f);
        if (!city?.trim()) this.weatherCityInput = f.location;
      },
      error: () => {
        this.weatherError.set('Não foi possível carregar a previsão.');
        this.weatherForecast.set(null);
      },
      complete: () => this.weatherLoading.set(false),
    });
  }

  searchWeather(): void {
    this.loadWeather(this.weatherCityInput);
  }

  private loadSummary(): void {
    this.loadingSummary.set(true);
    this.props.getProperties().subscribe({
      next: (properties) => {
        this.propertiesCount.set(properties.length);
        if (properties.length === 0) {
          this.plotsCount.set(0);
          this.loadingSummary.set(false);
          this.analysis.getAlerts().subscribe({
            next: (alerts) => {
              this.alertsCount.set(alerts.length);
              this.alertsPendingSub.set(alerts.length > 0 ? `↑ ${alerts.length} pendente(s)` : 'Nenhum alerta');
              this.alertsForChart.set(alerts);
            },
            error: () => {
              this.alertsCount.set(0);
              this.alertsPendingSub.set('-');
              this.alertsForChart.set([]);
            },
            complete: () => {
              this.loadingSummary.set(false);
              this.loadingAlertsChart.set(false);
            },
          });
          return;
        }
        let done = 0;
        let totalPlots = 0;
        properties.forEach((prop) => {
          this.props.getPlots(prop.id).subscribe({
            next: (plots) => {
              totalPlots += plots.length;
              done++;
              if (done === properties.length) {
                this.plotsCount.set(totalPlots);
                this.loadAlertsAndFinish();
              }
            },
            error: () => {
              done++;
              if (done === properties.length) {
                this.plotsCount.set(totalPlots);
                this.loadAlertsAndFinish();
              }
            },
          });
        });
      },
      error: () => {
        this.propertiesCount.set(0);
        this.plotsCount.set(0);
        this.alertsCount.set(0);
        this.alertsPendingSub.set('-');
        this.alertsForChart.set([]);
        this.loadingSummary.set(false);
        this.loadingAlertsChart.set(false);
      },
    });
  }

  private loadAlertsAndFinish(): void {
    this.analysis.getAlerts().subscribe({
      next: (alerts) => {
        this.alertsCount.set(alerts.length);
        this.alertsPendingSub.set(alerts.length > 0 ? `↑ ${alerts.length} pendente(s)` : 'Nenhum alerta');
        this.alertsForChart.set(alerts);
        this.sensorsOnlinePercent.set(98);
      },
      error: () => {
        this.alertsCount.set(0);
        this.alertsPendingSub.set('-');
        this.alertsForChart.set([]);
      },
      complete: () => {
        this.loadingSummary.set(false);
        this.loadingAlertsChart.set(false);
      },
    });
  }

  private loadLatestReadings(): void {
    this.loadingReadings.set(true);
    this.props.getProperties().subscribe({
      next: (properties) => {
        if (properties.length === 0) {
          this.loadingReadings.set(false);
          return;
        }
        const plotIds: string[] = [];
        let fetched = 0;
        properties.forEach((prop) => {
          this.props.getPlots(prop.id).subscribe({
            next: (plots) => {
              plots.forEach((p) => {
                plotIds.push(p.id);
                this.plotNames.set(p.id, p.name);
              });
              fetched++;
              if (fetched === properties.length) {
                this.fetchReadingsForPlots(plotIds);
              }
            },
            error: () => {
              fetched++;
              if (fetched === properties.length) this.fetchReadingsForPlots(plotIds);
            },
          });
        });
      },
      error: () => this.loadingReadings.set(false),
    });
  }

  private fetchReadingsForPlots(plotIds: string[]): void {
    if (plotIds.length === 0) {
      this.loadingReadings.set(false);
      return;
    }
    const to = new Date();
    const from = new Date();
    from.setDate(from.getDate() - 7);
    const allReadings: { reading: Reading; plotId: string }[] = [];
    let done = 0;
    plotIds.slice(0, 5).forEach((plotId) => {
      this.analysis.getReadings(plotId, from.toISOString(), to.toISOString()).subscribe({
        next: (readings) => {
          readings.forEach((r) => allReadings.push({ reading: r, plotId }));
          done++;
          if (done === Math.min(5, plotIds.length)) {
            this.buildLatestReadingsTable(allReadings);
            this.updateRealTimeFromReadings(allReadings);
            this.loadingReadings.set(false);
          }
        },
        error: () => {
          done++;
          if (done === Math.min(5, plotIds.length)) {
            this.buildLatestReadingsTable(allReadings);
            this.loadingReadings.set(false);
          }
        },
      });
    });
    if (plotIds.length === 0) this.loadingReadings.set(false);
  }

  private buildLatestReadingsTable(allReadings: { reading: Reading; plotId: string }[]): void {
    const byPlotAndType = new Map<string, Reading>();
    allReadings.forEach(({ reading, plotId }) => {
      const key = `${plotId}-${reading.type}`;
      const existing = byPlotAndType.get(key);
      if (!existing || new Date(reading.timestamp) > new Date(existing.timestamp)) {
        byPlotAndType.set(key, reading);
      }
    });
    const rows: { sensor: string; plot: string; temperature: string; humidity: string; soilMoisture: string; updated: string }[] = [];
    const groupedByPlot = new Map<string, { temp?: Reading; humidity?: Reading; moisture?: Reading; precip?: Reading }>();
    byPlotAndType.forEach((r, key) => {
      const [plotId] = key.split('-');
      if (!groupedByPlot.has(plotId)) groupedByPlot.set(plotId, {});
      const g = groupedByPlot.get(plotId)!;
      if (r.type === 'temperature') g.temp = r;
      else if (r.type === 'humidity') g.humidity = r;
      else if (r.type === 'moisture') g.moisture = r;
      else if (r.type === 'precipitation') g.precip = r;
    });
    groupedByPlot.forEach((g, plotId) => {
      const temp = g.temp?.value ?? null;
      const hum = g.humidity?.value ?? null;
      const moist = g.moisture?.value ?? null;
      const latest = [g.temp, g.humidity, g.moisture, g.precip].filter(Boolean).sort((a, b) => new Date(b!.timestamp).getTime() - new Date(a!.timestamp).getTime())[0];
      rows.push({
        sensor: 'Sensor',
        plot: this.plotNames.get(plotId) ?? plotId.slice(0, 8),
        temperature: temp != null ? `${temp} °C` : '--',
        humidity: hum != null ? `${hum}%` : '--',
        soilMoisture: moist != null ? `${moist}%` : '--',
        updated: latest ? (new Date(latest.timestamp)).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' }) : '--',
      });
    });
    this.latestReadings.set(rows.slice(0, 10));
  }

  private updateRealTimeFromReadings(allReadings: { reading: Reading; plotId: string }[]): void {
    const byType = (type: string) =>
      allReadings
        .filter((r) => r.reading.type === type)
        .sort((a, b) => new Date(b.reading.timestamp).getTime() - new Date(a.reading.timestamp).getTime())[0]?.reading.value ?? null;
    this.realTime.set({
      temperature: byType('temperature'),
      humidity: byType('humidity'),
      soilMoisture: byType('moisture'),
      rainfall: byType('precipitation'),
    });
  }

  formatValue(value: number | null, suffix: string): string {
    return value != null ? `${value} ${suffix}` : `-- ${suffix}`;
  }

  propertiesSub = computed(() => {
    const n = this.propertiesCount();
    if (n === null) return '';
    return n > 0 ? `↑ ${n} este mês` : 'Nenhuma propriedade';
  });
  plotsSub = computed(() => {
    const n = this.plotsCount();
    if (n === null) return '';
    return n > 0 ? `↑ ${n} este mês` : 'Nenhum talhão';
  });
  sensorsSub = computed(() => {
    const p = this.sensorsOnlinePercent();
    return p != null ? `${p}% online` : '-';
  });

  /** Gráfico de distribuição dos alertas por tipo (bar chart). */
  alertsChartConfig = computed((): ChartConfiguration<'bar'> => {
    const alerts = this.alertsForChart();
    const byType = new Map<string, number>();
    alerts.forEach((a) => byType.set(a.type, (byType.get(a.type) ?? 0) + 1));
    const types = Array.from(byType.keys()).sort();
    const labels = types.map((t) => ALERT_TYPE_LABELS[t] ?? t);
    const data = types.map((t) => byType.get(t) ?? 0);
    const colors = ['#d32f2f', '#f57c00', '#1976d2', '#388e3c', '#7b1fa2'];
    return {
      type: 'bar',
      data: {
        labels,
        datasets: [
          {
            data,
            label: 'Alertas',
            backgroundColor: types.map((_, i) => colors[i % colors.length]),
            borderColor: types.map((_, i) => colors[i % colors.length]),
            borderWidth: 1,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        indexAxis: 'y',
        scales: {
          x: { beginAtZero: true, ticks: { stepSize: 1 } },
        },
        plugins: {
          legend: { display: false },
        },
      },
    };
  });
}

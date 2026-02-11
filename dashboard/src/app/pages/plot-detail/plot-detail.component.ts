import { Component, OnInit, signal, computed } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { NgChartsModule } from 'ng2-charts';
import { ChartConfiguration } from 'chart.js';
import { PropertiesService, Plot } from '../../services/properties.service';
import { AnalysisService, Reading, PlotStatus } from '../../services/analysis.service';

@Component({
  selector: 'app-plot-detail',
  standalone: true,
  imports: [RouterLink, NgChartsModule],
  template: `
    <div class="toolbar">
      <a [routerLink]="['/properties', propertyId()]" class="btn btn-secondary">← Voltar à propriedade</a>
    </div>
    @if (loading()) {
      <p>Carregando...</p>
    }
    @if (error()) {
      <p class="status-drought">{{ error() }}</p>
    }
    @if (plot(); as pl) {
      <div class="card">
        <h1>{{ pl.name }}</h1>
        <p>Cultura: {{ pl.culture }}</p>
        @if (status(); as st) {
          <p [class]="getStatusClass(st.status)">
            Status: {{ getStatusLabel(st.status) }} — {{ st.message }}
          </p>
        }
      </div>

      <h2>Leituras de sensores</h2>
      @if (readings().length === 0) {
        <p>Nenhuma leitura no período. Envie dados pela API de Ingestão.</p>
      }
      @if (readings().length > 0) {
        <div class="chart-container">
          <h3>Umidade (%)</h3>
          <canvas baseChart
            [data]="chartMoisture().data"
            [options]="chartMoisture().options"
            [type]="'line'">
          </canvas>
        </div>
        <div class="chart-container">
          <h3>Temperatura (°C)</h3>
          <canvas baseChart
            [data]="chartTemperature().data"
            [options]="chartTemperature().options"
            [type]="'line'">
          </canvas>
        </div>
        <div class="chart-container">
          <h3>Precipitação (mm)</h3>
          <canvas baseChart
            [data]="chartPrecipitation().data"
            [options]="chartPrecipitation().options"
            [type]="'line'">
          </canvas>
        </div>
      }
    }
  `,
})
export class PlotDetailComponent implements OnInit {
  plot = signal<Plot | null>(null);
  status = signal<PlotStatus | null>(null);
  readings = signal<Reading[]>([]);
  loading = signal(true);
  error = signal('');
  propertyId = signal('');

  private plotId = '';

  constructor(
    private route: ActivatedRoute,
    private props: PropertiesService,
    private analysis: AnalysisService,
  ) {}

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;
    this.plotId = id;
    this.loading.set(true);
    this.props.getPlot(id).subscribe({
      next: (p) => {
        this.plot.set(p);
        this.propertyId.set(p.propertyId);
        this.loadStatus();
        this.loadReadings();
      },
      error: () => {
        this.error.set('Talhão não encontrado.');
        this.loading.set(false);
      },
      complete: () => this.loading.set(false),
    });
  }

  loadStatus() {
    this.analysis.getPlotStatus(this.plotId).subscribe({
      next: (st) => this.status.set(st),
      error: () => this.status.set(null),
    });
  }

  loadReadings() {
    const to = new Date();
    const from = new Date();
    from.setDate(from.getDate() - 30);
    this.analysis
      .getReadings(this.plotId, from.toISOString(), to.toISOString())
      .subscribe({
        next: (list) => this.readings.set(list),
        error: () => this.readings.set([]),
      });
  }

  chartMoisture = computed(() => this.buildChart('moisture', 'Umidade %', '#2e7d32'));
  chartTemperature = computed(() => this.buildChart('temperature', 'Temperatura °C', '#1565c0'));
  chartPrecipitation = computed(() => this.buildChart('precipitation', 'Precipitação mm', '#00838f'));

  getStatusClass(status: string): string {
    if (status === 'DroughtAlert') return 'status-drought';
    if (status === 'PlagueRisk') return 'status-plague';
    return 'status-normal';
  }

  getStatusLabel(status: string): string {
    if (status === 'DroughtAlert') return 'Alerta de Seca';
    if (status === 'PlagueRisk') return 'Risco de Praga';
    return status;
  }

  private buildChart(type: string, label: string, color: string): ChartConfiguration<'line'> {
    const list = this.readings().filter((r) => r.type === type);
    const labels = list.map((r) => new Date(r.timestamp).toLocaleDateString());
    const values = list.map((r) => r.value);
    return {
      type: 'line',
      data: {
        labels,
        datasets: [{ data: values, label, borderColor: color, fill: false, tension: 0.3 }],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
          y: { beginAtZero: true },
        },
      },
    };
  }
}

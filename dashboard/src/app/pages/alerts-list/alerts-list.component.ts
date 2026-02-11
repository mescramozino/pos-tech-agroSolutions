import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { AnalysisService, Alert } from '../../services/analysis.service';
import { PropertiesService } from '../../services/properties.service';

@Component({
  selector: 'app-alerts-list',
  standalone: true,
  imports: [RouterLink, DatePipe],
  template: `
    <h1>Alertas</h1>
    @if (loading()) {
      <p>Carregando...</p>
    }
    @if (error()) {
      <p class="status-drought">{{ error() }}</p>
    }
    @if (!loading() && !error()) {
      @if (alerts().length === 0) {
        <p class="status-normal">Nenhum alerta no momento.</p>
      }
      @if (alerts().length > 0) {
        @for (a of alerts(); track a.id) {
          <div class="card">
            <span [class]="a.type === 'Drought' ? 'status-drought' : a.type === 'Plague' ? 'status-plague' : 'status-alert'">{{ getAlertTypeLabel(a.type) }}</span>
            <p>{{ a.message }}</p>
            <p><small>{{ a.createdAt | date:'short' }}</small></p>
            @if (plotName(a.plotId); as name) {
              <a [routerLink]="['/plots', a.plotId]">Ver talhão: {{ name }}</a>
            } @else {
              <a [routerLink]="['/plots', a.plotId]">Ver talhão</a>
            }
          </div>
        }
      }
    }
  `,
})
export class AlertsListComponent implements OnInit {
  alerts = signal<Alert[]>([]);
  loading = signal(true);
  error = signal('');
  private plotNames: Record<string, string> = {};

  constructor(
    private analysis: AnalysisService,
    private props: PropertiesService,
  ) {}

  ngOnInit() {
    this.loading.set(true);
    this.analysis.getAlerts().subscribe({
      next: (list) => {
        this.alerts.set(list);
        this.loadPlotNames(list.map((a) => a.plotId).filter((id, i, arr) => arr.indexOf(id) === i));
      },
      error: () => {
        this.error.set('Erro ao carregar alertas.');
        this.loading.set(false);
      },
      complete: () => this.loading.set(false),
    });
  }

  private loadPlotNames(plotIds: string[]) {
    plotIds.forEach((id) => {
      this.props.getPlot(id).subscribe({
        next: (p) => (this.plotNames[id] = p.name),
        error: () => {},
      });
    });
  }

  plotName(plotId: string): string | null {
    return this.plotNames[plotId] ?? null;
  }

  getAlertTypeLabel(type: string): string {
    if (type === 'Drought') return 'Alerta de Seca';
    if (type === 'Plague') return 'Risco de Praga';
    return type;
  }
}

import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { AnalysisService, Alert } from '../../services/analysis.service';
import { PropertiesService } from '../../services/properties.service';
import type { Plot } from '../../services/properties.service';

interface PlotInfo {
  name: string;
  propertyId: string;
  culture: string;
}

@Component({
  selector: 'app-alerts-list',
  standalone: true,
  imports: [RouterLink, DatePipe],
  templateUrl: './alerts-list.component.html',
  styleUrls: ['./alerts-list.component.css'],
})
export class AlertsListComponent implements OnInit {
  alerts = signal<Alert[]>([]);
  loading = signal(true);
  error = signal('');
  plotInfo = signal<Record<string, PlotInfo>>({});
  propertyNames = signal<Record<string, string>>({});

  constructor(
    private analysis: AnalysisService,
    private props: PropertiesService,
  ) {}

  ngOnInit() {
    this.loading.set(true);
    this.analysis.getAlerts().subscribe({
      next: (list) => {
        this.alerts.set(list);
        const plotIds = [...new Set(list.map((a) => a.plotId))];
        this.loadPlotAndPropertyNames(plotIds);
      },
      error: () => {
        this.error.set('Erro ao carregar alertas.');
        this.loading.set(false);
      },
      complete: () => this.loading.set(false),
    });
  }

  private loadPlotAndPropertyNames(plotIds: string[]) {
    if (plotIds.length === 0) return;
    const plotInfo: Record<string, PlotInfo> = {};
    const propertyIds = new Set<string>();
    let done = 0;
    const total = plotIds.length;
    const maybeDone = () => {
      done++;
      if (done === total) {
        [...propertyIds].forEach((id) => {
          this.props.getProperty(id).subscribe({
            next: (p) => {
              this.propertyNames.update((m) => ({ ...m, [id]: p.name }));
            },
            error: () => {},
          });
        });
      }
    };
    plotIds.forEach((id) => {
      this.props.getPlot(id).subscribe({
        next: (p: Plot) => {
          plotInfo[id] = { name: p.name, propertyId: p.propertyId, culture: p.culture };
          propertyIds.add(p.propertyId);
          this.plotInfo.set({ ...plotInfo });
          maybeDone();
        },
        error: () => maybeDone(),
      });
    });
  }

  plotName(plotId: string): string | null {
    return this.plotInfo()[plotId]?.name ?? null;
  }

  propertyName(plotId: string): string | null {
    const info = this.plotInfo()[plotId];
    return info ? this.propertyNames()[info.propertyId] ?? null : null;
  }

  culture(plotId: string): string | null {
    return this.plotInfo()[plotId]?.culture ?? null;
  }

  getSeverity(type: string): 'critical' | 'warning' | 'info' {
    if (type === 'Drought' || type === 'Flood') return 'critical';
    if (type === 'Plague' || type === 'Frost') return 'warning';
    if (type === 'Info') return 'info';
    return 'warning';
  }

  getAlertTypeLabel(type: string): string {
    if (type === 'Drought') return 'Crítico';
    if (type === 'Plague') return 'Aviso';
    if (type === 'Frost') return 'Geada';
    if (type === 'Flood') return 'Alagamento';
    if (type === 'Info') return 'Informativo';
    return type;
  }

  getAlertTitle(type: string, plotId: string): string {
    const label = this.getAlertTypeLabel(type);
    const plot = this.plotName(plotId);
    return plot ? `${label} - ${plot}` : label;
  }

  timeAgo(isoDate: string): string {
    const d = new Date(isoDate);
    const now = new Date();
    const diffMs = now.getTime() - d.getTime();
    const diffM = Math.floor(diffMs / 60000);
    const diffH = Math.floor(diffM / 60);
    const diffD = Math.floor(diffH / 24);
    if (diffM < 1) return 'Agora';
    if (diffM < 60) return `Há ${diffM} minuto${diffM !== 1 ? 's' : ''}`;
    if (diffH < 24) return `Há ${diffH} hora${diffH !== 1 ? 's' : ''}`;
    return `Há ${diffD} dia${diffD !== 1 ? 's' : ''}`;
  }

  resolveAlert(alert: Alert) {
    this.alerts.update((list) => list.filter((a) => a.id !== alert.id));
  }
}

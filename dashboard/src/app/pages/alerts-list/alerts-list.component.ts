import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { AnalysisService, Alert } from '../../services/analysis.service';
import { PropertiesService } from '../../services/properties.service';

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

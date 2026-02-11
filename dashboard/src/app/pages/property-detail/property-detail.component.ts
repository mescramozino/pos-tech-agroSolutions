import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { PropertiesService, Property, Plot, CreatePlotRequest } from '../../services/properties.service';

@Component({
  selector: 'app-property-detail',
  standalone: true,
  imports: [RouterLink, FormsModule],
  template: `
    <div class="toolbar">
      <a routerLink="/properties" class="btn btn-secondary">← Voltar</a>
    </div>
    @if (loading()) {
      <p>Carregando...</p>
    }
    @if (error()) {
      <p class="status-drought">{{ error() }}</p>
    }
    @if (property(); as p) {
      <div class="card">
        @if (editing()) {
          <form (ngSubmit)="saveProperty()">
            <div class="form-group">
              <label>Nome</label>
              <input [(ngModel)]="editName" name="editName" required />
            </div>
            <div class="form-group">
              <label>Localização</label>
              <input [(ngModel)]="editLocation" name="editLocation" />
            </div>
            <button type="submit" class="btn btn-primary">Salvar</button>
            <button type="button" class="btn btn-secondary" (click)="cancelEdit()">Cancelar</button>
          </form>
        } @else {
          <h1>{{ p.name }}</h1>
          @if (p.location) { <p>{{ p.location }}</p> }
          <button class="btn btn-primary" (click)="startEdit(p)">Editar</button>
          <button class="btn btn-danger" (click)="deleteProperty(p.id)">Excluir</button>
        }
      </div>

      <h2>Talhões</h2>
      @if (showNewPlotForm()) {
        <div class="card">
          <h3>Novo talhão</h3>
          <form (ngSubmit)="createPlot()">
            <div class="form-group">
              <label>Nome</label>
              <input [(ngModel)]="newPlotName" name="newPlotName" required />
            </div>
            <div class="form-group">
              <label>Cultura</label>
              <input [(ngModel)]="newPlotCulture" name="newPlotCulture" required />
            </div>
            <button type="submit" class="btn btn-primary">Criar</button>
            <button type="button" class="btn btn-secondary" (click)="showNewPlotForm.set(false)">Cancelar</button>
          </form>
        </div>
      } @else {
        <button class="btn btn-primary" (click)="showNewPlotForm.set(true)" style="margin-bottom: 1rem;">Novo talhão</button>
      }

      @if (plots().length === 0 && !showNewPlotForm()) {
        <p>Nenhum talhão. Adicione um para começar.</p>
      }
      @for (plot of plots(); track plot.id) {
        <div class="card">
          <h3><a [routerLink]="['/plots', plot.id]">{{ plot.name }}</a></h3>
          <p>Cultura: {{ plot.culture }}</p>
          <a [routerLink]="['/plots', plot.id]">Ver sensores e gráficos</a>
          <button class="btn btn-danger" style="margin-left: 1rem;" (click)="deletePlot(plot)">Excluir talhão</button>
        </div>
      }
    }
  `,
})
export class PropertyDetailComponent implements OnInit {
  property = signal<Property | null>(null);
  plots = signal<Plot[]>([]);
  loading = signal(true);
  error = signal('');
  editing = signal(false);
  editName = '';
  editLocation = '';
  showNewPlotForm = signal(false);
  newPlotName = '';
  newPlotCulture = '';

  private propertyId = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private props: PropertiesService,
  ) {}

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id || id === 'new') {
      this.router.navigate(['/properties']);
      return;
    }
    this.propertyId = id;
    this.loading.set(true);
    this.props.getProperty(id).subscribe({
      next: (p) => {
        this.property.set(p);
        this.editName = p.name;
        this.editLocation = p.location ?? '';
        this.loadPlots();
      },
      error: () => {
        this.error.set('Propriedade não encontrada.');
        this.loading.set(false);
      },
      complete: () => this.loading.set(false),
    });
  }

  loadPlots() {
    this.props.getPlots(this.propertyId).subscribe({
      next: (list) => this.plots.set(list),
      error: () => this.plots.set([]),
    });
  }

  startEdit(p: Property) {
    this.editName = p.name;
    this.editLocation = p.location ?? '';
    this.editing.set(true);
  }

  cancelEdit() {
    this.editing.set(false);
  }

  saveProperty() {
    const p = this.property();
    if (!p) return;
    this.props.updateProperty(p.id, { name: this.editName, location: this.editLocation || null }).subscribe({
      next: () => {
        this.property.set({ ...p, name: this.editName, location: this.editLocation || null });
        this.editing.set(false);
      },
      error: () => this.error.set('Erro ao salvar.'),
    });
  }

  deleteProperty(id: string) {
    if (!confirm('Excluir esta propriedade e todos os talhões?')) return;
    this.props.deleteProperty(id).subscribe({
      next: () => this.router.navigate(['/properties']),
      error: () => this.error.set('Erro ao excluir.'),
    });
  }

  createPlot() {
    const body: CreatePlotRequest = { name: this.newPlotName, culture: this.newPlotCulture };
    this.props.createPlot(this.propertyId, body).subscribe({
      next: (plot) => {
        this.plots.update((list) => [...list, plot]);
        this.showNewPlotForm.set(false);
        this.newPlotName = '';
        this.newPlotCulture = '';
      },
      error: () => this.error.set('Erro ao criar talhão.'),
    });
  }

  deletePlot(plot: Plot) {
    if (!confirm('Excluir o talhão "' + plot.name + '"?')) return;
    this.props.deletePlot(plot.id).subscribe({
      next: () => this.plots.update((list) => list.filter((pl) => pl.id !== plot.id)),
      error: () => this.error.set('Erro ao excluir talhão.'),
    });
  }
}

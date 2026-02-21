import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { PropertiesService, Property, Plot, CreatePlotRequest } from '../../services/properties.service';

@Component({
  selector: 'app-property-detail',
  standalone: true,
  imports: [RouterLink, FormsModule],
  templateUrl: './property-detail.component.html',
  styleUrls: ['./property-detail.component.css'],
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

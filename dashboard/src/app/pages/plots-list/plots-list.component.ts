import { Component, OnInit, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { PropertiesService, Property, Plot } from '../../services/properties.service';

type PlotItem = { plot: Plot; property: Property };

@Component({
  selector: 'app-plots-list',
  standalone: true,
  imports: [RouterLink, FormsModule, DatePipe],
  templateUrl: './plots-list.component.html',
  styleUrls: ['./plots-list.component.css'],
})
export class PlotsListComponent implements OnInit {
  plots = signal<PlotItem[]>([]);
  properties = signal<Property[]>([]);
  loading = signal(false);
  error = signal('');

  searchText = signal('');
  page = signal(1);
  pageSize = signal(10);
  sortBy = signal<'name' | 'culture' | 'createdAt' | 'propertyName'>('name');
  sortDir = signal<'asc' | 'desc'>('asc');

  pageSizeOptions = [5, 10, 20];
  sortByOptions: { value: 'name' | 'culture' | 'createdAt' | 'propertyName'; label: string }[] = [
    { value: 'name', label: 'Nome' },
    { value: 'culture', label: 'Cultura' },
    { value: 'propertyName', label: 'Propriedade' },
    { value: 'createdAt', label: 'Criado em' },
  ];

  filteredAndSorted = computed(() => {
    const list = this.plots();
    const search = this.searchText().trim().toLowerCase();
    const by = this.sortBy();
    const dir = this.sortDir();
    let result = search
      ? list.filter(
          (item) =>
            item.plot.name.toLowerCase().includes(search) ||
            item.plot.culture.toLowerCase().includes(search) ||
            item.property.name.toLowerCase().includes(search)
        )
      : [...list];
    result = [...result].sort((a, b) => {
      let cmp = 0;
      if (by === 'name') cmp = a.plot.name.localeCompare(b.plot.name);
      else if (by === 'culture') cmp = a.plot.culture.localeCompare(b.plot.culture);
      else if (by === 'propertyName') cmp = a.property.name.localeCompare(b.property.name);
      else cmp = new Date(a.plot.createdAt).getTime() - new Date(b.plot.createdAt).getTime();
      return dir === 'asc' ? cmp : -cmp;
    });
    return result;
  });

  totalFiltered = computed(() => this.filteredAndSorted().length);

  paginated = computed(() => {
    const list = this.filteredAndSorted();
    const p = this.page();
    const size = this.pageSize();
    const start = (p - 1) * size;
    return list.slice(start, start + size);
  });

  totalPages = computed(() =>
    Math.max(1, Math.ceil(this.totalFiltered() / this.pageSize()))
  );

  pageNumbers = computed(() => {
    const n = this.totalPages();
    return Array.from({ length: n }, (_, i) => i + 1);
  });

  canPrev = computed(() => this.page() > 1);
  canNext = computed(() => this.page() < this.totalPages());

  formModalOpen = signal(false);
  editingItem = signal<PlotItem | null>(null);
  deleteModalOpen = signal(false);
  deletingItem = signal<PlotItem | null>(null);
  formSaving = signal(false);
  formError = signal('');

  formPropertyId = '';
  formName = '';
  formCulture = '';

  constructor(private props: PropertiesService) {}

  ngOnInit() {
    this.loading.set(true);
    this.props.getProperties().subscribe({
      next: (properties) => {
        this.properties.set(properties);
        if (properties.length === 0) {
          this.loading.set(false);
          return;
        }
        let done = 0;
        const total = properties.length;
        const list: PlotItem[] = [];
        properties.forEach((prop) => {
          this.props.getPlots(prop.id).subscribe({
            next: (plotList) => {
              plotList.forEach((plot) => list.push({ plot, property: prop }));
              done++;
              if (done === total) {
                this.plots.set(list);
                this.loading.set(false);
              }
            },
            error: () => {
              done++;
              if (done === total) this.loading.set(false);
            },
          });
        });
      },
      error: () => {
        this.error.set('Erro ao carregar talhões.');
        this.loading.set(false);
      },
    });
  }

  applyFilters() {
    this.page.set(1);
  }

  setSearch(value: string) {
    this.searchText.set(value);
    this.page.set(1);
  }

  setPage(p: number) {
    this.page.set(Math.max(1, Math.min(p, this.totalPages())));
  }

  prevPage() {
    if (this.canPrev()) this.setPage(this.page() - 1);
  }

  nextPage() {
    if (this.canNext()) this.setPage(this.page() + 1);
  }

  openCreateModal() {
    this.editingItem.set(null);
    const props = this.properties();
    this.formPropertyId = props.length > 0 ? props[0].id : '';
    this.formName = '';
    this.formCulture = '';
    this.formError.set('');
    this.formModalOpen.set(true);
  }

  openEditModal(item: PlotItem) {
    this.editingItem.set(item);
    this.formPropertyId = item.plot.propertyId;
    this.formName = item.plot.name;
    this.formCulture = item.plot.culture;
    this.formError.set('');
    this.formModalOpen.set(true);
  }

  closeFormModal() {
    this.formModalOpen.set(false);
    this.editingItem.set(null);
  }

  savePlot() {
    this.formError.set('');
    const name = this.formName.trim();
    const culture = this.formCulture.trim();
    if (!name || !culture) {
      this.formError.set('Nome e cultura são obrigatórios.');
      return;
    }
    const item = this.editingItem();
    this.formSaving.set(true);
    if (item) {
      this.props.updatePlot(item.plot.id, { name, culture }).subscribe({
        next: () => {
          this.plots.update((list) =>
            list.map((i) =>
              i.plot.id === item.plot.id
                ? { ...i, plot: { ...i.plot, name, culture } }
                : i
            )
          );
          this.closeFormModal();
        },
        error: () => {
          this.formError.set('Erro ao salvar alterações.');
          this.formSaving.set(false);
        },
        complete: () => this.formSaving.set(false),
      });
    } else {
      if (!this.formPropertyId) {
        this.formError.set('Selecione uma propriedade.');
        this.formSaving.set(false);
        return;
      }
      this.props.createPlot(this.formPropertyId, { name, culture }).subscribe({
        next: (plot) => {
          const prop = this.properties().find((p) => p.id === this.formPropertyId);
          if (prop) this.plots.update((list) => [...list, { plot, property: prop }]);
          this.closeFormModal();
        },
        error: () => {
          this.formError.set('Erro ao criar talhão.');
          this.formSaving.set(false);
        },
        complete: () => this.formSaving.set(false),
      });
    }
  }

  openDeleteModal(item: PlotItem) {
    this.deletingItem.set(item);
    this.deleteModalOpen.set(true);
  }

  closeDeleteModal() {
    this.deleteModalOpen.set(false);
    this.deletingItem.set(null);
  }

  confirmDelete() {
    const item = this.deletingItem();
    if (!item) return;
    this.props.deletePlot(item.plot.id).subscribe({
      next: () => {
        this.plots.update((list) => list.filter((i) => i.plot.id !== item.plot.id));
        this.closeDeleteModal();
      },
      error: () => this.error.set('Erro ao excluir.'),
    });
  }
}

import { Component, OnInit, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { PropertiesService, Property } from '../../services/properties.service';

@Component({
  selector: 'app-properties-list',
  standalone: true,
  imports: [RouterLink, FormsModule, DatePipe],
  templateUrl: './properties-list.component.html',
  styleUrls: ['./properties-list.component.css'],
})
export class PropertiesListComponent implements OnInit {
  properties = signal<Property[]>([]);
  loading = signal(false);
  error = signal('');
  plotsCountMap = signal<Record<string, number>>({});

  searchText = signal('');
  page = signal(1);
  pageSize = signal(10);
  sortBy = signal<'name' | 'createdAt'>('name');
  sortDir = signal<'asc' | 'desc'>('asc');

  pageSizeOptions = [5, 10, 20];
  sortByOptions: { value: 'name' | 'createdAt'; label: string }[] = [
    { value: 'name', label: 'Nome' },
    { value: 'createdAt', label: 'Criado em' },
  ];

  filteredAndSorted = computed(() => {
    const list = this.properties();
    const search = this.searchText().trim().toLowerCase();
    const by = this.sortBy();
    const dir = this.sortDir();
    let result = search
      ? list.filter(
          (p) =>
            p.name.toLowerCase().includes(search) ||
            (p.location ?? '').toLowerCase().includes(search)
        )
      : [...list];
    result = [...result].sort((a, b) => {
      let cmp = 0;
      if (by === 'name') cmp = a.name.localeCompare(b.name);
      else cmp = new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
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

  getPlotsCount(id: string): number {
    return this.plotsCountMap()[id] ?? 0;
  }

  formModalOpen = signal(false);
  editingProperty = signal<Property | null>(null);
  deleteModalOpen = signal(false);
  deletingProperty = signal<Property | null>(null);
  formSaving = signal(false);
  formError = signal('');

  formName = '';
  formLocation = '';

  constructor(private props: PropertiesService) {}

  ngOnInit() {
    this.loading.set(true);
    this.props.getProperties().subscribe({
      next: (list) => {
        this.properties.set(list);
        this.loadPlotsCounts(list);
      },
      error: () => this.error.set('Erro ao carregar propriedades.'),
      complete: () => this.loading.set(false),
    });
  }

  private loadPlotsCounts(list: Property[]) {
    if (list.length === 0) return;
    const map: Record<string, number> = {};
    let done = 0;
    list.forEach((p) => {
      this.props.getPlots(p.id).subscribe({
        next: (plots) => {
          map[p.id] = plots.length;
          done++;
          if (done === list.length) this.plotsCountMap.set(map);
        },
        error: () => {
          map[p.id] = 0;
          done++;
          if (done === list.length) this.plotsCountMap.set(map);
        },
      });
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
    this.editingProperty.set(null);
    this.formName = '';
    this.formLocation = '';
    this.formError.set('');
    this.formModalOpen.set(true);
  }

  openEditModal(property: Property) {
    this.editingProperty.set(property);
    this.formName = property.name;
    this.formLocation = property.location ?? '';
    this.formError.set('');
    this.formModalOpen.set(true);
  }

  closeFormModal() {
    this.formModalOpen.set(false);
    this.editingProperty.set(null);
  }

  saveProperty() {
    this.formError.set('');
    const name = this.formName.trim();
    const location = this.formLocation.trim() || null;
    if (!name) {
      this.formError.set('Nome da propriedade é obrigatório.');
      return;
    }
    const prop = this.editingProperty();
    this.formSaving.set(true);
    if (prop) {
      this.props.updateProperty(prop.id, { name, location }).subscribe({
        next: () => {
          this.properties.update((list) =>
            list.map((p) => (p.id === prop.id ? { ...p, name, location } : p))
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
      this.props.createProperty({ name, location }).subscribe({
        next: (created) => {
          this.properties.update((list) => [...list, created]);
          this.loadPlotsCounts([created]);
          this.closeFormModal();
        },
        error: () => {
          this.formError.set('Erro ao criar propriedade.');
          this.formSaving.set(false);
        },
        complete: () => this.formSaving.set(false),
      });
    }
  }

  openDeleteModal(property: Property) {
    this.deletingProperty.set(property);
    this.deleteModalOpen.set(true);
  }

  closeDeleteModal() {
    this.deleteModalOpen.set(false);
    this.deletingProperty.set(null);
  }

  confirmDelete() {
    const prop = this.deletingProperty();
    if (!prop) return;
    this.props.deleteProperty(prop.id).subscribe({
      next: () => {
        this.properties.update((list) => list.filter((p) => p.id !== prop.id));
        this.plotsCountMap.update((map) => {
          const next = { ...map };
          delete next[prop.id];
          return next;
        });
        this.closeDeleteModal();
      },
      error: () => this.error.set('Erro ao excluir.'),
    });
  }
}

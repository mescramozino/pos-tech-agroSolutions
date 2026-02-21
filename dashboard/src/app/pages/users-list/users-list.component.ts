import { Component, OnInit, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { UsersService, User } from '../../services/users.service';

@Component({
  selector: 'app-users-list',
  standalone: true,
  imports: [RouterLink, FormsModule, DatePipe],
  templateUrl: './users-list.component.html',
  styleUrls: ['./users-list.component.css'],
})
export class UsersListComponent implements OnInit {
  users = signal<User[]>([]);
  loading = signal(false);
  error = signal('');

  searchText = signal('');
  page = signal(1);
  pageSize = signal(10);
  sortBy = signal<'email' | 'createdAt'>('email');
  sortDir = signal<'asc' | 'desc'>('asc');

  pageSizeOptions = [5, 10, 20];
  sortByOptions: { value: 'email' | 'createdAt'; label: string }[] = [
    { value: 'email', label: 'E-mail' },
    { value: 'createdAt', label: 'Criado em' },
  ];

  filteredAndSorted = computed(() => {
    const list = this.users();
    const search = this.searchText().trim().toLowerCase();
    const by = this.sortBy();
    const dir = this.sortDir();
    let result = search
      ? list.filter((u) => u.email.toLowerCase().includes(search))
      : [...list];
    result = [...result].sort((a, b) => {
      let cmp = 0;
      if (by === 'email') cmp = a.email.localeCompare(b.email);
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

  formModalOpen = signal(false);
  editingUser = signal<User | null>(null);
  deleteModalOpen = signal(false);
  deletingUser = signal<User | null>(null);
  formSaving = signal(false);
  formError = signal('');

  formEmail = '';
  formPassword = '';

  constructor(private usersService: UsersService) {}

  ngOnInit() {
    this.loading.set(true);
    this.usersService.getUsers().subscribe({
      next: (list) => this.users.set(list),
      error: () => this.error.set('Erro ao carregar usuários.'),
      complete: () => this.loading.set(false),
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
    this.editingUser.set(null);
    this.formEmail = '';
    this.formPassword = '';
    this.formError.set('');
    this.formModalOpen.set(true);
  }

  openEditModal(user: User) {
    this.editingUser.set(user);
    this.formEmail = user.email;
    this.formPassword = '';
    this.formError.set('');
    this.formModalOpen.set(true);
  }

  closeFormModal() {
    this.formModalOpen.set(false);
    this.editingUser.set(null);
  }

  saveUser() {
    this.formError.set('');
    const email = this.formEmail.trim();
    const user = this.editingUser();
    if (!email) {
      this.formError.set('E-mail é obrigatório.');
      return;
    }
    if (!user && !this.formPassword.trim()) {
      this.formError.set('Senha é obrigatória ao criar usuário.');
      return;
    }
    this.formSaving.set(true);
    if (user) {
      this.usersService.updateUser(user.id, { email }).subscribe({
        next: () => {
          this.users.update((list) =>
            list.map((u) => (u.id === user.id ? { ...u, email } : u))
          );
          this.closeFormModal();
        },
        error: (err) => {
          const msg = err?.error ?? 'Erro ao salvar alterações.';
          this.formError.set(typeof msg === 'string' ? msg : 'Erro ao salvar alterações.');
          this.formSaving.set(false);
        },
        complete: () => this.formSaving.set(false),
      });
    } else {
      this.usersService.createUser({ email, password: this.formPassword.trim() }).subscribe({
        next: (created) => {
          this.users.update((list) => [...list, created]);
          this.closeFormModal();
        },
        error: (err) => {
          const msg = err?.error ?? 'Erro ao criar usuário.';
          this.formError.set(typeof msg === 'string' ? msg : 'Erro ao criar usuário.');
          this.formSaving.set(false);
        },
        complete: () => this.formSaving.set(false),
      });
    }
  }

  openDeleteModal(user: User) {
    this.deletingUser.set(user);
    this.deleteModalOpen.set(true);
  }

  closeDeleteModal() {
    this.deleteModalOpen.set(false);
    this.deletingUser.set(null);
  }

  confirmDelete() {
    const user = this.deletingUser();
    if (!user) return;
    this.usersService.deleteUser(user.id).subscribe({
      next: () => {
        this.users.update((list) => list.filter((u) => u.id !== user.id));
        this.closeDeleteModal();
      },
      error: () => this.error.set('Erro ao excluir.'),
    });
  }
}
